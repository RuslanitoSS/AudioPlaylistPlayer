using AudioPlaylistPlayer.Core.Models;

namespace AudioPlaylistPlayer.Core.Models;

public static class PlaybackModeNames
{
    public static IReadOnlyList<PlaybackMode> All { get; } =
    [
        PlaybackMode.Sequential,
        PlaybackMode.RepeatOne,
        PlaybackMode.RepeatAll,
        PlaybackMode.Shuffle
    ];

    public static string Display(PlaybackMode mode) => mode switch
    {
        PlaybackMode.RepeatOne => "Повтор одного",
        PlaybackMode.RepeatAll => "Повтор всех",
        PlaybackMode.Shuffle => "Перемешивание",
        _ => "По порядку"
    };

    public static bool TryParse(string text, out PlaybackMode mode)
    {
        switch (text.Trim().ToLowerInvariant())
        {
            case "sequential":
            case "порядок":
            case "по-порядку":
                mode = PlaybackMode.Sequential;
                return true;
            case "repeat-one":
            case "repeatone":
            case "повтор-одного":
                mode = PlaybackMode.RepeatOne;
                return true;
            case "repeat-all":
            case "repeatall":
            case "повтор-всех":
                mode = PlaybackMode.RepeatAll;
                return true;
            case "shuffle":
            case "перемешать":
            case "перемешивание":
                mode = PlaybackMode.Shuffle;
                return true;
            default:
                mode = default;
                return false;
        }
    }
}
