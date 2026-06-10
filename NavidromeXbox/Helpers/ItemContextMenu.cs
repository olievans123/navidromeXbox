using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NavidromeXbox.Models;
using NavidromeXbox.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace NavidromeXbox.Helpers
{
    /// <summary>
    /// The controller "options" menu. The Menu button asks for a context menu on whatever
    /// item currently has focus; we read its DataContext and build a list of relevant actions
    /// (play, queue, navigate, star). Works against the shared item templates because the
    /// item container's DataContext is always one of our model types.
    /// </summary>
    public static class ItemContextMenu
    {
        static SubsonicApiService Api => AppState.Current.Api;
        static PlaybackService Player => AppState.Current.Playback;

        /// <summary>Show the menu for the focused item. Returns false if nothing actionable is focused.</summary>
        public static bool ShowForFocused()
        {
            if (!(FocusManager.GetFocusedElement() is FrameworkElement focused)) return false;
            var data = FindModel(focused);
            var menu = Build(data);
            if (menu == null) return false;
            try { menu.ShowAt(focused); } catch { return false; }
            return true;
        }

        /// <summary>Walk up from the focused element until we hit an element bound to a known model.</summary>
        static object FindModel(DependencyObject start)
        {
            var el = start;
            while (el != null)
            {
                if (el is FrameworkElement fe && IsKnown(fe.DataContext)) return fe.DataContext;
                el = VisualTreeHelper.GetParent(el);
            }
            return null;
        }

        static bool IsKnown(object o) =>
            o is Song || o is Album || o is Artist || o is Playlist || o is RadioStation;

        static MenuFlyout Build(object data)
        {
            var menu = new MenuFlyout { Placement = FlyoutPlacementMode.Bottom };
            switch (data)
            {
                case Song s:
                    Add(menu, "Play", "", () => Player.PlayQueue(new List<Song> { s }, 0));
                    Add(menu, "Play next", "", () => Player.PlayNext(s));
                    Add(menu, "Add to queue", "", () => Player.AddToQueue(s));
                    if (!string.IsNullOrEmpty(s.AlbumId)) Add(menu, "Go to album", "", () => MainPage.Instance?.OpenAlbum(s.AlbumId));
                    if (!string.IsNullOrEmpty(s.ArtistId)) Add(menu, "Go to artist", "", () => MainPage.Instance?.OpenArtist(s.ArtistId));
                    Add(menu, s.Starred ? "Unstar" : "Star", s.Starred ? "" : "",
                        () => Star(s.Id, !s.Starred, "id", v => s.Starred = v));
                    break;

                case Album a:
                    Add(menu, "Play", "", () => _ = PlayAlbumAsync(a.Id, false));
                    Add(menu, "Shuffle", "", () => _ = PlayAlbumAsync(a.Id, true));
                    Add(menu, "Add to queue", "", () => _ = QueueAlbumAsync(a.Id));
                    if (!string.IsNullOrEmpty(a.ArtistId)) Add(menu, "Go to artist", "", () => MainPage.Instance?.OpenArtist(a.ArtistId));
                    Add(menu, a.Starred ? "Unstar" : "Star", a.Starred ? "" : "",
                        () => Star(a.Id, !a.Starred, "albumId", v => a.Starred = v));
                    break;

                case Playlist p:
                    Add(menu, "Play", "", () => _ = PlayPlaylistAsync(p.Id, false));
                    Add(menu, "Shuffle", "", () => _ = PlayPlaylistAsync(p.Id, true));
                    Add(menu, "Add to queue", "", () => _ = QueuePlaylistAsync(p.Id));
                    break;

                case Artist ar:
                    Add(menu, "Go to artist", "", () => MainPage.Instance?.OpenArtist(ar.Id));
                    Add(menu, ar.Starred ? "Unstar" : "Star", ar.Starred ? "" : "",
                        () => Star(ar.Id, !ar.Starred, "artistId", v => ar.Starred = v));
                    break;

                case RadioStation rs:
                    Add(menu, "Play", "", () => Player.PlayRadio(rs));
                    break;

                default:
                    return null;
            }
            return menu.Items.Count > 0 ? menu : null;
        }

        static void Add(MenuFlyout menu, string text, string glyph, Action onClick)
        {
            var item = new MenuFlyoutItem { Text = text };
            if (!string.IsNullOrEmpty(glyph))
                item.Icon = new FontIcon { Glyph = glyph, FontFamily = new FontFamily("Segoe MDL2 Assets") };
            item.Click += (s, e) => onClick();
            menu.Items.Add(item);
        }

        static async void Star(string id, bool star, string kind, Action<bool> apply)
        {
            try { await Api.StarAsync(id, star, kind); apply(star); } catch { }
        }

        static async Task PlayAlbumAsync(string id, bool shuffle)
        {
            try
            {
                var album = await Api.GetAlbumAsync(id);
                if (album != null && album.Songs.Count > 0)
                {
                    Player.PlayQueue(album.Songs, 0);
                    if (shuffle && !Player.Shuffle) Player.ToggleShuffle();
                }
            }
            catch { }
        }

        static async Task QueueAlbumAsync(string id)
        {
            try
            {
                var album = await Api.GetAlbumAsync(id);
                if (album != null && album.Songs.Count > 0) Player.AddToQueue(album.Songs);
            }
            catch { }
        }

        static async Task PlayPlaylistAsync(string id, bool shuffle)
        {
            try
            {
                var pl = await Api.GetPlaylistAsync(id);
                if (pl != null && pl.Songs.Count > 0)
                {
                    Player.PlayQueue(pl.Songs, 0);
                    if (shuffle && !Player.Shuffle) Player.ToggleShuffle();
                }
            }
            catch { }
        }

        static async Task QueuePlaylistAsync(string id)
        {
            try
            {
                var pl = await Api.GetPlaylistAsync(id);
                if (pl != null && pl.Songs.Count > 0) Player.AddToQueue(pl.Songs);
            }
            catch { }
        }
    }
}
