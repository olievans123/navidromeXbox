using System;
using System.Linq;
using NavidromeXbox.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace NavidromeXbox.Views
{
    public sealed partial class SettingsPage : Page
    {
        bool _ready;

        public SettingsPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            _ready = false;
            ServerText.Text = Settings.ServerUrl;
            UserText.Text = "Signed in as " + Settings.Username;

            SelectByTag(BitrateBox, Settings.MaxBitRate.ToString());
            SelectByTag(FormatBox, string.IsNullOrEmpty(Settings.TranscodeFormat) ? "raw" : Settings.TranscodeFormat);
            ScrobbleToggle.IsOn = Settings.ScrobbleEnabled;

            HomeNewestToggle.IsOn = Settings.HomeNewest;
            HomeRecentToggle.IsOn = Settings.HomeRecent;
            HomeFrequentToggle.IsOn = Settings.HomeFrequent;
            HomeRandomToggle.IsOn = Settings.HomeRandom;
            HomeStarredToggle.IsOn = Settings.HomeStarred;

            NavAlbumsToggle.IsOn = Settings.NavAlbums;
            NavArtistsToggle.IsOn = Settings.NavArtists;
            NavPlaylistsToggle.IsOn = Settings.NavPlaylists;
            NavGenresToggle.IsOn = Settings.NavGenres;
            NavRadioToggle.IsOn = Settings.NavRadio;
            NavSearchToggle.IsOn = Settings.NavSearch;
            _ready = true;

            var user = await AppState.Current.EnsureUserAsync();
            VersionText.Text = user != null ? $"Server API {user.ServerVersion}" : "";
        }

        static void SelectByTag(ComboBox box, string tag)
        {
            var item = box.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (i.Tag as string) == tag);
            box.SelectedItem = item ?? box.Items.OfType<ComboBoxItem>().FirstOrDefault();
        }

        void Setting_Changed(object sender, RoutedEventArgs e)
        {
            if (!_ready) return;
            int.TryParse((BitrateBox.SelectedItem as ComboBoxItem)?.Tag as string, out int br);
            Settings.MaxBitRate = br;
            Settings.TranscodeFormat = (FormatBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "raw";
            Settings.ScrobbleEnabled = ScrobbleToggle.IsOn;
            Settings.SavePlayback();
        }

        void Layout_Changed(object sender, RoutedEventArgs e)
        {
            if (!_ready) return;
            Settings.HomeNewest = HomeNewestToggle.IsOn;
            Settings.HomeRecent = HomeRecentToggle.IsOn;
            Settings.HomeFrequent = HomeFrequentToggle.IsOn;
            Settings.HomeRandom = HomeRandomToggle.IsOn;
            Settings.HomeStarred = HomeStarredToggle.IsOn;

            Settings.NavAlbums = NavAlbumsToggle.IsOn;
            Settings.NavArtists = NavArtistsToggle.IsOn;
            Settings.NavPlaylists = NavPlaylistsToggle.IsOn;
            Settings.NavGenres = NavGenresToggle.IsOn;
            Settings.NavRadio = NavRadioToggle.IsOn;
            Settings.NavSearch = NavSearchToggle.IsOn;
            Settings.SaveLayout();

            // The side menu reflects changes immediately; Home re-reads on its next visit.
            MainPage.Instance?.ApplyNavCustomization();
        }

        async void SignOut_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Sign out?",
                Content = "This disconnects from the server and stops playback.",
                PrimaryButtonText = "Sign out",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                AppState.Current.SignOut();
                MainPage.Instance?.ReturnToLogin();
            }
        }
    }
}
