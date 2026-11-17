using System.Collections.Generic;
using UnityEngine;

namespace SHHH.Game
{
    public enum AudioBus { Sfx, Voice, Music, Ambience, UI }

    /// <summary>
    /// Pooled spatial audio. Clips live in Resources/Audio as "<key>_<n>" variants (random pick, no immediate repeat).
    /// Playback loudness is presentation only — gameplay noise is computed by the host noise model, never from audio.
    /// </summary>
    public sealed class AudioBank : MonoBehaviour
    {
        public static AudioBank I { get; private set; }
        readonly Dictionary<string, AudioClip[]> _clips = new Dictionary<string, AudioClip[]>();
        readonly Dictionary<string, int> _last = new Dictionary<string, int>();
        readonly List<AudioSource> _pool = new List<AudioSource>();
        AudioSource _music, _ambience;
        int _next;

        public static float Master = 1f;
        /// <summary>QA (Editor only): every sound played - time, key, clip, bus, volume, distance, busy sources, voice it cut off.
        /// Written to Automation/audio_trace.csv when play mode ends.</summary>
        public static List<string> Trace;
        void OnDestroy()
        {
            if (!Application.isEditor || Trace == null || Trace.Count == 0) return;
            try { System.IO.File.WriteAllLines(System.IO.Path.Combine(Application.dataPath, "../../Automation/audio_trace.csv"), Trace); } catch { }
        }
        /// <summary>QA: how often each sound group actually played (clip found) this session.</summary>
        public static readonly Dictionary<string, int> Played = new Dictionary<string, int>();
        public static readonly float[] BusVolume = { 1f, 1f, 0.5f, 0.6f, 0.8f };

        void Awake()
        {
            I = this; if (Application.isEditor) Trace = new List<string> { "t,key,clip,bus,vol,dist,busy,cut" };
            BootTimeline.Mark("audio bank: loading clips");
            foreach (var c in Resources.LoadAll<AudioClip>("Audio"))
            {
                string n = c.name; int us = n.LastIndexOf('_');
                string key = us > 0 && int.TryParse(n.Substring(us + 1), out _) ? n.Substring(0, us) : n;
                _clips.TryGetValue(key, out var arr);
                var list = arr != null ? new List<AudioClip>(arr) : new List<AudioClip>();
                list.Add(c); _clips[key] = list.ToArray();
            }
            BootTimeline.Mark($"audio bank: {_clips.Count} sounds loaded");
            for (int i = 0; i < 24; i++)
            {
                var g = new GameObject("sfx" + i); g.transform.SetParent(transform, false);
                var s = g.AddComponent<AudioSource>();
                s.playOnAwake = false; s.spatialBlend = 1f; s.rolloffMode = AudioRolloffMode.Logarithmic; s.minDistance = 0.6f; s.maxDistance = 18f; s.dopplerLevel = 0f;
                _pool.Add(s);
            }
            _music = MakeLoop("music"); _ambience = MakeLoop("ambience");
            Debug.Log($"[SHHH] AudioBank loaded {_clips.Count} sound groups");
        }

        AudioSource MakeLoop(string n)
        {
            var g = new GameObject(n); g.transform.SetParent(transform, false);
            var s = g.AddComponent<AudioSource>(); s.loop = true; s.spatialBlend = 0f; s.playOnAwake = false; return s;
        }

        public bool Has(string key) => _clips.ContainsKey(key);

        /// <summary>Random variant; never one of the last two played (when there are 4+), never the last one (2-3 variants).</summary>
        public AudioClip Pick(string key)
        {
            if (!_clips.TryGetValue(key, out var arr) || arr.Length == 0) return null;
            _last.TryGetValue(key, out int l1); _last2.TryGetValue(key, out int l2);
            int i = Random.Range(0, arr.Length);
            for (int k = 0; k < arr.Length && arr.Length > 1 && (i == l1 - 1 || (arr.Length >= 4 && i == l2 - 1)); k++) i = (i + 1) % arr.Length;
            _last2[key] = l1; _last[key] = i + 1; return arr[i];
        }
        readonly Dictionary<string, int> _last2 = new Dictionary<string, int>();

