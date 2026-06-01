using System;
using NavidromeXbox.Helpers;
using NavidromeXbox.Models;
using NavidromeXbox.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace NavidromeXbox.Views
{
    public sealed partial class AlbumDetailPage : Page
    {
        Album _album;

        public AlbumDetailPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            string id = e.Parameter as string;
            if (string.IsNullOrEmpty(id)) return;
            Busy.IsActive = true;
            try
            {
                _album = await AppState.Current.Api.GetAlbumAsync(id);
                if (_album != null) Render(_album);
            }
            catch (Exception ex)
            {
                MetaText.Text = "Couldn't load album: " + ex.Message;
            }
            finally { Busy.IsActive = false; }
        }

        void Render(Album a)
        {
            TitleText.Text = a.Name;
            ArtistText.Text = a.ArtistName;
            ArtistButton.IsEnabled = !string.IsNullOrEmpty(a.ArtistId);
            MetaText.Text = a.Meta;
            if (a.CoverArtLargeUri != null) CoverImage.Source = new BitmapImage(a.CoverArtLargeUri);
            SongList.ItemsSource = a.Songs;
            UpdateStarGlyph();
            PlayButton.FocusOnLoad();
        }

        void UpdateStarGlyph() => StarButton.Content = _album != null && _album.Starred ? "\uEB52" : "\uEB51";

        void Song_Click(object sender, ItemClickEventArgs e)
        {
            if (_album == null) return;
            int idx = _album.Songs.IndexOf(e.ClickedItem as Song);
            if (idx < 0) idx = 0;
            AppState.Current.Playback.PlayQueue(_album.Songs, idx);
        }

        void Play_Click(object sender, RoutedEventArgs e)
        {
            if (_album?.Songs.Count > 0) AppState.Current.Playback.PlayQueue(_album.Songs, 0);
        }

        void Shuffle_Click(object sender, RoutedEventArgs e)
        {
            if (_album?.Songs.Count > 0)
            {
                AppState.Current.Playback.PlayQueue(_album.Songs, 0);
                if (!AppState.Current.Playback.Shuffle) AppState.Current.Playback.ToggleShuffle();
            }
        }

        void Queue_Click(object sender, RoutedEventArgs e)
        {
            if (_album?.Songs.Count > 0) AppState.Current.Playback.AddToQueue(_album.Songs);
        }

        async void Star_Click(object sender, RoutedEventArgs e)
        {
            if (_album == null) return;
            try
            {
                await AppState.Current.Api.StarAsync(_album.Id, !_album.Starred, "albumId");
                _album.Starred = !_album.Starred;
                UpdateStarGlyph();
            }
            catch { }
        }

        void Artist_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_album?.ArtistId)) MainPage.Instance?.OpenArtist(_album.ArtistId);
        }
    }
}
