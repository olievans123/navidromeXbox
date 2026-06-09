using System.ComponentModel;
using NavidromeXbox.Helpers;
using NavidromeXbox.Models;
using NavidromeXbox.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace NavidromeXbox.Views
{
    public sealed partial class NowPlayingPage : Page
    {
        PlaybackService Player => AppState.Current.Playback;
        bool _suppressSeek;
        string _lastCover;

        public NowPlayingPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Player.PropertyChanged += OnPlayerChanged;
            RenderAll();
            if (Player.HasCurrent) PlayBtn.FocusOnLoad();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Player.PropertyChanged -= OnPlayerChanged;
        }

        void OnPlayerChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(PlaybackService.PositionSeconds):
                case nameof(PlaybackService.Position):
                case nameof(PlaybackService.Duration):
                    UpdateProgress();
                    break;
                case nameof(PlaybackService.CurrentSong):
                case nameof(PlaybackService.HasCurrent):
                    RenderAll();
                    break;
                case nameof(PlaybackService.PlayPauseGlyph):
                case nameof(PlaybackService.IsPlaying):
                    PlayBtn.Content = Player.PlayPauseGlyph;
                    break;
                case nameof(PlaybackService.RepeatGlyph):
                    RepeatBtn.Content = Player.RepeatGlyph;
                    break;
                case nameof(PlaybackService.Shuffle):
                case nameof(PlaybackService.RepeatActive):
                    UpdateToggleTints();
                    break;
            }
        }

        void RenderAll()
        {
            bool has = Player.HasCurrent;
            PlayerRoot.Visibility = has ? Visibility.Visible : Visibility.Collapsed;
            EmptyState.Visibility = has ? Visibility.Collapsed : Visibility.Visible;

            var s = Player.CurrentSong;
            if (s != null)
            {
                TitleText.Text = s.Title;
                ArtistText.Text = s.ArtistName;
                AlbumText.Text = s.AlbumName;
                if (s.CoverArt != _lastCover)
                {
                    _lastCover = s.CoverArt;
                    CoverImage.Source = s.CoverArtLargeUri != null ? new BitmapImage(s.CoverArtLargeUri) : null;
                }
                StarBtn.Content = s.Starred ? "\uEB52" : "\uEB51";
            }
            PlayBtn.Content = Player.PlayPauseGlyph;
            RepeatBtn.Content = Player.RepeatGlyph;
            UpdateToggleTints();
            UpdateProgress();
        }

        // Accent the shuffle / repeat icons while their mode is engaged.
        void UpdateToggleTints()
        {
            var accent = (Brush)Application.Current.Resources["AccentBrush"];
            var idle = (Brush)Application.Current.Resources["TextPrimaryBrush"];
            ShuffleBtn.Foreground = Player.Shuffle ? accent : idle;
            RepeatBtn.Foreground = Player.RepeatActive ? accent : idle;
        }

        void UpdateProgress()
        {
            _suppressSeek = true;
            SeekSlider.Maximum = Player.DurationSeconds;
            SeekSlider.Value = Player.PositionSeconds;
            _suppressSeek = false;
            PosText.Text = Format.Duration(Player.Position);
            DurText.Text = Format.Duration(Player.Duration);
        }

        void Seek_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_suppressSeek) return;
            Player.SeekTo(e.NewValue);
        }

        void PlayPause_Click(object sender, RoutedEventArgs e) => Player.TogglePlayPause();
        void Prev_Click(object sender, RoutedEventArgs e) => Player.Previous();
        void Next_Click(object sender, RoutedEventArgs e) => Player.Next();
        void Shuffle_Click(object sender, RoutedEventArgs e) => Player.ToggleShuffle();
        void Repeat_Click(object sender, RoutedEventArgs e) => Player.CycleRepeat();
        void Star_Click(object sender, RoutedEventArgs e) { Player.ToggleStarCurrent(); RenderAll(); }
        void Queue_Click(object sender, RoutedEventArgs e) => MainPage.Instance?.NavigateTo("queue");
        void Browse_Click(object sender, RoutedEventArgs e) => MainPage.Instance?.NavigateTo("albums");
    }
}
