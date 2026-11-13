namespace AudioPlaylistPlayer.Mobile;

public partial class PlaylistDetailPage : ContentPage
{
    public PlaylistDetailPage()
    {
        InitializeComponent();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}