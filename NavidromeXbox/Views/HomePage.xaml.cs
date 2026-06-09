using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NavidromeXbox.Models;
using NavidromeXbox.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace NavidromeXbox.Views
{
    public sealed partial class HomePage : Page
    {
        bool _loaded;
        string _loadedSig;

        public HomePage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // Reload when the user has changed which shelves to show; otherwise keep the cache.
            if (_loaded && ShelfSignature() == _loadedSig) return;
            await LoadAsync();
        }

        static string ShelfSignature() =>
            $"{Settings.HomeNewest}|{Settings.HomeRecent}|{Settings.HomeFrequent}|{Settings.HomeRandom}|{Settings.HomeStarred}";

        async Task LoadAsync()
        {
            Busy.IsActive = true;
            ErrorText.Visibility = Visibility.Collapsed;
            var api = AppState.Current.Api;

            var user = await AppState.Current.EnsureUserAsync();
            GreetingText.Text = user != null ? $"Welcome, {user.Username}" : "Home";
            SubText.Text = user != null ? $"Connected to {user.ServerName}" : "";

            // Collapse the disabled shelves up front; only fetch the ones we'll show.
            NewestSection.Visibility = Vis(Settings.HomeNewest);
            RecentSection.Visibility = Vis(Settings.HomeRecent);
            FrequentSection.Visibility = Vis(Settings.HomeFrequent);
            RandomSection.Visibility = Vis(Settings.HomeRandom);
            StarredSection.Visibility = Vis(Settings.HomeStarred);

            try
            {
                var newest = Settings.HomeNewest ? api.GetAlbumList2Async("newest", 24) : NoAlbums();
                var recent = Settings.HomeRecent ? api.GetAlbumList2Async("recent", 24) : NoAlbums();
                var frequent = Settings.HomeFrequent ? api.GetAlbumList2Async("frequent", 24) : NoAlbums();
                var random = Settings.HomeRandom ? api.GetAlbumList2Async("random", 24) : NoAlbums();
                var starredTask = Settings.HomeStarred ? api.GetStarred2Async() : Task.FromResult(new SearchResults());
                await Task.WhenAll(newest, recent, frequent, random, starredTask);

                if (Settings.HomeNewest) NewestShelf.ItemsSource = newest.Result;
                if (Settings.HomeRecent) RecentShelf.ItemsSource = recent.Result;
                if (Settings.HomeFrequent) FrequentShelf.ItemsSource = frequent.Result;
                if (Settings.HomeRandom) RandomShelf.ItemsSource = random.Result;

                if (Settings.HomeStarred)
                {
                    var albums = starredTask.Result.Albums;
                    if (albums.Count > 0) StarredShelf.ItemsSource = albums;
                    else StarredSection.Visibility = Visibility.Collapsed;
                }

                _loaded = true;
                _loadedSig = ShelfSignature();
            }
            catch (Exception ex)
            {
                ErrorText.Text = "Couldn't load your library.\n" + ex.Message;
                ErrorText.Visibility = Visibility.Visible;
            }
            finally
            {
                Busy.IsActive = false;
            }
        }

        static Visibility Vis(bool on) => on ? Visibility.Visible : Visibility.Collapsed;
        static Task<List<Album>> NoAlbums() => Task.FromResult(new List<Album>());

        void Album_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Album a) MainPage.Instance?.OpenAlbum(a.Id);
        }
    }
}
