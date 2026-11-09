using AudioPlaylistPlayer.Core.Models;

namespace AudioPlaylistPlayer.Core.Playback;

/// <summary>
/// Выход без звука: консоль и тесты меняют модель, не открывая устройство.
/// </summary>
public sealed class SilentAudioOutput : IAudioOutput
{
    public double PositionSeconds { get; private set; }

    public double? DurationSeconds { get; set; }

    public Track? CurrentTrack { get; private set; }

    public bool IsPlaying { get; private set; }

    public double Volume { get; private set; }

    public event EventHandler? PlaybackCompleted;

    public Task PlayAsync(Track track, double positionSeconds, double volume, CancellationToken cancellationToken = default)
    {
        CurrentTrack = track;
        PositionSeconds = Math.Max(0, positionSeconds);
        Volume = volume;
        IsPlaying = true;
        return Task.CompletedTask;
    }

    public void Pause() => IsPlaying = false;

    public void Resume() => IsPlaying = true;

    public void Stop()
    {
        IsPlaying = false;
        PositionSeconds = 0;
    }

    public void Seek(double positionSeconds) => PositionSeconds = Math.Max(0, positionSeconds);

    public void SetVolume(double volume) => Volume = volume;

    public void RaiseCompleted() => PlaybackCompleted?.Invoke(this, EventArgs.Empty);
}
