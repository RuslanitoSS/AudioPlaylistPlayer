using AudioPlaylistPlayer.Mobile.ViewModels;

namespace AudioPlaylistPlayer.Mobile;

public partial class HomePage : ContentPage
{
    public HomePage(AppViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}