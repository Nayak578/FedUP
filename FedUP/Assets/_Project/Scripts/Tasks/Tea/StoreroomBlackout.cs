using System.Collections;
using FedUp.Audio;
using FedUp.Ghost;
using FedUp.Interaction;
using FedUp.Player;
using FedUp.Story;
using UnityEngine;
using UnityEngine.UI;

namespace FedUp.Tasks.Tea
{
    /// <summary>
    /// The Storeroom blackout (STORY_BIBLE "The Storeroom blackout" + P1; DESIGN_LOGIC #79-80). Taking the tea from the
    /// shelf under the axe, during the hunt: the cold, the lights go, black. Six seconds in the Locked Room (you can look,
    /// not walk; something crosses the window). Black again. You wake lying in the bathtub, still holding what you
    /// carried, the milk beside you if it was still hidden there. Once a night. Not a cutscene: first person throughout.
    /// </summary>
    public class StoreroomBlackout : MonoBehaviour
    {
        const int TeaItem = 2, MilkItem = 0;

        [SerializeField] TeaTask tea;
        [SerializeField] FirstPersonMotor motor;
        [SerializeField] Interactor interactor;
        [SerializeField] PhoneTorch torch;
        [SerializeField] NightPhaseController phase;
        [SerializeField] Image black;
        [Header("The Locked Room (the dream)")]
        [SerializeField] Transform dreamSpot;
        [SerializeField] Transform dreamLookAt;
        [SerializeField] Light dreamLight;
        [Tooltip("Crosses the window for a moment: a glimpse, under 0.3 s (writing rules).")]
        [SerializeField] GameObject glimpse;
        [SerializeField] Vector3 glimpseTravel = new Vector3(0f, 0f, 1.2f);
        [SerializeField, Range(0.05f, 0.3f)] float glimpseSeconds = 0.25f;
        [SerializeField, Min(2f)] float dreamSeconds = 6f;
        [Header("The bathtub")]
        [SerializeField] Transform tubLie;
        [SerializeField] Transform tubStand;
        [SerializeField] float lyingEyeHeight = 0.55f;
        [SerializeField] MonologueLibrarySO lines;

        public bool Happened { get; private set; }
        public bool Running { get; private set; }
        public bool InDream { get; private set; }
        public bool InTub { get; private set; }
        public bool GlimpseShown { get; private set; }
        public float GlimpseVisibleFor { get; private set; }
        public bool MilkWasBeside { get; private set; }

        AudioSource sfx;

        void Awake()
        {
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            if (glimpse != null) glimpse.SetActive(false);
            if (dreamLight != null) dreamLight.enabled = false;
            if (black != null) SetBlack(0f);
        }

        void Start()
        {
            if (tea != null) tea.Taken += OnTaken;
        }

        void OnTaken(int item)
        {
            if (!isActiveAndEnabled) return;
            if (!BlackoutRules.ShouldBlackOut(tea.Session.Stage, item, TeaItem, Happened)) return;
            Happened = true;
            StartCoroutine(Blackout());
        }

        IEnumerator Blackout()
        {
            Running = true;
            motor.Locked = true;
            interactor.enabled = false;
            // the cold: the hum stops, the light goes, black
            lines?.Say("blackout_cold");
            sfx.PlayOneShot(PlaceholderTones.Get("whisper"), 0.6f);
            yield return new WaitForSeconds(1.2f);
            bool torchWas = torch != null && torch.Available;
            if (torch != null) torch.Available = false;
            yield return Fade(0f, 1f, 0.9f);
            yield return new WaitForSeconds(0.8f);

            // the Locked Room: you can look, you can't walk
            InDream = true;
            motor.Teleport(dreamSpot.position, dreamSpot.eulerAngles.y);
            Look(dreamLookAt.position);
            if (dreamLight != null) dreamLight.enabled = true;
            motor.Locked = false;
            motor.FeetFrozen = true;
            yield return Fade(1f, 0f, 1.4f);
            yield return new WaitForSeconds(dreamSeconds * 0.45f);
            // something passes the window
            if (glimpse != null)
            {
                Vector3 from = glimpse.transform.position;
                glimpse.SetActive(true);
                GlimpseShown = true;
                float t0 = Time.time;
                for (float t = 0f; t < glimpseSeconds; t += Time.deltaTime)
                {
                    glimpse.transform.position = from + glimpseTravel * (t / glimpseSeconds);
                    yield return null;
                }
                glimpse.SetActive(false);
                GlimpseVisibleFor = Time.time - t0;
                glimpse.transform.position = from;
            }
            yield return new WaitForSeconds(dreamSeconds * 0.55f);
            yield return Fade(0f, 1f, 1.0f);
            if (dreamLight != null) dreamLight.enabled = false;
            InDream = false;
            motor.FeetFrozen = false;
            motor.Locked = true;
            yield return new WaitForSeconds(1.0f);

            // the bathtub: you're lying in it; the milk is here if it was still hidden
            InTub = true;
            MilkWasBeside = BlackoutRules.MilkBesideYou(tea.Session.StateOf(MilkItem));
            motor.Teleport(tubLie.position, tubLie.eulerAngles.y);
            motor.EyeHeightOverride = lyingEyeHeight;
            yield return Fade(1f, 0f, 1.6f);
            lines?.Say(MilkWasBeside ? "blackout_tub_milk" : "blackout_tub");
            yield return new WaitForSeconds(3.2f);
            motor.EyeHeightOverride = null;
            motor.Teleport(tubStand.position, tubStand.eulerAngles.y);
            InTub = false;
            if (torch != null) torch.Available = torchWas || torch.Available;
            motor.Locked = false;
            interactor.enabled = true;
            Running = false;
        }

        IEnumerator Fade(float from, float to, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime) { SetBlack(Mathf.Lerp(from, to, t / seconds)); yield return null; }
            SetBlack(to);
        }

        void SetBlack(float a)
        {
            if (black == null) return;
            var c = black.color; c.a = a; black.color = c;
        }

        void Look(Vector3 p)
        {
            Vector3 d = p - interactor.transform.position;
            motor.SetLook(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg);
        }
    }
}
