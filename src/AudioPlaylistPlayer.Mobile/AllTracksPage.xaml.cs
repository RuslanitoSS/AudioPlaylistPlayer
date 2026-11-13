using AudioPlaylistPlayer.Mobile.ViewModels;
namespace AudioPlaylistPlayer.Mobile;
public partial class AllTracksPage : ContentPage
{
    public AllTracksPage(AppViewModel vm) { InitializeComponent(); BindingContext = vm; }
}