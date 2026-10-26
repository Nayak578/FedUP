using System.Collections;
using FedUp.Audio;
using FedUp.Frustration;
using FedUp.Fx;
using FedUp.Player;
using FedUp.Story;
using UnityEngine;
using UnityEngine.AI;

namespace FedUp.Tasks.Cat
{
    /// <summary>
    /// GDD Phase 1 Cat Visit #1 (+ Task 8 rules). Once you stand or sit still, a cat you don't own walks in from
    /// the dark back hall and sits near you. Hold E to pet it: it purrs, the screen softens (-20). Above 60% it
    /// may hiss and vanish to the couch instead; try again after 30 s (-10). Ignore it and it walks back into the
    /// dark. Either way the visit is recorded for the ending (mandatory-cat rule).
    /// </summary>
    public class CatVisitTask : TaskBehaviour
    {
        [SerializeField, Range(1, 3)] int visit = 1;
        [SerializeField] CatTuningSO tuning;
        [SerializeField] NavMeshAgent cat;
        [SerializeField] TaskInteractable petTarget;
        [SerializeField] Transform body;
        [Tooltip("Where it comes in from and goes back to.")]
        [SerializeField] Transform entry;
        [Tooltip("Where it reappears after a hiss (GDD waypoints: bed, couch, windowsill).")]
        [SerializeField] Transform hissPerch;
        [SerializeField] FirstPersonMotor motor;
        [SerializeField] FrustrationRequestChannelSO frustration;
        [SerializeField] FrustrationChannelSO frustrationState;
        [SerializeField] RunStateSO runState;
        [SerializeField] MonologueLibrarySO lines;
        [SerializeField] ScreenSoften glow;
        [Tooltip("The Mr. Robot touch (designer, 26 Oct): said to you, deniably, after the cat has gone.")]
        [SerializeField] string toPlayerLineId = "to_player_cat";

        public CatVisitSession Session { get; private set; }
        public bool Arrived { get; private set; }
        public bool Hissed { get; private set; }
        public NavMeshAgent Cat => cat;
        /// <summary>Tests/autopilot: the next pet's dice roll (hiss check).</summary>
        public float? ForcedRoll { get; set; }

        AudioSource voice;
        Vector3 bodyScale;
        bool skipWait;

        void Awake()
        {
            if (petTarget != null) { petTarget.available = false; petTarget.prompt = "Pet the cat"; }
            voice = cat != null ? cat.gameObject.AddComponent<AudioSource>() : gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0.8f;
            if (body != null) bodyScale = body.localScale;
            if (cat != null) cat.gameObject.SetActive(false);
        }

        protected override void OnBegin()
        {
            Session = new CatVisitSession(tuning.hissAbove, tuning.hissChance, tuning.retryCooldownSeconds,
                tuning.patienceSeconds, tuning.petted != null ? tuning.petted.amount : -20f,
                tuning.pettedAfterHiss != null ? tuning.pettedAfterHiss.amount : -10f);
            petTarget.holdSeconds = tuning.petHoldSeconds;
            petTarget.Used += _ => Pet();
            StartCoroutine(Visit());
        }

        /// <summary>Tests: don't wait for the player to stand still.</summary>
        public void DebugArriveNow() => skipWait = true;

        IEnumerator Visit()
        {
            // GDD: it comes in while you're sitting or standing still
            float still = 0f;
            Vector3 last = motor.transform.position;
            while (still < tuning.arriveAfterStillSeconds && !skipWait)
            {
                yield return null;
                Vector3 now = motor.transform.position;
                still = (now - last).sqrMagnitude < 0.0004f ? still + Time.deltaTime : 0f;
                last = now;
            }

            cat.gameObject.SetActive(true);
            cat.Warp(entry.position);
            cat.speed = tuning.walkSpeed;
            yield return WalkTo(SitSpotNearPlayer(), 40f);
            SitDown();
            Arrived = true;
            lines?.Say("cat_arrive");

            while (!Session.Done)
            {
                Session.Tick(Time.deltaTime);
                if (Hissed && Session.CooldownLeft <= 0f && !petTarget.available && !Session.Done) petTarget.available = true;
                if (Session.LeftThisTick) { yield return Leave(false); yield break; }
                yield return null;
            }
        }

