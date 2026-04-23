using System.Diagnostics;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace Tunetastic.Common.Services;

public class AudioService : IDisposable, IAudioSessionEventsHandler, IMMNotificationClient
{
	private readonly MMDeviceEnumerator _enumerator;
	private AudioSessionControl? _appSession;
	private MMDevice _currentDevice;

	public event Action<double, bool>? VolumeChanged;
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

	private (MMDevice device, AudioSessionControl session)? FindAppSession(int pid)
	{
		foreach (var device in _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
		{
			var sessions = device.AudioSessionManager.Sessions;
			for (int i = 0; i < sessions.Count; i++)
				if (sessions[i].GetProcessID == pid)
					return (device, sessions[i]);
		}
		return null;
	}

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
	}

	private async Task WaitAndSubscribeToAppVolumeAsync()
	{
		while (true)
		{
			var found = FindAppSession(Environment.ProcessId);
			if (found is not null)
			{
				_appSession = found.Value.session;
				_appSession.RegisterEventClient(this);
				return;
			}
			await Task.Delay(500);
		}
	}

	public void SubscribeToAppVolume()
	{
		_appSession?.UnRegisterEventClient(this);
		var found = FindAppSession(Environment.ProcessId);
		if (found is null) return;
		_appSession = found.Value.session;
		_appSession.RegisterEventClient(this);
	}

	private void UnsubscribeFromDevice(MMDevice device)
	{
		device.AudioEndpointVolume.OnVolumeNotification -= OnVolumeNotification;
	}

	private void OnVolumeNotification(AudioVolumeNotificationData data)
	{
		VolumeChanged?.Invoke((double)data.MasterVolume * 100, data.Muted);
	}

	void IAudioSessionEventsHandler.OnVolumeChanged(float volume, bool isMuted) => AppVolumeChanged?.Invoke((double)volume * 100, isMuted);

	public double GetVolume() => _currentDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100;

	public float GetAppVolume() => FindAppSession(Environment.ProcessId)?.session.SimpleAudioVolume.Volume * 100 ?? 0;

	public void SetVolume(double volume)
	{
		var Actualvolume = Math.Clamp((float)volume / 100f, 0f, 1f);
		_currentDevice.AudioEndpointVolume.MasterVolumeLevelScalar = Actualvolume;
	}

	public void SetAppVolume(double volume)
	{
		var session = FindAppSession(Environment.ProcessId)?.session;
		if (session != null)
			session.SimpleAudioVolume.Volume = Math.Clamp((float)volume / 100f, 0f, 1f);
	}

	public void SetMute(bool mute) => _currentDevice.AudioEndpointVolume.Mute = mute;

	public void SetAppMute(bool mute)
	{
		var session = FindAppSession(Environment.ProcessId)?.session;
		if (session != null)
			session.SimpleAudioVolume.Mute = mute;
	}
	public bool IsMuted() => _currentDevice.AudioEndpointVolume.Mute;

	public bool IsAppMuted() => FindAppSession(Environment.ProcessId)?.session.SimpleAudioVolume.Mute ?? false;

	public void Dispose()
	{
		UnsubscribeFromDevice(_currentDevice);
		_appSession?.UnRegisterEventClient(this);
		_enumerator.UnregisterEndpointNotificationCallback(this);
		_currentDevice?.Dispose();
		_enumerator?.Dispose();
	}

	void IAudioSessionEventsHandler.OnDisplayNameChanged(string displayName) { }
	void IAudioSessionEventsHandler.OnIconPathChanged(string iconPath) { }
	void IAudioSessionEventsHandler.OnChannelVolumeChanged(uint channelCount, IntPtr newVolumes, uint channelIndex) { }
	void IAudioSessionEventsHandler.OnGroupingParamChanged(ref Guid groupingId) { }
	void IAudioSessionEventsHandler.OnStateChanged(AudioSessionState state) { }
	void IAudioSessionEventsHandler.OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason) { }

	void IMMNotificationClient.OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
	{
		if (flow != DataFlow.Render) return;
		UnsubscribeFromDevice(_currentDevice);
		_currentDevice.Dispose();
		_currentDevice = _enumerator.GetDevice(defaultDeviceId);
		SubscribeToDevice(_currentDevice);
		SubscribeToAppVolume();
	}

	// Rest are required but unused
	void IMMNotificationClient.OnDeviceAdded(string deviceId) { }
	void IMMNotificationClient.OnDeviceRemoved(string deviceId) { }
	void IMMNotificationClient.OnDeviceStateChanged(string deviceId, DeviceState newState) { }
	void IMMNotificationClient.OnPropertyValueChanged(string deviceId, PropertyKey key) { }

}
