using System;
using System.Collections.Generic;
using NavidromeXbox.Helpers;

namespace NavidromeXbox.Models
{
    /// <summary>
    /// DTOs for the subset of the Subsonic / OpenSubsonic API that Navidrome serves.
    /// Bound members are properties (UWP {Binding} ignores fields). Cover-art and a few
    /// display helpers are computed so XAML can bind them directly.
    /// </summary>

    public class SubsonicUser
    {
        public string Username { get; set; }
        public string ServerName { get; set; }   // host shown in the UI
        public string ServerVersion { get; set; }
        public string Initial => string.IsNullOrEmpty(Username) ? "?" : Username.Substring(0, 1).ToUpperInvariant();
    }

    /// <summary>An item that has cover art — albums, songs, artists, playlists all share this shape.</summary>
    public abstract class CoverArtItem
    {
        public string Id { get; set; }
        public string CoverArt { get; set; }

        /// <summary>Grid-tile sized cover (300px). Falls back to a neutral placeholder when there's no art.</summary>
        public Uri CoverArtUri => BuildCover(300);
        /// <summary>Large cover for the now-playing / detail hero (640px).</summary>
        public Uri CoverArtLargeUri => BuildCover(640);

        Uri BuildCover(int size)
        {
            var url = Services.AppState.Current.Api?.CoverArtUrl(CoverArt, size);
            return url != null ? new Uri(url) : null;
        }
    }

    public class Artist : CoverArtItem
    {
        public string Name { get; set; }
        public int? AlbumCount { get; set; }
        public bool Starred { get; set; }
        public string Subtitle => AlbumCount.HasValue ? Format.TrackCount(AlbumCount.Value).Replace("track", "album") : "";
    }

    /// <summary>A letter group in the artist index (getArtists).</summary>
    public class ArtistIndexGroup
    {
        public string Key { get; set; }
        public List<Artist> Items { get; set; } = new List<Artist>();
    }

    public class Album : CoverArtItem
    {
        public string Name { get; set; }
        public string ArtistName { get; set; }
        public string ArtistId { get; set; }
        public int? Year { get; set; }
        public int? SongCount { get; set; }
        public int? DurationSeconds { get; set; }
        public string Genre { get; set; }
        public bool Starred { get; set; }
        public int? PlayCount { get; set; }
        public List<Song> Songs { get; set; } = new List<Song>();

        public string YearText => Format.Year(Year);
        public string Meta
        {
            get
            {
                var parts = new List<string>();
                if (Year.HasValue && Year > 0) parts.Add(Year.Value.ToString());
                if (SongCount.HasValue) parts.Add(Format.TrackCount(SongCount.Value));
                var dur = Format.LongDuration(DurationSeconds);
                if (!string.IsNullOrEmpty(dur)) parts.Add(dur);
                return string.Join("  •  ", parts);
            }
        }
    }

    public class Song : CoverArtItem
    {
        public string Title { get; set; }
        public string AlbumName { get; set; }
        public string AlbumId { get; set; }
        public string ArtistName { get; set; }
        public string ArtistId { get; set; }
        public int? Track { get; set; }
        public int? DiscNumber { get; set; }
        public int? Year { get; set; }
        public string Genre { get; set; }
        public int? DurationSeconds { get; set; }
        public int? BitRate { get; set; }
        public string Suffix { get; set; }     // file format, e.g. "flac"
        public bool Starred { get; set; }
        public int? Rating { get; set; }

        /// <summary>For internet radio: a ready-to-play URL used instead of the /rest/stream endpoint.</summary>
        public string StreamOverride { get; set; }
        public bool IsRadio => !string.IsNullOrEmpty(StreamOverride);

        public string DurationText => IsRadio ? "Live" : Format.Duration(DurationSeconds);
        public string TrackText => Track.HasValue && Track > 0 ? Track.Value.ToString() : "•";
        public string ArtistAndAlbum =>
            string.IsNullOrEmpty(AlbumName) ? ArtistName : $"{ArtistName}  —  {AlbumName}";
    }

    public class Playlist : CoverArtItem
    {
        public string Name { get; set; }
        public string Comment { get; set; }
        public string Owner { get; set; }
        public int? SongCount { get; set; }
        public int? DurationSeconds { get; set; }
        public bool Public { get; set; }
        public List<Song> Songs { get; set; } = new List<Song>();

        public string Meta
        {
            get
            {
                var parts = new List<string>();
                if (SongCount.HasValue) parts.Add(Format.TrackCount(SongCount.Value));
                var dur = Format.LongDuration(DurationSeconds);
                if (!string.IsNullOrEmpty(dur)) parts.Add(dur);
                return string.Join("  •  ", parts);
            }
        }
    }

    /// <summary>A user-configured internet radio station (getInternetRadioStations).</summary>
    public class RadioStation
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string StreamUrl { get; set; }
        public string HomepageUrl { get; set; }
        public string Subtitle => string.IsNullOrWhiteSpace(HomepageUrl) ? "Internet radio" : HomepageUrl;
    }

    public class Genre
    {
        public string Name { get; set; }
        public int? AlbumCount { get; set; }
        public int? SongCount { get; set; }
        public string Subtitle
        {
            get
            {
                var parts = new List<string>();
                if (AlbumCount.HasValue) parts.Add($"{AlbumCount} albums");
                if (SongCount.HasValue) parts.Add($"{SongCount} songs");
                return string.Join("  •  ", parts);
            }
        }
    }

    /// <summary>Result of search3 — split into the three entity kinds.</summary>
    public class SearchResults
    {
        public List<Artist> Artists { get; set; } = new List<Artist>();
        public List<Album> Albums { get; set; } = new List<Album>();
        public List<Song> Songs { get; set; } = new List<Song>();
        public bool IsEmpty => Artists.Count == 0 && Albums.Count == 0 && Songs.Count == 0;
    }

    public class ArtistInfo
    {
        public string Biography { get; set; }
        public Uri ImageUri { get; set; }
        public List<Artist> Similar { get; set; } = new List<Artist>();
    }
}
