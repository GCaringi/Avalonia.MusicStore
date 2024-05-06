using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using Avalonia.MusicStore.Models;
using ReactiveUI;

namespace Avalonia.MusicStore.ViewModels;

public class MusicStoreViewModel : ViewModelBase
{
    private string? _searchText;
    private bool _isBusy;
    public ReactiveCommand<Unit, AlbumViewModel?> BuyMusicCommand { get; }

    public string? SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    private AlbumViewModel? _selectedAlbum;

    public ObservableCollection<AlbumViewModel> SearchResults { get; } = new();

    public AlbumViewModel? SelectedAlbum
    {
        get => _selectedAlbum;
        set => this.RaiseAndSetIfChanged(ref _selectedAlbum, value);
    }

    public MusicStoreViewModel()
    {
        this.WhenAnyValue(x => x.SearchText).Throttle(TimeSpan.FromMilliseconds(400)).ObserveOn(RxApp.TaskpoolScheduler)
            .Subscribe(DoSearch!);

        BuyMusicCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            return SelectedAlbum;
        });
    }

    private async void DoSearch(string? search)
    {
        IsBusy = true;
        SearchResults.Clear();

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var albums = await Album.SearchAsync(search);

            foreach (var album in albums)
            {
                var vm = new AlbumViewModel(album);
                SearchResults.Add(vm);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                LoadCover(cancellationToken);
            }
        }

        IsBusy = false;
    }

    private async void LoadCover(CancellationToken cancellationToken)
    {
        foreach (var album in SearchResults.ToList())
        {
            await album.LoadCover();

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private CancellationTokenSource? _cancellationTokenSource;
}