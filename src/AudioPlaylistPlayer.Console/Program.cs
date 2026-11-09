using AudioPlaylistPlayer.Core.Commands;

var workingFile = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "player-state.json"));
var application = new PlayerApplication(workingFile);
return await application.ExecuteAsync(args, Console.Out, Console.Error);
