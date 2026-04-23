using System.Diagnostics;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace Tunetastic.Common.Services;

public class AudioService : IDisposable, IMMNotificationClient
{
	private readonly MMDeviceEnumerator _enumerator;
	private MMDevice _currentDevice;
	private readonly List<SessionEventHandler> _sessionHandlers = new();
	private readonly object _sessionsLock = new();
	private volatile bool _suppressAppVolumeEvent = false;

	public event Action<double, bool>? SystemVolumeChanged;
	public event Action<double, bool>? AppVolumeChanged;

	private MMDevice GetFreshDevice() => _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

	public AudioService()
	{
		_enumerator = new MMDeviceEnumerator();
		_currentDevice = GetFreshDevice();
		SubscribeToDevice(_currentDevice);
		_enumerator.RegisterEndpointNotificationCallback(this);
		_ = WaitAndSubscribeToAppVolumeAsync();
	}

	// ─── Per-session event wrapper ────────────────────────────────────────────

	private class SessionEventHandler : IAudioSessionEventsHandler
	{
		private readonly AudioService _owner;
		public readonly AudioSessionControl Session;

		public SessionEventHandler(AudioService owner, AudioSessionControl session)
		{
			_owner = owner;
			Session = session;
		}

		public void OnVolumeChanged(float volume, bool isMuted)
		{
			if (_owner._suppressAppVolumeEvent) return;
			_owner.AppVolumeChanged?.Invoke((double)volume * 100, isMuted);
		}

		public void OnStateChanged(AudioSessionState state)
		{
			if (state == AudioSessionState.AudioSessionStateExpired)
				_owner.RemoveSession(this);
		}

		public void OnSessionDisconnected(AudioSessionDisconnectReason reason)
			=> _owner.RemoveSession(this);

		public void OnDisplayNameChanged(string displayName) { }
		public void OnIconPathChanged(string iconPath) { }
		public void OnChannelVolumeChanged(uint channelCount, IntPtr newVolumes, uint channelIndex) { }
		public void OnGroupingParamChanged(ref Guid groupingId) { }
	}

	// ─── Session tracking ─────────────────────────────────────────────────────

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

	public List<(string Id, string Name)> GetAudioDevices()
	{
		return _enumerator
			.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
			.Select(d => (d.ID, d.FriendlyName))
			.ToList();
	}

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

	public void SubscribeToAppVolume()
	{
		ClearAllSessions();
		var found = FindAllAppSessions(Environment.ProcessId);
		foreach (var session in found)
			AddSession(session);
	}

	// ─── Device management ────────────────────────────────────────────────────

	public void SwitchDevice(string deviceId)
	{
		UnsubscribeFromDevice(_currentDevice);
		_currentDevice.Dispose();

		_currentDevice = _enumerator.GetDevice(deviceId);
		SubscribeToDevice(_currentDevice);
	}

	private void SubscribeToDevice(MMDevice device)
	{
		device.AudioEndpointVolume.OnVolumeNotification += OnVolumeNotification;
		device.AudioSessionManager.OnSessionCreated += OnSessionCreated;
	}

	private void UnsubscribeFromDevice(MMDevice device)
	{
		device.AudioEndpointVolume.OnVolumeNotification -= OnVolumeNotification;
		device.AudioSessionManager.OnSessionCreated -= OnSessionCreated;
	}

	private void OnVolumeNotification(AudioVolumeNotificationData data)
	{
		SystemVolumeChanged?.Invoke((double)data.MasterVolume * 100, data.Muted);
	}

	private void OnSessionCreated(object sender, IAudioSessionControl newSession)
	{
		var session = new AudioSessionControl(newSession);
		if (session.GetProcessID != Environment.ProcessId) return;
		AddSession(session);
	}

	// ─── Volume get/set ───────────────────────────────────────────────────────

	public double GetVolume() => _currentDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100;

	public double GetAppVolume()
	{
		lock (_sessionsLock)
			return (_sessionHandlers.FirstOrDefault()?.Session.SimpleAudioVolume.Volume ?? 0) * 100;
	}

	public void SetVolume(double volume)
	{
		var actual = Math.Clamp((float)volume / 100f, 0f, 1f);
		_currentDevice.AudioEndpointVolume.MasterVolumeLevelScalar = actual;
	}

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

	public void SetMute(bool mute) => _currentDevice.AudioEndpointVolume.Mute = mute;

	public void SetAppMute(bool mute)
	{
		lock (_sessionsLock)
		{
			foreach (var h in _sessionHandlers)
				try { h.Session.SimpleAudioVolume.Mute = mute; } catch { }
		}
	}

	public bool IsMuted() => _currentDevice.AudioEndpointVolume.Mute;

	public bool IsAppMuted()
	{
		lock (_sessionsLock)
			return _sessionHandlers.FirstOrDefault()?.Session.SimpleAudioVolume.Mute ?? false;
	}

	// ─── IMMNotificationClient ────────────────────────────────────────────────

	void IMMNotificationClient.OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
	{
		if (flow != DataFlow.Render) return;
		UnsubscribeFromDevice(_currentDevice);
		_currentDevice.Dispose();
		_currentDevice = _enumerator.GetDevice(defaultDeviceId);
		SubscribeToDevice(_currentDevice);
		SubscribeToAppVolume();
	}

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

	void IMMNotificationClient.OnDeviceAdded(string deviceId) { }
	void IMMNotificationClient.OnDeviceRemoved(string deviceId) { }
	void IMMNotificationClient.OnPropertyValueChanged(string deviceId, PropertyKey key) { }

	// ─── Dispose ──────────────────────────────────────────────────────────────

	public void Dispose()
	{
		UnsubscribeFromDevice(_currentDevice);
		ClearAllSessions();
		_enumerator.UnregisterEndpointNotificationCallback(this);
		_currentDevice?.Dispose();
		_enumerator?.Dispose();
	}
}
