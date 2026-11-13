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

    public AppViewModel(PlayerController player, MusicLibrary library)
    {
        _player = player;
        _library = library;
        
        Playlists = new ObservableCollection<Playlist>();
        AllTracks = new ObservableCollection<Track>();
        
        RefreshPlaylists();
        RefreshAllTracks();
        
        PlayAllCommand = new Command(async () => await PlayAllAsync());
        NavigateToAllTracksCommand = new Command(() => Shell.Current.GoToAsync("AllTracksPage"));
        NavigateToPlaylistsCommand = new Command(() => Shell.Current.GoToAsync("PlaylistsPage"));
        

        CreatePlaylistCommand = new Command(async () => await CreatePlaylistAsync());
        AddTracksCommand = new Command(async () => await AddTracksAsync());
        PlayTrackCommand = new Command<Track>(track => PlayTrack(track));
       
    public ObservableCollection<Playlist> Playlists { get; }
    public ObservableCollection<Track> AllTracks { get; }

    public ICommand CreatePlaylistCommand { get; }
    public ICommand AddTracksCommand { get; }
    public ICommand PlayTrackCommand { get; }
    
    public ICommand NavigateToAllTracksCommand { get; }
    public ICommand NavigateToPlaylistsCommand { get; }
    public ICommand PlayAllCommand { get; }

    private void RefreshPlaylists()
    {
        Playlists.Clear();
        // Здесь нужно получать плейлисты из PlaylistLibrary
        // Пока заглушка
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
        if (_library.AllTracks.Count == 0) return;

        var allMusic = new Playlist("Вся музыка");
        foreach (var t in _library.AllTracks) 
        {
            allMusic.AddTrackId(t.Id);
        }

        _player.SelectPlaylist(allMusic, 0);
        await _player.PlayAsync(); // <-- ИСПРАВЛЕНИЕ: Явный запуск воспроизведения после выбора
        
        // Переход на страницу очереди, где находится плеер
        await Shell.Current.GoToAsync("QueuePage");
    }
    private async Task CreatePlaylistAsync()
    {
        var page = Application.Current?.Windows[0]?.Page;
        if (page == null) return;

        var name = await page.DisplayPromptAsync("Новый плейлист", "Введите название:", "Создать", "Отмена");
        if (!string.IsNullOrWhiteSpace(name))
        {
            // Нужно добавить в PlaylistLibrary
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
                
                using var sourceStream = await file.OpenReadAsync();
                using var fileStream = File.Create(cachePath);
                await sourceStream.CopyToAsync(fileStream);

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

    private void PlayTrack(Track track)
    {
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

        if (index >= 0)
        {
            _player.SelectPlaylist(allMusic, index);
            Shell.Current.GoToAsync(nameof(QueuePage));
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}