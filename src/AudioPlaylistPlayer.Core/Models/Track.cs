namespace AudioPlaylistPlayer.Core.Models;

public sealed class Track
{
    public Track(string title, TrackSource source, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Название трека не задано.", nameof(title));
        ArgumentNullException.ThrowIfNull(source);

        Id = id ?? Guid.NewGuid();
        Title = title.Trim();
        Source = source;
    }

    public Guid Id { get; }

    public string Title { get; }

    public TrackSource Source { get; }
}
