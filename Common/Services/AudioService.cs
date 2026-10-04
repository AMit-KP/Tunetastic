using System.Diagnostics;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace Tunetastic.Common.Services;

/// <summary>
/// Provides audio service functionality for managing system and application volume controls.
/// </summary>
public class AudioService : IDisposable, IMMNotificationClient
{
	private readonly MMDeviceEnumerator _enumerator;
	private MMDevice _currentDevice;
	private readonly List<SessionEventHandler> _sessionHandlers = new();
	private CancellationTokenSource? _sessionWaitCts;
	private readonly object _sessionsLock = new();
	private volatile bool _suppressAppVolumeEvent = false;
	private readonly Microsoft.UI.Dispatching.DispatcherQueue? _dispatcherQueue;
	private bool _disposed;

	/// <summary>
	/// Occurs when the system volume changes.
	/// </summary>
	public event Action<double, bool>? SystemVolumeChanged;

	/// <summary>
	/// Occurs when the application volume changes.
	/// </summary>
	public event Action<double, bool>? AppVolumeChanged;

	/// <summary>
	/// Occurs when the default output device is replaced. Volume/mute notifications only fire on an
	/// actual level change, not on a device switch, so listeners must re-read GetVolume()/IsMuted()
	/// (or the app equivalents) here to stay in sync with the new device.
	/// </summary>
	public event Action? DeviceChanged;

	/// <summary>
	/// Gets the default audio endpoint device.
	/// </summary>
	/// <returns>The current MMDevice instance.</returns>
	private MMDevice GetFreshDevice() => _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

	/// <summary>
	/// Initializes a new instance of the AudioService class.
	/// </summary>
	public AudioService()
	{
		_dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
		_enumerator = new MMDeviceEnumerator();
		_currentDevice = GetFreshDevice();
		SubscribeToDevice(_currentDevice);
		_enumerator.RegisterEndpointNotificationCallback(this);
		// FindAllAppSessions' first pass runs synchronously before its first await; keep its COM
		// enumeration off the UI thread.
		_ = Task.Run(WaitAndSubscribeToAppVolumeAsync);
	}

	// ─── Per-session event wrapper ────────────────────────────────────────────

	/// <summary>
	/// Handles audio session events for individual applications.
	/// </summary>
	private class SessionEventHandler : IAudioSessionEventsHandler
	{
		private readonly AudioService _owner;
		public readonly AudioSessionControl Session;

		/// <summary>
		/// Initializes a new instance of the SessionEventHandler class.
		/// </summary>
		/// <param name="owner">The owner AudioService instance.</param>
		/// <param name="session">The audio session control.</param>
		public SessionEventHandler(AudioService owner, AudioSessionControl session)
		{
			_owner = owner;
			Session = session;
		}

		/// <summary>
		/// Called when the volume of the audio session changes.
		/// </summary>
		/// <param name="volume">The new volume level.</param>
		/// <param name="isMuted">Indicates whether the session is muted.</param>
		public void OnVolumeChanged(float volume, bool isMuted)
		{
			if (_owner._suppressAppVolumeEvent) return;
			_owner.AppVolumeChanged?.Invoke((double)volume * 100, isMuted);
		}

		/// <summary>
		/// Called when the state of the audio session changes.
		/// </summary>
		/// <param name="state">The new audio session state.</param>
		public void OnStateChanged(AudioSessionState state)
		{
			// Windows requires notification callbacks to return quickly. RemoveSession unregisters
			// over COM - a call that can block while the session's device is being torn down (e.g.
			// Bluetooth switching off) - and may restart the wait (a full device enumeration). Run
			// it off the notification thread.
			if (state == AudioSessionState.AudioSessionStateExpired)
				_ = Task.Run(() => _owner.RemoveSession(this));
		}

		/// <summary>
		/// Called when the audio session is disconnected.
		/// </summary>
		/// <param name="reason">The reason for disconnection.</param>
		public void OnSessionDisconnected(AudioSessionDisconnectReason reason)
			=> _ = Task.Run(() => _owner.RemoveSession(this));

		/// <summary>
		/// Called when the display name of the audio session changes.
		/// </summary>
		/// <param name="displayName">The new display name.</param>
		public void OnDisplayNameChanged(string displayName) { }

		/// <summary>
		/// Called when the icon path of the audio session changes.
		/// </summary>
		/// <param name="iconPath">The new icon path.</param>
		public void OnIconPathChanged(string iconPath) { }

