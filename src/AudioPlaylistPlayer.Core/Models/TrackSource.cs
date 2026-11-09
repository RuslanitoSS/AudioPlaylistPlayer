namespace AudioPlaylistPlayer.Core.Models;

/// <summary>
/// Откуда брать звук трека. Сами байты сюда не входят:
/// плеер хранит только описание источника.
/// </summary>
public abstract class TrackSource
{
    public abstract string Kind { get; }

    public abstract string Location { get; }
}
