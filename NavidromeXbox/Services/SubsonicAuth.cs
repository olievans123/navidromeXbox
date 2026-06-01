using System;
using System.Net.Http;
using System.Threading.Tasks;
using NavidromeXbox.Helpers;
using Newtonsoft.Json.Linq;

namespace NavidromeXbox.Services
{
    /// <summary>
    /// Builds the Subsonic authentication query and verifies credentials.
    ///
    /// Subsonic auth is salted-token: every request carries
    ///   u=&lt;user&gt;&amp;t=md5(password+salt)&amp;s=&lt;salt&gt;&amp;v=1.16.1&amp;c=NavidromeXbox&amp;f=json
    /// A fresh random salt is generated per request, so the password is never sent.
    /// </summary>
    public static class SubsonicAuth
    {
        public const string ApiVersion = "1.16.1";
        public const string ClientName = "NavidromeXbox";

        /// <summary>The shared auth + format query fragment for the currently signed-in user (no leading '?').</summary>
        public static string Query() => Query(Settings.Username, Settings.Password);

        public static string Query(string username, string password)
        {
            string salt = Hashing.RandomSalt();
            string token = Hashing.Md5Hex((password ?? "") + salt);
            return "u=" + Uri.EscapeDataString(username ?? "") +
                   "&t=" + token +
                   "&s=" + salt +
                   "&v=" + ApiVersion +
                   "&c=" + ClientName +
                   "&f=json";
        }

        /// <summary>
        /// Attempt a /rest/ping against the given server with the given creds.
        /// Returns (ok, errorMessage, serverVersion). Does not mutate Settings.
        /// </summary>
        public static async Task<(bool ok, string error, string version)> TestConnectionAsync(string serverUrl, string username, string password)
        {
            serverUrl = Settings.NormalizeUrl(serverUrl);
            if (string.IsNullOrWhiteSpace(serverUrl)) return (false, "Enter a server address.", null);
            if (string.IsNullOrWhiteSpace(username)) return (false, "Enter a username.", null);

            string url = serverUrl + "/rest/ping.view?" + Query(username, password);
            try
            {
                using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) })
                {
                    http.DefaultRequestHeaders.UserAgent.ParseAdd(ClientName + "/1.0");
                    using (var resp = await http.GetAsync(url))
                    {
                        string body = await resp.Content.ReadAsStringAsync();
                        if (!resp.IsSuccessStatusCode)
                            return (false, $"Server returned HTTP {(int)resp.StatusCode}. Check the address.", null);

                        JObject root;
                        try { root = JObject.Parse(body); }
                        catch { return (false, "That doesn't look like a Subsonic/Navidrome server.", null); }

                        var sr = root["subsonic-response"];
                        if (sr == null) return (false, "Unexpected response from the server.", null);

                        string status = sr.Value<string>("status");
                        if (status == "ok")
                            return (true, null, sr.Value<string>("version"));

                        // Surface the server's own error (e.g. wrong username/password = code 40).
                        var err = sr["error"];
                        string msg = err?.Value<string>("message") ?? "Authentication failed.";
                        return (false, msg, null);
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, "Couldn't reach the server: " + ex.Message, null);
            }
        }
    }
}
