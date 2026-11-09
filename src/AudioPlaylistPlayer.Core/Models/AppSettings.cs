namespace AudioPlaylistPlayer.Core.Models;

public sealed class AppSettings
{
    public AppThemePreference Theme { get; set; } = AppThemePreference.System;

    public double Volume { get; set; } = 0.8;

    public PlaybackMode DefaultPlaybackMode { get; set; } = PlaybackMode.Sequential;

    public void CopyFrom(AppSettings other)
    {
        ArgumentNullException.ThrowIfNull(other);
        Theme = other.Theme;
        Volume = other.Volume;
        DefaultPlaybackMode = other.DefaultPlaybackMode;
    }
}
