﻿using Avalonia.MusicStore.Models;
using ReactiveUI;
using System.Linq;
using System.Reactive.Concurrency;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows.Input;

namespace Avalonia.MusicStore.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public ICommand BuyMusicCommand { get; }
    public Interaction<MusicStoreViewModel, AlbumViewModel?> ShowDialog { get; }
    public ObservableCollection<AlbumViewModel> Albums { get; } = new();
    
    public MainWindowViewModel()
    {
        ShowDialog = new Interaction<MusicStoreViewModel, AlbumViewModel?>();
        
        BuyMusicCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var store = new MusicStoreViewModel();
            var result = await ShowDialog.Handle(store);
            if (result is not null)
            {
                Albums.Add(result);
                await result.SaveToDiskAsync();
            }
        });
        
        RxApp.MainThreadScheduler.Schedule(LoadAlbums);
    }
    
    private async void LoadAlbums()
    {
        var albums = (await Album.LoadCachedAsync()).Select(x => new AlbumViewModel(x));

        foreach (var album in albums)
        {
            Albums.Add(album);
        }

        foreach (var album in Albums.ToList())
        {
            await album.LoadCover();
        }
    }
}