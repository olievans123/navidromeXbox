using System;
using System.ComponentModel;
using NavidromeXbox.Helpers;
using NavidromeXbox.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace NavidromeXbox.Controls
{
    /// <summary>
    /// The persistent mini player docked at the bottom of the shell. Binds to the shared
    /// <see cref="PlaybackService"/> and exposes <see cref="ExpandRequested"/> so the shell
    /// can open the full-screen Now Playing view.
    /// </summary>
    public sealed partial class NowPlayingBar : UserControl
    {
        public event Action ExpandRequested;
        PlaybackService Player => AppState.Current.Playback;

        public NowPlayingBar()
        {
            this.InitializeComponent();
            this.DataContext = Player;
            Player.PropertyChanged += OnPlayerChanged;
            this.Loaded += (s, e) => { UpdateTime(); UpdateToggleTints(); };
        }

        void OnPlayerChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlaybackService.PositionSeconds) ||
                e.PropertyName == nameof(PlaybackService.Position) ||
                e.PropertyName == nameof(PlaybackService.Duration) ||
                e.PropertyName == nameof(PlaybackService.CurrentSong))
            {
                UpdateTime();
            }
            else if (e.PropertyName == nameof(PlaybackService.Shuffle) ||
                     e.PropertyName == nameof(PlaybackService.RepeatActive))
            {
                UpdateToggleTints();
            }
        }

        void UpdateTime()
        {
            if (Player.CurrentSong?.IsRadio == true)
            {
                // A live stream has no fixed length — show LIVE and hide the progress track.
                TimeText.Text = "Live";
                Progress.Visibility = Visibility.Collapsed;
                return;
            }
            Progress.Visibility = Visibility.Visible;
            TimeText.Text = $"{Format.Duration(Player.Position)} / {Format.Duration(Player.Duration)}";
        }

        // Accent the shuffle / repeat icons while their mode is engaged.
        void UpdateToggleTints()
        {
            var accent = (Brush)Application.Current.Resources["AccentBrush"];
            var idle = (Brush)Application.Current.Resources["TextPrimaryBrush"];
            ShuffleBtn.Foreground = Player.Shuffle ? accent : idle;
            RepeatBtn.Foreground = Player.RepeatActive ? accent : idle;
        }

        void Info_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) => ExpandRequested?.Invoke();
        void PlayPause_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) => Player.TogglePlayPause();
        void Prev_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) => Player.Previous();
        void Next_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) => Player.Next();
        void Shuffle_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) => Player.ToggleShuffle();
        void Repeat_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) => Player.CycleRepeat();
    }
}
