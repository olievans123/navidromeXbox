using System.Linq;
using NavidromeXbox.Services;
using NavidromeXbox.Views;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace NavidromeXbox
{
    public sealed partial class MainPage : Page
    {
        public static MainPage Instance { get; private set; }
        string _currentTag = "home";

        public MainPage()
        {
            this.InitializeComponent();
            Instance = this;
            this.KeyDown += Page_KeyDown;
            this.Loaded += OnLoaded;
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            MiniPlayer.ExpandRequested += () => OpenNowPlaying();
            if (!AppState.Current.IsSignedIn)
            {
                // First run / signed out: take over the whole frame with the login flow.
                ContentFrame.Navigate(typeof(LoginPage));
                HighlightNav(null);
            }
            else
            {
                GoTo("home");
            }
        }

        // Gamepad Menu/View toggles the nav from anywhere; B / Esc closes it.
        void Page_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.GamepadMenu || e.Key == VirtualKey.GamepadView)
            {
                SetPane(!NavSplit.IsPaneOpen);
                e.Handled = true;
            }
            else if (NavSplit.IsPaneOpen && (e.Key == VirtualKey.GamepadB || e.Key == VirtualKey.Escape))
            {
                ClosePane();
                e.Handled = true;
            }
        }

        void ToggleNav_Click(object sender, RoutedEventArgs e) => SetPane(!NavSplit.IsPaneOpen);

        void SetPane(bool open)
        {
            NavSplit.IsPaneOpen = open;
            if (open) FocusActiveNav();
            else MenuButton.Focus(FocusState.Programmatic);
        }

        void ClosePane()
        {
            NavSplit.IsPaneOpen = false;
            MenuButton.Focus(FocusState.Programmatic);
        }

        void FocusActiveNav()
        {
            var btn = NavItems.Children.OfType<Button>().FirstOrDefault(b => (b.Tag as string) == _currentTag)
                      ?? NavItems.Children.OfType<Button>().FirstOrDefault();
            btn?.Focus(FocusState.Programmatic);
        }

        void Nav_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as FrameworkElement)?.Tag as string;
            if (tag == null) return;
            NavSplit.IsPaneOpen = false;
            GoTo(tag);
        }

        /// <summary>Pages call this to switch the top-level section.</summary>
        public void NavigateTo(string tag)
        {
            NavSplit.IsPaneOpen = false;
            GoTo(tag);
        }

        public void OpenAlbum(string id) { NavSplit.IsPaneOpen = false; ContentFrame.Navigate(typeof(AlbumDetailPage), id); HighlightNav("albums"); _currentTag = "albums"; }
        public void OpenArtist(string id) { NavSplit.IsPaneOpen = false; ContentFrame.Navigate(typeof(ArtistDetailPage), id); HighlightNav("artists"); _currentTag = "artists"; }
        public void OpenPlaylist(string id) { NavSplit.IsPaneOpen = false; ContentFrame.Navigate(typeof(PlaylistDetailPage), id); HighlightNav("playlists"); _currentTag = "playlists"; }
        public void OpenNowPlaying() { NavSplit.IsPaneOpen = false; GoTo("nowplaying"); }

        /// <summary>Called by LoginPage once a server connection is established.</summary>
        public void OnSignedIn() => GoTo("home");

        /// <summary>Return the whole frame to the login flow after signing out.</summary>
        public void ReturnToLogin()
        {
            NavSplit.IsPaneOpen = false;
            ContentFrame.Navigate(typeof(LoginPage));
            HighlightNav(null);
            _currentTag = null;
        }

        void GoTo(string tag)
        {
            switch (tag)
            {
                case "home": ContentFrame.Navigate(typeof(HomePage)); break;
                case "albums": ContentFrame.Navigate(typeof(AlbumsPage)); break;
                case "artists": ContentFrame.Navigate(typeof(ArtistsPage)); break;
                case "playlists": ContentFrame.Navigate(typeof(PlaylistsPage)); break;
                case "genres": ContentFrame.Navigate(typeof(GenresPage)); break;
                case "search": ContentFrame.Navigate(typeof(SearchPage)); break;
                case "nowplaying": ContentFrame.Navigate(typeof(NowPlayingPage)); break;
                case "queue": ContentFrame.Navigate(typeof(QueuePage)); break;
                case "settings": ContentFrame.Navigate(typeof(SettingsPage)); break;
            }
            _currentTag = tag;
            HighlightNav(tag);
        }

        void HighlightNav(string tag)
        {
            var activeBg = (Brush)Application.Current.Resources["AccentBrush"];
            var activeFg = new SolidColorBrush(Color.FromArgb(0xFF, 0x06, 0x20, 0x1E));
            var inactiveBg = (Brush)Application.Current.Resources["AppSurfaceHighBrush"];
            var inactiveFg = (Brush)Application.Current.Resources["TextPrimaryBrush"];

            foreach (var b in NavItems.Children.OfType<Button>())
            {
                bool active = (b.Tag as string) == tag;
                b.Background = active ? activeBg : inactiveBg;
                b.Foreground = active ? activeFg : inactiveFg;
            }
        }
    }
}
