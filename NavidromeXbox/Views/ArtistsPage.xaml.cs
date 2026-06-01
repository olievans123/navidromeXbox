using System;
using NavidromeXbox.Models;
using NavidromeXbox.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace NavidromeXbox.Views
{
    public sealed partial class ArtistsPage : Page
    {
        bool _loaded;

        public ArtistsPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (_loaded) return;
            try
            {
                var groups = await AppState.Current.Api.GetArtistsAsync();
                IndexHost.ItemsSource = groups;
                EmptyText.Text = "No artists found.";
                EmptyText.Visibility = groups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                _loaded = true;
            }
            catch (Exception ex)
            {
                EmptyText.Text = "Couldn't load artists: " + ex.Message;
                EmptyText.Visibility = Visibility.Visible;
            }
            finally { Busy.IsActive = false; }
        }

        void Artist_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Artist a) MainPage.Instance?.OpenArtist(a.Id);
        }
    }
}