        // ------------------------------------------------------------------ V13 sound budget (owner 2026-11-15: "a lot of repetition in
        // the sfx and voice overs, and collisions of sounds ... too much noise"). Presentation only: gameplay noise (Mom's meter) is
        // computed by the host noise model and never depends on what is audible. Per key: a minimum gap between starts and a cap on
        // copies playing at once; at most MaxVoices sources at once; a new sound takes a free source, else replaces the quietest
        // effect - never a voice line (voices used to be cut off by the round-robin pool).
        static readonly (string prefix, float gap, int max)[] Limits =
        {
            ("mom_wake_stinger", 45f, 1), ("inspect_stinger", 20f, 1), ("countdown", 3f, 1),
            ("door_creak", 3f, 1), ("door_close_soft", 1f, 1), ("lid_open", 0.8f, 1), ("lid_close", 0.8f, 1),
            ("cat_meow", 2.5f, 1), ("dog_bark", 2.5f, 1), ("dog_huff", 2.5f, 1), ("bot_giggle", 4f, 1),
            ("chore_done", 1f, 1), ("step_done", 0.3f, 1), ("recipe_undone", 1.5f, 1), ("trap_set", 1f, 1), ("card_show", 0.3f, 1), ("point_at", 0.3f, 1),
            ("slot_click", 0.12f, 2), ("grab", 0.1f, 2), ("ui_click", 0.05f, 2), ("ui_hover", 0.08f, 1),
            ("impact_", 0.04f, 3), ("wipe", 0.15f, 2), ("squeak", 0.3f, 1), ("sponge", 0.2f, 1), ("rustle", 0.25f, 2), ("splash", 0.2f, 2), ("pour", 0.3f, 1),
        };
        public const int MaxVoices = 12;
        readonly Dictionary<string, float> _lastAt = new Dictionary<string, float>();
        string[] _srcKey; AudioBus[] _srcBus; float[] _srcVol;
        /// <summary>QA: sounds skipped by the budget this session, per key.</summary>
        public static readonly Dictionary<string, int> Skipped = new Dictionary<string, int>();
        /// <summary>Mom (or another voice line) is speaking until this time: music dips meanwhile.</summary>
        public static float VoiceUntil;

        static (float gap, int max) LimitFor(string key)
        {
            foreach (var l in Limits) if (key.StartsWith(l.prefix)) return (l.gap, l.max);
            return (0f, 0);
        }

        bool Skip(string key, string why)
        {
            Skipped.TryGetValue(key, out var n); Skipped[key] = n + 1;
            Trace?.Add($"{Time.time:0.00},SKIP:{key},{why},,,,,"); return false;
        }

        int SourceFor(AudioBus bus, float vol)
        {
            if (_srcKey == null) { _srcKey = new string[_pool.Count]; _srcBus = new AudioBus[_pool.Count]; _srcVol = new float[_pool.Count]; }
            int playing = 0; foreach (var q in _pool) if (q.isPlaying) playing++;
            if (playing < MaxVoices)
                for (int k = 0; k < _pool.Count; k++) { int i = (_next + k) % _pool.Count; if (!_pool[i].isPlaying) { _next = (i + 1) % _pool.Count; return i; } }
            // full: replace the quietest effect (never a voice line; a quieter new effect is the one skipped)
            int best = -1; float bv = float.MaxValue;
            for (int i = 0; i < _pool.Count; i++)
                if (_pool[i].isPlaying && _srcBus[i] != AudioBus.Voice && _srcVol[i] < bv) { bv = _srcVol[i]; best = i; }
            if (best < 0 || (bus != AudioBus.Voice && bv > vol)) return -1;
            return best;
        }

