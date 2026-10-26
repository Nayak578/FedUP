using System.Collections;
using FedUp.Audio;
using FedUp.Frustration;
using FedUp.Interaction;
using FedUp.Player;
using FedUp.Qte;
using FedUp.Story;
using UnityEngine;

namespace FedUp.Tasks.Night
{
    /// <summary>
    /// The final sleep (GDD Final Sleep Task + Task 7; DESIGN_LOGIC #70-72). His own bed, upstairs, for the first time
    /// tonight. The lights turn red. Above 50% you must breathe to the heartbeat first (three beats; a miss is a
    /// scream). Then the blanket lifts: press E to wake up, or do nothing and stay asleep. The ending follows.
    /// </summary>
    public class FinalSleepTask : TaskBehaviour
    {
        [SerializeField] FinalSleepTuningSO tuning;
        [SerializeField] TaskInteractable bed;
        [SerializeField] Transform lieSpot;
        [SerializeField] Transform lookAt;
        [SerializeField] float lyingEyeHeight = 0.6f;
        [SerializeField] Transform blanket;
        [SerializeField] Vector3 blanketLift = new Vector3(0f, 0.9f, 0f);
        [Tooltip("GDD: 'if frustration is high, ghost arms barely form in the fog around the bed frame'.")]
        [SerializeField] GameObject ghostArms;
        [SerializeField] Color redLight = new Color(0.9f, 0.12f, 0.08f);
        [SerializeField] FirstPersonMotor motor;
        [SerializeField] Interactor interactor;
        [SerializeField] FrustrationRequestChannelSO frustration;
        [SerializeField] FrustrationChannelSO frustrationState;
        [SerializeField] RunStateSO runState;
        [SerializeField] QteDisplayChannelSO qte;
        [SerializeField] MonologueLibrarySO lines;
        [SerializeField] SubtitleChannelSO subtitles;
        [SerializeField] EndingPlayer endings;
        [Tooltip("Cat visits that may still be waiting when you lie down: they leave.")]
        [SerializeField] Cat.CatPerchTask[] waitingCats = new Cat.CatPerchTask[0];
        [Tooltip("Mid-task calls that may still be ringing (or not yet rung) when you lie down: they go to the machine.")]
        [SerializeField] Phone.PhoneCallTask[] lateCalls = new Phone.PhoneCallTask[0];

        public FinalSleepSession Session { get; private set; }
        public bool Lying { get; private set; }
        public bool Resolved { get; private set; }
        /// <summary>The blanket is lifting: a press now wakes you (tests/autopilot wait for this).</summary>
        public bool AwaitingWake { get; private set; }
        public Ending Result { get; private set; }
        public float MarkerPosition { get; private set; }
        public bool InZone => MarkerPosition >= tuning.zoneStart && MarkerPosition <= tuning.zoneEnd;
        /// <summary>Frustration the moment you chose (what the ending table reads).</summary>
        public float FrustrationAtChoice { get; private set; }

        AudioSource sfx;
        Vector3 blanketRest;

        void Awake()
        {
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            if (blanket != null) blanketRest = blanket.localPosition;
            if (ghostArms != null) ghostArms.SetActive(false);
        }

        protected override void OnBegin()
        {
            Session = new FinalSleepSession(tuning.disruptAbove, tuning.beats, tuning.maxFails,
                tuning.scream != null ? tuning.scream.amount : 15f, tuning.retry != null ? tuning.retry.amount : 10f,
                tuning.clean != null ? tuning.clean.amount : -20f, tuning.choiceSeconds);
            bed.prompt = "Go to sleep";
            bed.mode = InteractionMode.Hold;
            bed.holdSeconds = tuning.lieDownHoldSeconds;
            bed.available = true;
            bed.Used += _ => { if (State == TaskState.Active && !Lying) StartCoroutine(Sleep()); };
            lines?.Say("final_go_to_bed");
        }

        float F => frustrationState != null ? frustrationState.Last.Hidden : 0f;

