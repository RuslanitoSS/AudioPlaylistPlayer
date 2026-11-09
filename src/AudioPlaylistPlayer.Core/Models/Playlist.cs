namespace AudioPlaylistPlayer.Core.Models;

public sealed class Playlist
{
    private readonly List<Track> _tracks = [];

    public Playlist(string name, Guid? id = null, IEnumerable<Track>? tracks = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = id ?? Guid.NewGuid();
        Name = name.Trim();
        if (tracks is not null)
            _tracks.AddRange(tracks);
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public IReadOnlyList<Track> Tracks => _tracks;

    public event EventHandler<TrackEventArgs>? TrackAdded;

    public event EventHandler<TrackEventArgs>? TrackRemoved;

    public event EventHandler<TrackMovedEventArgs>? TrackMoved;

    public event EventHandler? Renamed;

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var trimmed = name.Trim();
        if (trimmed == Name)
            return;

        Name = trimmed;
        Renamed?.Invoke(this, EventArgs.Empty);
    }

    public void Add(Track track)
    {
        ArgumentNullException.ThrowIfNull(track);
        _tracks.Add(track);
        TrackAdded?.Invoke(this, new TrackEventArgs(track, _tracks.Count - 1));
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _tracks.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "В плейлисте нет трека с таким номером.");

        var track = _tracks[index];
        _tracks.RemoveAt(index);
        TrackRemoved?.Invoke(this, new TrackEventArgs(track, index));
    }

    public void Move(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= _tracks.Count)
            throw new ArgumentOutOfRangeException(nameof(oldIndex));
        if (newIndex < 0 || newIndex >= _tracks.Count)
            throw new ArgumentOutOfRangeException(nameof(newIndex));
        if (oldIndex == newIndex)
            return;

        var track = _tracks[oldIndex];
        _tracks.RemoveAt(oldIndex);
        _tracks.Insert(newIndex, track);
        TrackMoved?.Invoke(this, new TrackMovedEventArgs(track, oldIndex, newIndex));
    }
}
