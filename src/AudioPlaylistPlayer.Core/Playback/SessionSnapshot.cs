using AudioPlaylistPlayer.Core.Models;

namespace AudioPlaylistPlayer.Core.Playback;

public sealed record SessionSnapshot(
    Guid? PlaylistId,
    int TrackIndex,
    double PositionSeconds,
    PlaybackStatus Status,
    PlaybackMode Mode,
    IReadOnlyList<int> ShuffleOrder,
    double Volume)
{
    public static SessionSnapshot Empty { get; } = new(
        null,
        0,
        0,
        PlaybackStatus.Stopped,
        PlaybackMode.Sequential,
        [],
        0.8);
}