        IEnumerator Sleep()
        {
            Lying = true;
            bed.available = false;
            foreach (var cat in waitingCats) cat?.Dismiss();
            foreach (var call in lateCalls) call?.RingOutNow();
            motor.Locked = true;
            motor.Teleport(lieSpot.position, lieSpot.eulerAngles.y);
            motor.EyeHeightOverride = lyingEyeHeight;
            Look(lookAt.position);
            lines?.Say("final_lie");
            yield return TurnRed(2f);

            float delta = Session.LieDown(F);
            if (delta < 0f) Request(tuning.clean);
            while (Session.Stage == FinalSleepStage.Breathing) yield return Breathe();
            qte?.Raise(QteDisplay.Hidden);
            // a breath of stillness first: the last beat's press must never count as waking up
            yield return new WaitForSeconds(0.8f);

            // the blanket lifts; press to wake, or don't
            if (ghostArms != null) ghostArms.SetActive(F > tuning.disruptAbove);
            subtitles?.Say("[ E — wake up ]", tuning.choiceSeconds, SubtitleStyle.Caption);
            AwaitingWake = true;
            while (Session.Stage == FinalSleepStage.Choice)
            {
                if (blanket != null) blanket.localPosition = blanketRest + blanketLift * (1f - Session.ChoiceLeft / tuning.choiceSeconds);
                if (interactor.InteractPressedThisFrame) Session.WakeUp();
                else Session.Tick(Time.deltaTime);
                yield return null;
            }
            AwaitingWake = false;
            FrustrationAtChoice = F;
            Result = EndingResolver.Resolve(runState != null && runState.CatPettedEver, runState != null ? runState.CallsAnswered : 0, Session.Chosen, FrustrationAtChoice);
            Resolved = true;
            Debug.Log($"ENDING {Result}: cat={runState?.CatPettedEver} calls={runState?.CallsAnswered} choice={Session.Chosen} frustration={FrustrationAtChoice:F1}");
            if (ghostArms != null) ghostArms.SetActive(false);
            Complete();
            endings.Play(Result);
        }

        IEnumerator Breathe()
        {
            // one heartbeat: the marker crosses the bar; press E in the zone
            sfx.PlayOneShot(PlaceholderTones.Get("thunk"), 0.8f);
            bool pressed = false, onBeat = false;
            for (float t = 0f; t < tuning.beatSeconds; t += Time.deltaTime)
            {
                MarkerPosition = TimingQte.MarkerPosition(t, tuning.beatSeconds);
                qte?.Raise(new QteDisplay(true, QteKind.Timing, MarkerPosition, tuning.zoneStart, tuning.zoneEnd, (float)Session.BeatsHit / tuning.beats, "BREATHE"));
                if (interactor.InteractPressedThisFrame)
                {
                    pressed = true;
                    onBeat = TimingQte.Judge(MarkerPosition, tuning.zoneStart, tuning.zoneEnd) == TimingJudgement.Perfect;
                    break;
                }
                yield return null;
            }
            if (!pressed) onBeat = false;
            MarkerPosition = 0f; // between beats the bar is empty: nothing to press
            qte?.Raise(new QteDisplay(true, QteKind.Timing, 0f, tuning.zoneStart, tuning.zoneEnd, (float)Session.BeatsHit / tuning.beats, "BREATHE"));
            int failsBefore = Session.Fails;
            float delta = Session.Beat(onBeat);
            if (Session.Fails > failsBefore)
            {
                // the scream: a jolt of the ghost right over the bed (a glimpse, under 0.3 s)
                Request(tuning.scream);
                if (delta > (tuning.scream != null ? tuning.scream.amount : 15f) + 0.01f) Request(tuning.retry);
                sfx.PlayOneShot(PlaceholderTones.Get("sting"));
                if (ghostArms != null) ghostArms.SetActive(true);
                yield return new WaitForSeconds(0.25f);
                if (ghostArms != null) ghostArms.SetActive(false);
                lines?.Say(Session.Nightmare ? "final_nightmare" : "final_scream");
                yield return new WaitForSeconds(1.2f);
            }
            else if (delta < 0f) Request(tuning.clean);
            else yield return new WaitForSeconds(0.25f);
        }

        void Request(FrustrationEventSO ev) { if (ev != null) frustration?.Request(ev); }

        IEnumerator TurnRed(float seconds)
        {
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            var from = new Color[lights.Length];
            for (int i = 0; i < lights.Length; i++) from[i] = lights[i].color;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                for (int i = 0; i < lights.Length; i++) if (lights[i] != null && lights[i].type != LightType.Directional) lights[i].color = Color.Lerp(from[i], redLight, t / seconds);
                yield return null;
            }
        }

        void Look(Vector3 p)
        {
            Vector3 d = p - (lieSpot.position + Vector3.up * lyingEyeHeight);
            motor.SetLook(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg);
        }

        /// <summary>Tests/autopilot: lie down now, without walking upstairs.</summary>
        public void DebugLieDownNow() { if (State == TaskState.Active && !Lying) StartCoroutine(Sleep()); }

        public override void DebugComplete()
        {
            StopAllCoroutines();
            if (bed != null) bed.available = false;
            qte?.Raise(QteDisplay.Hidden);
            motor.EyeHeightOverride = null;
            motor.Locked = false;
            base.DebugComplete();
        }
    }
}
