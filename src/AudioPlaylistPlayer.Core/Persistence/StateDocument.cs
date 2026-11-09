namespace AudioPlaylistPlayer.Core.Persistence;

public sealed class StateDocument
{
    public int Version { get; set; } = JsonStateStore.CurrentVersion;

    public SettingsDocument Settings { get; set; } = new();

    public LibraryDocument Library { get; set; } = new();

    public SessionDocument Session { get; set; } = new();
}

public sealed class SettingsDocument
{
    public string Theme { get; set; } = "System";

    public double Volume { get; set; } = 0.8;

    public string DefaultPlaybackMode { get; set; } = "Sequential";
}

public sealed class LibraryDocument
{
    public List<PlaylistDocument> Playlists { get; set; } = [];
}

public sealed class PlaylistDocument
{
    public Guid Id { get; set; }

    public string Name { get; set; } = "";

    public List<TrackDocument> Tracks { get; set; } = [];
}

public sealed class TrackDocument
{
    public Guid Id { get; set; }

    public string Title { get; set; } = "";

    public string SourceKind { get; set; } = "file";

    public string Location { get; set; } = "";
}

public sealed class SessionDocument
{
    public Guid? PlaylistId { get; set; }

    public int TrackIndex { get; set; }

    public double PositionSeconds { get; set; }

    public string Status { get; set; } = "Stopped";

    public string Mode { get; set; } = "Sequential";

    public List<int> ShuffleOrder { get; set; } = [];

    public double Volume { get; set; } = 0.8;
}
