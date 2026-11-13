namespace AudioPlaylistPlayer.Core.Models;

public sealed class Playlist
{
    private readonly List<Guid> _trackIds = [];

    public Playlist(string name, Guid? id = null, IEnumerable<Guid>? trackIds = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = id ?? Guid.NewGuid();
        Name = name.Trim();
        if (trackIds is not null)
            _trackIds.AddRange(trackIds);
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public string Description { get; set; } = string.Empty;
    
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Список ID треков в плейлисте (порядок важен).
    /// </summary>
    public IReadOnlyList<Guid> TrackIds => _trackIds;

    /// <summary>
    /// Количество треков в плейлисте.
    /// </summary>
    public int TrackCount => _trackIds.Count;

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

    /// <summary>
    /// Добавляет ID трека в плейлист.
    /// </summary>
    public void AddTrackId(Guid trackId)
    {
        _trackIds.Add(trackId);
        TrackAdded?.Invoke(this, new TrackEventArgs(trackId, _trackIds.Count - 1));
    }

    /// <summary>
    /// Удаляет трек по индексу.
    /// </summary>
    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _trackIds.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "В плейлисте нет трека с таким номером.");

        var trackId = _trackIds[index];
        _trackIds.RemoveAt(index);
        TrackRemoved?.Invoke(this, new TrackEventArgs(trackId, index));
    }

    /// <summary>
    /// Перемещает трек с одной позиции на другую.
    /// </summary>
    public void Move(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= _trackIds.Count)
            throw new ArgumentOutOfRangeException(nameof(oldIndex));
        if (newIndex < 0 || newIndex >= _trackIds.Count)
            throw new ArgumentOutOfRangeException(nameof(newIndex));
        if (oldIndex == newIndex)
            return;

        var trackId = _trackIds[oldIndex];
        _trackIds.RemoveAt(oldIndex);
        _trackIds.Insert(newIndex, trackId);
        TrackMoved?.Invoke(this, new TrackMovedEventArgs(trackId, oldIndex, newIndex));
    }

    /// <summary>
    /// Получает реальные объекты треков через MusicLibrary.
    /// </summary>
    public IReadOnlyList<Track> GetTracks(MusicLibrary library)
    {
        ArgumentNullException.ThrowIfNull(library);
        return library.GetTracksByIds(_trackIds);
    }
}