namespace AudioPlaylistPlayer.Core.Models;

public class TrackEventArgs : EventArgs
{
    public TrackEventArgs(Guid trackId, int index)
    {
        TrackId = trackId;
        Index = index;
    }

    public Guid TrackId { get; }
    public int Index { get; }
}