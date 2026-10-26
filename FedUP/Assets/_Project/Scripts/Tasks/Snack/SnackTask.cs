using System.Collections;
using FedUp.Audio;
using FedUp.Frustration;
using FedUp.Interaction;
using FedUp.Qte;
using FedUp.Story;
using UnityEngine;

namespace FedUp.Tasks.Snack
{
    /// <summary>
    /// GDD Grab a Snack (+ Task 3). Open the fridge ("Back so soon?") and grab the snack bar. Above 30% it slides
    /// away from your look; grabbing while it slides is a failed grab (+5). Hover 3 s and the fridge mocks you (+3).
    /// Grab after 10 s open and a ghost hand yanks it: mash E to keep it, or lose it (+10). Leave the door open
    /// 15 s and it slams. The snack: -5. Sometimes there's a juice box too (-5, optional).
    /// </summary>
    public class SnackTask : TaskBehaviour
    {
        [SerializeField] SnackTuningSO tuning;
        [SerializeField] TaskInteractable fridge;
        [SerializeField] Transform doorHinge;
        [SerializeField] float doorOpenAngle = -100f;
        [SerializeField] GameObject inside;
        [SerializeField] Light fridgeLight;
        [SerializeField] TaskInteractable snack;
        [Tooltip("The two ends of the shelf the snack slides between.")]
        [SerializeField] Transform shelfA, shelfB;
        [SerializeField] TaskInteractable juiceBox;
        [SerializeField] Interactor interactor;
        [SerializeField] FrustrationRequestChannelSO frustration;
        [SerializeField] FrustrationChannelSO frustrationState;
        [SerializeField] QteDisplayChannelSO qte;
        [SerializeField] SubtitleChannelSO subtitles;
        [SerializeField] MonologueLibrarySO lines;

        public SnackSession Session { get; private set; }
        public bool Tugging => tug != null;
        public bool JuiceOffered { get; private set; }
        public bool JuiceDrunk { get; private set; }
        public bool Got { get; private set; }
        public bool Lost { get; private set; }
        public int FailedGrabs { get; private set; }
        /// <summary>Tests: decide the juice box instead of rolling for it.</summary>
        public bool? ForcedJuice { get; set; }

        MashChallenge tug;
        AudioSource hum, sfx;
        bool atA = true, juiceRolled;

        void Awake()
        {
            hum = (fridge != null ? fridge.gameObject : gameObject).AddComponent<AudioSource>();
            hum.playOnAwake = false; hum.loop = true; hum.spatialBlend = 0.9f; hum.volume = 0.5f;
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            if (fridge != null) fridge.available = false;
            if (snack != null) { snack.available = false; snack.prompt = "Grab the snack bar"; }
            if (juiceBox != null) { juiceBox.available = false; juiceBox.gameObject.SetActive(false); juiceBox.prompt = "Drink the juice box"; }
            SetOpen(false);
        }

        protected override void OnBegin()
        {
            Session = new SnackSession(tuning.slideAbove, tuning.grabWindow, tuning.slideSeconds, tuning.sarcasmAfter, tuning.handAfterOpen, tuning.slamAfter);
            fridge.available = true;
            fridge.prompt = "Open the fridge";
            fridge.Used += _ => Open();
            snack.Used += _ => TryGrab();
            juiceBox.Used += _ => Drink();
        }

        void SetOpen(bool open)
        {
            if (doorHinge != null) doorHinge.localRotation = Quaternion.Euler(0f, open ? doorOpenAngle : 0f, 0f);
            if (inside != null) inside.SetActive(open);
            if (fridgeLight != null) fridgeLight.enabled = open;
            if (snack != null) snack.available = open && !Got && !Lost && State == TaskState.Active;
            if (juiceBox != null) juiceBox.available = open && JuiceOffered && !JuiceDrunk;
        }

        void Open()
        {
            if (State != TaskState.Active || Session.IsOpen) return;
            if (!juiceRolled)
            {
                juiceRolled = true;
                JuiceOffered = ForcedJuice ?? (Random.value < tuning.juiceChance);
                if (juiceBox != null) juiceBox.gameObject.SetActive(JuiceOffered);
            }
            Session.Open();
            fridge.available = false;
            SetOpen(true);
            hum.clip = PlaceholderTones.Get("buzz");
            hum.pitch = 0.6f;
            hum.Play();
            subtitles?.Say("FRIDGE:  'Back so soon?'", 2.2f, SubtitleStyle.Caption);
        }

