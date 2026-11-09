namespace AudioPlaylistPlayer.Core.Models;

public sealed class TrackEventArgs : EventArgs
{
    public TrackEventArgs(Track track, int index)
    {
        Track = track;
        Index = index;
    }

    public Track Track { get; }

    public int Index { get; }
}
