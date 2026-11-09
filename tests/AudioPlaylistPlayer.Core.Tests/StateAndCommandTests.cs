using AudioPlaylistPlayer.Core.Commands;
using AudioPlaylistPlayer.Core.Models;
using AudioPlaylistPlayer.Core.Persistence;

namespace AudioPlaylistPlayer.Core.Tests;

public class StateAndCommandTests
{
    [Fact]
    public async Task SaveAndLoad_RoundtripPlaylistsPositionAndSettings()
    {
        var directory = Path.Combine(Path.GetTempPath(), "audio-player-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var working = Path.Combine(directory, "player-state.json");
        var exported = Path.Combine(directory, "session.json");

        var first = new PlayerApplication(working);
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        Assert.Equal(0, await first.ExecuteAsync(["playlist", "create", "Дорога"], stdout, stderr));
        Assert.Equal(0, await first.ExecuteAsync(["playlist", "add", "Дорога", @"D:\music\a.mp3"], stdout, stderr));
        Assert.Equal(0, await first.ExecuteAsync(["playlist", "add", "Дорога", @"D:\music\b.mp3"], stdout, stderr));
        Assert.Equal(0, await first.ExecuteAsync(["mode", "shuffle"], stdout, stderr));
        first.State.Settings.Theme = AppThemePreference.Dark;
        first.Player.SelectPlaylist(first.State.Library.RequireByName("Дорога"), 1);
        await first.Player.SeekAsync(14.2);
        first.Player.Pause();
        Assert.Equal(0, await first.ExecuteAsync(["state", "save", exported], stdout, stderr));

        var loaded = new JsonStateStore().Load(exported);
        Assert.Equal(AppThemePreference.Dark, loaded.Settings.Theme);
        Assert.Equal(PlaybackMode.Shuffle, loaded.Session.Mode);
        Assert.Equal(1, loaded.Session.TrackIndex);
        Assert.Equal(14.2, loaded.Session.PositionSeconds, 3);
        var playlist = Assert.Single(loaded.Library.Playlists);
        Assert.Equal("Дорога", playlist.Name);
        Assert.Equal(["a", "b"], playlist.Tracks.Select(track => track.Title));
        Assert.All(playlist.Tracks, track => Assert.IsType<FileTrackSource>(track.Source));
        Assert.DoesNotContain("ID3", await File.ReadAllTextAsync(exported));
        Assert.DoesNotContain("AAAA", await File.ReadAllTextAsync(exported));

        var second = new PlayerApplication(Path.Combine(directory, "other.json"));
        Assert.Equal(0, await second.ExecuteAsync(["state", "load", exported], new StringWriter(), new StringWriter()));
        Assert.Equal("Дорога", Assert.Single(second.State.Library.Playlists).Name);
        Assert.Equal(14.2, second.Player.PositionSeconds, 3);
        Assert.Equal(PlaybackMode.Shuffle, second.Player.Mode);
    }

    [Fact]
    public async Task RemoveTrack_UsesOneBasedNumber()
    {
        var directory = Path.Combine(Path.GetTempPath(), "audio-player-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var app = new PlayerApplication(Path.Combine(directory, "player-state.json"));
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        await app.ExecuteAsync(["playlist", "create", "Дорога"], stdout, stderr);
        await app.ExecuteAsync(["playlist", "add", "Дорога", @"D:\music\a.mp3"], stdout, stderr);
        await app.ExecuteAsync(["playlist", "add", "Дорога", @"D:\music\b.mp3"], stdout, stderr);
        var code = await app.ExecuteAsync(["playlist", "remove", "Дорога", "1"], stdout, stderr);
        Assert.Equal(0, code);
        Assert.Equal(["b"], app.State.Library.RequireByName("Дорога").Tracks.Select(track => track.Title));
    }

    [Theory]
    [InlineData(new string[] { }, CommandKind.Help)]
    [InlineData(new[] { "--help" }, CommandKind.Help)]
    [InlineData(new[] { "playlist", "list" }, CommandKind.PlaylistList)]
    [InlineData(new[] { "mode", "repeat-one" }, CommandKind.Mode)]
    [InlineData(new[] { "nope" }, CommandKind.Error)]
    public void Parser_RecognizesCommands(string[] args, CommandKind expected)
    {
        var command = CommandParser.Parse(args);
        Assert.Equal(expected, command.Kind);
        if (expected == CommandKind.Mode)
            Assert.Equal(PlaybackMode.RepeatOne, command.Mode);
    }
}
