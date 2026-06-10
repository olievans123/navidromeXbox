using System;
using NavidromeXbox.Helpers;
using NavidromeXbox.Models;
using NavidromeXbox.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace NavidromeXbox.Views
{
    public sealed partial class RadioPage : Page
    {
        bool _loaded;

        public RadioPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (_loaded) return;
            Busy.IsActive = true;
            try
            {
                var stations = await AppState.Current.Api.GetInternetRadioStationsAsync();
                StationList.ItemsSource = stations;
                EmptyText.Text = "No radio stations yet.\nAdd them in the Navidrome web UI (Settings → Internet Radio).";
                EmptyText.Visibility = stations.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                _loaded = true;
                StationList.FocusFirstItem();
            }
            catch (Exception ex)
            {
                EmptyText.Text = "Couldn't load radio stations: " + ex.Message;
                EmptyText.Visibility = Visibility.Visible;
            }
            finally { Busy.IsActive = false; }
        }

        void Station_Click(object sender, ItemClickEventArgs e)
        {
            if (!(e.ClickedItem is RadioStation s)) return;
            AppState.Current.Playback.PlayRadio(s);
            MainPage.Instance?.OpenNowPlaying();
        }
    }
}
