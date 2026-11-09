using AudioPlaylistPlayer.Core.Models;

namespace AudioPlaylistPlayer.Core.Commands;

public static class CommandParser
{
    public static CommandRequest Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0 || IsHelp(args[0]))
            return CommandRequest.Help();

        return args[0].ToLowerInvariant() switch
        {
            "playlist" => ParsePlaylist(args),
            "mode" => ParseMode(args),
            "play" => ExpectExact(args, CommandKind.Play),
            "pause" => ExpectExact(args, CommandKind.Pause),
            "next" => ExpectExact(args, CommandKind.Next),
            "prev" or "previous" => ExpectExact(args, CommandKind.Previous),
            "state" => ParseState(args),
            _ => CommandRequest.Fail($"Неизвестная команда «{args[0]}».")
        };
    }

    private static CommandRequest ParsePlaylist(IReadOnlyList<string> args)
    {
        if (args.Count < 2)
            return CommandRequest.Fail("Укажите действие плейлиста: list, create, delete, rename, add, remove.");

        var action = args[1].ToLowerInvariant();
        return action switch
        {
            "list" => ExpectCount(args, 2, CommandKind.PlaylistList, "playlist list"),
            "create" => NameOnly(args, CommandKind.PlaylistCreate, "playlist create <название>"),
            "delete" => NameOnly(args, CommandKind.PlaylistDelete, "playlist delete <название>"),
            "rename" => Rename(args),
            "add" => Add(args),
            "remove" => Remove(args),
            _ => CommandRequest.Fail($"Неизвестное действие плейлиста «{args[1]}».")
        };
    }

    private static CommandRequest ParseMode(IReadOnlyList<string> args)
    {
        if (args.Count != 2)
            return CommandRequest.Fail("Формат: mode <sequential|repeat-one|repeat-all|shuffle>.");

        if (!PlaybackModeNames.TryParse(args[1], out var mode))
            return CommandRequest.Fail($"Неизвестный режим «{args[1]}».");

        return new CommandRequest { Kind = CommandKind.Mode, Mode = mode };
    }

    private static CommandRequest ParseState(IReadOnlyList<string> args)
    {
        if (args.Count != 3)
            return CommandRequest.Fail("Формат: state save <файл.json> или state load <файл.json>.");

        var path = args[2];
        return args[1].ToLowerInvariant() switch
        {
            "save" => new CommandRequest { Kind = CommandKind.StateSave, Path = path },
            "load" => new CommandRequest { Kind = CommandKind.StateLoad, Path = path },
            _ => CommandRequest.Fail("Для state доступны только save и load.")
        };
    }

    private static CommandRequest NameOnly(IReadOnlyList<string> args, CommandKind kind, string usage)
    {
        if (args.Count != 3 || string.IsNullOrWhiteSpace(args[2]))
            return CommandRequest.Fail($"Формат: {usage}.");

        return new CommandRequest { Kind = kind, PlaylistName = args[2] };
    }

    private static CommandRequest Rename(IReadOnlyList<string> args)
    {
        if (args.Count != 4 || string.IsNullOrWhiteSpace(args[2]) || string.IsNullOrWhiteSpace(args[3]))
            return CommandRequest.Fail("Формат: playlist rename <название> <новое-название>.");

        return new CommandRequest
        {
            Kind = CommandKind.PlaylistRename,
            PlaylistName = args[2],
            NewName = args[3]
        };
    }

    private static CommandRequest Add(IReadOnlyList<string> args)
    {
        if (args.Count != 4 || string.IsNullOrWhiteSpace(args[2]) || string.IsNullOrWhiteSpace(args[3]))
            return CommandRequest.Fail("Формат: playlist add <название> <путь-к-файлу>.");

        return new CommandRequest
        {
            Kind = CommandKind.PlaylistAdd,
            PlaylistName = args[2],
            Path = args[3]
        };
    }

    private static CommandRequest Remove(IReadOnlyList<string> args)
    {
        if (args.Count != 4 || string.IsNullOrWhiteSpace(args[2]))
            return CommandRequest.Fail("Формат: playlist remove <название> <номер-с-единицы>.");

        if (!int.TryParse(args[3], out var number) || number < 1)
            return CommandRequest.Fail("Номер трека должен быть целым числом от 1.");

        return new CommandRequest
        {
            Kind = CommandKind.PlaylistRemove,
            PlaylistName = args[2],
            TrackNumber = number
        };
    }

    private static CommandRequest ExpectExact(IReadOnlyList<string> args, CommandKind kind)
    {
        if (args.Count != 1)
            return CommandRequest.Fail($"Команда {args[0]} не принимает параметры.");

        return CommandRequest.Simple(kind);
    }

    private static CommandRequest ExpectCount(IReadOnlyList<string> args, int count, CommandKind kind, string usage)
    {
        if (args.Count != count)
            return CommandRequest.Fail($"Формат: {usage}.");

        return CommandRequest.Simple(kind);
    }

    private static bool IsHelp(string token)
    {
        return token.Equals("--help", StringComparison.OrdinalIgnoreCase)
            || token.Equals("-h", StringComparison.OrdinalIgnoreCase)
            || token.Equals("/?", StringComparison.OrdinalIgnoreCase)
            || token.Equals("help", StringComparison.OrdinalIgnoreCase);
    }
}
