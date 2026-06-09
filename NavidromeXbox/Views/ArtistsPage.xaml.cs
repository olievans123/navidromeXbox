using System;
using System.Collections.Generic;
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
                // The server hands back A–Z index groups; flatten them (already alphabetical)
                // into one list so the GridView can virtualize the whole library.
                var groups = await AppState.Current.Api.GetArtistsAsync();
                var all = new List<Artist>();
                foreach (var g in groups) all.AddRange(g.Items);
                ArtistsGrid.ItemsSource = all;

                EmptyText.Text = "No artists found.";
                EmptyText.Visibility = all.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
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
