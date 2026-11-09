using AudioPlaylistPlayer.Core.Models;
using AudioPlaylistPlayer.Core.Playback;

namespace AudioPlaylistPlayer.Core.Tests;

public class PlayerControllerTests
{
    [Fact]
    public async Task NaturalEnd_RepeatsOneAndStopsAfterSequential()
    {
        var (player, output) = Create("a", "b");
        player.SelectPlaylist(player.Library.Playlists[0]);
        player.SetMode(PlaybackMode.RepeatOne);
        await player.PlayAsync();
        output.RaiseCompleted();
        Assert.Equal(0, player.CurrentIndex);
        Assert.Equal(PlaybackStatus.Playing, player.Status);

        player.SetMode(PlaybackMode.Sequential);
        player.SelectPlaylist(player.Library.Playlists[0], 1);
        await player.PlayAsync();
        output.RaiseCompleted();
        Assert.Equal(PlaybackStatus.Stopped, player.Status);
        Assert.Equal(1, player.CurrentIndex);
    }

    [Fact]
    public async Task Previous_RestartsTrackWhenPositionIsPastThreeSeconds()
    {
        var (player, _) = Create("a", "b");
        player.SelectPlaylist(player.Library.Playlists[0], 1);
        await player.PlayAsync();
        await player.SeekAsync(10);
        await player.PreviousAsync();
        Assert.Equal(1, player.CurrentIndex);
        Assert.Equal(0, player.PositionSeconds);
    }

    [Fact]
    public void ShuffleOrder_IsPermutationStartingAtCurrentTrack()
    {
        var (player, _) = Create("a", "b", "c", "d");
        player.SelectPlaylist(player.Library.Playlists[0], 2);
        player.SetMode(PlaybackMode.Shuffle);
        Assert.Equal(4, player.ShuffleOrder.Count);
        Assert.Equal(2, player.ShuffleOrder[0]);
        Assert.Equal(4, player.ShuffleOrder.Distinct().Count());
    }

    [Fact]
    public async Task MovingCurrentTrack_FollowsIt()
    {
        var (player, _) = Create("a", "b", "c");
        var playlist = player.Library.Playlists[0];
        player.SelectPlaylist(playlist, 0);
        await player.PlayAsync();
        playlist.Move(0, 2);
        Assert.Equal(2, player.CurrentIndex);
        Assert.Equal("a", player.CurrentTrack!.Title);
    }

    [Fact]
    public void ApplySession_RemembersPlayingWithoutStartingOutput()
    {
        var (player, output) = Create("a", "b");
        var playlist = player.Library.Playlists[0];
        player.SelectPlaylist(playlist, 1);
        player.SetMode(PlaybackMode.RepeatAll);
        player.SetVolume(0.4);
        player.ApplySession(new SessionSnapshot(
            playlist.Id,
            1,
            12.5,
            PlaybackStatus.Playing,
            PlaybackMode.RepeatAll,
            [],
            0.4));

        Assert.False(output.IsPlaying);
        Assert.Equal(12.5, player.PositionSeconds);
        Assert.Equal(PlaybackStatus.Playing, player.CaptureSession().Status);
        Assert.True(player.ShouldResumePlayback);
    }

    private static (PlayerController Player, SilentAudioOutput Output) Create(params string[] titles)
    {
        var library = new PlaylistLibrary();
        var playlist = library.Create("Дорога");
        foreach (var title in titles)
            playlist.Add(new Track(title, new FileTrackSource($"C:\\music\\{title}.mp3")));

        var output = new SilentAudioOutput();
        var player = new PlayerController(library, new AppSettings(), output);
        return (player, output);
    }
}
