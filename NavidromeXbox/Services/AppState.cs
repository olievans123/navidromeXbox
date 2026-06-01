using System.Threading.Tasks;
using NavidromeXbox.Models;

namespace NavidromeXbox.Services
{
    /// <summary>
    /// Process-wide state: the API client, the audio engine, and the signed-in user.
    /// Pages reach in through <see cref="Current"/>.
    /// </summary>
    public sealed class AppState
    {
        public static AppState Current { get; } = new AppState();

        public SubsonicApiService Api { get; }
        public PlaybackService Playback { get; }
        public SubsonicUser User { get; private set; }

        public bool IsSignedIn => Settings.HasCredentials;

        AppState()
        {
            Api = new SubsonicApiService();
            Playback = new PlaybackService();
        }

        /// <summary>Verify the stored credentials by pinging; caches the user. Returns null when it fails.</summary>
        public async Task<SubsonicUser> EnsureUserAsync()
        {
            if (!Settings.HasCredentials) { User = null; return null; }
            if (User != null) return User;
            try { User = await Api.GetUserAsync(); }
            catch { User = null; }
            return User;
        }

        /// <summary>Persist a new connection and verify it. On success the user is cached.</summary>
        public async Task<(bool ok, string error)> SignInAsync(string serverUrl, string username, string password)
        {
            var result = await SubsonicAuth.TestConnectionAsync(serverUrl, username, password);
            if (!result.ok) return (false, result.error);

            Settings.SaveConnection(serverUrl, username, password);
            User = new SubsonicUser
            {
                Username = Settings.Username,
                ServerVersion = result.version,
            };
            try { User = await Api.GetUserAsync(); } catch { }
            return (true, null);
        }

        public void SignOut()
        {
            Playback.ClearQueue();
            Settings.Clear();
            User = null;
        }
    }
}
