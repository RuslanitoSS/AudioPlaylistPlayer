using AudioPlaylistPlayer.Core.Help;
using AudioPlaylistPlayer.Core.Models;
using AudioPlaylistPlayer.Core.Persistence;
using AudioPlaylistPlayer.Core.Playback;

namespace AudioPlaylistPlayer.Core.Commands;

public sealed class PlayerApplication
{
    private readonly JsonStateStore _store = new();

    public PlayerApplication(string workingFilePath, IAudioOutput? output = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workingFilePath);
        WorkingFilePath = workingFilePath;
        
        State = File.Exists(workingFilePath) 
            ? _store.Load(workingFilePath) 
            : new AppState();

        // ВАЖНО: передаем MusicLibrary, а не PlaylistLibrary
        Player = new PlayerController(State.MusicLibrary, State.Settings, output ?? new SilentAudioOutput());
        Player.ApplySession(State.Session);
    }

    public string WorkingFilePath { get; }
    public AppState State { get; }
    public PlayerController Player { get; }

    public async Task<int> ExecuteAsync(
        IReadOnlyList<string> args,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken = default)
    {
        var command = CommandParser.Parse(args);
        try
        {
            switch (command.Kind)
            {
                case CommandKind.Help:
                    stdout.WriteLine(HelpText.Text);
                    return 0;
                case CommandKind.Error:
                    stderr.WriteLine(command.Error);
                    stdout.WriteLine(HelpText.Text);
                    return 1;
                case CommandKind.StateLoad:
                    Replace(_store.Load(command.Path!));
                    SaveWorking();
                    stdout.WriteLine($"Состояние загружено из «{command.Path}» и записано в «{WorkingFilePath}».");
                    stdout.WriteLine(Describe());
                    return 0;
                case CommandKind.StateSave:
                    _store.Save(command.Path!, State, Player);
                    SaveWorking();
                    stdout.WriteLine($"Состояние сохранено в «{command.Path}».");
                    return 0;
                default:
                    await ApplyAsync(command, stdout, cancellationToken);
                    SaveWorking();
                    return 0;
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or IOException or InvalidDataException or ArgumentOutOfRangeException)
        {
            stderr.WriteLine(ex.Message);
            return 1;
        }
    }

    private async Task ApplyAsync(CommandRequest command, TextWriter stdout, CancellationToken cancellationToken)
    {
        switch (command.Kind)
        {
            case CommandKind.PlaylistList:
                stdout.WriteLine(Describe());
                break;
            case CommandKind.PlaylistCreate:
                State.PlaylistLibrary.Create(command.PlaylistName!);
                stdout.WriteLine($"Плейлист «{command.PlaylistName}» создан.");
                break;
            case CommandKind.PlaylistDelete:
                var deleted = State.PlaylistLibrary.RequireByName(command.PlaylistName!);
                State.PlaylistLibrary.Remove(deleted.Id);
                stdout.WriteLine($"Плейлист «{deleted.Name}» удалён.");
                break;
            case CommandKind.PlaylistRename:
                var renamed = State.PlaylistLibrary.RequireByName(command.PlaylistName!);
                var clash = State.PlaylistLibrary.FindByName(command.NewName!);
                if (clash is not null && clash.Id != renamed.Id)
                    throw new InvalidOperationException($"Плейлист «{clash.Name}» уже есть.");
                renamed.Rename(command.NewName!);
                stdout.WriteLine($"Плейлист переименован в «{renamed.Name}».");
                break;
            case CommandKind.PlaylistAdd:
                var target = State.PlaylistLibrary.RequireByName(command.PlaylistName!);
                var title = Path.GetFileNameWithoutExtension(command.Path!);
                if (string.IsNullOrWhiteSpace(title)) title = command.Path!;
                
                var newTrack = new Track(title, new FileTrackSource(command.Path!));
                
                // ШАГ 1: Добавляем трек в глобальную библиотеку
                State.MusicLibrary.AddTrack(newTrack);
                // ШАГ 2: Добавляем ТОЛЬКО ID в плейлист
                target.AddTrackId(newTrack.Id);
                
                stdout.WriteLine($"Трек «{title}» добавлен в «{target.Name}».");
                break;
            case CommandKind.PlaylistRemove:
                var playlist = State.PlaylistLibrary.RequireByName(command.PlaylistName!);
                var index = command.TrackNumber!.Value - 1;
                if (index >= playlist.TrackCount)
                    throw new ArgumentOutOfRangeException(nameof(command), "В плейлисте нет трека с таким номером.");
                
                var removedId = playlist.TrackIds[index];
                playlist.RemoveAt(index);
                
                var removedTrack = State.MusicLibrary.GetTrack(removedId);
                stdout.WriteLine($"Трек «{removedTrack?.Title ?? "Unknown"}» удалён из «{playlist.Name}».");
                break;
            case CommandKind.Mode:
                Player.SetMode(command.Mode!.Value);
                stdout.WriteLine($"Режим: {PlaybackModeNames.Display(Player.Mode)}.");
                break;
            case CommandKind.Play:
                await Player.PlayAsync(cancellationToken);
                stdout.WriteLine(Player.CurrentTrack is null
                    ? "Нет треков для воспроизведения."
                    : $"Играет «{Player.CurrentTrack.Title}».");
                break;
            case CommandKind.Pause:
                Player.Pause();
                stdout.WriteLine(Player.Status == PlaybackStatus.Paused ? "Пауза." : "Сейчас ничего не играет.");
                break;
            case CommandKind.Next:
                await Player.NextAsync(automatic: false, cancellationToken);
                stdout.WriteLine(DescribePosition());
                break;
            case CommandKind.Previous:
                await Player.PreviousAsync(cancellationToken);
                stdout.WriteLine(DescribePosition());
                break;
            default:
                throw new InvalidOperationException("Команда не поддерживается.");
        }
    }

    private void Replace(AppState loaded)
    {
        State.MusicLibrary = loaded.MusicLibrary;
        State.PlaylistLibrary = loaded.PlaylistLibrary;
        State.Settings.CopyFrom(loaded.Settings);
        Player.ApplySession(loaded.Session);
    }

    private void SaveWorking()
    {
        State.Session = Player.CaptureSession();
        _store.Save(WorkingFilePath, State, Player);
    }

    public string Describe()
    {
        var lines = new List<string>();
        if (State.PlaylistLibrary.Playlists.Count == 0)
        {
            lines.Add("Плейлистов нет.");
        }
        else
        {
            lines.Add("Плейлисты:");
            foreach (var playlist in State.PlaylistLibrary.Playlists)
            {
                var marker = Player.CurrentPlaylist?.Id == playlist.Id ? "*" : "-";
                // Получаем реальные треки через библиотеку для отображения
                var tracks = playlist.GetTracks(State.MusicLibrary);
                lines.Add($"{marker} {playlist.Name} ({tracks.Count})");
                for (var i = 0; i < tracks.Count; i++)
                    lines.Add($"  {i + 1}. {tracks[i].Title}");
            }
        }
        lines.Add(DescribePosition());
        lines.Add($"Тема: {AppThemeNames.Display(State.Settings.Theme)}.");
        return string.Join(Environment.NewLine, lines);
    }

    private string DescribePosition()
    {
        var track = Player.CurrentTrack?.Title ?? "нет";
        var playlist = Player.CurrentPlaylist?.Name ?? "нет";
        return $"Сейчас: {playlist} / {track}, {PlaybackStatusNames.Display(Player.Status)}, режим {PlaybackModeNames.Display(Player.Mode)}, позиция {Player.PositionSeconds:0.#} с.";
    }
}