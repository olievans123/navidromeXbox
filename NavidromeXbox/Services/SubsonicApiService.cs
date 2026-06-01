using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using NavidromeXbox.Models;
using Newtonsoft.Json.Linq;

namespace NavidromeXbox.Services
{
    /// <summary>Thrown when the server replies with a Subsonic error envelope.</summary>
    public sealed class SubsonicException : Exception
    {
        public int Code { get; }
        public SubsonicException(int code, string message) : base(message) { Code = code; }
    }

    /// <summary>
    /// Async wrapper over the Navidrome / Subsonic REST API. Reads JSON (f=json) and maps
    /// the relevant slices onto the <see cref="Models"/> DTOs. Binary endpoints (cover art,
    /// stream) are exposed as fully-formed URLs so the player and Image controls can use them
    /// directly.
    /// </summary>
    public sealed class SubsonicApiService
    {
        readonly HttpClient _http;

        public SubsonicApiService()
        {
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _http.DefaultRequestHeaders.UserAgent.ParseAdd(SubsonicAuth.ClientName + "/1.0");
        }

        string Base => Settings.ServerUrl;

        // ----------------------------------------------------------- core

        async Task<JToken> Rest(string method, string extra = null)
        {
            if (string.IsNullOrEmpty(Base)) throw new InvalidOperationException("No server configured.");
            string url = Base + "/rest/" + method + ".view?" + SubsonicAuth.Query() + (extra ?? "");
            using (var resp = await _http.GetAsync(url))
            {
                string body = await resp.Content.ReadAsStringAsync();
                resp.EnsureSuccessStatusCode();
                var sr = JObject.Parse(body)["subsonic-response"];
                if (sr == null) throw new SubsonicException(-1, "Malformed server response.");
                if (sr.Value<string>("status") != "ok")
                {
                    var err = sr["error"];
                    throw new SubsonicException(err?.Value<int?>("code") ?? -1,
                        err?.Value<string>("message") ?? "Server error.");
                }
                return sr;
            }
        }

        /// <summary>Wrap a single JObject child or an array into a uniform list (Subsonic is inconsistent here).</summary>
        static IEnumerable<JObject> Many(JToken parent, string name)
        {
            var node = parent?[name];
            if (node == null) yield break;
            if (node is JArray arr) { foreach (var t in arr) if (t is JObject o) yield return o; }
            else if (node is JObject single) yield return single;
        }

        static bool IsStarred(JObject o) => o["starred"] != null && o["starred"].Type != JTokenType.Null;

        // ----------------------------------------------------------- binary URLs

        public string CoverArtUrl(string coverArtId, int size = 300)
        {
            if (string.IsNullOrEmpty(Base) || string.IsNullOrEmpty(coverArtId)) return null;
            return Base + "/rest/getCoverArt.view?" + SubsonicAuth.Query() +
                   "&id=" + Uri.EscapeDataString(coverArtId) + "&size=" + size;
        }

        /// <summary>Stream URL honouring the user's transcoding preference.</summary>
        public string StreamUrl(string songId)
        {
            if (string.IsNullOrEmpty(Base) || string.IsNullOrEmpty(songId)) return null;
            string extra = "&id=" + Uri.EscapeDataString(songId);
            if (Settings.MaxBitRate > 0) extra += "&maxBitRate=" + Settings.MaxBitRate;
            var fmt = Settings.TranscodeFormat;
            if (!string.IsNullOrEmpty(fmt) && fmt != "raw") extra += "&format=" + Uri.EscapeDataString(fmt);
            return Base + "/rest/stream.view?" + SubsonicAuth.Query() + extra;
        }

        // ----------------------------------------------------------- parsing

        static Album ParseAlbum(JObject o) => new Album
        {
            Id = o.Value<string>("id"),
            Name = o.Value<string>("name") ?? o.Value<string>("album") ?? o.Value<string>("title"),
            ArtistName = o.Value<string>("artist"),
            ArtistId = o.Value<string>("artistId"),
            CoverArt = o.Value<string>("coverArt"),
            Year = o.Value<int?>("year"),
            Genre = o.Value<string>("genre"),
            SongCount = o.Value<int?>("songCount"),
            DurationSeconds = o.Value<int?>("duration"),
            PlayCount = o.Value<int?>("playCount"),
            Starred = IsStarred(o),
        };

        static Song ParseSong(JObject o) => new Song
        {
            Id = o.Value<string>("id"),
            Title = o.Value<string>("title"),
            AlbumName = o.Value<string>("album"),
            AlbumId = o.Value<string>("albumId"),
            ArtistName = o.Value<string>("artist"),
            ArtistId = o.Value<string>("artistId"),
            CoverArt = o.Value<string>("coverArt"),
            Track = o.Value<int?>("track"),
            DiscNumber = o.Value<int?>("discNumber"),
            Year = o.Value<int?>("year"),
            Genre = o.Value<string>("genre"),
            DurationSeconds = o.Value<int?>("duration"),
            BitRate = o.Value<int?>("bitRate"),
            Suffix = o.Value<string>("suffix"),
            Rating = o.Value<int?>("userRating"),
            Starred = IsStarred(o),
        };

