namespace Tunetastic.Views;

/// <summary>
/// The MusicControl class provides functionality to control the playback of audio tracks.
/// It allows operations such as play, pause, stop, fast forward, rewind, and volume adjustments.
/// </summary>
public sealed partial class MusicControl : Page
{

    public MusicControl()
    {
        ViewModel = App.GetService<MusicControlViewModel>();
        InitializeComponent();
    }

    public MusicControlViewModel ViewModel
    {
        get;
    }
}
