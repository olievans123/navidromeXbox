using NavidromeXbox.Helpers;
using NavidromeXbox.Models;
using NavidromeXbox.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace NavidromeXbox.Views
{
    public sealed partial class QueuePage : Page
    {
        PlaybackService Player => AppState.Current.Playback;

        public QueuePage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            QueueList.ItemsSource = Player.Queue;
            Player.Queue.CollectionChanged += Queue_Changed;
            UpdateEmpty();
            // Don't focus an empty list (no target); fall back to the Clear button if it's live.
            if (Player.Queue.Count > 0) QueueList.FocusFirstItem();
            else if (ClearButton.IsEnabled) ClearButton.Focus(FocusState.Programmatic);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Player.Queue.CollectionChanged -= Queue_Changed;
        }

        void Queue_Changed(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => UpdateEmpty();

        void UpdateEmpty()
        {
            bool empty = Player.Queue.Count == 0;
            EmptyText.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
            ClearButton.IsEnabled = !empty;
        }

        void Song_Click(object sender, ItemClickEventArgs e)
        {
            int idx = Player.Queue.IndexOf(e.ClickedItem as Song);
            if (idx >= 0) Player.JumpTo(idx);
        }

        void Clear_Click(object sender, RoutedEventArgs e) => Player.ClearQueue();
    }
}
