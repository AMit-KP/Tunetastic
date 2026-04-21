using NAudio.CoreAudioApi;

namespace Tunetastic.Common.Services;

public class AudioService : IDisposable
{
	private MMDevice _device;
	private readonly MMDeviceEnumerator _enumerator;

	public event Action<float, bool>? VolumeChanged;

	public AudioService()
	{
		_enumerator = new MMDeviceEnumerator();
		_device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
		SubscribeToDevice(_device);
	}

	public List<(string Id, string Name)> GetAudioDevices()
	{
		return _enumerator
			.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
			.Select(d => (d.ID, d.FriendlyName))
			.ToList();
	}

	public void SwitchDevice(string deviceId)
	{
		UnsubscribeFromDevice(_device);
		_device.Dispose();

		_device = _enumerator.GetDevice(deviceId);
		SubscribeToDevice(_device);
	}

	private void SubscribeToDevice(MMDevice device)
	{
		device.AudioEndpointVolume.OnVolumeNotification += OnVolumeNotification;
	}

	private void UnsubscribeFromDevice(MMDevice device)
	{
		device.AudioEndpointVolume.OnVolumeNotification -= OnVolumeNotification;
	}

	private void OnVolumeNotification(AudioVolumeNotificationData data)
	{
		VolumeChanged?.Invoke(data.MasterVolume, data.Muted);
	}

	public float GetVolume() => _device.AudioEndpointVolume.MasterVolumeLevelScalar;

	public void SetVolume(float volume)
	{
		volume = Math.Clamp(volume, 0f, 1f);
		_device.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
	}

	public void SetMute(bool mute) => _device.AudioEndpointVolume.Mute = mute;

	public void Dispose()
	{
		UnsubscribeFromDevice(_device);
		_device?.Dispose();
		_enumerator?.Dispose();
	}
}
