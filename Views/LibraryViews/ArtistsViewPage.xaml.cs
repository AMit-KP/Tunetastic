// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Tunetastic.Views.LibraryViews;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ArtistsViewPage : Page
{
	public ArtistsViewPage()
	{
		this.InitializeComponent();
	}

	/// <summary>
	/// Stores a collection of humorous or witty messages displayed when the user's song library is empty or unconfigured.
	/// </summary>
	/// <remarks>
	/// The <c>GoToMessages</c> field contains a predefined list of strings, each consisting of two lines of text split by a newline character.
	/// These messages are intended to provide a lighthearted and engaging user experience, encouraging users to either add tracks,
	/// configure their settings, or scan their music libraries. Each usage randomly selects a message from this list.
	/// </remarks>
	private readonly List<string> GoToMessages = new()
	{
		"“The stage is set, but no one showed up to perform.”\nPlease, check your settings to ensure your libraries are added and songs/tracks have been scanned—or this page starts auditioning shadows as headliners.",
		"“Microphones are hot, the crowd is quiet, and the artists are missing.”\nScan your music or this becomes the saddest karaoke bar in history.",
		"“All ego, no echo.”\nAdd your libraries or the page starts booking imaginary influencers.",
		"“Spotlights are on. The talent took a personal day.”\nScan your tracks or this page starts reciting inspirational quotes in falsetto.",
		"“Not even a one-hit wonder wandered in.”\nCheck your settings or this turns into a museum of hypothetical fame.",
		"“The green room is stocked, but even the ghost of Elvis declined.”\nScan or this page starts using mannequins for acoustic sets.",
		"“Every dressing room is empty—just snacks and broken dreams.”\nAdd your libraries or this page starts performing interpretive dance alone.",
		"“The silence is louder than a forgotten mixtape.”\nScan your music before this page pens a ballad titled ‘Unavailable.’",
		"“Even the auto-tune couldn’t find someone to fix.”\nCheck your settings or this page starts assigning stage names to white space.",
		"“ArtistPage is manifesting fame without a single file.”\nScan your songs before it legally changes its name to ‘Tumbleweed Tour.’"
	};
}
