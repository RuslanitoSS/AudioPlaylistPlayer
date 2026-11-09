using AudioPlaylistPlayer.Core.Models;
using AudioPlaylistPlayer.Core.Playback;

namespace AudioPlaylistPlayer.Core.Tests;

public class PlaybackCursorTests
{
    [Theory]
    [InlineData(PlaybackMode.Sequential, 0, false, 1)]
    [InlineData(PlaybackMode.Sequential, 2, true, -1)]
    [InlineData(PlaybackMode.RepeatAll, 2, true, 0)]
    [InlineData(PlaybackMode.RepeatOne, 1, true, 1)]
    [InlineData(PlaybackMode.RepeatOne, 1, false, 2)]
    public void NextIndex_FollowsMode(PlaybackMode mode, int current, bool automatic, int expected)
    {
        var next = PlaybackCursor.NextIndex(mode, current, 3, [], automatic);
        Assert.Equal(expected, next);
    }

    [Fact]
    public void Shuffle_WrapsAlongGivenOrder()
    {
        IReadOnlyList<int> order = [2, 0, 1];
        Assert.Equal(0, PlaybackCursor.NextIndex(PlaybackMode.Shuffle, 2, 3, order, automatic: false));
        Assert.Equal(1, PlaybackCursor.NextIndex(PlaybackMode.Shuffle, 0, 3, order, automatic: true));
        Assert.Equal(2, PlaybackCursor.NextIndex(PlaybackMode.Shuffle, 1, 3, order, automatic: true));
        Assert.Equal(1, PlaybackCursor.PreviousIndex(PlaybackMode.Shuffle, 2, 3, order));
    }
}
