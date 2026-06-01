using System;
using System.Collections.Generic;
using NavidromeXbox.Helpers;
using NavidromeXbox.Models;
using NavidromeXbox.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace NavidromeXbox.Views
{
    public sealed partial class SearchPage : Page
    {
        readonly DispatcherTimer _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        readonly List<Song> _songs = new List<Song>();
        string _pending;

        public SearchPage()
        {
            this.InitializeComponent();
            _debounce.Tick += async (s, e) => { _debounce.Stop(); await RunSearchAsync(_pending); };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) => SearchBox.FocusOnLoad();

        void Search_Changed(object sender, TextChangedEventArgs e)
        {
            _pending = SearchBox.Text?.Trim();
            _debounce.Stop();
            if (string.IsNullOrEmpty(_pending)) { ClearResults(); return; }
            _debounce.Start();
        }

        async System.Threading.Tasks.Task RunSearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) { ClearResults(); return; }
            Busy.IsActive = true;
            EmptyText.Visibility = Visibility.Collapsed;
            try
            {
                var res = await AppState.Current.Api.Search3Async(query);

                ArtistResults.ItemsSource = res.Artists;
                ArtistsSection.Visibility = res.Artists.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                AlbumResults.ItemsSource = res.Albums;
                AlbumsSection.Visibility = res.Albums.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                _songs.Clear();
                _songs.AddRange(res.Songs);
                SongResults.ItemsSource = null;
                SongResults.ItemsSource = _songs;
                SongsSection.Visibility = res.Songs.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                if (res.IsEmpty)
                {
                    EmptyText.Text = $"No results for “{query}”.";
                    EmptyText.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                EmptyText.Text = "Search failed: " + ex.Message;
                EmptyText.Visibility = Visibility.Visible;
            }
            finally { Busy.IsActive = false; }
        }

        void ClearResults()
        {
            ArtistsSection.Visibility = Visibility.Collapsed;
            AlbumsSection.Visibility = Visibility.Collapsed;
            SongsSection.Visibility = Visibility.Collapsed;
            EmptyText.Visibility = Visibility.Collapsed;
        }

        void Artist_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Artist a) MainPage.Instance?.OpenArtist(a.Id);
        }

        void Album_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Album a) MainPage.Instance?.OpenAlbum(a.Id);
        }

        void Song_Click(object sender, ItemClickEventArgs e)
        {
            int idx = _songs.IndexOf(e.ClickedItem as Song);
            if (idx < 0) idx = 0;
            if (_songs.Count > 0) AppState.Current.Playback.PlayQueue(_songs, idx);
        }
    }
}
