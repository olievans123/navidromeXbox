using System;
using NavidromeXbox.Models;
using NavidromeXbox.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace NavidromeXbox.Views
{
    public sealed partial class PlaylistsPage : Page
    {
        public PlaylistsPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            Busy.IsActive = true;
            try
            {
                var lists = await AppState.Current.Api.GetPlaylistsAsync();
                Grid.ItemsSource = lists;
                EmptyText.Visibility = lists.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                EmptyText.Text = "Couldn't load playlists: " + ex.Message;
                EmptyText.Visibility = Visibility.Visible;
            }
            finally { Busy.IsActive = false; }
        }

        void Playlist_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Playlist p) MainPage.Instance?.OpenPlaylist(p.Id);
        }
    }
}
