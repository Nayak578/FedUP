using UnityEngine;
using UnityEngine.InputSystem;

namespace FedUp.Player
{
    /// <summary>
    /// His phone torch (STORY_FLOW Phase 2: "his only light, and it flickers by itself"). F toggles it once it's
    /// available (from the wake-up on). Every so often it cuts out for a moment on its own.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class PhoneTorch : MonoBehaviour
    {
        [SerializeField] InputActionAsset controls;
        [SerializeField, Min(1f)] float flickerEveryMin = 18f;
        [SerializeField, Min(1f)] float flickerEveryMax = 35f;

        Light beam;
        float fullIntensity;
        InputAction toggle;
        float nextFlicker, flickerEnds;
        bool wantOn;

        public bool Available { get; set; }
        /// <summary>Gate 5a: the battery is nearly gone (the reason to go upstairs for the charger). Dim and stuttering.</summary>
        public bool Dying { get; set; }
        public bool On => wantOn;
        public bool Lit => beam != null && beam.enabled;

        void Awake()
        {
            beam = GetComponent<Light>();
            beam.enabled = false;
            fullIntensity = beam.intensity;
            if (controls != null) toggle = controls.FindAction("Player/Torch", true);
            nextFlicker = Time.time + Random.Range(flickerEveryMin, flickerEveryMax);
        }

        /// <summary>Autopilot/tests: press F once.</summary>
        public void VirtualToggle() => Toggle();

        void Toggle()
        {
            if (!Available) return;
            wantOn = !wantOn;
        }

        void Update()
        {
            if (toggle != null && toggle.WasPressedThisFrame()) Toggle();
            if (Time.time >= nextFlicker)
            {
                flickerEnds = Time.time + Random.Range(0.08f, 0.3f);
                nextFlicker = flickerEnds + (Dying ? Random.Range(1.2f, 3.5f) : Random.Range(flickerEveryMin, flickerEveryMax));
            }
            beam.intensity = Dying ? fullIntensity * 0.4f : fullIntensity;
            beam.enabled = Available && wantOn && Time.time >= flickerEnds;
        }
    }
}
