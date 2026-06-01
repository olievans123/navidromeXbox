using NavidromeXbox.Helpers;
using NavidromeXbox.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace NavidromeXbox.Views
{
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Pre-fill anything we already know so re-auth is a one-button affair.
            ServerBox.Text = Settings.ServerUrl ?? "";
            UserBox.Text = Settings.Username ?? "";
            ServerBox.FocusOnLoad();
        }

        async void Connect_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Visibility = Visibility.Collapsed;
            string server = ServerBox.Text;
            string user = UserBox.Text;
            string pass = PassBox.Password;

            SetBusy(true);
            var (ok, error) = await AppState.Current.SignInAsync(server, user, pass);
            SetBusy(false);

            if (ok)
            {
                MainPage.Instance?.OnSignedIn();
            }
            else
            {
                StatusText.Text = error ?? "Could not connect.";
                StatusText.Visibility = Visibility.Visible;
            }
        }

        void SetBusy(bool busy)
        {
            Busy.IsActive = busy;
            ConnectButton.Content = busy ? "" : "Connect";
            ConnectButton.IsEnabled = !busy;
        }
    }
}
