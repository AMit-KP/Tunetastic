using Tunetastic.Views.LibraryViews;

namespace Tunetastic.Views;

/// <summary>
/// Represents a page designed to display or manage library-related content.
/// </summary>
public sealed partial class LibraryPage : Page
{
    public LibraryPage()
    {
        this.InitializeComponent();
        LibraryFrame.Navigate(typeof(AllSongsViewPage));
    }

}
