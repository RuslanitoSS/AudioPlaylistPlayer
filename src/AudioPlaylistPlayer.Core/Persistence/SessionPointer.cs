namespace AudioPlaylistPlayer.Core.Persistence;

public static class SessionPointer
{
    public static void Remember(string pointerFile, string statePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pointerFile);
        ArgumentException.ThrowIfNullOrWhiteSpace(statePath);
        var directory = Path.GetDirectoryName(pointerFile);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(pointerFile, statePath);
    }

    public static string? Read(string pointerFile)
    {
        if (string.IsNullOrWhiteSpace(pointerFile) || !File.Exists(pointerFile))
            return null;

        var path = File.ReadAllText(pointerFile).Trim();
        return path.Length == 0 ? null : path;
    }
}