        void Update()
        {
            if (Session == null || State != TaskState.Active) return;
            if (tug != null) { TickTug(); return; }
            if (!Session.IsOpen) return;
            float f = frustrationState != null ? frustrationState.Last.Hidden : 0f;
            bool looking = interactor != null && ReferenceEquals(interactor.Current, snack);
            Session.Tick(Time.deltaTime, looking, f);
            if (Session.SlidAwayThisTick) StartCoroutine(Slide());
            if (Session.SarcasmThisTick)
            {
                frustration?.Request(tuning.sarcasm);
                subtitles?.Say("FRIDGE:  'Midnight snack? Again?'", 2.4f, SubtitleStyle.Caption);
            }
            if (Session.SlammedThisTick)
            {
                SetOpen(false);
                hum.Stop();
                sfx.PlayOneShot(PlaceholderTones.Get("thunk"));
                lines?.Say("fridge_slam");
                fridge.available = true;
            }
        }

        IEnumerator Slide()
        {
            Vector3 from = snack.transform.position;
            Vector3 to = (atA ? shelfB : shelfA).position;
            atA = !atA;
            sfx.PlayOneShot(PlaceholderTones.Get("click"));
            for (float t = 0f; t < tuning.slideSeconds; t += Time.deltaTime)
            {
                snack.transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / tuning.slideSeconds));
                yield return null;
            }
            snack.transform.position = to;
        }

        void TryGrab()
        {
            if (State != TaskState.Active || tug != null) return;
            switch (Session.Grab())
            {
                case GrabResult.Grabbed: Success(); break;
                case GrabResult.FailedGrab:
                    FailedGrabs++;
                    frustration?.Request(tuning.failedGrab);
                    lines?.Say("snack_missed");
                    break;
                case GrabResult.GhostHand:
                    tug = new MashChallenge(tuning.handPresses, tuning.handSeconds);
                    snack.available = false;
                    sfx.PlayOneShot(PlaceholderTones.Get("thunk"));
                    subtitles?.Say("[ something at the back of the fridge pulls the other way ]", 2.2f, SubtitleStyle.Caption);
                    break;
            }
        }

        void TickTug()
        {
            bool pressed = virtualMash;
            virtualMash = false;
            if (pressed) tug.Press();
            tug.Tick(Time.deltaTime);
            qte?.Raise(new QteDisplay(true, QteKind.Mash, 0, 0, 0, tug.Progress, "IT'S PULLING BACK!   MASH  E"));
            if (!tug.Finished) return;
            bool won = tug.Succeeded;
            tug = null;
            qte?.Raise(QteDisplay.Hidden);
            if (won) { Success(); return; }
            Lost = true;
            frustration?.Request(tuning.qteFail);
            sfx.PlayOneShot(PlaceholderTones.Get("giggle"));
            snack.gameObject.SetActive(false);
            lines?.Say("snack_lost");
            Close();
        }

        bool virtualMash;
        /// <summary>Mash E during the ghost-hand tug (wired from the interact button; tests/autopilot call it).</summary>
        public void Mash() => virtualMash = true;

        void LateUpdate()
        {
            // the interact button mashes the tug (the snack itself isn't targetable while it's being pulled)
            if (tug != null && interactor != null && interactor.InteractPressedThisFrame) virtualMash = true;
        }

        void Success()
        {
            Got = true;
            frustration?.Request(tuning.success);
            snack.gameObject.SetActive(false);
            lines?.Say("snack_got");
            Close();
        }

        void Drink()
        {
            if (JuiceDrunk) return;
            JuiceDrunk = true;
            frustration?.Request(tuning.juiceBox);
            juiceBox.gameObject.SetActive(false);
            lines?.Say("juice_box");
        }

        void Close()
        {
            StartCoroutine(CloseSoon());
        }

        IEnumerator CloseSoon()
        {
            yield return new WaitForSeconds(JuiceOffered && !JuiceDrunk ? 4f : 1.2f); // a moment to see the juice box
            SetOpen(false);
            hum.Stop();
            Complete();
        }

        public override void DebugComplete()
        {
            StopAllCoroutines();
            tug = null;
            qte?.Raise(QteDisplay.Hidden);
            SetOpen(false);
            if (hum != null) hum.Stop();
            if (fridge != null) fridge.available = false;
            base.DebugComplete();
        }
    }
}
