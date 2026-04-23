using NAudio.CoreAudioApi;

namespace Tunetastic.Common.Services;

public class AudioService : IDisposable
{
	private MMDevice _device;
	private readonly MMDeviceEnumerator _enumerator;

	public event Action<double, bool>? VolumeChanged;

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
		VolumeChanged?.Invoke((double)data.MasterVolume * 100, data.Muted);
	}

	public double GetVolume() => _device.AudioEndpointVolume.MasterVolumeLevelScalar * 100;

	public void SetVolume(double volume)
	{
		var Actualvolume = Math.Clamp((float)volume / 100f, 0f, 1f);
		_device.AudioEndpointVolume.MasterVolumeLevelScalar = Actualvolume;
	}

	public void SetMute(bool mute) => _device.AudioEndpointVolume.Mute = mute;
	public bool IsMuted() => _device.AudioEndpointVolume.Mute;

	public void Dispose()
	{
		UnsubscribeFromDevice(_device);
		_device?.Dispose();
		_enumerator?.Dispose();
	}
}
