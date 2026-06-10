using Windows.Security.Credentials;
using Windows.Storage;

namespace NavidromeXbox.Services
{
    /// <summary>
    /// Process-wide, persisted preferences. The server URL / username and playback
    /// preferences live in roaming-but-local <see cref="ApplicationData"/>; the password
    /// is kept in the Windows <see cref="PasswordVault"/> so it never sits in plaintext.
    ///
    /// Subsonic uses salted-token auth (token = md5(password + per-request salt)), so the
    /// client must retain the password to authenticate each call — hence the vault.
    /// </summary>
    public static class Settings
    {
        const string VaultResource = "NavidromeXbox";
        const string VaultUser = "password";
        const string PwFallbackKey = "pw";   // used only when the credential vault is unavailable

        static ApplicationDataContainer Local => ApplicationData.Current.LocalSettings;

        // ---- connection ----
        public static string ServerUrl { get; set; }      // e.g. https://music.example.com
        public static string Username { get; set; }
        public static string Password { get; private set; }

        // ---- playback / transcoding ----
        /// <summary>0 = original/no transcode; otherwise a max kbps cap passed to stream.view.</summary>
        public static int MaxBitRate { get; set; }
        /// <summary>"" / "raw" = original; otherwise a target container (e.g. "mp3", "opus").</summary>
        public static string TranscodeFormat { get; set; }
        public static bool ScrobbleEnabled { get; set; }
        public static bool GaplessEnabled { get; set; }

        // ---- home screen layout (which shelves appear, default all on) ----
        public static bool HomeNewest { get; set; }
        public static bool HomeRecent { get; set; }
        public static bool HomeFrequent { get; set; }
        public static bool HomeRandom { get; set; }
        public static bool HomeStarred { get; set; }

        // ---- side menu layout (which optional sections appear, default all on) ----
        public static bool NavAlbums { get; set; }
        public static bool NavArtists { get; set; }
        public static bool NavPlaylists { get; set; }
        public static bool NavGenres { get; set; }
        public static bool NavRadio { get; set; }
        public static bool NavSearch { get; set; }

        public static bool HasServer => !string.IsNullOrWhiteSpace(ServerUrl) && !string.IsNullOrWhiteSpace(Username);
        public static bool HasCredentials => HasServer && !string.IsNullOrEmpty(Password);

        static bool GetBool(string key, bool dflt) => Local.Values[key] is bool b ? b : dflt;

        public static void Load()
        {
            ServerUrl = Local.Values["ServerUrl"] as string ?? "";
            Username = Local.Values["Username"] as string ?? "";
            MaxBitRate = Local.Values["MaxBitRate"] is int mb ? mb : 0;
            TranscodeFormat = Local.Values["TranscodeFormat"] as string ?? "raw";
            ScrobbleEnabled = GetBool("ScrobbleEnabled", true);
            GaplessEnabled = GetBool("GaplessEnabled", true);

            HomeNewest = GetBool("HomeNewest", true);
            HomeRecent = GetBool("HomeRecent", true);
            HomeFrequent = GetBool("HomeFrequent", true);
            HomeRandom = GetBool("HomeRandom", true);
            HomeStarred = GetBool("HomeStarred", true);

            NavAlbums = GetBool("NavAlbums", true);
            NavArtists = GetBool("NavArtists", true);
            NavPlaylists = GetBool("NavPlaylists", true);
            NavGenres = GetBool("NavGenres", true);
            NavRadio = GetBool("NavRadio", true);
            NavSearch = GetBool("NavSearch", true);

            Password = LoadPassword();
        }

        public static void SaveConnection(string serverUrl, string username, string password)
        {
            ServerUrl = NormalizeUrl(serverUrl);
            Username = username?.Trim();
            Password = password ?? "";
            Local.Values["ServerUrl"] = ServerUrl;
            Local.Values["Username"] = Username;
            StorePassword(Password);
        }

        public static void SavePlayback()
        {
            Local.Values["MaxBitRate"] = MaxBitRate;
            Local.Values["TranscodeFormat"] = TranscodeFormat ?? "raw";
            Local.Values["ScrobbleEnabled"] = ScrobbleEnabled;
            Local.Values["GaplessEnabled"] = GaplessEnabled;
        }

        public static void SaveLayout()
        {
            Local.Values["HomeNewest"] = HomeNewest;
            Local.Values["HomeRecent"] = HomeRecent;
            Local.Values["HomeFrequent"] = HomeFrequent;
            Local.Values["HomeRandom"] = HomeRandom;
            Local.Values["HomeStarred"] = HomeStarred;

            Local.Values["NavAlbums"] = NavAlbums;
            Local.Values["NavArtists"] = NavArtists;
            Local.Values["NavPlaylists"] = NavPlaylists;
            Local.Values["NavGenres"] = NavGenres;
            Local.Values["NavRadio"] = NavRadio;
            Local.Values["NavSearch"] = NavSearch;
        }

        public static void Clear()
        {
            ServerUrl = ""; Username = ""; Password = "";
            Local.Values.Remove("ServerUrl");
            Local.Values.Remove("Username");
            ClearPassword();
        }

        /// <summary>Trim trailing slash and a stray /rest path; prepend https:// when no scheme given.</summary>
        public static string NormalizeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "";
            url = url.Trim();
            if (!url.Contains("://")) url = "https://" + url;
            url = url.TrimEnd('/');
            if (url.EndsWith("/rest")) url = url.Substring(0, url.Length - 5);
            return url;
        }

        // ----------------------------------------------------- password vault

        static void StorePassword(string password)
        {
            ClearPassword();
            if (string.IsNullOrEmpty(password)) return;
            try
            {
                var vault = new PasswordVault();
                vault.Add(new PasswordCredential(VaultResource, VaultUser, password));
                Local.Values.Remove(PwFallbackKey);   // vault worked — keep no plaintext copy
            }
            catch
            {
                // Some Xbox configurations can't reach the credential vault; fall back to the
                // app's sandboxed local store so the user still stays signed in across launches.
                Local.Values[PwFallbackKey] = password;
            }
        }

        static string LoadPassword()
        {
            try
            {
                var vault = new PasswordVault();
                var cred = vault.Retrieve(VaultResource, VaultUser);
                cred.RetrievePassword();
                if (!string.IsNullOrEmpty(cred.Password)) return cred.Password;
            }
            catch { /* not in the vault — try the fallback below */ }
            return Local.Values[PwFallbackKey] as string ?? "";
        }

        static void ClearPassword()
        {
            try
            {
                var vault = new PasswordVault();
                foreach (var c in vault.FindAllByResource(VaultResource)) vault.Remove(c);
            }
            catch { /* nothing stored */ }
            Local.Values.Remove(PwFallbackKey);
        }
    }
}
