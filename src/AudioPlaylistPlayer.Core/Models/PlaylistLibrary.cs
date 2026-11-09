namespace AudioPlaylistPlayer.Core.Models;

public sealed class PlaylistLibrary
{
    private readonly List<Playlist> _playlists = [];

    public PlaylistLibrary(IEnumerable<Playlist>? playlists = null)
    {
        if (playlists is null)
            return;

        foreach (var playlist in playlists)
            AddExisting(playlist);
    }

    public IReadOnlyList<Playlist> Playlists => _playlists;

    public event EventHandler<Playlist>? PlaylistAdded;

    public event EventHandler<Playlist>? PlaylistRemoved;

    public Playlist Create(string name)
    {
        EnsureUniqueName(name, null);
        var playlist = new Playlist(name);
        _playlists.Add(playlist);
        PlaylistAdded?.Invoke(this, playlist);
        return playlist;
    }

    public void AddExisting(Playlist playlist)
    {
        ArgumentNullException.ThrowIfNull(playlist);
        EnsureUniqueName(playlist.Name, playlist.Id);
        _playlists.Add(playlist);
        PlaylistAdded?.Invoke(this, playlist);
    }

    public void Remove(Guid id)
    {
        var index = _playlists.FindIndex(playlist => playlist.Id == id);
        if (index < 0)
            throw new InvalidOperationException("Плейлист не найден.");

        var playlist = _playlists[index];
        _playlists.RemoveAt(index);
        PlaylistRemoved?.Invoke(this, playlist);
    }

    public Playlist? FindById(Guid id) => _playlists.FirstOrDefault(playlist => playlist.Id == id);

    public Playlist? FindByName(string name) =>
        _playlists.FirstOrDefault(playlist => playlist.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));

    public Playlist RequireByName(string name)
    {
        return FindByName(name) ?? throw new InvalidOperationException($"Плейлист «{name}» не найден.");
    }

    private void EnsureUniqueName(string name, Guid? ignoreId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var clash = _playlists.FirstOrDefault(playlist =>
            playlist.Id != ignoreId &&
            playlist.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
        if (clash is not null)
            throw new InvalidOperationException($"Плейлист «{clash.Name}» уже есть.");
    }
}
