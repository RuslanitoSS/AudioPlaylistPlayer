using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using AudioPlaylistPlayer.Core.Models;
using AudioPlaylistPlayer.Core.Playback;

namespace AudioPlaylistPlayer.Mobile.ViewModels;

public sealed class AppViewModel : INotifyPropertyChanged
{
    private readonly PlayerController _player;
    private readonly MusicLibrary _library;
    private readonly PlaylistLibrary _playlistLibrary;

    public AppViewModel(PlayerController player, MusicLibrary library, PlaylistLibrary playlistLibrary)
    {
        _player = player;
        _library = library;
        _playlistLibrary = playlistLibrary;

        Playlists = new ObservableCollection<Playlist>();
        AllTracks = new ObservableCollection<Track>();

        RefreshPlaylists();
        RefreshAllTracks();

        PlayAllCommand = new Command(async () => await PlayAllAsync());

        // ИСПРАВЛЕНИЕ: раньше использовался Shell.Current.GoToAsync("AllTracksPage"),
        // но эти страницы не зарегистрированы как маршруты Shell — переход бросал
        // InvalidOperationException внутри async-lambda команды, и приложение падало.
        // Теперь используется прямой Navigation.PushAsync с созданием страниц через DI.
        NavigateToAllTracksCommand = new Command(async () => await NavigateAsync<AllTracksPage>("Не удалось открыть «Все треки»"));
        NavigateToPlaylistsCommand = new Command(async () => await NavigateAsync<PlaylistsPage>("Не удалось открыть «Плейлисты»"));

        CreatePlaylistCommand = new Command(async () => await CreatePlaylistAsync());
        AddTracksCommand = new Command(async () => await AddTracksAsync());
        PlayTrackCommand = new Command<Track?>(track => { if (track is not null) _ = PlayTrackAsync(track); });
    }

    public ObservableCollection<Playlist> Playlists { get; }
    public ObservableCollection<Track> AllTracks { get; }

    public ICommand CreatePlaylistCommand { get; }
    public ICommand AddTracksCommand { get; }
    public ICommand PlayTrackCommand { get; }

    public ICommand NavigateToAllTracksCommand { get; }
    public ICommand NavigateToPlaylistsCommand { get; }
    public ICommand PlayAllCommand { get; }

    private static INavigation CurrentNavigation =>
        Application.Current?.Windows?.FirstOrDefault()?.Page?.Navigation
        ?? throw new InvalidOperationException("Навигация недоступна.");

    private static Task NavigateAsync<TPage>(string errorTitle) where TPage : Page
    {
        try
        {
            var services = IPlatformApplication.Current?.Services;
            if (services is null)
                return Task.CompletedTask;

            // Страницы зарегистрированы в DI как transient — создаём через сервис-провайдер.
            var page = ActivatorUtilities.CreateInstance<TPage>(services);
            return CurrentNavigation.PushAsync(page);
        }
        catch (Exception ex)
        {
            var page = Application.Current?.Windows?.FirstOrDefault()?.Page;
            return page?.DisplayAlertAsync(errorTitle, ex.Message, "ОК") ?? Task.CompletedTask;
        }
    }

    private void RefreshPlaylists()
    {
        Playlists.Clear();
        foreach (var playlist in _playlistLibrary.Playlists)
        {
            Playlists.Add(playlist);
        }
    }

    private void RefreshAllTracks()
    {
        AllTracks.Clear();
        foreach (var track in _library.AllTracks)
        {
            AllTracks.Add(track);
        }
    }

