namespace AudioPlaylistPlayer.Core.Models;

public sealed class TrackMovedEventArgs : EventArgs
{
    public TrackMovedEventArgs(Track track, int oldIndex, int newIndex)
    {
        Track = track;
        OldIndex = oldIndex;
        NewIndex = newIndex;
    }

    public Track Track { get; }

    public int OldIndex { get; }

    public int NewIndex { get; }
}
