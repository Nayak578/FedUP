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
                _ => Make("click", 0.05f, t => Sine(1800f, t) * Mathf.Exp(-t * 90f) * 0.6f),
            };
            cache[name] = c;
            return c;
        }

        static float Sine(float hz, float t) => Mathf.Sin(2f * Mathf.PI * hz * t);
        static float Vib(float t) => 1f + 0.006f * Mathf.Sin(2f * Mathf.PI * 5f * t);
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
