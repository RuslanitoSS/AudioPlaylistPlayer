using AudioPlaylistPlayer.Core.Models;

namespace AudioPlaylistPlayer.Core.Tests;

public class PlaylistTests
{
    [Fact]
    public void AddRemoveAndMove_RaiseEventsAndKeepOrder()
    {
        var playlist = new Playlist("Дорога");
        var added = new List<int>();
        var removed = new List<int>();
        var moved = new List<(int From, int To)>();
        playlist.TrackAdded += (_, args) => added.Add(args.Index);
        playlist.TrackRemoved += (_, args) => removed.Add(args.Index);
        playlist.TrackMoved += (_, args) => moved.Add((args.OldIndex, args.NewIndex));

        playlist.Add(Track("a"));
        playlist.Add(Track("b"));
        playlist.Add(Track("c"));
        playlist.Move(2, 0);
        playlist.RemoveAt(1);

        Assert.Equal([0, 1, 2], added);
        Assert.Equal([(2, 0)], moved);
        Assert.Equal([1], removed);
        Assert.Equal(["c", "b"], playlist.Tracks.Select(track => track.Title));
    }

    [Fact]
    public void Library_RejectsDuplicateNames()
    {
        var library = new PlaylistLibrary();
        library.Create("Вечер");
        var error = Assert.Throws<InvalidOperationException>(() => library.Create("вечер"));
        Assert.Contains("уже есть", error.Message);
    }

    [Fact]
    public void Rename_RaisesEvent()
    {
        var playlist = new Playlist("Старое");
        var raised = false;
        playlist.Renamed += (_, _) => raised = true;
        playlist.Rename("Новое");
        Assert.True(raised);
        Assert.Equal("Новое", playlist.Name);
    }

    private static Track Track(string title) => new(title, new FileTrackSource($"C:\\music\\{title}.mp3"));
}
