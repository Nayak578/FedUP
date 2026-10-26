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

        void Awake()
        {
            baseline = new float[roomLights.Length];
            for (int i = 0; i < roomLights.Length; i++) baseline[i] = roomLights[i] != null ? roomLights[i].intensity : 0f;
            if (phase2Look != null) phase2Look.weight = 0f;
            if (torch != null) torch.Available = false;
        }

        void Start()
        {
            director?.EnterPhase(1);
            // make the night's sounds now, at load, not mid-scene (a 6 s drone costs a visible hitch to generate)
            foreach (var tone in new[] { "drone", "whisper", "step", "sting", "slam", "creak",
                                         "lofi", "kettle", "whoosh", "fire", "whine", "giggle", "buzz", "thunk", "pop", "click" }) PlaceholderTones.Get(tone);
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
