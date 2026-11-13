using AudioPlaylistPlayer.Core.Models;
using AudioPlaylistPlayer.Core.Playback;

namespace AudioPlaylistPlayer.Core.Persistence;

public sealed record PersistedTrack(
    Guid Id,
    string Title,
    string SourceKind,
    string Location);

public sealed class PersistedPlaylist
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Guid> TrackIds { get; set; } = [];
}

public sealed class PersistedSettings
{
    public AppThemePreference Theme { get; set; } = AppThemePreference.System;
    public double Volume { get; set; } = 0.8;
    public PlaybackMode DefaultPlaybackMode { get; set; } = PlaybackMode.Sequential;
}

public sealed class PersistedSession
{
    public Guid? PlaylistId { get; set; }
    public int TrackIndex { get; set; }
    public double PositionSeconds { get; set; }
    public PlaybackStatus Status { get; set; }
    public PlaybackMode Mode { get; set; }
    public List<int> ShuffleOrder { get; set; } = [];
    public double Volume { get; set; } = 0.8;
}

/// <summary>
/// Плоский документ состояния, удобный для хранения в JSON-файле.
/// </summary>
public sealed class PersistedAppState
{
    public int Version { get; set; } = 3;
    public PersistedSettings Settings { get; set; } = new();
    public List<PersistedTrack> Tracks { get; set; } = [];
    public List<PersistedPlaylist> Playlists { get; set; } = [];
    public PersistedSession Session { get; set; } = new();
}

public static class AppStateMapper
{
    public static PersistedAppState ToPersisted(AppState state, PlayerController player)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(player);

        var snapshot = player.CaptureSession();
        var persisted = new PersistedAppState
        {
            Settings = new PersistedSettings
            {
                Theme = state.Settings.Theme,
                Volume = state.Settings.Volume,
                DefaultPlaybackMode = state.Settings.DefaultPlaybackMode
            },
            Tracks = state.MusicLibrary.AllTracks.Select(t => new PersistedTrack(
                t.Id,
                t.Title,
                t.Source.Kind,
                t.Source.Location)).ToList(),
            Playlists = state.PlaylistLibrary.Playlists.Select(p => new PersistedPlaylist
            {
                Id = p.Id,
                Name = p.Name,
                TrackIds = p.TrackIds.ToList()
            }).ToList(),
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

        return persisted;
    }

    public static void ApplyTo(PersistedAppState persisted, MusicLibrary musicLibrary, PlaylistLibrary playlistLibrary, AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(persisted);
        ArgumentNullException.ThrowIfNull(musicLibrary);
        ArgumentNullException.ThrowIfNull(playlistLibrary);
        ArgumentNullException.ThrowIfNull(settings);

        foreach (var trackDto in persisted.Tracks)
        {
            if (!string.Equals(trackDto.SourceKind, FileTrackSource.KindName, StringComparison.OrdinalIgnoreCase))
                continue;

            var track = new Track(trackDto.Title, new FileTrackSource(trackDto.Location), trackDto.Id);
            musicLibrary.AddTrack(track);
        }

        foreach (var playlistDto in persisted.Playlists)
        {
            if (playlistLibrary.FindById(playlistDto.Id) is not null)
                continue;

            var playlist = new Playlist(playlistDto.Name, playlistDto.Id, playlistDto.TrackIds);
            playlistLibrary.AddExisting(playlist);
        }

        settings.Theme = persisted.Settings.Theme;
        settings.Volume = Math.Clamp(persisted.Settings.Volume, 0, 1);
        settings.DefaultPlaybackMode = persisted.Settings.DefaultPlaybackMode;
    }

    public static SessionSnapshot ToSessionSnapshot(PersistedSession session) => new(
        session.PlaylistId,
        session.TrackIndex,
        session.PositionSeconds,
        session.Status,
        session.Mode,
        session.ShuffleOrder,
        session.Volume);
}
