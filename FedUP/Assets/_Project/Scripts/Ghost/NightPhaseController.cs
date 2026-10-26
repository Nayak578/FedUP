using System;
using FedUp.Audio;
using FedUp.Player;
using FedUp.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace FedUp.Ghost
{
    /// <summary>
    /// Phase 1 (evening) → Phase 2 (the wake-up, GDD: "darker, more fog, colder lighting; whispers, droning").
    /// The "sick version" of the same house (Cry of Fear study): every light off except the few that draw the
    /// path, the rest give only a weak glow if switched on; thicker, colder fog; a colder grade; a drone and
    /// whispers; the phone torch becomes available.
    /// </summary>
    public class NightPhaseController : MonoBehaviour
    {
        [Tooltip("Every room light in the house.")]
        [SerializeField] Light[] roomLights = Array.Empty<Light>();
        [Tooltip("The few left burning in Phase 2 (they draw the path). See DESIGN_LOGIC.md #32.")]
        [SerializeField] Light[] keepLit = Array.Empty<Light>();
        [SerializeField, Range(0f, 1f)] float keepLitScale = 0.35f;
        [Tooltip("What a switched-off light gives if you switch it back on in Phase 2.")]
        [SerializeField, Range(0f, 1f)] float switchedOnScale = 0.25f;
        [SerializeField] Volume phase2Look;
        [SerializeField] float phase2FogDensity = 0.06f;
        [SerializeField] Color phase2FogColor = new Color(0.05f, 0.05f, 0.09f);
        [SerializeField] Color phase2Ambient = new Color(0.04f, 0.045f, 0.06f);
        [SerializeField] AudioSource drone;
        [SerializeField] AudioSource whispers;
        [SerializeField] PhoneTorch torch;
        [SerializeField] GhostDirector director;
        [Header("Phase 3 (the haunted night)")]
        [Tooltip("GDD art notes: purple fog is the supernatural signal.")]
        [SerializeField] Color phase3FogColor = new Color(0.1f, 0.05f, 0.13f);
        [SerializeField] float phase3FogDensity = 0.065f;
        [Tooltip("Other lights that go out in a power cut (the TV).")]
        [SerializeField] Light[] alsoOnMains = Array.Empty<Light>();

        public int Phase { get; private set; } = 1;
        public bool DronePlaying => drone != null && drone.isPlaying;
        public System.Collections.Generic.IReadOnlyList<Light> KeepLit => keepLit;
        public event Action<int> PhaseChanged;

        float[] baseline;
        Color[] baselineColour;
        (Color colour, float density, Color ambient) eveningFog;

        /// <summary>
        /// The loop ending: the house as it was at the start of the evening, as if nothing happened. Every room light back
        /// at its evening brightness and colour, no fog of the night, no drone.
        /// </summary>
        public void RestoreEvening()
        {
            for (int i = 0; i < roomLights.Length; i++)
            {
                var l = roomLights[i];
                if (l == null) continue;
                var f = l.GetComponent<LightFlicker>();
                if (f != null) f.enabled = false;
                l.intensity = baseline[i];
                l.color = baselineColour[i];
                l.enabled = true;
            }
            RenderSettings.fogColor = eveningFog.colour;
            RenderSettings.fogDensity = eveningFog.density;
            RenderSettings.ambientLight = eveningFog.ambient;
            if (phase2Look != null) phase2Look.weight = 0f;
            if (drone != null) drone.Stop();
            if (whispers != null) whispers.Stop();
        }

        /// <summary>The night's sounds stop (dying in your sleep: the silence after).</summary>
        public void Silence()
        {
            if (drone != null) drone.Stop();
            if (whispers != null) whispers.Stop();
        }

        void Awake()
        {
            baseline = new float[roomLights.Length];
            baselineColour = new Color[roomLights.Length];
            for (int i = 0; i < roomLights.Length; i++)
            {
                baseline[i] = roomLights[i] != null ? roomLights[i].intensity : 0f;
                baselineColour[i] = roomLights[i] != null ? roomLights[i].color : Color.white;
            }
            eveningFog = (RenderSettings.fogColor, RenderSettings.fogDensity, RenderSettings.ambientLight);
            if (phase2Look != null) phase2Look.weight = 0f;
            if (torch != null) torch.Available = false;
        }

        void Start()
        {
            director?.EnterPhase(1);
            // make the night's sounds now, at load, not mid-scene (a 6 s drone costs a visible hitch to generate)
            foreach (var tone in new[] { "drone", "whisper", "step", "sting", "slam", "creak",
                                         "lofi", "kettle", "whoosh", "fire", "whine", "giggle", "buzz", "thunk", "pop", "click",
                                         "tin", "beep", "ring", "jingle", "purr", "hiss" }) PlaceholderTones.Get(tone);
        }

        /// <summary>The house turns. Called while the screen is black (the sleep scene).</summary>
        public void EnterPhase2()
        {
            if (Phase >= 2) return;
            Phase = 2;
            for (int i = 0; i < roomLights.Length; i++)
            {
                var l = roomLights[i];
                if (l == null) continue;
                bool keep = Array.IndexOf(keepLit, l) >= 0;
                float k = keep ? keepLitScale : switchedOnScale;
                l.intensity = baseline[i] * k;
                l.color = Color.Lerp(l.color, new Color(0.75f, 0.8f, 1f), 0.5f); // colder light
                l.enabled = keep;
                var flicker = l.GetComponent<LightFlicker>();
                if (keep && flicker == null) flicker = l.gameObject.AddComponent<LightFlicker>();
                if (flicker != null) flicker.SetBase(l.intensity);
            }
            RenderSettings.fogDensity = phase2FogDensity;
            RenderSettings.fogColor = phase2FogColor;
            RenderSettings.ambientLight = phase2Ambient;
            if (phase2Look != null) phase2Look.weight = 1f;
            if (drone != null) { drone.clip = PlaceholderTones.Get("drone"); drone.loop = true; drone.Play(); }
            if (whispers != null) { whispers.clip = PlaceholderTones.Get("whisper"); whispers.loop = true; whispers.Play(); }
            if (torch != null) torch.Available = true;
            PhaseChanged?.Invoke(Phase);
        }

        /// <summary>He's awake and in control: the haunting starts (after the director's grace).</summary>
        public void StartHaunting() => director?.EnterPhase(Phase);

        /// <summary>Phase 3 (GDD: haunted night tasks, high haunting): the fog turns purple and thickens.</summary>
        public void EnterPhase3()
        {
            if (Phase < 2) EnterPhase2();
            if (Phase >= 3) return;
            Phase = 3;
            RenderSettings.fogColor = phase3FogColor;
            RenderSettings.fogDensity = phase3FogDensity;
            director?.EnterPhase(3);
            PhaseChanged?.Invoke(Phase);
        }

        public bool PowerOn { get; private set; } = true;
        bool[] litBeforeCut;

        /// <summary>The mains: a power cut kills every room light and the TV (the torch is a phone, it stays).</summary>
        public void SetPower(bool on)
        {
            if (on == PowerOn) return;
            PowerOn = on;
            var all = AllMains();
            if (!on)
            {
                litBeforeCut = new bool[all.Length];
                for (int i = 0; i < all.Length; i++) { litBeforeCut[i] = all[i] != null && all[i].enabled; if (all[i] != null) all[i].enabled = false; }
                if (drone != null) drone.Pause();
            }
            else
            {
                for (int i = 0; i < all.Length; i++) if (all[i] != null && litBeforeCut != null && i < litBeforeCut.Length) all[i].enabled = litBeforeCut[i];
                if (drone != null) drone.UnPause();
            }
            foreach (var l in all) { var f = l != null ? l.GetComponent<LightFlicker>() : null; if (f != null) f.enabled = on; }
            // no power, no switches: you can't flip your way out of a power cut
            foreach (var sw in FindObjectsByType<FedUp.Interaction.LightSwitch>(FindObjectsSortMode.None)) sw.enabled = on;
        }

        Light[] AllMains()
        {
            var all = new Light[roomLights.Length + alsoOnMains.Length];
            roomLights.CopyTo(all, 0);
            alsoOnMains.CopyTo(all, roomLights.Length);
            return all;
        }

        /// <summary>
        /// The house holds its breath (GDD Cat Visit #2-3: "a dramatic reduction in ambient noise and temporarily
        /// pauses ghost activity"): the drone and whispers dip, and the director stays quiet.
        /// </summary>
        public void Hush(float seconds)
        {
            director?.Hush(seconds);
            StartCoroutine(HushRoutine(seconds));
        }

        System.Collections.IEnumerator HushRoutine(float seconds)
        {
            float d = drone != null ? drone.volume : 0f, w = whispers != null ? whispers.volume : 0f;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                // down fast, back up slowly over the last third
                float k = t < 0.6f ? 1f - t / 0.6f * 0.85f : t < seconds * 0.66f ? 0.15f : 0.15f + 0.85f * (t - seconds * 0.66f) / (seconds * 0.34f);
                if (drone != null) drone.volume = d * k;
                if (whispers != null) whispers.volume = w * k;
                yield return null;
            }
            if (drone != null) drone.volume = d;
            if (whispers != null) whispers.volume = w;
        }

        /// <summary>The lights stutter for a moment (things vanish; something passes).</summary>
        public void Flicker(float seconds) => StartCoroutine(FlickerRoutine(seconds));

        System.Collections.IEnumerator FlickerRoutine(float seconds)
        {
            var all = AllMains();
            var was = new bool[all.Length];
            for (int i = 0; i < all.Length; i++) was[i] = all[i] != null && all[i].enabled;
            for (float t = 0f; t < seconds; t += 0.08f)
            {
                bool on = UnityEngine.Random.value > 0.5f;
                for (int i = 0; i < all.Length; i++) if (all[i] != null && was[i]) all[i].enabled = on;
                yield return new WaitForSeconds(0.08f);
            }
            if (PowerOn) for (int i = 0; i < all.Length; i++) if (all[i] != null) all[i].enabled = was[i];
        }
    }
}
