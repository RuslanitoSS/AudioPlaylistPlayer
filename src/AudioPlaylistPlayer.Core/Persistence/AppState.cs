using AudioPlaylistPlayer.Core.Models;
using AudioPlaylistPlayer.Core.Playback;

namespace AudioPlaylistPlayer.Core.Persistence;

public sealed class AppState
{
    public PlaylistLibrary Library { get; } = new();

    public AppSettings Settings { get; } = new();

    public SessionSnapshot Session { get; set; } = SessionSnapshot.Empty;
}
