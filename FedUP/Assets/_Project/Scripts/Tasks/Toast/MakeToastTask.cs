using System.Collections;
using FedUp.Audio;
using FedUp.Frustration;
using FedUp.Qte;
using FedUp.Story;
using UnityEngine;

namespace FedUp.Tasks.Toast
{
    public enum ToasterState { Idle, Jammed, Toasting, Ready, Cooldown }

    /// <summary>
    /// GDD Task 1, Make Toast. Press the toaster, then press again while the marker is in the golden zone.
    /// Too early: the crooked toaster launches it. Too late: it burns. Above 30% frustration it jams first
    /// (mash to unjam). Two failures in a row set off the smoke alarm. Golden toast left 15 s gets eaten.
    /// </summary>
    public class MakeToastTask : TaskBehaviour
    {
        [SerializeField] ToastTuningSO tuning;
        [SerializeField] TaskInteractable toaster;
        [SerializeField] TaskInteractable toastPickup;
        [SerializeField] Transform toastSpawn;
        [SerializeField] Transform[] launchTargets = new Transform[0];
        [SerializeField] GameObject ghostCat;
        [SerializeField] SmokeAlarm alarm;
        [SerializeField] FrustrationRequestChannelSO frustration;
        [SerializeField] FrustrationChannelSO frustrationState;
        [SerializeField] QteDisplayChannelSO qte;
        [SerializeField] MonologueLibrarySO lines;
        [SerializeField] Material burntMaterial;

        public ToasterState Toaster { get; private set; } = ToasterState.Idle;
        public float MarkerPosition { get; private set; }
        public ToastOutcome? LastOutcome { get; private set; }
        public int Attempts { get; private set; }
        public int ToastsEaten { get; private set; }
        public ToastTuningSO Tuning => tuning;

        ToastSession session;
        MashChallenge mash;
        float elapsed, readyTimer;
        bool saidOpeningLine;
        AudioSource sfx;

        void Awake()
        {
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0.6f;
            if (toaster != null) toaster.available = false;
            if (toastPickup != null) { toastPickup.available = false; toastPickup.gameObject.SetActive(false); }
            if (ghostCat != null) ghostCat.SetActive(false);
        }

        protected override void OnBegin()
        {
            session = new ToastSession(tuning.alarmAfterFailuresInARow);
            toaster.Used += OnToaster;
            toastPickup.Used += OnPickup;
            if (alarm != null) alarm.Silenced += () => session.AlarmSilenced();
            SetIdle();
            // The opening line waits for the first press, so it never talks over the end of the file scene.
        }

        void SetIdle()
        {
            Toaster = ToasterState.Idle;
            toaster.available = true;
            toaster.prompt = "Make toast";
            qte?.Raise(QteDisplay.Hidden);
        }

        void OnToaster(TaskInteractable t)
        {
            switch (Toaster)
            {
                case ToasterState.Idle:
                    float f = frustrationState != null ? frustrationState.Last.Hidden : 0f;
                    if (ToastSession.ShouldJam(f, tuning.jamAboveFrustration))
                    {
                        Toaster = ToasterState.Jammed;
                        mash = new MashChallenge(tuning.mashPresses, tuning.mashSeconds);
                        toaster.prompt = "Unjam it (mash E)";
                        sfx.PlayOneShot(PlaceholderTones.Get("thunk"));
                        lines?.Say("toaster_jammed");
                    }
                    else StartToasting();
                    break;
                case ToasterState.Jammed:
                    mash.Press();
                    sfx.PlayOneShot(PlaceholderTones.Get("click"));
                    break;
                case ToasterState.Toasting:
                    Resolve(TimingQte.Judge(MarkerPosition, tuning.goldenZoneStart, tuning.goldenZoneEnd));
                    break;
            }
        }

        void StartToasting()
        {
            if (Attempts == 0 && !saidOpeningLine) { lines?.Say("toaster_start"); saidOpeningLine = true; }
            Toaster = ToasterState.Toasting;
            elapsed = 0f;
            MarkerPosition = 0f;
            toaster.prompt = "Pop it now";
            sfx.PlayOneShot(PlaceholderTones.Get("click"));
        }

        void Update()
        {
            if (State != TaskState.Active) return;
            switch (Toaster)
            {
                case ToasterState.Jammed:
                    mash.Tick(Time.deltaTime);
                    qte?.Raise(new QteDisplay(true, QteKind.Mash, 0, 0, 0, mash.Progress, "MASH  E  TO UNJAM"));
                    if (mash.Succeeded) { lines?.Say("toaster_unjammed"); StartToasting(); }
                    else if (mash.Failed) Resolve(TimingJudgement.Late);
                    break;
                case ToasterState.Toasting:
                    elapsed += Time.deltaTime;
                    MarkerPosition = TimingQte.MarkerPosition(elapsed, tuning.sweepSeconds);
                    qte?.Raise(new QteDisplay(true, QteKind.Timing, MarkerPosition, tuning.goldenZoneStart, tuning.goldenZoneEnd, 0, "PRESS  E  WHEN IT'S GOLDEN"));
                    if (elapsed >= tuning.sweepSeconds + tuning.graceAfterEnd) Resolve(TimingJudgement.Late);
                    break;
                case ToasterState.Ready:
                    readyTimer += Time.deltaTime;
                    if (readyTimer >= tuning.pickupSeconds) StartCoroutine(GhostEatsToast());
                    break;
            }
        }