        public AudioSource Play(string key, Vector3 pos, float volume = 1f, float pitchJitter = 0.06f, AudioBus bus = AudioBus.Sfx, bool spatial = true)
        {
            if (!_clips.ContainsKey(key)) return null;
            var lim = LimitFor(key);
            if (lim.gap > 0f && _lastAt.TryGetValue(key, out var la) && Time.time - la < lim.gap) { Skip(key, "gap"); return null; }
            if (lim.max > 0 && _srcKey != null) { int same = 0; for (int i = 0; i < _pool.Count; i++) if (_pool[i].isPlaying && _srcKey[i] == key) same++; if (same >= lim.max) { Skip(key, "copies"); return null; } }
            int si = SourceFor(bus, volume); if (si < 0) { Skip(key, "busy"); return null; }
            var clip = Pick(key); if (clip == null) return null;
            Played.TryGetValue(key, out var pc); Played[key] = pc + 1; _lastAt[key] = Time.time;
            var s = _pool[si];
            if (Trace != null) { var cam = Camera.main; int busy = 0; foreach (var q in _pool) if (q.isPlaying) busy++;
                Trace.Add($"{Time.time:0.00},{key},{clip.name},{bus},{volume:0.00},{(spatial && cam != null ? Vector3.Distance(cam.transform.position, pos) : 0f):0.0},{busy},{(s.isPlaying && s.clip != null ? "CUT:" + s.clip.name : "")}"); }
            s.Stop(); s.transform.position = pos; s.clip = clip;
            s.spatialBlend = spatial ? 1f : 0f;
            s.volume = Mathf.Clamp01(volume) * BusVolume[(int)bus] * Master;
            // animals and voices vary more so repeats sound less identical
            bool critter = key.StartsWith("cat_") || key.StartsWith("dog_") || key.StartsWith("bot_");
            s.pitch = 1f + Random.Range(-1f, 1f) * (critter ? Mathf.Max(pitchJitter, 0.12f) : pitchJitter);
            _srcKey[si] = key; _srcBus[si] = bus; _srcVol[si] = volume;
            if (bus == AudioBus.Voice) VoiceUntil = Mathf.Max(VoiceUntil, Time.time + clip.length / Mathf.Max(0.5f, s.pitch));
            s.Play();
            return s;
        }

        public void Play2D(string key, float volume = 1f, AudioBus bus = AudioBus.UI) => Play(key, Vector3.zero, volume, 0.02f, bus, false);

        public void SetMusic(string key) { SetLoop(_music, key, AudioBus.Music); }
        public void SetAmbience(string key) { SetLoop(_ambience, key, AudioBus.Ambience); }

        void SetLoop(AudioSource s, string key, AudioBus bus)
        {
            var clip = key != null ? Pick(key) : null;
            if (clip == null) { s.Stop(); s.clip = null; return; }
            if (s.clip == clip && s.isPlaying) return;
            s.clip = clip; s.volume = BusVolume[(int)bus] * Master; s.Play();
        }

        // ------------------------------------------------------------------ V13 match music (owner 2026-11-15)
        // Licensed tracks by Kevin MacLeod (incompetech.com, CC BY 4.0 - credit in Docs/SHHH_ASSET_LICENSES.md). The menu has its own track
        // (music_menu, never used in a match). Each match starts with the NEXT gameplay track (music_play_N, rotation kept across sessions)
        // and moves on to the following one when a track ends; the final minute crossfades to the fast track (music_hurry, looped).
        // Mom awake ducks the music a little; the final inspection / result drops it low. Two sources crossfade.
        AudioSource[] _deck; int _cur; bool _matchMusic, _hurry; int _playPos; float _duck = 1f, _startedAt;
        readonly float[] _deckVol = new float[2];
        bool _lastMinuteSaid, _inspectPlayed;
        public string CurrentTrack => _deck != null && _deck[_cur].clip != null ? _deck[_cur].clip.name : null;

