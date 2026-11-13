using Microsoft.Extensions.DependencyInjection;
using AudioPlaylistPlayer.Core.Models;
using AudioPlaylistPlayer.Core.Persistence;
using AudioPlaylistPlayer.Core.Playback;

namespace AudioPlaylistPlayer.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
        window.Stopped += (_, _) => SaveState();
        window.Destroying += (_, _) => SaveState();
        return window;
    }

    private static void SaveState()
    {
        try
        {
            var services = IPlatformApplication.Current?.Services;
            if (services is null) return;

            var store = services.GetRequiredService<AppStateStore>();
            var library = services.GetRequiredService<MusicLibrary>();
            var playlists = services.GetRequiredService<PlaylistLibrary>();
            var settings = services.GetRequiredService<AppSettings>();
            var player = services.GetRequiredService<PlayerController>();

            store.SaveFrom(MauiProgram.StateFilePath, library, playlists, settings, player);
        }
        catch
        {
            // Не должны ронять приложение из-за ошибки сохранения
        }
    }
}
