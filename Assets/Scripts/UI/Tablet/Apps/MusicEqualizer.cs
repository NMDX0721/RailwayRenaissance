using System;
using System.Collections.Generic;
using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    // The music app's equalizer and replay gain (after the equalizer dialog and the loudness control of Folia; the code is our own).
    //   - Ten peaking bands from 31 Hz to 16 kHz, each -12..+12 dB, run as biquads in OnAudioFilterRead on both playback decks.
    //   - Replay gain: the loudness of a track is estimated once from a few short windows of its samples, and the deck's gain brings it to a common
    //     level (at most 10 dB down and 6 dB up) so that tracks of very different mastering do not jump in volume.
    public static class MusicEqualizer
    {
        public const int BandCount = 10;
        public static readonly float[] Frequencies = { 31f, 62f, 125f, 250f, 500f, 1000f, 2000f, 4000f, 8000f, 16000f };
        public static readonly string[] BandLabels = { "31", "62", "125", "250", "500", "1k", "2k", "4k", "8k", "16k" };
        public const string CustomName = "自定义";
        public static readonly string[] PresetNames = { "平直", "低音增强", "人声", "明亮", "温暖", "电台", "柔和" };
        static readonly float[][] PresetGains =
        {
            new float[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new float[] { 6, 5, 4, 2, 0, 0, 0, 0, 0, 0 },
            new float[] { -2, -1, 0, 1, 3, 4, 3, 1, 0, -1 },
            new float[] { 0, 0, 0, 0, 0, 1, 2, 4, 5, 6 },
            new float[] { 3, 3, 2, 1, 0, -1, -1, -2, -3, -4 },
            new float[] { -12, -9, -4, 0, 3, 4, 3, 0, -6, -12 },
            new float[] { 2, 1, 0, 0, 0, -1, -2, -3, -4, -5 },
        };
        public const float TargetDb = -17f, MaxCut = -10f, MaxBoost = 6f;

        public static bool Enabled;
        public static bool ReplayGain;
        public static string Preset = PresetNames[0];
        public static readonly float[] Gains = new float[BandCount];

        static readonly Dictionary<AudioClip, float> loudness = new Dictionary<AudioClip, float>();

        public static void Load(MusicLibraryState state)
        {
            Enabled = state.eqOn; ReplayGain = state.replayGain;
            Preset = string.IsNullOrEmpty(state.eqPreset) ? PresetNames[0] : state.eqPreset;
            Array.Clear(Gains, 0, BandCount);
            if (state.eqGains != null) for (int i = 0; i < BandCount && i < state.eqGains.Length; i++) Gains[i] = Mathf.Clamp(state.eqGains[i], -12f, 12f);
            else ChoosePreset(Preset);
        }

        public static void Store(MusicLibraryState state)
        {
            state.eqOn = Enabled; state.replayGain = ReplayGain; state.eqPreset = Preset; state.eqGains = (float[])Gains.Clone();
        }

        public static void ChoosePreset(string name)
        {
            int index = Array.IndexOf(PresetNames, name);
            if (index < 0) return;
            Preset = name; Array.Copy(PresetGains[index], Gains, BandCount);
        }

        public static void SetBand(int band, float db)
        {
            Gains[band] = Mathf.Clamp(Mathf.Round(db * 2f) / 2f, -12f, 12f);
            Preset = CustomName;
            for (int p = 0; p < PresetGains.Length; p++)
            {
                bool same = true;
                for (int i = 0; i < BandCount && same; i++) same = Mathf.Abs(PresetGains[p][i] - Gains[i]) < .01f;
                if (same) { Preset = PresetNames[p]; break; }
            }
        }

        public static string SummaryText()
        {
            string eq = Enabled ? Preset : "关";
            return ReplayGain ? eq + " · 统一响度" : eq;
        }

        // ---- Loudness ----
        // RMS in dBFS of up to eight half-second windows spread over the track; silent windows do not count. NaN when the samples cannot be read.
        public static float MeasureDb(AudioClip clip)
        {
            if (clip == null) return float.NaN;
            if (loudness.TryGetValue(clip, out float known)) return known;
            float result = float.NaN;
            try
            {
                int channels = Mathf.Max(1, clip.channels), window = Mathf.Min(clip.samples, clip.frequency / 2);
                if (window > 0 && clip.samples > 0)
                {
                    var buffer = new float[window * channels];
                    double power = 0; int counted = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        int at = (int)((clip.samples - window) * (.05f + .9f * i / 7f));
                        if (!clip.GetData(buffer, Mathf.Max(0, at))) continue;
                        double sum = 0; for (int k = 0; k < buffer.Length; k++) sum += buffer[k] * buffer[k];
                        double mean = sum / buffer.Length;
                        if (mean < 1e-6) continue;      // below -60 dBFS: a gap, not music
                        power += mean; counted++;
                    }
                    if (counted > 0) result = (float)(10.0 * Math.Log10(power / counted));
                }
            }
            catch (Exception) { result = float.NaN; }
            loudness[clip] = result;
            if (loudness.Count > 64) { loudness.Clear(); loudness[clip] = result; }
            return result;
        }

        public static float GainFor(AudioClip clip)
        {
            if (!ReplayGain) return 1f;
            float db = MeasureDb(clip);
            if (float.IsNaN(db)) return 1f;
            return Mathf.Pow(10f, Mathf.Clamp(TargetDb - db, MaxCut, MaxBoost) / 20f);
        }

        // ---- Deck hookup ----
        public static MusicEqFilter Attach(AudioSource deck)
        {
            if (deck == null) return null;
            var filter = deck.GetComponent<MusicEqFilter>();
            if (filter == null) filter = deck.gameObject.AddComponent<MusicEqFilter>();
            filter.Configure(Enabled ? Gains : null);
            return filter;
        }

        /// <summary>Sets up a deck for the clip it is about to play: the bands and the clip's replay gain.</summary>
        public static void Prepare(AudioSource deck, AudioClip clip)
        {
            var filter = Attach(deck);
            if (filter != null) filter.SetGain(GainFor(clip));
        }
    }

    // Lives next to an AudioSource and filters what it plays; the settings are handed over from the main thread as immutable arrays.
    [DisallowMultipleComponent]
    public sealed class MusicEqFilter : MonoBehaviour
    {
        volatile float[] coefficients;           // 5 per band (b0 b1 b2 a1 a2), normalised; null when no band is moved
        volatile float gain = 1f;
        readonly double[] z1 = new double[2 * MusicEqualizer.BandCount], z2 = new double[2 * MusicEqualizer.BandCount];
        int sampleRate = 48000;

        void Awake() { sampleRate = AudioSettings.outputSampleRate; }

        public void SetGain(float value) { gain = value; }

        public void Configure(float[] gainsDb)
        {
            sampleRate = AudioSettings.outputSampleRate;
            if (sampleRate <= 0) { coefficients = null; return; }
            bool flat = true;
            if (gainsDb != null) foreach (float g in gainsDb) if (Mathf.Abs(g) > .05f) flat = false;
            if (flat) { coefficients = null; return; }
            var next = new float[MusicEqualizer.BandCount * 5];
            for (int band = 0; band < MusicEqualizer.BandCount; band++)
            {
                int o = band * 5;
                if (MusicEqualizer.Frequencies[band] >= sampleRate * .5)
                {
                    next[o] = 1f;
                    continue;
                }
                double a = Math.Pow(10.0, gainsDb[band] / 40.0), w = 2.0 * Math.PI * MusicEqualizer.Frequencies[band] / sampleRate;
                double alpha = Math.Sin(w) / (2.0 * 1.1), cos = Math.Cos(w);
                double b0 = 1 + alpha * a, b1 = -2 * cos, b2 = 1 - alpha * a, a0 = 1 + alpha / a, a1 = -2 * cos, a2 = 1 - alpha / a;
                next[o] = (float)(b0 / a0); next[o + 1] = (float)(b1 / a0); next[o + 2] = (float)(b2 / a0); next[o + 3] = (float)(a1 / a0); next[o + 4] = (float)(a2 / a0);
            }
            coefficients = next;
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            var c = coefficients; float g = gain;
            if (c == null && Mathf.Abs(g - 1f) < .001f) return;
            int frames = data.Length / channels, used = Mathf.Min(channels, 2);
            for (int frame = 0; frame < frames; frame++)
            {
                for (int ch = 0; ch < used; ch++)
                {
                    int i = frame * channels + ch;
                    double x = data[i];
                    if (c != null)
                    {
                        for (int band = 0; band < MusicEqualizer.BandCount; band++)
                        {
                            int o = band * 5, s = ch * MusicEqualizer.BandCount + band;
                            double y = c[o] * x + z1[s];
                            z1[s] = c[o + 1] * x - c[o + 3] * y + z2[s];
                            z2[s] = c[o + 2] * x - c[o + 4] * y;
                            x = y;
                        }
                    }
                    float v = (float)x * g;
                    float mag = v < 0 ? -v : v;
                    if (mag > .9f) { float over = (float)Math.Tanh((mag - .9f) / .1f) * .1f; v = (v < 0 ? -1f : 1f) * (.9f + over); }   // soft ceiling instead of hard clipping
                    data[i] = v;
                }
            }
        }
    }
}