    private async Task PlayAllAsync()
    {
        var page = Application.Current?.Windows?.FirstOrDefault()?.Page;
        if (page is null) return;

        if (_library.AllTracks.Count == 0)
        {
            await page.DisplayAlertAsync("Пусто", "Сначала добавьте аудиофайлы кнопкой «Добавить».", "ОК");
            return;
        }

        var allMusic = new Playlist("Вся музыка");
        foreach (var t in _library.AllTracks)
        {
            allMusic.AddTrackId(t.Id);
        }

        _player.SelectPlaylist(allMusic, 0);

        // ИСПРАВЛЕНИЕ КРАША: ошибки воспроизведения (отсутствующий/повреждённый файл,
        // нечитаемый формат) раньше уходили в необработанное исключение и приложение
        // вылетало. Теперь всё перехватывается и показывается пользователю.
        try
        {
            await _player.PlayAsync();
        }
        catch (Exception ex)
        {
            _player.Stop();
            await page.DisplayAlertAsync("Ошибка воспроизведения", ex.Message, "ОК");
            return;
        }

        // Переход на страницу очереди, где находится плеер
        await NavigateAsync<QueuePage>("Не удалось открыть очередь воспроизведения");
    }
    private async Task CreatePlaylistAsync()
    {
        var page = Application.Current?.Windows[0]?.Page;
        if (page == null) return;

        var name = await page.DisplayPromptAsync("Новый плейлист", "Введите название:", "Создать", "Отмена");
        if (!string.IsNullOrWhiteSpace(name))
        {
            try
            {
                _playlistLibrary.Create(name);
                RefreshPlaylists();
            }
            catch (InvalidOperationException ex)
            {
                await page.DisplayAlertAsync("Ошибка", ex.Message, "ОК");
            }
        }
    }

    private async Task AddTracksAsync()
    {
        var page = Application.Current?.Windows[0]?.Page;
        if (page == null) return;

        try
        {
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.iOS, new[] { "public.audio" } },
                { DevicePlatform.Android, new[] { "audio/*" } },
                { DevicePlatform.WinUI, new[] { ".mp3", ".wav", ".m4a", ".ogg", ".flac" } },
            });

            var results = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Выберите аудиофайлы",
                FileTypes = customFileType,
            });

            if (results == null || !results.Any()) return;

            foreach (var file in results)
            {
                var cachePath = Path.Combine(FileSystem.CacheDirectory, file.FileName);
                
                using (var sourceStream = await file.OpenReadAsync())
                using (var destStream = File.Create(cachePath))
                {
                    await sourceStream.CopyToAsync(destStream);
                }

                // Резервная копия в AppDataDirectory — кэш может быть очищён ОС,
                // и тогда воспроизведение падало бы с FileNotFoundException.
                var backupDir = Path.Combine(FileSystem.AppDataDirectory, "TrackBackup");
                Directory.CreateDirectory(backupDir);
                File.Copy(cachePath, Path.Combine(backupDir, file.FileName), overwrite: true);

                var track = new Track(file.FileName, new FileTrackSource(cachePath));
                _library.AddTrack(track);
            }
            
            RefreshAllTracks();
            await page.DisplayAlertAsync("Успех", $"Добавлено {results.Count()} треков", "ОК");
        }
        catch (Exception ex)
        {
            await page.DisplayAlertAsync("Ошибка", $"Не удалось добавить треки: {ex.Message}", "ОК");
        }
    }

    private async Task PlayTrackAsync(Track track)
    {
        var page = Application.Current?.Windows?.FirstOrDefault()?.Page;
        if (page is null) return;

        // Создаем временный плейлист из всех треков и запускаем нужный
        var allMusic = new Playlist("Все треки");
        foreach (var t in _library.AllTracks) allMusic.AddTrackId(t.Id);

        // Ищем индекс вручную, так как IReadOnlyList<Guid> не имеет метода IndexOf
        int index = -1;
        for (int i = 0; i < allMusic.TrackIds.Count; i++)
        {
            if (allMusic.TrackIds[i] == track.Id)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            await page.DisplayAlertAsync("Ошибка", "Трек не найден в библиотеке.", "ОК");
            return;
        }

        _player.SelectPlaylist(allMusic, index);

        // ИСПРАВЛЕНИЕ КРАША: запуск воспроизведения защищён от ошибок —
        // вместо вылета приложения показываем сообщение.
        try
        {
            await _player.PlayAsync();
        }
        catch (Exception ex)
        {
            _player.Stop();
            await page.DisplayAlertAsync("Ошибка воспроизведения", ex.Message, "ОК");
            return;
        }

        await NavigateAsync<QueuePage>("Не удалось открыть очередь воспроизведения");
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}