using System;
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

        public HomePage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (_loaded) return;
            await LoadAsync();
        }

        async Task LoadAsync()
        {
            Busy.IsActive = true;
            ErrorText.Visibility = Visibility.Collapsed;
            var api = AppState.Current.Api;

            var user = await AppState.Current.EnsureUserAsync();
            GreetingText.Text = user != null ? $"Welcome, {user.Username}" : "Home";
            SubText.Text = user != null ? $"Connected to {user.ServerName}" : "";

            try
            {
                NewestShelf.ItemsSource = await api.GetAlbumList2Async("newest", 24);
                RecentShelf.ItemsSource = await api.GetAlbumList2Async("recent", 24);
                FrequentShelf.ItemsSource = await api.GetAlbumList2Async("frequent", 24);
                RandomShelf.ItemsSource = await api.GetAlbumList2Async("random", 24);

                var starred = await api.GetStarred2Async();
                if (starred.Albums.Count > 0) StarredShelf.ItemsSource = starred.Albums;
                else StarredSection.Visibility = Visibility.Collapsed;

                _loaded = true;
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

        void Album_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Album a) MainPage.Instance?.OpenAlbum(a.Id);
        }
    }
}
