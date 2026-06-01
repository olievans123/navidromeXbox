using System;
using NavidromeXbox.Models;
using NavidromeXbox.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace NavidromeXbox.Views
{
    public sealed partial class GenresPage : Page
    {
        bool _loaded;

        public GenresPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (_loaded) return;
            Busy.IsActive = true;
            try
            {
                var genres = await AppState.Current.Api.GetGenresAsync();
                genres.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
                Grid.ItemsSource = genres;
                EmptyText.Text = "No genres found.";
                EmptyText.Visibility = genres.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                _loaded = true;
            }
            catch (Exception ex)
            {
                EmptyText.Text = "Couldn't load genres: " + ex.Message;
                EmptyText.Visibility = Visibility.Visible;
            }
            finally { Busy.IsActive = false; }
        }

        async void Genre_Click(object sender, ItemClickEventArgs e)
        {
            if (!(e.ClickedItem is Genre g)) return;
            Busy.IsActive = true;
            try
            {
                var songs = await AppState.Current.Api.GetRandomSongsAsync(150, g.Name);
                if (songs.Count > 0)
                {
                    AppState.Current.Playback.PlayQueue(songs, 0);
                    MainPage.Instance?.OpenNowPlaying();
                }
            }
            catch { }
            finally { Busy.IsActive = false; }
        }
    }
}
