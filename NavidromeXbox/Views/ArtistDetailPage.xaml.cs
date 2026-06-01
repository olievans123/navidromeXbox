using System;
using System.Collections.Generic;
using NavidromeXbox.Helpers;
using NavidromeXbox.Models;
using NavidromeXbox.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace NavidromeXbox.Views
{
    public sealed partial class ArtistDetailPage : Page
    {
        Artist _artist;
        readonly List<Song> _topSongs = new List<Song>();

        public ArtistDetailPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            string id = e.Parameter as string;
            if (string.IsNullOrEmpty(id)) return;
            Busy.IsActive = true;
            var api = AppState.Current.Api;
            try
            {
                var (artist, albums) = await api.GetArtistAsync(id);
                _artist = artist;
                NameText.Text = artist?.Name ?? "";
                CountText.Text = albums.Count == 1 ? "1 album" : $"{albums.Count} albums";
                if (artist?.CoverArtUri != null) ArtistImage.Source = new BitmapImage(artist.CoverArtUri);
                AlbumsGrid.ItemsSource = albums;
                PlayButton.FocusOnLoad();

                // Secondary detail — bio + top songs — best-effort.
                try
                {
                    var info = await api.GetArtistInfoAsync(id);
                    if (!string.IsNullOrWhiteSpace(info.Biography))
                    {
                        BioText.Text = System.Text.RegularExpressions.Regex.Replace(info.Biography, "<.*?>", "").Trim();
                        BioText.Visibility = Visibility.Visible;
                    }
                    if (info.ImageUri != null && artist?.CoverArt == null)
                        ArtistImage.Source = new BitmapImage(info.ImageUri);
                }
                catch { }

                try
                {
                    var top = await api.GetTopSongsAsync(artist?.Name ?? "", 10);
                    _topSongs.Clear();
                    _topSongs.AddRange(top);
                    TopSongs.ItemsSource = _topSongs;
                    TopSongsSection.Visibility = top.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                }
                catch { TopSongsSection.Visibility = Visibility.Collapsed; }
            }
            catch (Exception ex)
            {
                CountText.Text = "Couldn't load artist: " + ex.Message;
            }
            finally { Busy.IsActive = false; }
        }

        void Album_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Album a) MainPage.Instance?.OpenAlbum(a.Id);
        }

        void Song_Click(object sender, ItemClickEventArgs e)
        {
            int idx = _topSongs.IndexOf(e.ClickedItem as Song);
            if (idx < 0) idx = 0;
            if (_topSongs.Count > 0) AppState.Current.Playback.PlayQueue(_topSongs, idx);
        }

        void Play_Click(object sender, RoutedEventArgs e)
        {
            if (_topSongs.Count > 0) AppState.Current.Playback.PlayQueue(_topSongs, 0);
        }

        void Shuffle_Click(object sender, RoutedEventArgs e)
        {
            if (_topSongs.Count > 0)
            {
                AppState.Current.Playback.PlayQueue(_topSongs, 0);
                if (!AppState.Current.Playback.Shuffle) AppState.Current.Playback.ToggleShuffle();
            }
        }
    }
}
