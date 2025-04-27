namespace Tunetastic.Views;

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
