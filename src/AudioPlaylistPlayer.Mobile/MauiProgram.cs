using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using AudioPlaylistPlayer.Mobile.Services;
using AudioPlaylistPlayer.Core.Persistence;
using AudioPlaylistPlayer.Core.Playback;
using AudioPlaylistPlayer.Core.Models;
using AudioPlaylistPlayer.Mobile.ViewModels;

namespace AudioPlaylistPlayer.Mobile;

public static class MauiProgram
{
    public static string StateFilePath => Path.Combine(FileSystem.AppDataDirectory, "player-state.json");

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // 1. Библиотеки (хранилища данных)
        var musicLibrary = new MusicLibrary();
        var playlistLibrary = new PlaylistLibrary();
        var settings = new AppSettings();

        // 2. Загружаем сохранённое состояние (треки, плейлисты, настройки, сессия)
        var stateStore = new AppStateStore();
        var persisted = stateStore.LoadInto(StateFilePath, musicLibrary, playlistLibrary, settings);

        // 3. Аудио сервисы
        builder.Services.AddSingleton<IAudioManager>(AudioManager.Current);
        builder.Services.AddSingleton<MauiAudioOutput>();

        builder.Services.AddSingleton(musicLibrary);
        builder.Services.AddSingleton(playlistLibrary);
        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton(stateStore);

        // 4. Регистрируем PlayerController
        builder.Services.AddSingleton(sp =>
        {
            var library = sp.GetRequiredService<MusicLibrary>();
            var playlists = sp.GetRequiredService<PlaylistLibrary>();
            var appSettings = sp.GetRequiredService<AppSettings>();
            var audioOutput = sp.GetRequiredService<MauiAudioOutput>();

            var player = new PlayerController(library, appSettings, audioOutput, playlists);
            player.ApplySession(AppStateMapper.ToSessionSnapshot(persisted.Session));
            return player;
        });

        // 5. Регистрируем ViewModel
        builder.Services.AddSingleton<AppViewModel>();

        // 6. Страницы
        builder.Services.AddTransient<AllTracksPage>();
        builder.Services.AddTransient<PlaylistsPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<QueuePage>();
        builder.Services.AddTransient<PlaylistDetailPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
