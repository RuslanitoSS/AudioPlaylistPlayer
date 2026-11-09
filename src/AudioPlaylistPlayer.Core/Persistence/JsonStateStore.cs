using System.Text.Json;
using AudioPlaylistPlayer.Core.Models;
using AudioPlaylistPlayer.Core.Playback;

namespace AudioPlaylistPlayer.Core.Persistence;

public sealed class JsonStateStore
{
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Save(string path, AppState state, PlayerController player)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var document = ToDocument(state, player.CaptureSession());
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(path, JsonSerializer.Serialize(document, Options));
    }

    public AppState Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
            throw new FileNotFoundException("Файл состояния не найден.", path);

        var json = File.ReadAllText(path);
        var document = JsonSerializer.Deserialize<StateDocument>(json, Options)
            ?? throw new InvalidDataException("Файл состояния пуст.");

        if (document.Version != CurrentVersion)
            throw new InvalidDataException($"Версия файла состояния {document.Version} не поддерживается.");

        return FromDocument(document);
    }

    public static StateDocument ToDocument(AppState state, SessionSnapshot session)
    {
        return new StateDocument
        {
            Version = CurrentVersion,
            Settings = new SettingsDocument
            {
                Theme = state.Settings.Theme.ToString(),
                Volume = state.Settings.Volume,
                DefaultPlaybackMode = state.Settings.DefaultPlaybackMode.ToString()
            },
            Library = new LibraryDocument
            {
                Playlists = state.Library.Playlists.Select(playlist => new PlaylistDocument
                {
                    Id = playlist.Id,
                    Name = playlist.Name,
                    Tracks = playlist.Tracks.Select(track => new TrackDocument
                    {
                        Id = track.Id,
                        Title = track.Title,
                        SourceKind = track.Source.Kind,
                        Location = track.Source.Location
                    }).ToList()
                }).ToList()
            },
            Session = new SessionDocument
            {
                PlaylistId = session.PlaylistId,
                TrackIndex = session.TrackIndex,
                PositionSeconds = session.PositionSeconds,
                Status = session.Status.ToString(),
                Mode = session.Mode.ToString(),
                ShuffleOrder = session.ShuffleOrder.ToList(),
                Volume = session.Volume
            }
        };
    }

    public static AppState FromDocument(StateDocument document)
    {
        var state = new AppState();
        state.Settings.Theme = ParseEnum<AppThemePreference>(document.Settings.Theme, "тема");
        state.Settings.Volume = Math.Clamp(document.Settings.Volume, 0, 1);
        state.Settings.DefaultPlaybackMode = ParseEnum<PlaybackMode>(document.Settings.DefaultPlaybackMode, "режим");

        foreach (var playlistDocument in document.Library.Playlists)
        {
            var tracks = playlistDocument.Tracks.Select(CreateTrack);
            state.Library.AddExisting(new Playlist(playlistDocument.Name, playlistDocument.Id, tracks));
        }

        var session = document.Session;
        state.Session = new SessionSnapshot(
            session.PlaylistId,
            session.TrackIndex,
            session.PositionSeconds,
            ParseEnum<PlaybackStatus>(session.Status, "статус"),
            ParseEnum<PlaybackMode>(session.Mode, "режим воспроизведения"),
            session.ShuffleOrder,
            Math.Clamp(session.Volume, 0, 1));
        state.Settings.Volume = state.Session.Volume;
        state.Settings.DefaultPlaybackMode = state.Session.Mode;
        return state;
    }

    private static Track CreateTrack(TrackDocument document)
    {
        TrackSource source = document.SourceKind switch
        {
            FileTrackSource.KindName => new FileTrackSource(document.Location),
            _ => throw new InvalidDataException($"Неизвестный источник трека «{document.SourceKind}».")
        };

        return new Track(document.Title, source, document.Id);
    }

    private static TEnum ParseEnum<TEnum>(string value, string field) where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed))
            return parsed;

        throw new InvalidDataException($"Некорректное значение «{value}» в поле «{field}».");
    }
}
