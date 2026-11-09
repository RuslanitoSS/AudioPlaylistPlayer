namespace AudioPlaylistPlayer.Core.Models;

/// <summary>
/// Файл на устройстве: путь Windows или content URI Android.
/// </summary>
public sealed class FileTrackSource : TrackSource
{
    public const string KindName = "file";

    public FileTrackSource(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Расположение файла не задано.", nameof(location));

        Location = location.Trim();
    }

    public override string Kind => KindName;

    public override string Location { get; }
}
