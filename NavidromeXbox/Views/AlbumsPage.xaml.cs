using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using NavidromeXbox.Helpers;
using NavidromeXbox.Models;
using NavidromeXbox.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace NavidromeXbox.Views
{
    public sealed partial class AlbumsPage : Page
    {
        const int PageSize = 60;
        readonly ObservableCollection<Album> _albums = new ObservableCollection<Album>();
        string _type = "newest";
        int _offset;
        bool _loading;
        bool _ready;   // suppress the ComboBox SelectionChanged that fires during InitializeComponent

        public AlbumsPage()
        {
            this.InitializeComponent();
            Grid.ItemsSource = _albums;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            _ready = true;
            if (_albums.Count == 0)
            {
                await ReloadAsync();
                Grid.FocusFirstItem();   // drop focus onto the first album
            }
        }

        async void Sort_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready) return;
            _type = (SortBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "newest";
            await ReloadAsync();
        }

        async Task ReloadAsync()
        {
            _albums.Clear();
            _offset = 0;
            await LoadPageAsync();
        }

        async Task LoadPageAsync()
        {
            if (_loading) return;
            _loading = true;
            Busy.IsActive = true;
            EmptyText.Visibility = Visibility.Collapsed;
            MoreButton.Visibility = Visibility.Collapsed;
            try
            {
                var page = await AppState.Current.Api.GetAlbumList2Async(_type, PageSize, _offset);
                foreach (var a in page) _albums.Add(a);
                _offset += page.Count;
                MoreButton.Visibility = page.Count == PageSize ? Visibility.Visible : Visibility.Collapsed;
                EmptyText.Visibility = _albums.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                EmptyText.Text = "Couldn't load albums: " + ex.Message;
                EmptyText.Visibility = Visibility.Visible;
            }
            finally
            {
                Busy.IsActive = false;
                _loading = false;
            }
        }

        async void More_Click(object sender, RoutedEventArgs e) => await LoadPageAsync();

        void Album_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Album a) MainPage.Instance?.OpenAlbum(a.Id);
        }
    }
}