		/// <summary>
		/// Called when the channel volume of the audio session changes.
		/// </summary>
		/// <param name="channelCount">The number of channels.</param>
		/// <param name="newVolumes">Pointer to the new volume values.</param>
		/// <param name="channelIndex">The index of the changed channel.</param>
		public void OnChannelVolumeChanged(uint channelCount, IntPtr newVolumes, uint channelIndex) { }

		/// <summary>
		/// Called when the grouping parameter of the audio session changes.
		/// </summary>
		/// <param name="groupingId">The new grouping identifier.</param>
		public void OnGroupingParamChanged(ref Guid groupingId) { }
	}

	// ─── Session tracking ─────────────────────────────────────────────────────

	/// <summary>
	/// Adds an audio session to the tracking list.
	/// </summary>
	/// <param name="session">The audio session control to add.</param>
	private void AddSession(AudioSessionControl session)
	{
		var handler = new SessionEventHandler(this, session);

		// RegisterEventClient is a COM call into Core Audio that synchronizes with Windows' shared
		// notification thread. Taking _sessionsLock across it deadlocks during a device connect
		// (e.g. Bluetooth): that thread may be dispatching a device callback to the UI thread and
		// waiting on it (FlyleafLib's handler does), while UI volume reads wait on _sessionsLock.
		try { session.RegisterEventClient(handler); }
		catch { /* session may already be gone */ return; }

		bool added;
		lock (_sessionsLock)
		{
			// Avoid duplicates (same session re-discovered). Compared by instance, so no COM calls
			// are needed under the lock.
			added = !_sessionHandlers.Any(h => h.Session.Equals(session));
			if (added)
				_sessionHandlers.Add(handler);
		}
		if (!added)
		{
			try { session.UnRegisterEventClient(handler); } catch { /* registration was the only thing to undo */ }
		}
	}

	/// <summary>
	/// Removes an audio session from the tracking list.
	/// </summary>
	/// <param name="handler">The session event handler to remove.</param>
	private void RemoveSession(SessionEventHandler handler)
	{
		bool needsRewait;
		lock (_sessionsLock)
		{
			_sessionHandlers.Remove(handler);
			needsRewait = _sessionHandlers.Count == 0;
		}

		// Same hazard as AddSession: unregistering is a COM call into Core Audio. Keep it outside
		// _sessionsLock; the session may already be gone.
		try { handler.Session.UnRegisterEventClient(handler); } catch { /* session may already be gone */ }

		// WaitAndSubscribeToAppVolumeAsync runs FindAllAppSessions synchronously before its first
		// await, which enumerates every output device over COM. Starting it while _sessionsLock is
		// still held would block any other thread waiting on that lock (e.g. the UI thread reading
		// app volume) for as long as that enumeration takes, which can stall during a device change.
		if (needsRewait)
			_ = WaitAndSubscribeToAppVolumeAsync();
	}

	/// <summary>
	/// Clears all tracked audio sessions.
	/// </summary>
	private void ClearAllSessions()
	{
		SessionEventHandler[] handlers;
		lock (_sessionsLock)
		{
			handlers = _sessionHandlers.ToArray();
			_sessionHandlers.Clear();
		}
		foreach (var h in handlers)
			try { h.Session.UnRegisterEventClient(h); } catch { /* session may already be gone */ }
	}

	// ─── Device/session discovery ─────────────────────────────────────────────

	/// <summary>
	/// Gets a list of available audio devices.
	/// </summary>
	/// <returns>A list of tuples containing device IDs and names.</returns>
	public List<(string Id, string Name)> GetAudioDevices()
	{
		return _enumerator
			.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
			.Select(d => (d.ID, d.FriendlyName))
			.ToList();
	}

	/// <summary>
	/// Gets a list of audio sessions currently running.
	/// </summary>
	/// <returns>A list of tuples containing session names and process IDs.</returns>
	public List<(string Name, int Pid)> GetAudioSessions()
	{
		var sessions = GetFreshDevice().AudioSessionManager.Sessions;
		var result = new List<(string, int)>();

		for (int i = 0; i < sessions.Count; i++)
		{
			var session = sessions[i];
			var pid = (int)session.GetProcessID;
			if (pid == 0) continue;

			try
			{
				var process = Process.GetProcessById(pid);
				result.Add((process.ProcessName, pid));
			}
			catch (ArgumentException)
			{
				// Process no longer running, skip it
			}
		}

		return result;
	}

