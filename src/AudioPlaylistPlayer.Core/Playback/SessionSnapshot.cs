using AudioPlaylistPlayer.Core.Models;

namespace AudioPlaylistPlayer.Core.Playback;

public sealed class SessionSnapshot
{
    // Конструктор без параметров — для создания по умолчанию в AppState
    public SessionSnapshot() 
    {
        PlaylistId = null;
        TrackIndex = 0;
        PositionSeconds = 0;
        Status = PlaybackStatus.Stopped;
        Mode = (PlaybackMode)0; 
        ShuffleOrder = Array.Empty<int>();
        Volume = 1.0;
    }

    // Основной конструктор
    public SessionSnapshot(
        Guid? playlistId,
        int trackIndex,
        double positionSeconds,
        PlaybackStatus status,
        PlaybackMode mode,
        IReadOnlyList<int> shuffleOrder,
        double volume)
    {
        PlaylistId = playlistId;
        TrackIndex = trackIndex;
        PositionSeconds = positionSeconds;
        Status = status;
        Mode = mode;
        ShuffleOrder = shuffleOrder;
        Volume = volume;
    }

    public Guid? PlaylistId { get; }
    public int TrackIndex { get; }
    public double PositionSeconds { get; }
    public PlaybackStatus Status { get; }
    public PlaybackMode Mode { get; }
    public IReadOnlyList<int> ShuffleOrder { get; }
    public double Volume { get; }
}