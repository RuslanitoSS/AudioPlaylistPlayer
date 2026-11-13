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

        // 1. Сначала регистрируем MusicLibrary, чтобы он был доступен везде
        builder.Services.AddSingleton<MusicLibrary>();

        // 2. Аудио сервисы
        builder.Services.AddSingleton<IAudioManager>(AudioManager.Current);
        builder.Services.AddSingleton<MauiAudioOutput>();

        // 3. Регистрируем PlayerController
        builder.Services.AddSingleton(sp =>
        {
            var library = sp.GetRequiredService<MusicLibrary>();
            var workingFile = Path.Combine(FileSystem.AppDataDirectory, "player-state.json");

            // Загружаем состояние (предполагаем, что state.Library - это PlaylistLibrary)
            var state = File.Exists(workingFile)
                ? new JsonStateStore().Load(workingFile)
                : new AppState();

            var audioOutput = sp.GetRequiredService<MauiAudioOutput>();

            // ВАЖНО: PlayerController теперь принимает MusicLibrary первым аргументом
            var player = new PlayerController(library, state.Settings, audioOutput);
            player.ApplySession(state.Session);
            // ==========================================
            // АВТОМАТИЧЕСКОЕ СКАНИРОВАНИЕ ПАПКИ
            // ==========================================
            if (state.MusicLibrary.Count == 0)
            {
                var musicFolder = @"C:\Music\MyCollection"; // Укажите вашу папку

                if (Directory.Exists(musicFolder))
                {
                    var mp3Files = Directory.GetFiles(musicFolder, "*.mp3");
                    var playlist = state.PlaylistLibrary.Create("Моя коллекция");

                    foreach (var filePath in mp3Files)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(filePath);
                        var track = new Track(fileName, new FileTrackSource(filePath));

                        // Добавляем в глобальную библиотеку
                        library.AddTrack(track);
                        // Добавляем ID в плейлист
                        playlist.AddTrackId(track.Id);
                    }

                    Console.WriteLine($"Найдено и добавлено {mp3Files.Length} треков");

                    if (playlist.TrackCount > 0)
                    {
                        player.SelectPlaylist(playlist, 0);
                    }
                }
            }
            // ==========================================
            return player;
        });

        // 4. Регистрируем ViewModel (DI сам найдет PlayerController и MusicLibrary)
        builder.Services.AddSingleton<AppViewModel>();

        // 5. Страницы
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