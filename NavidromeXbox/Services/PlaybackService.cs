using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NavidromeXbox.Models;
using NavidromeXbox.ViewModels;
using Newtonsoft.Json;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace NavidromeXbox.Services
{
    public enum RepeatMode { Off, All, One }

    /// <summary>
    /// The single audio engine for the app. Wraps a <see cref="MediaPlayer"/> driving a
    /// <see cref="MediaPlaybackList"/> (gapless queue), wires the System Media Transport
    /// Controls so the Xbox guide / remote can play-pause-skip, tracks the now-playing
    /// song + position for the UI, handles shuffle/repeat, and scrobbles plays back to
    /// Navidrome. Bindable via <see cref="ObservableObject"/>.
    /// </summary>
    public sealed class PlaybackService : ObservableObject
    {
        readonly MediaPlayer _player;
        readonly MediaPlaybackList _list = new MediaPlaybackList();
        readonly List<Song> _songs = new List<Song>();          // parallel to _list.Items (unshuffled order)
        readonly DispatcherTimer _tick = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        CoreDispatcher _dispatcher;

        // scrobble bookkeeping
        Song _scrobbleCandidate;
        double _lastPositionSeconds;

        public ObservableCollection<Song> Queue { get; } = new ObservableCollection<Song>();

        public PlaybackService()
        {
            _player = new MediaPlayer
            {
                AudioCategory = MediaPlayerAudioCategory.Media,
                AutoPlay = true,
            };
            _list.MaxPlayedItemsToKeepOpen = 3;
            _player.Source = _list;
            _player.CommandManager.IsEnabled = true;

            _list.CurrentItemChanged += OnCurrentItemChanged;
            _player.PlaybackSession.PlaybackStateChanged += OnPlaybackStateChanged;
            _player.MediaEnded += OnMediaEnded;

            _tick.Tick += (s, e) => UpdatePosition();
            _tick.Start();
        }

        void CaptureDispatcher()
        {
            if (_dispatcher == null)
            {
                try { _dispatcher = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher; }
                catch { }
            }
        }

        void RaiseOnUi(Action a)
        {
            CaptureDispatcher();
            if (_dispatcher != null) _ = _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => a());
            else a();
        }

        // ----------------------------------------------------------- bindable state

        Song _currentSong;
        public Song CurrentSong { get => _currentSong; private set { Set(ref _currentSong, value); Raise(nameof(HasCurrent)); } }
        public bool HasCurrent => _currentSong != null;

        public bool IsPlaying => _player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing;

        TimeSpan _position;
        public TimeSpan Position { get => _position; private set => Set(ref _position, value); }

        TimeSpan _duration;
        public TimeSpan Duration { get => _duration; private set => Set(ref _duration, value); }

        public double PositionSeconds => _position.TotalSeconds;
        public double DurationSeconds => _duration.TotalSeconds > 0 ? _duration.TotalSeconds : 1;

        RepeatMode _repeat = RepeatMode.Off;
        public RepeatMode Repeat
        {
            get => _repeat;
            private set { if (Set(ref _repeat, value)) { Raise(nameof(RepeatGlyph)); Raise(nameof(RepeatActive)); } }
        }
        public string RepeatGlyph => _repeat == RepeatMode.One ? "\uE8ED" /* RepeatOne */ : "\uE8EE" /* RepeatAll */;
        public bool RepeatActive => _repeat != RepeatMode.Off;

        public bool Shuffle
        {
            get => _list.ShuffleEnabled;
            private set { _list.ShuffleEnabled = value; Raise(nameof(Shuffle)); }
        }

        public string PlayPauseGlyph => IsPlaying ? "\uE769" /* Pause */ : "\uE768" /* Play */;

        // ----------------------------------------------------------- queue building

        MediaPlaybackItem BuildItem(Song song)
        {
            // Radio stations carry a ready-to-play URL; everything else streams via /rest/stream.
            var url = song.IsRadio ? song.StreamOverride : AppState.Current.Api.StreamUrl(song.Id);
            var source = MediaSource.CreateFromUri(new Uri(url));
            var item = new MediaPlaybackItem(source);

            var props = item.GetDisplayProperties();
            props.Type = MediaPlaybackType.Music;
            props.MusicProperties.Title = song.Title ?? "";
            props.MusicProperties.Artist = song.ArtistName ?? "";
            props.MusicProperties.AlbumTitle = song.AlbumName ?? "";
            if (song.Track.HasValue) props.MusicProperties.TrackNumber = (uint)song.Track.Value;
            var cover = AppState.Current.Api.CoverArtUrl(song.CoverArt, 600);
            if (cover != null) props.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(cover));
            item.ApplyDisplayProperties(props);
            return item;
        }

        /// <summary>Replace the queue with these songs and start at <paramref name="startIndex"/>.</summary>
        public void PlayQueue(IList<Song> songs, int startIndex = 0)
        {
            if (songs == null || songs.Count == 0) return;
            CaptureDispatcher();

            _list.Items.Clear();
            _songs.Clear();
            Queue.Clear();
            foreach (var s in songs)
            {
                _songs.Add(s);
                Queue.Add(s);
                _list.Items.Add(BuildItem(s));
            }

            uint idx = (uint)Math.Max(0, Math.Min(startIndex, songs.Count - 1));
            try { _list.MoveTo(idx); } catch { }
            _player.Play();
        }

        public void PlaySong(Song song) => PlayQueue(new List<Song> { song }, 0);

        /// <summary>Play a single internet-radio station (a continuous live stream).</summary>
        public void PlayRadio(RadioStation station)
        {
            if (station == null || string.IsNullOrEmpty(station.StreamUrl)) return;
            PlaySong(new Song
            {
                Id = station.Id ?? station.StreamUrl,
                Title = station.Name,
                ArtistName = "Internet radio",
                StreamOverride = station.StreamUrl,
            });
        }

        /// <summary>Enqueue at the end.</summary>
        public void AddToQueue(Song song)
        {
            _songs.Add(song);
            Queue.Add(song);
            _list.Items.Add(BuildItem(song));
            if (_list.Items.Count == 1) _player.Play();
        }

        public void AddToQueue(IEnumerable<Song> songs) { foreach (var s in songs) AddToQueue(s); }

        /// <summary>Insert right after the current track.</summary>
        public void PlayNext(Song song)
        {
            int cur = _list.CurrentItemIndex == uint.MaxValue ? _list.Items.Count - 1 : (int)_list.CurrentItemIndex;
            int at = Math.Max(0, Math.Min(cur + 1, _list.Items.Count));
            _songs.Insert(at, song);
            Queue.Insert(at, song);
            _list.Items.Insert(at, BuildItem(song));
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _list.Items.Count) return;
            if (index == (int)_list.CurrentItemIndex) return; // don't yank the playing track
            _list.Items.RemoveAt(index);
            _songs.RemoveAt(index);
            Queue.RemoveAt(index);
        }

        public void ClearQueue()
        {
            _player.Pause();
            _list.Items.Clear();
            _songs.Clear();
            Queue.Clear();
            CurrentSong = null;
        }

        // ----------------------------------------------------------- transport

        public void TogglePlayPause()
        {
            if (IsPlaying) _player.Pause(); else _player.Play();
        }

        public void Play() => _player.Play();
        public void Pause() => _player.Pause();
        public void Next() { try { _list.MoveNext(); } catch { } }

        /// <summary>Jump to a specific position in the queue (used by the Queue view).</summary>
        public void JumpTo(int index)
        {
            if (index < 0 || index >= _list.Items.Count) return;
            try { _list.MoveTo((uint)index); _player.Play(); } catch { }
        }

        public void Previous()
        {
            // Restart the track if we're more than 3s in; otherwise go to the previous one.
            if (_player.PlaybackSession.Position.TotalSeconds > 3) _player.PlaybackSession.Position = TimeSpan.Zero;
            else { try { _list.MovePrevious(); } catch { } }
        }

        public void SeekTo(double seconds)
        {
            try { _player.PlaybackSession.Position = TimeSpan.FromSeconds(seconds); } catch { }
        }

        public void SeekRelative(double deltaSeconds)
        {
            var pos = _player.PlaybackSession.Position.TotalSeconds + deltaSeconds;
            SeekTo(Math.Max(0, pos));
        }

        public void ToggleShuffle() => Shuffle = !Shuffle;

        public void CycleRepeat()
        {
            switch (_repeat)
            {
                case RepeatMode.Off: Repeat = RepeatMode.All; break;
                case RepeatMode.All: Repeat = RepeatMode.One; break;
                default: Repeat = RepeatMode.Off; break;
            }
            // MediaPlayer.IsLoopingEnabled is ignored when the source is a MediaPlaybackList,
            // so One keeps the list looping and bounces back in OnCurrentItemChanged instead.
            _list.AutoRepeatEnabled = _repeat != RepeatMode.Off;
        }

        // ----------------------------------------------------------- events

        void OnPlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            RaiseOnUi(() => { Raise(nameof(IsPlaying)); Raise(nameof(PlayPauseGlyph)); });
        }

        /// <summary>Fires when the whole list finishes (no auto-repeat) — settle the last track's scrobble.</summary>
        void OnMediaEnded(MediaPlayer sender, object args)
        {
            ScrobbleOutgoing();
            RaiseOnUi(() => { Raise(nameof(IsPlaying)); Raise(nameof(PlayPauseGlyph)); });
        }

        /// <summary>Submit the track we're leaving if it played long enough (Last.fm rules: 4 min or half).</summary>
        void ScrobbleOutgoing()
        {
            var outgoing = _scrobbleCandidate;
            _scrobbleCandidate = null;
            double played = _lastPositionSeconds;
            if (outgoing != null && !outgoing.IsRadio && Settings.ScrobbleEnabled)
            {
                double dur = outgoing.DurationSeconds ?? 0;
                bool enough = played >= 240 || (dur > 0 && played >= dur * 0.5);
                if (enough) _ = AppState.Current.Api.ScrobbleAsync(outgoing.Id, true);
            }
        }

        void OnCurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {
            ScrobbleOutgoing();

            // Repeat-one: when a track ends naturally, jump back to it instead of advancing.
            // (Manual skips arrive with Reason = AppRequested and pass through.)
            if (_repeat == RepeatMode.One &&
                args.Reason == MediaPlaybackItemChangedReason.EndOfStream &&
                args.OldItem != null && args.OldItem != args.NewItem)
            {
                int back = _list.Items.IndexOf(args.OldItem);
                if (back >= 0)
                {
                    try { _list.MoveTo((uint)back); return; } catch { }
                }
            }

            Song song = null;
            if (args.NewItem != null)
            {
                int idx = _list.Items.IndexOf(args.NewItem);
                if (idx >= 0 && idx < _songs.Count) song = _songs[idx];
            }

            _scrobbleCandidate = song;
            _lastPositionSeconds = 0;
            if (song != null && !song.IsRadio && Settings.ScrobbleEnabled)
                _ = AppState.Current.Api.ScrobbleAsync(song.Id, false);   // "now playing"

            RaiseOnUi(() =>
            {
                CurrentSong = song;
                Raise(nameof(IsPlaying));
                Raise(nameof(PlayPauseGlyph));
            });
        }

        void UpdatePosition()
        {
            var sess = _player.PlaybackSession;
            if (sess == null) return;
            _lastPositionSeconds = sess.Position.TotalSeconds;
            Position = sess.Position;
            Duration = sess.NaturalDuration;
            Raise(nameof(PositionSeconds));
            Raise(nameof(DurationSeconds));
        }

        // ----------------------------------------------------------- favourite

        public async void ToggleStarCurrent()
        {
            var s = CurrentSong;
            if (s == null) return;
            try { await AppState.Current.Api.StarAsync(s.Id, !s.Starred); s.Starred = !s.Starred; Raise(nameof(CurrentSong)); }
            catch { }
        }

        // ----------------------------------------------------------- persistence

        public void SaveQueue()
        {
            try
            {
                var snapshot = _songs.Select(s => new { s.Id, s.Title, s.AlbumName, s.AlbumId, s.ArtistName, s.ArtistId, s.CoverArt, s.DurationSeconds, s.Track }).ToList();
                ApplicationData.Current.LocalSettings.Values["Queue"] = JsonConvert.SerializeObject(snapshot);
                ApplicationData.Current.LocalSettings.Values["QueueIndex"] =
                    (int)(_list.CurrentItemIndex == uint.MaxValue ? 0 : _list.CurrentItemIndex);
            }
            catch { }
        }
    }
}
