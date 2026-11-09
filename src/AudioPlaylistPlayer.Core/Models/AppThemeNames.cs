namespace AudioPlaylistPlayer.Core.Models;

public static class AppThemeNames
{
    public static IReadOnlyList<AppThemePreference> All { get; } =
    [
        AppThemePreference.System,
        AppThemePreference.Light,
        AppThemePreference.Dark
    ];

    public static string Display(AppThemePreference theme) => theme switch
    {
        AppThemePreference.Light => "Светлая",
        AppThemePreference.Dark => "Тёмная",
        _ => "Как в системе"
    };
}