        static Artist ParseArtist(JObject o) => new Artist
        {
            Id = o.Value<string>("id"),
            Name = o.Value<string>("name"),
            CoverArt = o.Value<string>("coverArt") ?? o.Value<string>("id"),
            AlbumCount = o.Value<int?>("albumCount"),
            Starred = IsStarred(o),
        };

        static Playlist ParsePlaylist(JObject o) => new Playlist
        {
            Id = o.Value<string>("id"),
            Name = o.Value<string>("name"),
            Comment = o.Value<string>("comment"),
            Owner = o.Value<string>("owner"),
            CoverArt = o.Value<string>("coverArt"),
            SongCount = o.Value<int?>("songCount"),
            DurationSeconds = o.Value<int?>("duration"),
            Public = o.Value<bool?>("public") ?? false,
        };

        // ----------------------------------------------------------- account

        public async Task<SubsonicUser> GetUserAsync()
        {
            var sr = await Rest("ping");
            string host;
            try { host = new Uri(Base).Host; } catch { host = Base; }
            return new SubsonicUser
            {
                Username = Settings.Username,
                ServerName = host,
                ServerVersion = sr.Value<string>("version"),
            };
        }

        // ----------------------------------------------------------- albums

        /// <summary>type: newest, recent, frequent, random, alphabeticalByName, alphabeticalByArtist, starred, byGenre.</summary>
        public async Task<List<Album>> GetAlbumList2Async(string type, int size = 30, int offset = 0, string genre = null)
        {
            string extra = $"&type={type}&size={size}&offset={offset}";
            if (!string.IsNullOrEmpty(genre)) extra += "&genre=" + Uri.EscapeDataString(genre);
            var sr = await Rest("getAlbumList2", extra);
            var list = new List<Album>();
            foreach (var o in Many(sr["albumList2"], "album")) list.Add(ParseAlbum(o));
            return list;
        }

        public async Task<Album> GetAlbumAsync(string id)
        {
            var sr = await Rest("getAlbum", "&id=" + Uri.EscapeDataString(id));
            var node = sr["album"] as JObject;
            if (node == null) return null;
            var album = ParseAlbum(node);
            foreach (var s in Many(node, "song")) album.Songs.Add(ParseSong(s));
            return album;
        }

        // ----------------------------------------------------------- artists

        public async Task<List<ArtistIndexGroup>> GetArtistsAsync()
        {
            var sr = await Rest("getArtists");
            var groups = new List<ArtistIndexGroup>();
            foreach (var idx in Many(sr["artists"], "index"))
            {
                var g = new ArtistIndexGroup { Key = idx.Value<string>("name") };
                foreach (var a in Many(idx, "artist")) g.Items.Add(ParseArtist(a));
                if (g.Items.Count > 0) groups.Add(g);
            }
            return groups;
        }

        public async Task<(Artist artist, List<Album> albums)> GetArtistAsync(string id)
        {
            var sr = await Rest("getArtist", "&id=" + Uri.EscapeDataString(id));
            var node = sr["artist"] as JObject;
            if (node == null) return (null, new List<Album>());
            var artist = ParseArtist(node);
            var albums = new List<Album>();
            foreach (var a in Many(node, "album")) albums.Add(ParseAlbum(a));
            return (artist, albums);
        }

        public async Task<ArtistInfo> GetArtistInfoAsync(string id)
        {
            var sr = await Rest("getArtistInfo2", "&id=" + Uri.EscapeDataString(id) + "&count=12");
            var node = sr["artistInfo2"] as JObject;
            var info = new ArtistInfo();
            if (node == null) return info;
            info.Biography = node.Value<string>("biography");
            var img = node.Value<string>("largeImageUrl");
            if (!string.IsNullOrEmpty(img)) { try { info.ImageUri = new Uri(img); } catch { } }
            foreach (var a in Many(node, "similarArtist")) info.Similar.Add(ParseArtist(a));
            return info;
        }

        public async Task<List<Song>> GetTopSongsAsync(string artistName, int count = 10)
        {
            var sr = await Rest("getTopSongs", "&artist=" + Uri.EscapeDataString(artistName) + "&count=" + count);
            var list = new List<Song>();
            foreach (var s in Many(sr["topSongs"], "song")) list.Add(ParseSong(s));
            return list;
        }

        // ----------------------------------------------------------- genres

