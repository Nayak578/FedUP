using System.Collections.Generic;
using UnityEngine;

namespace FedUp.Audio
{
    /// <summary>
    /// Greybox placeholder sounds made from sine waves at runtime (no audio files yet).
    /// Real sounds replace these in the art passes.
    /// </summary>
    public static class PlaceholderTones
    {
        const int Rate = 44100;
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        public static AudioClip Get(string name)
        {
            name = (name ?? "click").Trim().ToLowerInvariant();
            if (cache.TryGetValue(name, out var c) && c != null) return c;
            c = name switch
            {
                "beep" => Make(name, 1.0f, t => (t % 0.5f) < 0.25f ? Sine(3100f, t) * 0.35f : 0f),
                "choir" => Make(name, 2.2f, t => Env(t, 2.2f) * 0.18f * (Sine(220f * Vib(t), t) + Sine(277.2f * Vib(t), t) + Sine(329.6f * Vib(t), t))),
                "buzz" => Make(name, 0.9f, t => (t % 0.3f) < 0.18f ? Sine(150f, t) * 0.4f : 0f),
                "ring" => Make(name, 3.0f, t => t < 1.2f && (t % 0.4f) < 0.2f ? 0.3f * (Sine(440f, t) + Sine(480f, t)) : 0f),
                "giggle" => Make(name, 0.9f, t => Env(t, 0.9f) * 0.25f * Sine(700f + 220f * Mathf.Sin(t * 40f), t) * ((t % 0.18f) < 0.11f ? 1f : 0f)),
                "thunk" => Make(name, 0.25f, t => Sine(90f, t) * Mathf.Exp(-t * 18f) * 0.8f),
                "pop" => Make(name, 0.12f, t => Sine(600f, t) * Mathf.Exp(-t * 40f) * 0.6f),
                "static" => Make(name, 2.0f, t => Noise(t) * 0.12f),
                "jingle" => Make(name, 2.0f, t => 0.18f * Sine(JingleNotes[(int)(t * 4f) % 4], t) * ((t * 4f % 1f) < 0.7f ? 1f : 0f)),
                "whisper" => Make(name, 3.0f, t => Noise(t) * 0.07f * (0.5f + 0.5f * Mathf.Sin(t * 5.3f)) * (0.6f + 0.4f * Mathf.Sin(t * 1.7f))),
                "lullaby" => Make(name, 4.0f, t => 0.12f * Sine(LullabyNotes[(int)t % 4], t) * Mathf.Clamp01(1f - (t % 1f) * 0.8f)),
                "purr" => Make(name, 2.0f, t => 0.35f * Sine(55f, t) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 24f * t)) * Env(t, 2.0f)),
                "hiss" => Make(name, 0.8f, t => Noise(t) * 0.3f * Env(t, 0.8f)),
                "step" => Make(name, 0.22f, t => (Noise(t) * 0.5f + Sine(70f, t)) * Mathf.Exp(-t * 26f) * 0.7f),
                "sting" => Make(name, 1.2f, t => (Sine(880f, t) + Sine(932f, t) + Sine(1245f, t) + Noise(t) * 0.6f) * 0.22f * Mathf.Exp(-t * 3.2f)),
                "slam" => Make(name, 0.9f, t => (Sine(55f, t) * 0.9f + Noise(t) * 0.7f * Mathf.Exp(-t * 30f)) * Mathf.Exp(-t * 6f)),
                "creak" => Make(name, 2.5f, t => 0.2f * Sine(180f + 60f * Mathf.Sin(t * 3.1f), t) * ((t * 23f % 1f) < 0.55f ? 1f : 0.3f) * Env(t, 2.5f)),
                // a slow warm chord loop that changes every 2 s (8 s)
                "lofi" => Make(name, 8.0f, t => 0.07f * Chord(LofiChords[(int)(t / 2f) % 4], t) * (0.75f + 0.25f * Mathf.Sin(t * 3.1f))),
                "kettle" => Make(name, 3.0f, t => 0.25f * Sine(1400f + 500f * Mathf.Sin(t * 9f), t) * Mathf.Clamp01(t / 1.5f)),
                "whoosh" => Make(name, 0.5f, t => Noise(t) * 0.35f * Mathf.Sin(Mathf.PI * t / 0.5f)),
                "fire" => Make(name, 3.0f, t => Noise(t) * 0.15f * (0.6f + 0.4f * Mathf.Sin(t * 13f))),
                "whine" => Make(name, 2.0f, t => 0.12f * Sine(620f + 40f * Mathf.Sin(t * 30f), t)),
                // whole cycles in 6 s so the loop has no click
                "drone" => Make(name, 6.0f, t => 0.25f * (Sine(48f, t) + Sine(48.5f, t) + 0.4f * Sine(96.5f, t)) + Noise(t) * 0.02f),
                _ => Make("click", 0.05f, t => Sine(1800f, t) * Mathf.Exp(-t * 90f) * 0.6f),
            };
            cache[name] = c;
            return c;
        }

        static readonly float[][] LofiChords = { new[] { 220f, 277.2f, 329.6f }, new[] { 196f, 246.9f, 293.7f }, new[] { 174.6f, 220f, 261.6f }, new[] { 196f, 233.1f, 293.7f } };
        static float Chord(float[] notes, float t) { float v = 0f; foreach (var n in notes) v += Sine(n, t); return v; }
        static readonly float[] JingleNotes = { 523f, 659f, 784f, 1046f };
        static readonly float[] LullabyNotes = { 392f, 330f, 349f, 294f };

        static float Sine(float hz, float t) => Mathf.Sin(2f * Mathf.PI * hz * t);
        static float Vib(float t) => 1f + 0.006f * Mathf.Sin(2f * Mathf.PI * 5f * t);
        // cheap repeatable white noise for static, whispers and hisses
        static float Noise(float t) { float x = Mathf.Sin(t * 12345.679f) * 43758.547f; return (x - Mathf.Floor(x)) * 2f - 1f; }
        static float Env(float t, float len) => Mathf.Clamp01(t / 0.15f) * Mathf.Clamp01((len - t) / 0.4f);

        static AudioClip Make(string name, float seconds, System.Func<float, float> f)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = f(i / (float)Rate);
            var clip = AudioClip.Create("tone_" + name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