        void Resolve(TimingJudgement judgement)
        {
            Attempts++;
            var outcome = session.Resolve(judgement);
            LastOutcome = outcome;
            qte?.Raise(QteDisplay.Hidden);
            sfx.PlayOneShot(PlaceholderTones.Get("pop"));
            switch (outcome)
            {
                case ToastOutcome.Golden:
                    Toaster = ToasterState.Ready;
                    readyTimer = 0f;
                    toaster.available = false;
                    toastPickup.gameObject.SetActive(true);
                    toastPickup.available = true;
                    lines?.Say("toast_golden");
                    break;
                case ToastOutcome.Burnt:
                    SpawnFailedToast(launched: false);
                    Fail("toast_burnt");
                    break;
                case ToastOutcome.Launched:
                    SpawnFailedToast(launched: true);
                    Fail("toast_launched");
                    break;
            }
        }

        void Fail(string lineId)
        {
            frustration?.Request(tuning.failed);
            if (session.AlarmShouldRing && alarm != null && !alarm.Ringing) alarm.StartRinging();
            else lines?.Say(lineId);
            StartCoroutine(Cooldown());
        }

        IEnumerator Cooldown()
        {
            Toaster = ToasterState.Cooldown;
            toaster.available = false;
            yield return new WaitForSeconds(tuning.cooldownAfterFailure);
            SetIdle();
        }

        void OnPickup(TaskInteractable t)
        {
            if (Toaster != ToasterState.Ready) return;
            toastPickup.available = false;
            toastPickup.gameObject.SetActive(false);
            frustration?.Request(tuning.success);
            lines?.Say("toast_taken");
            Toaster = ToasterState.Idle;
            toaster.available = false;
            Complete();
        }

        IEnumerator GhostEatsToast()
        {
            Toaster = ToasterState.Cooldown;
            toastPickup.available = false;
            toastPickup.gameObject.SetActive(false);
            ToastsEaten++;
            frustration?.Request(tuning.eatenByGhost);
            if (ghostCat != null) ghostCat.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            if (ghostCat != null) ghostCat.SetActive(false);
            lines?.Say("toast_eaten");
            yield return new WaitForSeconds(tuning.cooldownAfterFailure);
            SetIdle();
        }

        void SpawnFailedToast(bool launched)
        {
            var toast = GameObject.CreatePrimitive(PrimitiveType.Cube);
            toast.name = launched ? "GB_Toast_Launched" : "GB_Toast_Burnt";
            Destroy(toast.GetComponent<Collider>());
            toast.transform.position = toastSpawn.position;
            toast.transform.localScale = new Vector3(0.12f, 0.02f, 0.12f);
            if (burntMaterial != null) toast.GetComponent<Renderer>().sharedMaterial = burntMaterial;
            if (launched && launchTargets.Length > 0) StartCoroutine(Fly(toast.transform, PickTarget()));
            Destroy(toast, 20f);
        }

        Vector3 PickTarget()
        {
            // floor 60%, counter 30%, ceiling fan 10% (GDD: "ceiling fan (rare)")
            float r = Random.value;
            int i = r < 0.6f ? 0 : r < 0.9f ? 1 : 2;
            return launchTargets[Mathf.Min(i, launchTargets.Length - 1)].position;
        }

        IEnumerator Fly(Transform toast, Vector3 target)
        {
            Vector3 start = toast.position;
            float dur = 0.7f, apex = Mathf.Max(start.y, target.y) + 0.8f;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                if (toast == null) yield break;
                float k = t / dur;
                Vector3 p = Vector3.Lerp(start, target, k);
                p.y = Mathf.Lerp(Mathf.Lerp(start.y, apex, k), Mathf.Lerp(apex, target.y, k), k);
                toast.position = p;
                toast.Rotate(720f * Time.deltaTime, 0f, 360f * Time.deltaTime);
                yield return null;
            }
            if (toast != null) toast.position = target;
        }

        /// <summary>Tests: pretend the marker is at a given spot and press.</summary>
        public void DebugPressAt(float position)
        {
            if (Toaster != ToasterState.Toasting) return;
            MarkerPosition = position;
            Resolve(TimingQte.Judge(position, tuning.goldenZoneStart, tuning.goldenZoneEnd));
        }
    }
}
