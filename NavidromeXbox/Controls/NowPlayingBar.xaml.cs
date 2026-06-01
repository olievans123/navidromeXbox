using System;
using System.ComponentModel;
using NavidromeXbox.Helpers;
using NavidromeXbox.Services;
using Windows.UI.Xaml.Controls;

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
            this.Loaded += (s, e) => UpdateTime();
        }

        void OnPlayerChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlaybackService.PositionSeconds) ||
                e.PropertyName == nameof(PlaybackService.Position) ||
                e.PropertyName == nameof(PlaybackService.Duration))
            {
                UpdateTime();
            }
        }

        void UpdateTime()
        {
            TimeText.Text = $"{Format.Duration(Player.Position)} / {Format.Duration(Player.Duration)}";
        }

        void Info_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) => ExpandRequested?.Invoke();
        void PlayPause_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) => Player.TogglePlayPause();
        void Prev_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) => Player.Previous();
        void Next_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) => Player.Next();
        void Shuffle_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) => Player.ToggleShuffle();
        void Repeat_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) => Player.CycleRepeat();
    }
}