        Vector3 SitSpotNearPlayer()
        {
            Vector3 p = motor.transform.position;
            Vector3 from = entry.position - p; from.y = 0f;
            Vector3 want = p + (from.sqrMagnitude > 0.01f ? from.normalized : Vector3.forward) * tuning.sitDistance;
            return NavMesh.SamplePosition(want, out var hit, 1.5f, NavMesh.AllAreas) ? hit.position : p;
        }

        IEnumerator WalkTo(Vector3 to, float timeout)
        {
            StandUp();
            if (!cat.enabled)
            {
                // hopping down from a perch that isn't walkable floor
                cat.enabled = true;
                if (NavMesh.SamplePosition(cat.transform.position, out var down, 2f, NavMesh.AllAreas)) cat.Warp(down.position);
            }
            cat.isStopped = false;
            cat.SetDestination(to);
            float t = 0f;
            while ((cat.pathPending || cat.remainingDistance > 0.15f) && t < timeout) { t += Time.deltaTime; yield return null; }
            cat.isStopped = true;
        }

        void SitDown()
        {
            Vector3 look = motor.transform.position - cat.transform.position; look.y = 0f;
            if (look.sqrMagnitude > 0.01f) cat.transform.rotation = Quaternion.LookRotation(look);
            if (body != null) body.localScale = new Vector3(bodyScale.x, bodyScale.y * 0.7f, bodyScale.z * 0.8f); // sits: lower and shorter
            petTarget.available = true;
        }

        void StandUp()
        {
            if (body != null) body.localScale = bodyScale;
            petTarget.available = false;
        }

        void Pet()
        {
            if (!Arrived || Session.Done || State != TaskState.Active) return;
            float f = frustrationState != null ? frustrationState.Last.Hidden : 0f;
            float roll = ForcedRoll ?? Random.value;
            ForcedRoll = null;
            switch (Session.Pet(f, roll))
            {
                case PetResult.Purred: StartCoroutine(Purr()); break;
                case PetResult.Hissed: StartCoroutine(Hiss()); break;
            }
        }

        IEnumerator Purr()
        {
            petTarget.available = false;
            var ev = Session.HissedOnce ? tuning.pettedAfterHiss : tuning.petted;
            if (ev != null) frustration?.Request(ev);
            runState?.RecordCatVisit(visit, true);
            voice.clip = PlaceholderTones.Get("purr");
            voice.loop = true;
            voice.Play();
            glow?.Pulse();
            lines?.Say("cat_pet");
            yield return new WaitForSeconds(4f);
            voice.loop = false;
            voice.Stop();
            yield return Leave(true);
        }

        IEnumerator Hiss()
        {
            Hissed = true;
            petTarget.available = false;
            voice.PlayOneShot(PlaceholderTones.Get("hiss"));
            lines?.Say("cat_hiss");
            yield return new WaitForSeconds(0.4f);
            cat.gameObject.SetActive(false); // GDD: it hisses and teleports (a puff of smoke in the art pass)
            yield return new WaitForSeconds(0.6f);
            cat.enabled = false; // the perch (the couch) isn't walkable floor
            cat.transform.position = hissPerch.position;
            cat.gameObject.SetActive(true);
            SitDown();
            petTarget.available = false; // until the cooldown is over
        }

        IEnumerator Leave(bool petted)
        {
            if (!petted)
            {
                runState?.RecordCatVisit(visit, false);
                lines?.Say("cat_left");
            }
            yield return WalkTo(entry.position, 25f);
            cat.gameObject.SetActive(false);
            if (visit == 1 && !string.IsNullOrEmpty(toPlayerLineId)) StartCoroutine(SayLater(toPlayerLineId, 2.8f));
            Complete();
        }

        IEnumerator SayLater(string id, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            lines?.Say(id);
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
