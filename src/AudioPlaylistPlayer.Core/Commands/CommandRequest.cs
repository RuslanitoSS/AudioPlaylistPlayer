using AudioPlaylistPlayer.Core.Models;

namespace AudioPlaylistPlayer.Core.Commands;

public enum CommandKind
{
    Help,
    Error,
    PlaylistList,
    PlaylistCreate,
    PlaylistDelete,
    PlaylistRename,
    PlaylistAdd,
    PlaylistRemove,
    Mode,
    Play,
    Pause,
    Next,
    Previous,
    StateSave,
    StateLoad
}

public sealed class CommandRequest
{
    public CommandKind Kind { get; init; }

    public string? PlaylistName { get; init; }

    public string? NewName { get; init; }

    public string? Path { get; init; }

    public int? TrackNumber { get; init; }

    public PlaybackMode? Mode { get; init; }

    public string? Error { get; init; }

    public static CommandRequest Help() => new() { Kind = CommandKind.Help };

    public static CommandRequest Fail(string error) => new() { Kind = CommandKind.Error, Error = error };

    public static CommandRequest Simple(CommandKind kind) => new() { Kind = kind };
}