        public void SetMatchMusic(bool on)
        {
            if (on == _matchMusic) return;
            _matchMusic = on; _lastMinuteSaid = false; _inspectPlayed = false; _hurry = false; _duck = 1f;
            if (_deck == null) { _deck = new AudioSource[2]; for (int i = 0; i < 2; i++) { _deck[i] = MakeLoop("track" + i); _deck[i].loop = false; } }
            if (on)
            {
                SetMusic(null);
                _clips.TryGetValue("music_play", out var plays); int n = plays != null ? plays.Length : 0;
                _playPos = n > 0 ? (PlayerPrefs.GetInt("shhh_music_next", 0) % n) : 0;
                if (n > 0) { PlayerPrefs.SetInt("shhh_music_next", (_playPos + 1) % n); PlayerPrefs.Save(); }
                StartTrack(PlayClip(_playPos), false, true);
            }
            else foreach (var d in _deck) { d.Stop(); d.clip = null; }
        }

        AudioClip PlayClip(int i)
        {
            if (!_clips.TryGetValue("music_play", out var arr) || arr.Length == 0) return null;
            System.Array.Sort(arr, (a, b) => string.CompareOrdinal(a.name, b.name));
            return arr[((i % arr.Length) + arr.Length) % arr.Length];
        }

        void StartTrack(AudioClip c, bool loop, bool instant = false)
        {
            if (c == null) return;
            int o = 1 - _cur; var s = _deck[o];
            s.clip = c; s.loop = loop; s.time = 0f; s.Play();
            _deckVol[o] = instant ? 1f : 0f; if (instant) _deckVol[_cur] = 0f;
            _cur = o; _startedAt = Time.unscaledTime; Played.TryGetValue("music_track", out var pc); Played["music_track"] = pc + 1;
            Debug.Log($"[SHHH] music track: {c.name}{(loop ? " (loop)" : "")}");
        }

        void UpdateMatchMusic()
        {
            if (!_matchMusic || _deck == null) return;
            var r = GameServices.Round; float target = 1f;
            if (r != null)
            {
                bool awake = r.Mom.State != SHHH.Core.MomState.Sleeping;
                bool final = r.TimeRemaining <= 60f && r.Phase == SHHH.Core.RoundPhase.Playing;
                if (final && !_hurry) { _hurry = true; var h = Pick("music_hurry"); if (h != null) StartTrack(h, true); }
                target = awake ? 0.7f : 1f;
                if (r.FinalInspection || r.Phase == SHHH.Core.RoundPhase.Resolved) target = 0.35f;
                // presentation cues (every peer derives them from the replicated clock / Mom state)
                if (!_lastMinuteSaid && final && r.TimeRemaining > 55f) { _lastMinuteSaid = true; GameServices.Room?.Mom?.Say("mom_lastminute_" + Random.Range(1, 3), null); Play2D("countdown", 0.6f); }
                if (!_inspectPlayed && r.FinalInspection) { _inspectPlayed = true; Play2D("inspect_stinger", 0.8f); }
            }
            // a gameplay track that is about to end hands over to the next one
            var cur = _deck[_cur];
            if (!_hurry && cur.clip != null && !cur.loop && (cur.time >= cur.clip.length - 1.5f || (!cur.isPlaying && Time.unscaledTime - _startedAt > 3f)))
                StartTrack(PlayClip(++_playPos), false);
            if (Time.time < VoiceUntil) target *= 0.6f;   // V13: the music dips while Mom (or a voice line) speaks
            _duck = Mathf.MoveTowards(_duck, target, Time.unscaledDeltaTime * 0.8f);
            for (int i = 0; i < 2; i++)
            {
                _deckVol[i] = Mathf.MoveTowards(_deckVol[i], i == _cur ? 1f : 0f, Time.unscaledDeltaTime * 0.7f);
                _deck[i].volume = _deckVol[i] * _duck * BusVolume[(int)AudioBus.Music] * Master;
                if (i != _cur && _deckVol[i] <= 0f && _deck[i].isPlaying) _deck[i].Stop();
            }
        }
        void Update()
        {
            if (_music.clip != null) _music.volume = BusVolume[(int)AudioBus.Music] * Master;
            if (_ambience.clip != null) _ambience.volume = BusVolume[(int)AudioBus.Ambience] * Master;
            UpdateMatchMusic();
        }

        public static string MaterialKey(SHHH.Core.NoiseMaterial m) => "impact_" + m.ToString().ToLowerInvariant();
    }
}