        public async Task<List<Genre>> GetGenresAsync()
        {
            var sr = await Rest("getGenres");
            var list = new List<Genre>();
            foreach (var g in Many(sr["genres"], "genre"))
            {
                list.Add(new Genre
                {
                    Name = g.Value<string>("value") ?? g.Value<string>("name"),
                    SongCount = g.Value<int?>("songCount"),
                    AlbumCount = g.Value<int?>("albumCount"),
                });
            }
            return list;
        }

        public async Task<List<Song>> GetSongsByGenreAsync(string genre, int count = 100)
        {
            var sr = await Rest("getSongsByGenre", "&genre=" + Uri.EscapeDataString(genre) + "&count=" + count);
            var list = new List<Song>();
            foreach (var s in Many(sr["songsByGenre"], "song")) list.Add(ParseSong(s));
            return list;
        }

        public async Task<List<Song>> GetRandomSongsAsync(int count = 100, string genre = null)
        {
            string extra = "&size=" + count;
            if (!string.IsNullOrEmpty(genre)) extra += "&genre=" + Uri.EscapeDataString(genre);
            var sr = await Rest("getRandomSongs", extra);
            var list = new List<Song>();
            foreach (var s in Many(sr["randomSongs"], "song")) list.Add(ParseSong(s));
            return list;
        }

        // ----------------------------------------------------------- playlists

        public async Task<List<Playlist>> GetPlaylistsAsync()
        {
            var sr = await Rest("getPlaylists");
            var list = new List<Playlist>();
            foreach (var p in Many(sr["playlists"], "playlist")) list.Add(ParsePlaylist(p));
            return list;
        }

        public async Task<Playlist> GetPlaylistAsync(string id)
        {
            var sr = await Rest("getPlaylist", "&id=" + Uri.EscapeDataString(id));
            var node = sr["playlist"] as JObject;
            if (node == null) return null;
            var pl = ParsePlaylist(node);
            foreach (var s in Many(node, "entry")) pl.Songs.Add(ParseSong(s));
            return pl;
        }

        public async Task<string> CreatePlaylistAsync(string name, IEnumerable<string> songIds = null)
        {
            string extra = "&name=" + Uri.EscapeDataString(name);
            if (songIds != null) foreach (var id in songIds) extra += "&songId=" + Uri.EscapeDataString(id);
            var sr = await Rest("createPlaylist", extra);
            return (sr["playlist"] as JObject)?.Value<string>("id");
        }

        public Task AddToPlaylistAsync(string playlistId, IEnumerable<string> songIds)
        {
            string extra = "&playlistId=" + Uri.EscapeDataString(playlistId);
            foreach (var id in songIds) extra += "&songIdToAdd=" + Uri.EscapeDataString(id);
            return Rest("updatePlaylist", extra);
        }

        public Task RemoveFromPlaylistAsync(string playlistId, int index)
            => Rest("updatePlaylist", "&playlistId=" + Uri.EscapeDataString(playlistId) + "&songIndexToRemove=" + index);

        public Task DeletePlaylistAsync(string id) => Rest("deletePlaylist", "&id=" + Uri.EscapeDataString(id));

        // ----------------------------------------------------------- search & starred

        public async Task<SearchResults> Search3Async(string query, int count = 20)
        {
            var sr = await Rest("search3",
                "&query=" + Uri.EscapeDataString(query) +
                $"&artistCount={count}&albumCount={count}&songCount={count}");
            var node = sr["searchResult3"];
            var res = new SearchResults();
            if (node == null) return res;
            foreach (var a in Many(node, "artist")) res.Artists.Add(ParseArtist(a));
            foreach (var a in Many(node, "album")) res.Albums.Add(ParseAlbum(a));
            foreach (var s in Many(node, "song")) res.Songs.Add(ParseSong(s));
            return res;
        }

        public async Task<SearchResults> GetStarred2Async()
        {
            var sr = await Rest("getStarred2");
            var node = sr["starred2"];
            var res = new SearchResults();
            if (node == null) return res;
            foreach (var a in Many(node, "artist")) res.Artists.Add(ParseArtist(a));
            foreach (var a in Many(node, "album")) res.Albums.Add(ParseAlbum(a));
            foreach (var s in Many(node, "song")) res.Songs.Add(ParseSong(s));
            return res;
        }

        // ----------------------------------------------------------- mutations

        public Task StarAsync(string id, bool star, string kind = "id")
        {
            // kind: "id" (song/album), "albumId", or "artistId"
            string method = star ? "star" : "unstar";
            return Rest(method, "&" + kind + "=" + Uri.EscapeDataString(id));
        }

        public Task SetRatingAsync(string id, int rating)
            => Rest("setRating", "&id=" + Uri.EscapeDataString(id) + "&rating=" + rating);

        public async Task ScrobbleAsync(string id, bool submission)
        {
            try { await Rest("scrobble", "&id=" + Uri.EscapeDataString(id) + "&submission=" + (submission ? "true" : "false")); }
            catch { /* scrobbling is best-effort */ }
        }
    }
}
