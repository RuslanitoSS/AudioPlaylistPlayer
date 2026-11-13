using AudioPlaylistPlayer.Mobile.ViewModels;
namespace AudioPlaylistPlayer.Mobile;
public partial class PlaylistsPage : ContentPage
{
    public PlaylistsPage(AppViewModel vm) { InitializeComponent(); BindingContext = vm; }
}