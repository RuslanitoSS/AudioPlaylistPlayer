using AudioPlaylistPlayer.Core.Models;

namespace AudioPlaylistPlayer.Core.Playback;

public static class PlaybackCursor
{
    public static int NextIndex(
        PlaybackMode mode,
        int current,
        int count,
        IReadOnlyList<int> shuffleOrder,
        bool automatic)
    {
        if (count <= 0)
            return -1;

        if (current < 0)
            return mode == PlaybackMode.Shuffle && shuffleOrder.Count > 0 ? shuffleOrder[0] : 0;

        if (automatic && mode == PlaybackMode.RepeatOne)
            return current;

        if (mode == PlaybackMode.Shuffle && shuffleOrder.Count == count)
        {
            var position = IndexOf(shuffleOrder, current);
            if (position < 0)
                return shuffleOrder[0];

            var next = position + 1;
            return next >= count ? shuffleOrder[0] : shuffleOrder[next];
        }

        var sequential = current + 1;
        if (sequential < count)
            return sequential;

        return mode == PlaybackMode.RepeatAll ? 0 : -1;
    }

    public static int PreviousIndex(
        PlaybackMode mode,
        int current,
        int count,
        IReadOnlyList<int> shuffleOrder)
    {
        if (count <= 0)
            return -1;

        if (current < 0)
            return 0;

        if (mode == PlaybackMode.Shuffle && shuffleOrder.Count == count)
        {
            var position = IndexOf(shuffleOrder, current);
            if (position < 0)
                return shuffleOrder[0];

            return position == 0 ? shuffleOrder[count - 1] : shuffleOrder[position - 1];
        }

        if (current == 0)
            return mode == PlaybackMode.RepeatAll ? count - 1 : 0;

        return current - 1;
    }

    private static int IndexOf(IReadOnlyList<int> values, int wanted)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (values[i] == wanted)
                return i;
        }

        return -1;
    }
}
