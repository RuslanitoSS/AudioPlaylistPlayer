namespace AudioPlaylistPlayer.Core.Models;

public class TrackMovedEventArgs : EventArgs
{
    public TrackMovedEventArgs(Guid trackId, int oldIndex, int newIndex)
    {
        TrackId = trackId;
        OldIndex = oldIndex;
        NewIndex = newIndex;
    }

    public Guid TrackId { get; }
    public int OldIndex { get; }
    public int NewIndex { get; }
}