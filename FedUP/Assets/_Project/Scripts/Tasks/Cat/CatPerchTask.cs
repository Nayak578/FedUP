using System.Collections;
using FedUp.Audio;
using FedUp.Frustration;
using FedUp.Fx;
using FedUp.Ghost;
using FedUp.Player;
using FedUp.Story;
using UnityEngine;
using UnityEngine.AI;

namespace FedUp.Tasks.Cat
{
    /// <summary>
    /// GDD Cat Visit #2-3 (DESIGN_LOGIC #67-69): the cat is already somewhere awkward, on a perch, and its collar bell
    /// tells you roughly where. Walk up and hold E to pet it (-20; -10 after a hiss). Sprint at it and it jumps to its
    /// other perch. Above 60% it may hiss and jump (retry after 30 s). A pet hushes the house for a while. Ignore it and
    /// it leaves. Either way the visit is recorded for the ending (mandatory-cat rule).
    /// </summary>
    public class CatPerchTask : TaskBehaviour
    {
        [SerializeField, Range(2, 3)] int visit = 2;
        [SerializeField] CatTuningSO tuning;
        [SerializeField] NavMeshAgent cat;
        [SerializeField] TaskInteractable petTarget;
        [SerializeField] Transform body;
        [SerializeField] Transform perch;
        [SerializeField] Transform otherPerch;
        [SerializeField] FirstPersonMotor motor;
        [SerializeField] NightPhaseController phase;
        [SerializeField] FrustrationRequestChannelSO frustration;
        [SerializeField] FrustrationChannelSO frustrationState;
        [SerializeField] RunStateSO runState;
        [SerializeField] MonologueLibrarySO lines;
        [SerializeField] ScreenSoften glow;
        [SerializeField] string arriveLineId = "cat_back";

        public CatVisitSession Session { get; private set; }
        public Transform CurrentPerch { get; private set; }
        public int Spooked { get; private set; }
        public bool Hissed { get; private set; }
        public float? ForcedRoll { get; set; }

        AudioSource voice;
        Vector3 bodyScale;
        float nextJingle;
        bool hopping;

        void Awake()
        {
            if (body != null) bodyScale = body.localScale;
        }

        protected override void OnBegin()
        {
            Session = new CatVisitSession(tuning.hissAbove, tuning.hissChance, tuning.retryCooldownSeconds, tuning.perchPatienceSeconds,
                tuning.petted != null ? tuning.petted.amount : -20f, tuning.pettedAfterHiss != null ? tuning.pettedAfterHiss.amount : -10f);
            voice = cat.GetComponent<AudioSource>();
            if (voice == null) { voice = cat.gameObject.AddComponent<AudioSource>(); voice.playOnAwake = false; voice.spatialBlend = 0.8f; }
            petTarget.holdSeconds = tuning.petHoldSeconds;
            petTarget.prompt = "Pet the cat";
            petTarget.Used += _ => Pet();
            SitOn(perch);
            nextJingle = Time.time + 0.5f;
            lines?.Say(arriveLineId);
        }

        void SitOn(Transform p)
        {
            cat.enabled = false; // perches aren't walkable floor
            cat.transform.SetPositionAndRotation(p.position, p.rotation);
            cat.gameObject.SetActive(true);
            if (body != null) body.localScale = new Vector3(bodyScale.x, bodyScale.y * 0.7f, bodyScale.z * 0.8f);
            CurrentPerch = p;
            petTarget.available = !Hissed || Session.CooldownLeft <= 0f;
        }

        void Update()
        {
            if (State != TaskState.Active || Session == null || Session.Done || hopping) return;
            Session.Tick(Time.deltaTime);
            if (Hissed && Session.CooldownLeft <= 0f && !petTarget.available) petTarget.available = true;
            if (Time.time >= nextJingle)
            {
                voice.PlayOneShot(PlaceholderTones.Get("jingle"), 0.5f);
                nextJingle = Time.time + tuning.jingleEverySeconds;
            }
            // GDD: "approach slowly". Running at it sends it to its other perch.
            float d = Vector3.Distance(motor.transform.position, cat.transform.position);
            if (d < tuning.spookRadius && motor.CurrentSpeed > motor.Tuning.walkSpeed * 1.15f)
            {
                Spooked++;
                lines?.Say("cat_spooked");
                StartCoroutine(Hop(false));
                return;
            }
            if (Session.LeftThisTick) StartCoroutine(Leave());
        }

        IEnumerator Hop(bool hiss)
        {
            hopping = true;
            petTarget.available = false;
            voice.PlayOneShot(PlaceholderTones.Get(hiss ? "hiss" : "jingle"));
            yield return new WaitForSeconds(0.3f);
            cat.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.5f);
            SitOn(CurrentPerch == perch ? otherPerch : perch);
            if (hiss) petTarget.available = false; // until the retry cooldown is over
            hopping = false;
        }

        void Pet()
        {
            if (State != TaskState.Active || Session == null || Session.Done || hopping) return;
            float f = frustrationState != null ? frustrationState.Last.Hidden : 0f;
            float roll = ForcedRoll ?? Random.value;
            ForcedRoll = null;
            switch (Session.Pet(f, roll))
            {
                case PetResult.Purred: StartCoroutine(Purr()); break;
                case PetResult.Hissed:
                    Hissed = true;
                    lines?.Say("cat_hiss");
                    StartCoroutine(Hop(true));
                    break;
            }
        }

        IEnumerator Purr()
        {
            petTarget.available = false;
            var ev = Session.HissedOnce ? tuning.pettedAfterHiss : tuning.petted;
            if (ev != null) frustration?.Request(ev);
            runState?.RecordCatVisit(visit, true);
            phase?.Hush(tuning.hushSeconds);
            voice.clip = PlaceholderTones.Get("purr");
            voice.loop = true;
            voice.Play();
            glow?.Pulse();
            lines?.Say("cat_pet");
            yield return new WaitForSeconds(4f);
            voice.loop = false;
            voice.Stop();
            cat.gameObject.SetActive(false);
            Complete();
        }

        IEnumerator Leave()
        {
            petTarget.available = false;
            runState?.RecordCatVisit(visit, false);
            lines?.Say("cat_left");
            yield return new WaitForSeconds(0.5f);
            cat.gameObject.SetActive(false);
            Complete();
        }

        /// <summary>You went to bed with the cat still waiting: it leaves, unpetted.</summary>
        public void Dismiss()
        {
            if (State == TaskState.Waiting) { base.DebugComplete(); return; } // it hadn't come yet: now it won't
            if (State != TaskState.Active || Session == null || Session.Done) return;
            StopAllCoroutines();
            hopping = false;
            StartCoroutine(Leave());
        }

        public override void DebugComplete()
        {
            StopAllCoroutines();
            if (petTarget != null) petTarget.available = false;
            if (cat != null) cat.gameObject.SetActive(false);
            base.DebugComplete();
        }
    }
}
