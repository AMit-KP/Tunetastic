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
	private volatile MMDevice? _currentDevice;
	private readonly List<SessionEventHandler> _sessionHandlers = new();
	private CancellationTokenSource? _sessionWaitCts;
	private readonly object _sessionsLock = new();
	private volatile bool _suppressAppVolumeEvent = false;
	private bool _disposed;

	private double _cachedSystemVolume = 50;
	private bool _cachedSystemMuted;
	private double _cachedAppVolume = 50;
	private bool _cachedAppMuted;

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
		_enumerator = new MMDeviceEnumerator();
		_ = Task.Run(Initialize);
	}

	private void Initialize()
	{
		try
		{
			_currentDevice = GetFreshDevice();
			SubscribeToDevice(_currentDevice);
			_enumerator.RegisterEndpointNotificationCallback(this);
			RefreshSystemVolumeCache();
			_ = WaitAndSubscribeToAppVolumeAsync();
		}
		catch { /* no usable audio device */ }
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
		/// Whether the tracked session is currently active (rendering audio). Updated from session
		/// state events so readers never need a COM call to pick the live session. Optimistically
		/// true until the first state event arrives; sessions are added while active or when no
		/// active one exists.
		/// </summary>
		public volatile bool IsSessionActive = true;

		/// <summary>
		/// Last known session volume (0-1). Refreshed on volume events and when the session is
		/// added, so primary-session selection never needs a COM call under the lock. The silent
		/// SMTC dummy session is pinned at 0; see GetPrimaryHandler.
		/// </summary>
		public volatile float CachedVolume = 1f;

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
			CachedVolume = volume;

			// A stale session (the previous device's, until it expires) must not fight the live
			// one - its events would make the volume UI flicker between two values.
			if (_owner._suppressAppVolumeEvent || !_owner.IsPrimaryHandler(this)) return;
			_owner._cachedAppVolume = (double)volume * 100;
			_owner._cachedAppMuted = isMuted;
			_owner.AppVolumeChanged?.Invoke((double)volume * 100, isMuted);
		}

		/// <summary>
		/// Called when the state of the audio session changes.
		/// </summary>
		/// <param name="state">The new audio session state.</param>
		public void OnStateChanged(AudioSessionState state)
		{
			IsSessionActive = state == AudioSessionState.AudioSessionStateActive;

			// Windows requires notification callbacks to return quickly. RemoveSession unregisters
			// over COM - a call that can block while the session's device is being torn down (e.g.
			// Bluetooth switching off) - and may restart the wait (a full device enumeration). Run
			// it off the notification thread.
			if (state == AudioSessionState.AudioSessionStateExpired)
				_ = Task.Run(() => _owner.RemoveSession(this));
			else if (state == AudioSessionState.AudioSessionStateInactive)
				_ = Task.Run(() => _owner.PruneStaleSessions());
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
		// Read the state up front: a session whose device is being torn down can still enumerate
		// but be dead, and trusting it as active lets it push garbage (e.g. volume 0) that trips
		// the auto-pause feature. All AddSession paths run on background threads, so the COM call
		// is safe here.
		AudioSessionState state;
		try { state = session.State; }
		catch { return; }
		if (state == AudioSessionState.AudioSessionStateExpired) return;

		var handler = new SessionEventHandler(this, session)
		{
			IsSessionActive = state == AudioSessionState.AudioSessionStateActive
		};
		try { handler.CachedVolume = session.SimpleAudioVolume.Volume; }
		catch { /* the volume read can fail while the session is being torn down */ }

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
		// still held would block any other thread waiting on that lock for as long as that
		// enumeration takes, which can stall during a device change.
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

	/// <summary>
	/// Removes tracked sessions that are no longer active while another tracked session is.
	/// After a device switch the previous device's session lingers inactive until Windows expires
	/// it; keeping it around makes volume reads and the Windows mixer see two different values.
	/// Inactive sessions are kept when no active one exists (e.g. playback is paused).
	/// </summary>
	private void PruneStaleSessions()
	{
		List<SessionEventHandler> stale;
		lock (_sessionsLock)
		{
			if (_sessionHandlers.Count <= 1) return;
			if (!_sessionHandlers.Any(h => h.IsSessionActive)) return;

			stale = _sessionHandlers.Where(h => !h.IsSessionActive).ToList();
			foreach (var h in stale)
				_sessionHandlers.Remove(h);
		}
		foreach (var h in stale)
			try { h.Session.UnRegisterEventClient(h); } catch { /* session may already be gone */ }
	}

	/// <summary>
	/// Returns the handler of the session that owns the app volume: the most recently added
	/// session that is still active and carries real volume. Tunetastic has a second, silent
	/// session (the SMTC dummy player, pinned at volume 0) which re-creates on every device
	/// change - it must never own the app volume, or its zero pushes trip the auto-pause feature
	/// and the volume UI flickers between the real volume and 0. Sessions with zero volume are
	/// only used when no session with real volume exists.
	/// </summary>
	private SessionEventHandler? GetPrimaryHandler()
	{
		SessionEventHandler? primary;
		lock (_sessionsLock)
		{
			primary = PickPrimary(_sessionHandlers);
		}
		return primary;
	}

	/// <summary>
	/// Whether the given handler owns the app volume (see GetPrimaryHandler). Volume events from
	/// any other session are ignored so two sessions can't fight over the volume UI.
	/// </summary>
	private bool IsPrimaryHandler(SessionEventHandler handler)
	{
		lock (_sessionsLock)
		{
			return ReferenceEquals(PickPrimary(_sessionHandlers), handler);
		}
	}

	/// <summary>
	/// Picks the newest session that is active with non-zero volume; falls back to the newest
	/// active one, then the newest with non-zero volume, then the newest of all. Must be called
	/// under _sessionsLock.
	/// </summary>
	private static SessionEventHandler? PickPrimary(List<SessionEventHandler> handlers)
	{
		SessionEventHandler? newestActive = null, newestActiveAudible = null, newestAudible = null;
		foreach (var h in handlers)
		{
			if (h.IsSessionActive)
			{
				newestActive = h;
				if (h.CachedVolume > 0.001f)
					newestActiveAudible = h;
			}
			if (h.CachedVolume > 0.001f)
				newestAudible = h;
		}
		return newestActiveAudible ?? newestActive ?? newestAudible ?? handlers.LastOrDefault();
	}

	// ─── Device/session discovery ─────────────────────────────────────────────

	/// <summary>
	/// Gets a list of available audio devices.
	/// </summary>
	/// <returns>A list of tuples containing device IDs and names.</returns>
	public List<(string Id, string Name)> GetAudioDevices()
	{
		var result = new List<(string, string)>();
		var devices = new List<MMDevice>();
		try
		{
			foreach (var device in _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
			{
				devices.Add(device);
				result.Add((device.ID, device.FriendlyName));
			}
		}
		catch { /* enumeration can fail while devices churn */ }
		finally
		{
			DisposeDevices(devices);
		}
		return result;
	}

	/// <summary>
	/// Gets a list of audio sessions currently running.
	/// </summary>
	/// <returns>A list of tuples containing session names and process IDs.</returns>
	public List<(string Name, int Pid)> GetAudioSessions()
	{
		var result = new List<(string, int)>();
		MMDevice? device;
		try { device = GetFreshDevice(); }
		catch { return result; }

		try
		{
			var sessions = device.AudioSessionManager.Sessions;

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
		}
		catch { /* the device can be gone while it is being torn down */ }
		finally
		{
			DisposeDevices(new List<MMDevice> { device });
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

				var sessions = device.AudioSessionManager.Sessions;
				for (int i = 0; i < sessions.Count; i++)
				{
					var session = sessions[i];
					if (session.GetProcessID == pid)
						results.Add(session);
				}
			}

			var active = new List<AudioSessionControl>();
			var alive = new List<AudioSessionControl>();
			foreach (var session in results)
			{
				AudioSessionState state;
				try { state = session.State; }
				catch { continue; }
				if (state == AudioSessionState.AudioSessionStateExpired) continue;
				alive.Add(session);
				if (state == AudioSessionState.AudioSessionStateActive)
					active.Add(session);
			}
			return active.Count > 0 ? active : alive;
		}
		finally
		{
			DisposeDevices(devices);
		}
	}

	/// <summary>
	/// Disposes enumerated MMDevice wrappers on a thread-pool thread. Disposing a device makes COM
	/// calls (unregistering its session notifications) that can block on Windows' shared Core Audio
	/// notification thread - the same thread that delivers device callbacks to listeners such as
	/// FlyleafLib, whose handler dispatches back to the UI thread and waits on it. Disposing on the
	/// UI or notification thread can therefore deadlock the process, and a device invalidated
	/// mid-switch can throw from the same calls.
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
				catch
				{
					found = new();
				}

				if (found.Count > 0)
				{
					foreach (var session in found)
						AddSession(session);
					PruneStaleSessions();
					RefreshAppVolumeCache();
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
			return;
		}
		foreach (var session in found)
			AddSession(session);
		PruneStaleSessions();
		RefreshAppVolumeCache();
	}

	// ─── Device management ────────────────────────────────────────────────────

	/// <summary>
	/// Switches the audio output device.
	/// </summary>
	/// <param name="deviceId">The ID of the device to switch to.</param>
	public void SwitchDevice(string deviceId)
		=> _ = Task.Run(() => ReplaceDevice(() => _enumerator.GetDevice(deviceId)));

	/// <summary>
	/// Replaces the current device with a new one. Must only run on a background thread: the COM
	/// calls it makes block on Windows' shared Core Audio notification thread during device churn
	/// (see DisposeDevices).
	/// </summary>
	/// <param name="getDevice">Returns the device to switch to, or null if it cannot be opened.</param>
	private void ReplaceDevice(Func<MMDevice?> getDevice)
	{
		if (_disposed) return;

		MMDevice? newDevice;
		try { newDevice = getDevice(); }
		catch
		{
			return;
		}
		if (newDevice == null) return;

		var oldDevice = _currentDevice;
		_currentDevice = newDevice;

		try { if (oldDevice != null) UnsubscribeFromDevice(oldDevice); } catch { /* device may already be gone */ }
		try { oldDevice?.Dispose(); } catch { /* device may already be gone */ }
		try { SubscribeToDevice(newDevice); } catch { /* device not ready yet */ }

		ClearAllSessions();
		_ = WaitAndSubscribeToAppVolumeAsync();
		RefreshSystemVolumeCache();
		DeviceChanged?.Invoke();
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
		_cachedSystemVolume = (double)data.MasterVolume * 100;
		_cachedSystemMuted = data.Muted;
		SystemVolumeChanged?.Invoke((double)data.MasterVolume * 100, data.Muted);
	}

	/// <summary>
	/// Handles session creation events.
	/// </summary>
	/// <param name="sender">The event sender.</param>
	/// <param name="newSession">The new audio session control.</param>
	private void OnSessionCreated(object sender, IAudioSessionControl newSession)
	{
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
	public double GetVolume() => _cachedSystemVolume;

	/// <summary>
	/// Gets the current application volume level.
	/// </summary>
	/// <returns>The application volume as a percentage.</returns>
	public double GetAppVolume() => _cachedAppVolume;

	/// <summary>
	/// Sets the system volume level.
	/// </summary>
	/// <param name="volume">The volume level to set (0-100).</param>
	public void SetVolume(double volume)
	{
		var actual = Math.Clamp((float)volume / 100f, 0f, 1f);
		_cachedSystemVolume = actual * 100;
		_ = Task.Run(() =>
		{
			var device = _currentDevice;
			if (device == null) return;
			try { device.AudioEndpointVolume.MasterVolumeLevelScalar = actual; }
			catch { /* device may be gone */ }
		});
	}

	/// <summary>
	/// Sets the application volume level.
	/// </summary>
	/// <param name="volume">The volume level to set (0-100).</param>
	public void SetAppVolume(double volume)
	{
		var actual = Math.Clamp((float)volume / 100f, 0f, 1f);
		_cachedAppVolume = actual * 100;
		_ = Task.Run(() =>
		{
			SessionEventHandler[] handlers;
			lock (_sessionsLock)
			{
				if (_sessionHandlers.Count == 0) return;
				handlers = _sessionHandlers.ToArray();
			}
			_suppressAppVolumeEvent = true;
			foreach (var h in handlers)
			{
				try
				{
					h.Session.SimpleAudioVolume.Volume = actual;
					h.CachedVolume = actual;
				}
				catch { /* session may have expired between snapshot and set */ }
			}
			_suppressAppVolumeEvent = false;
		});
	}

	/// <summary>
	/// Sets the system mute state.
	/// </summary>
	/// <param name="mute">True to mute, false to unmute.</param>
	public void SetMute(bool mute)
	{
		_cachedSystemMuted = mute;
		_ = Task.Run(() =>
		{
			var device = _currentDevice;
			if (device == null) return;
			try { device.AudioEndpointVolume.Mute = mute; }
			catch { /* device may be gone */ }
		});
	}

	/// <summary>
	/// Sets the application mute state.
	/// </summary>
	/// <param name="mute">True to mute, false to unmute.</param>
	public void SetAppMute(bool mute)
	{
		_cachedAppMuted = mute;
		_ = Task.Run(() =>
		{
			SessionEventHandler[] handlers;
			lock (_sessionsLock) handlers = _sessionHandlers.ToArray();
			foreach (var h in handlers)
				try { h.Session.SimpleAudioVolume.Mute = mute; } catch { /* session may have expired */ }
		});
	}

	/// <summary>
	/// Gets the current system mute state.
	/// </summary>
	/// <returns>True if muted, false otherwise.</returns>
	public bool IsMuted() => _cachedSystemMuted;

	/// <summary>
	/// Gets the current application mute state.
	/// </summary>
	/// <returns>True if muted, false otherwise.</returns>
	public bool IsAppMuted() => _cachedAppMuted;

	/// <summary>
	/// Refreshes the cached system volume/mute from the current device. Must run off the UI thread.
	/// </summary>
	private void RefreshSystemVolumeCache()
	{
		var device = _currentDevice;
		if (device == null) return;
		try
		{
			_cachedSystemVolume = device.AudioEndpointVolume.MasterVolumeLevelScalar * 100;
			_cachedSystemMuted = device.AudioEndpointVolume.Mute;
		}
		catch { /* device may be gone */ }
	}

	/// <summary>
	/// Refreshes the cached app volume/mute from the primary session and pushes it to listeners.
	/// Must run off the UI thread.
	/// </summary>
	private void RefreshAppVolumeCache()
	{
		var handler = GetPrimaryHandler();
		if (handler == null) return;
		double volume;
		bool muted;
		try
		{
			volume = handler.Session.SimpleAudioVolume.Volume * 100;
			muted = handler.Session.SimpleAudioVolume.Mute;
		}
		catch
		{
			RemoveSession(handler);
			return;
		}

		if (Math.Abs(_cachedAppVolume - volume) < 0.5 && _cachedAppMuted == muted) return;
		_cachedAppVolume = volume;
		_cachedAppMuted = muted;
		AppVolumeChanged?.Invoke(volume, muted);
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
		if (flow != DataFlow.Render || role != Role.Multimedia) return;
		_ = Task.Run(() => ReplaceDevice(() => _enumerator.GetDevice(defaultDeviceId)));
	}

	/// <summary>
	/// Called when the state of an audio device changes.
	/// </summary>
	/// <param name="deviceId">The ID of the device.</param>
	/// <param name="newState">The new device state.</param>
	void IMMNotificationClient.OnDeviceStateChanged(string deviceId, DeviceState newState)
	{
		if (newState == DeviceState.Active) return;
		_ = Task.Run(() =>
		{
			if (deviceId != _currentDevice?.ID) return;
			ReplaceDevice(GetFreshDevice);
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
		var device = _currentDevice;
		if (device != null)
			try { UnsubscribeFromDevice(device); } catch { /* device may already be gone */ }
		ClearAllSessions();
		lock (_sessionsLock)
		{
			_sessionWaitCts?.Cancel();
		}
		try { _enumerator.UnregisterEndpointNotificationCallback(this); } catch { }
		try { device?.Dispose(); } catch { }
		_enumerator.Dispose();
	}
}
