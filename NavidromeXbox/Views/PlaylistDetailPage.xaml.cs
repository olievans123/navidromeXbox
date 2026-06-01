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
    public sealed partial class PlaylistDetailPage : Page
    {
        Playlist _playlist;

        public PlaylistDetailPage()
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
                _playlist = await AppState.Current.Api.GetPlaylistAsync(id);
                if (_playlist != null) Render(_playlist);
            }
            catch (Exception ex)
            {
                MetaText.Text = "Couldn't load playlist: " + ex.Message;
            }
            finally { Busy.IsActive = false; }
        }

        void Render(Playlist p)
        {
            TitleText.Text = p.Name;
            MetaText.Text = p.Meta;
            if (!string.IsNullOrWhiteSpace(p.Comment))
            {
                CommentText.Text = p.Comment;
                CommentText.Visibility = Visibility.Visible;
            }
            if (p.CoverArtLargeUri != null) CoverImage.Source = new BitmapImage(p.CoverArtLargeUri);
            SongList.ItemsSource = p.Songs;
            PlayButton.FocusOnLoad();
        }

        void Song_Click(object sender, ItemClickEventArgs e)
        {
            if (_playlist == null) return;
            int idx = _playlist.Songs.IndexOf(e.ClickedItem as Song);
            if (idx < 0) idx = 0;
            AppState.Current.Playback.PlayQueue(_playlist.Songs, idx);
        }

        void Play_Click(object sender, RoutedEventArgs e)
        {
            if (_playlist?.Songs.Count > 0) AppState.Current.Playback.PlayQueue(_playlist.Songs, 0);
        }

        void Shuffle_Click(object sender, RoutedEventArgs e)
        {
            if (_playlist?.Songs.Count > 0)
            {
                AppState.Current.Playback.PlayQueue(_playlist.Songs, 0);
                if (!AppState.Current.Playback.Shuffle) AppState.Current.Playback.ToggleShuffle();
            }
        }

        void Queue_Click(object sender, RoutedEventArgs e)
        {
            if (_playlist?.Songs.Count > 0) AppState.Current.Playback.AddToQueue(_playlist.Songs);
        }
    }
}
