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

        public static bool HasServer => !string.IsNullOrWhiteSpace(ServerUrl) && !string.IsNullOrWhiteSpace(Username);
        public static bool HasCredentials => HasServer && !string.IsNullOrEmpty(Password);

        public static void Load()
        {
            ServerUrl = Local.Values["ServerUrl"] as string ?? "";
            Username = Local.Values["Username"] as string ?? "";
            MaxBitRate = Local.Values["MaxBitRate"] is int mb ? mb : 0;
            TranscodeFormat = Local.Values["TranscodeFormat"] as string ?? "raw";
            ScrobbleEnabled = Local.Values["ScrobbleEnabled"] is bool sc ? sc : true;
            GaplessEnabled = Local.Values["GaplessEnabled"] is bool gp ? gp : true;
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
            var vault = new PasswordVault();
            vault.Add(new PasswordCredential(VaultResource, VaultUser, password));
        }

        static string LoadPassword()
        {
            try
            {
                var vault = new PasswordVault();
                var cred = vault.Retrieve(VaultResource, VaultUser);
                cred.RetrievePassword();
                return cred.Password;
            }
            catch { return ""; }
        }

        static void ClearPassword()
        {
            try
            {
                var vault = new PasswordVault();
                foreach (var c in vault.FindAllByResource(VaultResource)) vault.Remove(c);
            }
            catch { /* nothing stored */ }
        }
    }
}
