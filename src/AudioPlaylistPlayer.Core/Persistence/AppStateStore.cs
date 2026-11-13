using System.Text.Json;
using AudioPlaylistPlayer.Core.Models;
using AudioPlaylistPlayer.Core.Playback;

namespace AudioPlaylistPlayer.Core.Persistence;

/// <summary>
/// Хранилище состояния приложения в виде плоского JSON-документа.
/// Совместимо со всеми платформами (.NET для мобильных устройств включительно).
/// </summary>
public sealed class AppStateStore
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true
    };

    public PersistedAppState Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
            return new PersistedAppState();

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<PersistedAppState>(json, _options) ?? new PersistedAppState();
        }
        catch (JsonException)
        {
            // Повреждённый файл — начинаем с чистого состояния.
            return new PersistedAppState();
        }
    }

    public void Save(string filePath, PersistedAppState state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(state);

        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(state, _options);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Загружает состояние и наполняет им библиотеки/настройки.
    /// </summary>
    public PersistedAppState LoadInto(
        string filePath,
        MusicLibrary musicLibrary,
        PlaylistLibrary playlistLibrary,
        AppSettings settings)
    {
        var persisted = Load(filePath);
        AppStateMapper.ApplyTo(persisted, musicLibrary, playlistLibrary, settings);
        return persisted;
    }

    /// <summary>
    /// Сохраняет текущее состояние библиотек и плеера в файл.
    /// </summary>
    public void SaveFrom(
        string filePath,
        MusicLibrary musicLibrary,
        PlaylistLibrary playlistLibrary,
        AppSettings settings,
        PlayerController player)
    {
        var snapshot = player.CaptureSession();
        var persisted = new PersistedAppState
        {
            Settings = new PersistedSettings
            {
                Theme = settings.Theme,
                Volume = settings.Volume,
                DefaultPlaybackMode = settings.DefaultPlaybackMode
            },
            Tracks = musicLibrary.AllTracks
                .Select(t => new PersistedTrack(t.Id, t.Title, t.Source.Kind, t.Source.Location))
                .ToList(),
            Playlists = playlistLibrary.Playlists
                .Select(p => new PersistedPlaylist
                {
                    Id = p.Id,
                    Name = p.Name,
                    TrackIds = p.TrackIds.ToList()
                })
                .ToList(),
            Session = new PersistedSession
            {
                PlaylistId = snapshot.PlaylistId,
                TrackIndex = snapshot.TrackIndex,
                PositionSeconds = snapshot.PositionSeconds,
                Status = snapshot.Status,
                Mode = snapshot.Mode,
                ShuffleOrder = snapshot.ShuffleOrder.ToList(),
                Volume = snapshot.Volume
            }
        };

        Save(filePath, persisted);
    }
}
