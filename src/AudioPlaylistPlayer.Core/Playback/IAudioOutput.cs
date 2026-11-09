using AudioPlaylistPlayer.Core.Models;

namespace AudioPlaylistPlayer.Core.Playback;

public interface IAudioOutput
{
    double PositionSeconds { get; }

    double? DurationSeconds { get; }

    Task PlayAsync(Track track, double positionSeconds, double volume, CancellationToken cancellationToken = default);

    void Pause();

    void Resume();

    void Stop();

    void Seek(double positionSeconds);

    void SetVolume(double volume);

    event EventHandler? PlaybackCompleted;
}
