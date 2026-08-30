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
	private readonly object _sessionsLock = new();
	private volatile bool _suppressAppVolumeEvent = false;

	/// <summary>
	/// Occurs when the system volume changes.
	/// </summary>
	public event Action<double, bool>? SystemVolumeChanged;

	/// <summary>
	/// Occurs when the application volume changes.
	/// </summary>
	public event Action<double, bool>? AppVolumeChanged;

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
		_currentDevice = GetFreshDevice();
		SubscribeToDevice(_currentDevice);
		_enumerator.RegisterEndpointNotificationCallback(this);
		_ = WaitAndSubscribeToAppVolumeAsync();
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
			if (state == AudioSessionState.AudioSessionStateExpired)
				_owner.RemoveSession(this);
		}

		/// <summary>
		/// Called when the audio session is disconnected.
		/// </summary>
		/// <param name="reason">The reason for disconnection.</param>
		public void OnSessionDisconnected(AudioSessionDisconnectReason reason)
			=> _owner.RemoveSession(this);

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
		lock (_sessionsLock)
		{
			// Avoid duplicates (same session re-discovered)
			if (_sessionHandlers.Any(h => h.Session.GetProcessID == session.GetProcessID
				&& h.Session.Equals(session)))
				return;

			var handler = new SessionEventHandler(this, session);
			session.RegisterEventClient(handler);
			_sessionHandlers.Add(handler);
		}
	}

	/// <summary>
	/// Removes an audio session from the tracking list.
	/// </summary>
	/// <param name="handler">The session event handler to remove.</param>
	private void RemoveSession(SessionEventHandler handler)
	{
		lock (_sessionsLock)
		{
			try { handler.Session.UnRegisterEventClient(handler); } catch { /* session may already be gone */ }
			_sessionHandlers.Remove(handler);

			if (_sessionHandlers.Count == 0)
				_ = WaitAndSubscribeToAppVolumeAsync();
		}
	}

	/// <summary>
	/// Clears all tracked audio sessions.
	/// </summary>
	private void ClearAllSessions()
	{
		lock (_sessionsLock)
		{
			foreach (var h in _sessionHandlers)
				try { h.Session.UnRegisterEventClient(h); } catch { }
			_sessionHandlers.Clear();
		}
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
		foreach (var device in _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
		{
			var sessions = device.AudioSessionManager.Sessions;
			for (int i = 0; i < sessions.Count; i++)
				if (sessions[i].GetProcessID == pid)
					results.Add(sessions[i]);
		}
		return results;
	}

	/// <summary>
	/// Waits for application audio sessions to become available and subscribes to them.
	/// </summary>
	/// <returns>A task representing the asynchronous operation.</returns>
	private async Task WaitAndSubscribeToAppVolumeAsync()
	{
		while (true)
		{
			var found = FindAllAppSessions(Environment.ProcessId);
			if (found.Count > 0)
			{
				foreach (var session in found)
					AddSession(session);
				return;
			}
			await Task.Delay(500);
		}
	}

	/// <summary>
	/// Subscribes to application volume changes.
	/// </summary>
	public void SubscribeToAppVolume()
	{
		ClearAllSessions();
		var found = FindAllAppSessions(Environment.ProcessId);
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
		UnsubscribeFromDevice(_currentDevice);
		_currentDevice.Dispose();

		_currentDevice = _enumerator.GetDevice(deviceId);
		SubscribeToDevice(_currentDevice);
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
		var session = new AudioSessionControl(newSession);
		if (session.GetProcessID != Environment.ProcessId) return;
		AddSession(session);
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
			return (_sessionHandlers.FirstOrDefault()?.Session.SimpleAudioVolume.Volume ?? 0) * 100;
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
			return _sessionHandlers.FirstOrDefault()?.Session.SimpleAudioVolume.Mute ?? false;
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
		if (flow != DataFlow.Render) return;
		UnsubscribeFromDevice(_currentDevice);
		_currentDevice.Dispose();
		_currentDevice = _enumerator.GetDevice(defaultDeviceId);
		SubscribeToDevice(_currentDevice);
		SubscribeToAppVolume();
	}

	/// <summary>
	/// Called when the state of an audio device changes.
	/// </summary>
	/// <param name="deviceId">The ID of the device.</param>
	/// <param name="newState">The new device state.</param>
	void IMMNotificationClient.OnDeviceStateChanged(string deviceId, DeviceState newState)
	{
		if (deviceId != _currentDevice.ID) return;
		if (newState != DeviceState.Active)
		{
			UnsubscribeFromDevice(_currentDevice);
			_currentDevice.Dispose();
			_currentDevice = GetFreshDevice();
			SubscribeToDevice(_currentDevice);
			ClearAllSessions();
			_ = WaitAndSubscribeToAppVolumeAsync();
		}
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
		UnsubscribeFromDevice(_currentDevice);
		ClearAllSessions();
		_enumerator.UnregisterEndpointNotificationCallback(this);
		_currentDevice?.Dispose();
		_enumerator?.Dispose();
	}
}
