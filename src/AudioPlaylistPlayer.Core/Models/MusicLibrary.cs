namespace AudioPlaylistPlayer.Core.Models;

/// <summary>
/// Центральное хранилище всех треков в системе.
/// Обеспечивает единую точку доступа к трекам по их ID.
/// </summary>
public sealed class MusicLibrary
{
    private readonly Dictionary<Guid, Track> _allTracks = new();

    /// <summary>
    /// Все треки в библиотеке.
    /// </summary>
    public IReadOnlyCollection<Track> AllTracks => _allTracks.Values;

    /// <summary>
    /// Количество треков в библиотеке.
    /// </summary>
    public int Count => _allTracks.Count;

    /// <summary>
    /// Добавляет трек в библиотеку. Если трек с таким ID уже существует, обновляет его.
    /// </summary>
    public void AddTrack(Track track)
    {
        ArgumentNullException.ThrowIfNull(track);
        _allTracks[track.Id] = track;
    }

    /// <summary>
    /// Добавляет несколько треков.
    /// </summary>
    public void AddTracks(IEnumerable<Track> tracks)
    {
        ArgumentNullException.ThrowIfNull(tracks);
        foreach (var track in tracks)
        {
            AddTrack(track);
        }
    }

    /// <summary>
    /// Получает трек по ID. Возвращает null, если не найден.
    /// </summary>
    public Track? GetTrack(Guid id)
    {
        return _allTracks.TryGetValue(id, out var track) ? track : null;
    }

    /// <summary>
    /// Проверяет, существует ли трек с указанным ID.
    /// </summary>
    public bool ContainsTrack(Guid id)
    {
        return _allTracks.ContainsKey(id);
    }

    /// <summary>
    /// Удаляет трек из библиотеки.
    /// </summary>
    public bool RemoveTrack(Guid id)
    {
        return _allTracks.Remove(id);
    }

    /// <summary>
    /// Получает все треки по списку ID (для плейлиста).
    /// Пропускает ID, для которых треки не найдены.
    /// </summary>
    public IReadOnlyList<Track> GetTracksByIds(IEnumerable<Guid> trackIds)
    {
        ArgumentNullException.ThrowIfNull(trackIds);
        return trackIds
            .Select(id => GetTrack(id))
            .Where(track => track != null)
            .Cast<Track>()
            .ToList()
            .AsReadOnly();
    }
}