	/// <summary>
	/// Finds all audio sessions for a specific process ID.
	/// </summary>
	/// <param name="pid">The process ID to search for.</param>
	/// <returns>A list of audio session controls.</returns>
	private List<AudioSessionControl> FindAllAppSessions(int pid)
	{
		var results = new List<AudioSessionControl>();
		var devices = new List<MMDevice>();
		try
		{
			foreach (var device in _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
			{
				devices.Add(device);

				// Reading AudioSessionManager registers a session notification with Windows that stays
				// registered until the device is disposed. Without disposing, every pass leaves one more
				// per output device behind. The AudioSessionControls collected here are separate COM
				// objects and stay valid after the device is disposed.
				var sessions = device.AudioSessionManager.Sessions;
				for (int i = 0; i < sessions.Count; i++)
				{
					var session = sessions[i];
					if (session.GetProcessID == pid)
						results.Add(session);
				}
			}
		}
		finally
		{
			DisposeDevices(devices);
		}
		return results;
	}

	/// <summary>
	/// Disposes enumerated MMDevice wrappers on a thread-pool thread. Disposing a device makes COM
	/// calls (unregistering its session notifications) that can block on Windows' shared Core Audio
	/// notification thread - the same thread that delivers device callbacks to listeners such as
	/// FlyleafLib, whose handler dispatches back to the UI thread and waits on it (see ReplaceDevice).
	/// Disposing on the UI or notification thread can therefore deadlock the process, and a device
	/// invalidated mid-switch can throw from the same calls.
	/// </summary>
	/// <param name="devices">The devices to dispose. Each may already be gone.</param>
	private static void DisposeDevices(List<MMDevice> devices)
	{
		if (devices.Count == 0)
			return;
		_ = Task.Run(() =>
		{
			foreach (var device in devices)
			{
				try { device.Dispose(); }
				catch { /* a failed dispose leaves the notification to the device's finalizer */ }
			}
		});
	}

	/// <summary>
	/// Waits for application audio sessions to become available and subscribes to them.
	/// </summary>
	/// <returns>A task representing the asynchronous operation.</returns>
	private async Task WaitAndSubscribeToAppVolumeAsync()
	{
		// This is started from several places (startup, a session ending, a device change).
		// Only one wait should run at a time, so a new one cancels the previous one.
		CancellationToken token;
		lock (_sessionsLock)
		{
			if (_disposed) return;
			_sessionWaitCts?.Cancel();
			_sessionWaitCts = new CancellationTokenSource();
			token = _sessionWaitCts.Token;
		}

		try
		{
			while (!_disposed && !token.IsCancellationRequested)
			{
				List<AudioSessionControl> found;
				try
				{
					found = FindAllAppSessions(Environment.ProcessId);
				}
				catch (Exception)
				{
					// Devices can disappear mid-enumeration while outputs change. Try again next pass
					// instead of ending the wait for good.
					found = new();
				}

				if (found.Count > 0)
				{
					foreach (var session in found)
						AddSession(session);
					return;
				}
				await Task.Delay(500, token);
			}
		}
		catch (OperationCanceledException) { }
	}

	/// <summary>
	/// Subscribes to application volume changes.
	/// </summary>
	public void SubscribeToAppVolume()
	{
		ClearAllSessions();
		List<AudioSessionControl> found;
		try
		{
			found = FindAllAppSessions(Environment.ProcessId);
		}
		catch
		{
			// A device can disappear mid-enumeration while outputs change. Don't crash the UI thread
			// over it; OnSessionCreated still adds sessions as they appear.
			return;
		}
		foreach (var session in found)
			AddSession(session);
	}

	// ─── Device management ────────────────────────────────────────────────────

	/// <summary>
	/// Switches the audio output device.
	/// </summary>
	/// <param name="deviceId">The ID of the device to switch to.</param>
	public void SwitchDevice(string deviceId)
	{
		RunOnUIThread(() => ReplaceDevice(() => _enumerator.GetDevice(deviceId)));
	}

	/// <summary>
	/// Replaces the current device with a new one. Must run on the UI thread.
	/// </summary>
	/// <param name="getDevice">Returns the device to switch to.</param>
	/// <returns>True if the device was replaced, false if the new device could not be opened.</returns>
	private bool ReplaceDevice(Func<MMDevice> getDevice)
	{
		MMDevice newDevice;
		try
		{
			newDevice = getDevice();
		}
		catch (Exception)
		{
			// No usable output device (e.g. the last one was just unplugged). Keep the old one
			// rather than leaving _currentDevice disposed.
			return false;
		}

		var oldDevice = _currentDevice;
		_currentDevice = newDevice;

		// UnsubscribeFromDevice and SubscribeToDevice register and unregister session notifications
		// over COM, and Dispose releases COM objects. All of those calls synchronize with the
		// shared Core Audio notification thread that Windows uses to deliver device callbacks to
		// every listener in the process (not just this one - e.g. FlyleafLib registers its own).
		// That thread can itself be waiting on this UI thread (FlyleafLib's device-change handler
		// dispatches back to the UI thread and blocks on it), and while a device tears down (e.g.
		// Bluetooth switching off) the same calls block on the audio service - so doing any of this
		// on the UI thread deadlocks the app. Do it all off the UI thread. Subscribing can also
		// fail while the new device is still initializing; volume reads and writes keep working
		// either way, and the next device change retries.
		_ = Task.Run(() =>
		{
			try { UnsubscribeFromDevice(oldDevice); } catch { /* device may already be gone */ }
			try { oldDevice.Dispose(); } catch { /* device may already be gone */ }
			try { SubscribeToDevice(newDevice); } catch { /* device not ready yet */ }
		});
		return true;
	}

	/// <summary>
	/// Runs an action on the UI thread.
	/// </summary>
	/// <param name="action">The action to run.</param>
	private void RunOnUIThread(Action action)
	{
		// Windows raises the IMMNotificationClient callbacks on its own threads, while the UI thread
		// reads _currentDevice for volume and mute. Replacing the device on the UI thread means a
		// reader can never be holding a device that another thread has just disposed. It also keeps
		// the callbacks short, which Windows requires of them.
		if (_dispatcherQueue == null || _dispatcherQueue.HasThreadAccess)
		{
			action();
			return;
		}

		_dispatcherQueue.TryEnqueue(() =>
		{
			if (!_disposed) action();
		});
	}

	/// <summary>
	/// Subscribes to events from a specific audio device.
	/// </summary>
	/// <param name="device">The MMDevice to subscribe to.</param>
	private void SubscribeToDevice(MMDevice device)
	{
		device.AudioEndpointVolume.OnVolumeNotification += OnVolumeNotification;
		device.AudioSessionManager.OnSessionCreated += OnSessionCreated;
	}

	/// <summary>
	/// Unsubscribes from events of a specific audio device.
	/// </summary>
	/// <param name="device">The MMDevice to unsubscribe from.</param>
	private void UnsubscribeFromDevice(MMDevice device)
	{
		device.AudioEndpointVolume.OnVolumeNotification -= OnVolumeNotification;
		device.AudioSessionManager.OnSessionCreated -= OnSessionCreated;
	}

	/// <summary>
	/// Handles volume notification events from the audio device.
	/// </summary>
	/// <param name="data">The audio volume notification data.</param>
	private void OnVolumeNotification(AudioVolumeNotificationData data)
	{
		SystemVolumeChanged?.Invoke((double)data.MasterVolume * 100, data.Muted);
	}

	/// <summary>
	/// Handles session creation events.
	/// </summary>
	/// <param name="sender">The event sender.</param>
	/// <param name="newSession">The new audio session control.</param>
	private void OnSessionCreated(object sender, IAudioSessionControl newSession)
	{
		// Runs on Windows' notification thread. GetProcessID and AddSession (which registers
		// session events over COM) can block while a device connects or tears down; keep them off
		// the notification thread so it returns immediately.
		_ = Task.Run(() =>
		{
			var session = new AudioSessionControl(newSession);
			if (session.GetProcessID != Environment.ProcessId) return;
			AddSession(session);
		});
	}

	// ─── Volume get/set ───────────────────────────────────────────────────────

	/// <summary>
	/// Gets the current system volume level.
	/// </summary>
	/// <returns>The system volume as a percentage.</returns>
	public double GetVolume() => _currentDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100;

	/// <summary>
	/// Gets the current application volume level.
	/// </summary>
	/// <returns>The application volume as a percentage.</returns>
	public double GetAppVolume()
	{
		lock (_sessionsLock)
		{
			var handler = _sessionHandlers.FirstOrDefault();
			if (handler == null) return 0;
			try { return handler.Session.SimpleAudioVolume.Volume * 100; }
			catch { return 0; }   // session dying mid-read (device switch); the next volume event resyncs
		}
	}

	/// <summary>
	/// Sets the system volume level.
	/// </summary>
	/// <param name="volume">The volume level to set (0-100).</param>
	public void SetVolume(double volume)
	{
		var actual = Math.Clamp((float)volume / 100f, 0f, 1f);
		_currentDevice.AudioEndpointVolume.MasterVolumeLevelScalar = actual;
	}

	/// <summary>
	/// Sets the application volume level.
	/// </summary>
	/// <param name="volume">The volume level to set (0-100).</param>
	public void SetAppVolume(double volume)
	{
		lock (_sessionsLock)
		{
			if (_sessionHandlers.Count == 0) return;
			_suppressAppVolumeEvent = true;
			var actual = Math.Clamp((float)volume / 100f, 0f, 1f);
			foreach (var h in _sessionHandlers)
			{
				try { h.Session.SimpleAudioVolume.Volume = actual; }
				catch { /* session may have expired between lock and set */ }
			}
			_suppressAppVolumeEvent = false;
		}
	}

	/// <summary>
	/// Sets the system mute state.
	/// </summary>
	/// <param name="mute">True to mute, false to unmute.</param>
	public void SetMute(bool mute) => _currentDevice.AudioEndpointVolume.Mute = mute;

	/// <summary>
	/// Sets the application mute state.
	/// </summary>
	/// <param name="mute">True to mute, false to unmute.</param>
	public void SetAppMute(bool mute)
	{
		lock (_sessionsLock)
		{
			foreach (var h in _sessionHandlers)
				try { h.Session.SimpleAudioVolume.Mute = mute; } catch { }
		}
	}

	/// <summary>
	/// Gets the current system mute state.
	/// </summary>
	/// <returns>True if muted, false otherwise.</returns>
	public bool IsMuted() => _currentDevice.AudioEndpointVolume.Mute;

	/// <summary>
	/// Gets the current application mute state.
	/// </summary>
	/// <returns>True if muted, false otherwise.</returns>
	public bool IsAppMuted()
	{
		lock (_sessionsLock)
		{
			var handler = _sessionHandlers.FirstOrDefault();
			if (handler == null) return false;
			try { return handler.Session.SimpleAudioVolume.Mute; }
			catch { return false; }   // session dying mid-read (device switch)
		}
	}

	// ─── IMMNotificationClient ────────────────────────────────────────────────

	/// <summary>
	/// Called when the default audio device changes.
	/// </summary>
	/// <param name="flow">The data flow direction.</param>
	/// <param name="role">The role of the device.</param>
	/// <param name="defaultDeviceId">The ID of the new default device.</param>
	void IMMNotificationClient.OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
	{
		// Windows sends this once per role. Follow only the Multimedia default, the one
		// GetFreshDevice uses, or a separate Communications device (e.g. a headset) can win.
		if (flow != DataFlow.Render || role != Role.Multimedia) return;
		RunOnUIThread(() =>
		{
			if (ReplaceDevice(() => _enumerator.GetDevice(defaultDeviceId)))
			{
				// FindAllAppSessions enumerates every output device over COM and registers session
				// notifications - calls that synchronize with Windows' shared notification thread
				// and can block while a device tears down. Keep them off the UI thread; until the
				// resubscription finishes, OnSessionCreated keeps the session list current.
				_ = Task.Run(SubscribeToAppVolume);
				DeviceChanged?.Invoke();
			}
		});
	}

	/// <summary>
	/// Called when the state of an audio device changes.
	/// </summary>
	/// <param name="deviceId">The ID of the device.</param>
	/// <param name="newState">The new device state.</param>
	void IMMNotificationClient.OnDeviceStateChanged(string deviceId, DeviceState newState)
	{
		if (newState == DeviceState.Active) return;
		RunOnUIThread(() =>
		{
			if (deviceId != _currentDevice.ID) return;
			if (ReplaceDevice(GetFreshDevice))
			{
				_ = Task.Run(() =>
				{
					ClearAllSessions();
					_ = WaitAndSubscribeToAppVolumeAsync();
				});
				DeviceChanged?.Invoke();
			}
		});
	}

	/// <summary>
	/// Called when an audio device is added.
	/// </summary>
	/// <param name="deviceId">The ID of the added device.</param>
	void IMMNotificationClient.OnDeviceAdded(string deviceId) { }

	/// <summary>
	/// Called when an audio device is removed.
	/// </summary>
	/// <param name="deviceId">The ID of the removed device.</param>
	void IMMNotificationClient.OnDeviceRemoved(string deviceId) { }

	/// <summary>
	/// Called when a property value of an audio device changes.
	/// </summary>
	/// <param name="deviceId">The ID of the device.</param>
	/// <param name="key">The property key that changed.</param>
	void IMMNotificationClient.OnPropertyValueChanged(string deviceId, PropertyKey key) { }

	// ─── Dispose ──────────────────────────────────────────────────────────────

	/// <summary>
	/// Disposes of the AudioService resources.
	/// </summary>
	public void Dispose()
	{
		_disposed = true;
		UnsubscribeFromDevice(_currentDevice);
		ClearAllSessions();
		lock (_sessionsLock)
		{
			_sessionWaitCts?.Cancel();
		}
		_enumerator.UnregisterEndpointNotificationCallback(this);
		_currentDevice?.Dispose();
		_enumerator?.Dispose();
	}
}
