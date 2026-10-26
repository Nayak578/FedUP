using System.Collections;
using System.Collections.Generic;
using FedUp.Audio;
using FedUp.Frustration;
using FedUp.Ghost;
using FedUp.Interaction;
using FedUp.Qte;
using FedUp.Story;
using UnityEngine;

namespace FedUp.Tasks.Tea
{
    /// <summary>
    /// GDD Make Tea, the first Phase 3 task. Gather milk, sugar and tea in the kitchen: the lights stutter and they
    /// vanish into the house. Carry two at a time to the tea station. Walk back into the kitchen with the last one
    /// and the power cuts: flip the breaker in the back hall within 30 s, or everything re-scatters (+10, once a
    /// night). Put the kettle on; it screams in the boss's voice and the cabinets start throwing. Stay in the
    /// kitchen and dodge for 20 s (+7 a hit). Three hits and the stove catches fire (+15, no tea). Survive, turn the
    /// stove off: tea (-10).
    /// </summary>
    public class TeaTask : TaskBehaviour
    {
        public enum Step { Gather, Hunt, Boil, Dodge, Fire, StoveOff, Done }

        [SerializeField] TeaTuningSO tuning;
        [SerializeField] TaskInteractable[] items = new TaskInteractable[0];
        [SerializeField] string[] itemNames = { "milk", "sugar", "tea" };
        [SerializeField] Transform[] hideSpots = new Transform[0];
        [SerializeField] Transform[] placedSpots = new Transform[0];
        [SerializeField] TaskInteractable station;
        [SerializeField] TaskInteractable kettle;
        [SerializeField] TaskInteractable fuseBox;
        [Tooltip("The kitchen, as a box (this transform's centre and the size below).")]
        [SerializeField] Transform kitchen;
        [SerializeField] Vector3 kitchenSize = new Vector3(6f, 4f, 7f);
        [SerializeField] Transform[] throwFrom = new Transform[0];
        [SerializeField] GameObject junkOnStove;
        [SerializeField] Material junkMaterial;
        [SerializeField] Light fireLight;
        [SerializeField] ParticleSystem fireFx;
        [SerializeField] Transform player;
        [SerializeField] Transform eye;
        [SerializeField] NightPhaseController phase;
        [SerializeField] FrustrationRequestChannelSO frustration;
        [SerializeField] QteDisplayChannelSO qte;
        [SerializeField] SubtitleChannelSO subtitles;
        [SerializeField] MonologueLibrarySO lines;

        public TeaSession Session { get; private set; }
        public DodgeSession Dodge { get; private set; }
        public Step Current { get; private set; } = Step.Gather;
        public int Throws { get; private set; }
        /// <summary>Throws that actually left the cabinet (after the bang): dodge these in flight.</summary>
        public int Launched { get; private set; }
        public bool InKitchen => InKitchenAt(player.position);

        public override string Objective
        {
            get
            {
                switch (Current)
                {
                    case Step.Gather: return "Make tea: get the milk, sugar and tea";
                    case Step.Hunt:
                        if (Session.Stage == TeaStage.PowerCut)
                            return Session.PowerLeft > 0f ? $"The power's out: the fuse box in the back hall ({Mathf.CeilToInt(Session.PowerLeft)} s)" : "The power's out: the fuse box in the back hall";
                        return "Find the milk, sugar and tea" + (Session.Carrying > 0 ? $"  (carrying {CarriedNames()})" : "");
                    case Step.Boil: return "Wait for the kettle";
                    case Step.Dodge: return "Dodge! Stay in the kitchen";
                    case Step.Fire: return "Put out the fire";
                    case Step.StoveOff: return "Turn off the stove";
                    default: return objective;
                }
            }
        }

        AudioSource sfx, loop;
        bool wasInKitchen;
        float nextThrow;
        readonly List<Transform> flying = new List<Transform>();

        void Awake()
        {
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            loop = (kettle != null ? kettle.gameObject : gameObject).AddComponent<AudioSource>();
            loop.playOnAwake = false; loop.loop = true; loop.spatialBlend = 0.8f;
            foreach (var t in items) if (t != null) t.available = false;
            foreach (var t in new[] { station, kettle, fuseBox }) if (t != null) t.available = false;
            if (junkOnStove != null) junkOnStove.SetActive(false);
            if (fireLight != null) fireLight.enabled = false;
            if (fireFx != null) fireFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        protected override void OnBegin()
        {
            Session = new TeaSession(items.Length, tuning.carryLimit, tuning.powerSeconds, tuning.maxRescatters);
            phase?.EnterPhase3();
            for (int i = 0; i < items.Length; i++)
            {
                int k = i;
                items[i].available = true;
                items[i].prompt = "Take the " + itemNames[i];
                items[i].Used += _ => OnItem(k);
            }
            station.prompt = "Put them down";
            station.Used += _ => OnStation();
            fuseBox.prompt = "Flip the breaker";
            fuseBox.mode = InteractionMode.Hold; fuseBox.holdSeconds = 1f;
            fuseBox.Used += _ => OnFuse();
            kettle.Used += _ => OnKettle();
            lines?.Say("tea_start");
            wasInKitchen = InKitchen;
        }

        bool InKitchenAt(Vector3 p)
        {
            Vector3 d = p - kitchen.position;
            return Mathf.Abs(d.x) <= kitchenSize.x / 2f && Mathf.Abs(d.z) <= kitchenSize.z / 2f && Mathf.Abs(d.y) <= kitchenSize.y / 2f;
        }

        string CarriedNames()
        {
            var names = new List<string>();
            for (int i = 0; i < items.Length; i++) if (Session.StateOf(i) == TeaItemState.Carried) names.Add(itemNames[i]);
            return string.Join(", ", names);
        }

        // ---------------- the hunt ----------------

        void OnItem(int i)
        {
            if (Current == Step.Gather)
            {
                Session.Gather(i);
                items[i].available = false;
                items[i].gameObject.SetActive(false);
                sfx.PlayOneShot(PlaceholderTones.Get("click"));
                if (Session.Stage == TeaStage.Search) StartCoroutine(Vanish());
                return;
            }
            if (Current != Step.Hunt) return;
            if (!Session.PickUp(i)) { lines?.Say("tea_hands_full"); return; }
            items[i].available = false;
            items[i].gameObject.SetActive(false);
            sfx.PlayOneShot(PlaceholderTones.Get("click"));
            station.available = true;
        }

        IEnumerator Vanish()
        {
            phase?.Flicker(1.2f);
            sfx.PlayOneShot(PlaceholderTones.Get("giggle"));
            yield return new WaitForSeconds(1.2f);
            Hide();
            Current = Step.Hunt;
            lines?.Say("tea_vanished");
        }

        /// <summary>Everything still hidden goes to (or back to) its hiding place.</summary>
        void Hide()
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (Session.StateOf(i) != TeaItemState.Hidden) continue;
                items[i].transform.SetPositionAndRotation(hideSpots[i].position, hideSpots[i].rotation);
                items[i].gameObject.SetActive(true);
                items[i].available = true;
                items[i].prompt = "Take the " + itemNames[i];
            }
        }

        void OnStation()
        {
            if (Current != Step.Hunt) return;
            if (!phase.PowerOn) { lines?.Say("tea_dark"); return; }
            for (int i = 0; i < items.Length; i++)
            {
                if (Session.StateOf(i) != TeaItemState.Carried || !Session.Place(i)) continue;
                items[i].transform.SetPositionAndRotation(placedSpots[i].position, placedSpots[i].rotation);
                items[i].gameObject.SetActive(true);
                items[i].available = false;
            }
            sfx.PlayOneShot(PlaceholderTones.Get("pop"));
            station.available = false;
            if (Session.Stage == TeaStage.Brew)
            {
                kettle.available = true;
                kettle.prompt = "Put the kettle on";
                kettle.mode = InteractionMode.Press;
                lines?.Say("tea_all_here");
            }
        }

        void OnFuse()
        {
            if (Session.Stage != TeaStage.PowerCut) return;
            Session.RestorePower();
            phase?.SetPower(true);
            fuseBox.available = false;
            sfx.PlayOneShot(PlaceholderTones.Get("thunk"));
            qte?.Raise(QteDisplay.Hidden);
            lines?.Say("tea_power_back");
        }

        // ---------------- the kettle and the dodge ----------------

        void OnKettle()
        {
            switch (Current)
            {
                case Step.Hunt when Session.Stage == TeaStage.Brew:
                    kettle.available = false;
                    StartCoroutine(Boil());
                    break;
                case Step.Fire:
                    PutOut();
                    break;
                case Step.StoveOff:
                    StoveOff();
                    break;
            }
        }

        IEnumerator Boil()
        {
            Current = Step.Boil;
            loop.clip = PlaceholderTones.Get("kettle");
            loop.Play();
            yield return new WaitForSeconds(tuning.boilSeconds);
            // the absurd beat (STORY_FLOW): the kettle screams in the boss's voice
            subtitles?.Say("KETTLE (the boss's voice):  'WHERE.  IS.  MY.  FILE.'", 3f, SubtitleStyle.Caption);
            sfx.PlayOneShot(PlaceholderTones.Get("sting"));
            yield return new WaitForSeconds(1.2f);
            loop.Stop();
            if (junkOnStove != null) junkOnStove.SetActive(true);
            sfx.PlayOneShot(PlaceholderTones.Get("slam"));
            lines?.Say("tea_dodge");
            Dodge = new DodgeSession(tuning.dodgeSeconds, tuning.hitsForFire);
            nextThrow = Time.time + 1.2f;
            Current = Step.Dodge;
        }

        void Update()
        {
            if (Session == null || State != TaskState.Active) return;
            bool inKitchen = InKitchen;
            if (inKitchen && !wasInKitchen && Current == Step.Hunt && Session.EnterKitchen()) PowerCut();
            wasInKitchen = inKitchen;

            if (Current == Step.Hunt && Session.Stage == TeaStage.PowerCut)
            {
                Session.Tick(Time.deltaTime);
                if (Session.PowerLeft > 0f)
                    qte?.Raise(new QteDisplay(true, QteKind.Mash, 0, 0, 0, Session.PowerLeft / tuning.powerSeconds, "THE POWER'S OUT"));
                else qte?.Raise(QteDisplay.Hidden);
                if (Session.RescatteredThisTick) Rescatter();
            }

            if (Current == Step.Dodge)
            {
                Dodge.Tick(Time.deltaTime, inKitchen);
                float left = Mathf.Max(0f, tuning.dodgeSeconds - Dodge.Elapsed);
                qte?.Raise(new QteDisplay(true, QteKind.Mash, 0, 0, 0, left / tuning.dodgeSeconds, $"DODGE!   {Mathf.CeilToInt(left)}"));
                if (inKitchen && Time.time >= nextThrow && !Dodge.Done)
                {
                    nextThrow = Time.time + tuning.throwEvery;
                    StartCoroutine(Throw());
                }
                if (Dodge.Survived) { qte?.Raise(QteDisplay.Hidden); Current = Step.StoveOff; kettle.available = true; kettle.prompt = "Turn off the stove"; kettle.mode = InteractionMode.Hold; kettle.holdSeconds = 1f; lines?.Say("tea_survived"); }
            }
        }

        void PowerCut()
        {
            phase?.SetPower(false);
            sfx.PlayOneShot(PlaceholderTones.Get("thunk"));
            fuseBox.available = true;
            station.available = Session.Carrying > 0;
            lines?.Say("tea_power_cut");
        }

        void Rescatter()
        {
            frustration?.Request(tuning.rescatter);
            phase?.SetPower(true);
            fuseBox.available = false;
            station.available = false;
            for (int i = 0; i < items.Length; i++) if (Session.StateOf(i) == TeaItemState.Hidden) items[i].gameObject.SetActive(false);
            sfx.PlayOneShot(PlaceholderTones.Get("giggle"));
            Hide();
            lines?.Say("tea_rescatter");
        }

        IEnumerator Throw()
        {
            if (throwFrom.Length == 0) yield break;
            var from = throwFrom[Throws % throwFrom.Length];
            Throws++;
            sfx.PlayOneShot(PlaceholderTones.Get("thunk")); // a cabinet bangs open: here it comes
            yield return new WaitForSeconds(tuning.throwWarning);
            if (Dodge == null || Dodge.Done) yield break;
            var junk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            junk.name = "FX_ThrownJunk";
            Destroy(junk.GetComponent<Collider>());
            junk.transform.localScale = new Vector3(0.18f, 0.12f, 0.22f);
            if (junkMaterial != null) junk.GetComponent<Renderer>().sharedMaterial = junkMaterial;
            flying.Add(junk.transform);
            Launched++;
            Vector3 start = from.position;
            Vector3 chest = player.position + Vector3.up * 1.2f;
            sfx.PlayOneShot(PlaceholderTones.Get("whoosh"));
            for (float t = 0f; t < tuning.throwFlight; t += Time.deltaTime)
            {
                float k = t / tuning.throwFlight;
                Vector3 p = Vector3.Lerp(start, chest, k);
                p.y += Mathf.Sin(k * Mathf.PI) * 0.6f;
                junk.transform.position = p;
                junk.transform.Rotate(500f * Time.deltaTime, 320f * Time.deltaTime, 0f);
                yield return null;
            }
            // where you are now, not where you were: if you moved, it misses
            if (Dodge != null && !Dodge.Done && Vector3.Distance(player.position + Vector3.up * 1.2f, chest) <= tuning.hitRadius) OnHit();
            flying.Remove(junk.transform);
            Destroy(junk, 0.05f);
        }

        void OnHit()
        {
            sfx.PlayOneShot(PlaceholderTones.Get("thunk"));
            frustration?.Request(tuning.hit);
            bool fire = Dodge.Hit();
            if (!fire) { lines?.Say("tea_hit"); return; }
            frustration?.Request(tuning.fire);
            qte?.Raise(QteDisplay.Hidden);
            Current = Step.Fire;
            if (fireLight != null) fireLight.enabled = true;
            if (fireFx != null) fireFx.Play();
            loop.clip = PlaceholderTones.Get("fire");
            loop.Play();
            kettle.available = true;
            kettle.prompt = "Put out the fire";
            kettle.mode = InteractionMode.Hold;
            kettle.holdSeconds = 2f;
            lines?.Say("tea_fire");
        }

        void PutOut()
        {
            if (fireLight != null) fireLight.enabled = false;
            if (fireFx != null) fireFx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            loop.Stop();
            kettle.available = false;
            lines?.Say("tea_no_tea");
            Finish();
        }

        void StoveOff()
        {
            kettle.available = false;
            frustration?.Request(tuning.made);
            lines?.Say("tea_made");
            Finish();
        }

        void Finish()
        {
            Current = Step.Done;
            if (junkOnStove != null) junkOnStove.SetActive(false);
            Complete();
        }

        public override void DebugComplete()
        {
            StopAllCoroutines();
            foreach (var f in flying) if (f != null) Destroy(f.gameObject);
            flying.Clear();
            qte?.Raise(QteDisplay.Hidden);
            if (phase != null && !phase.PowerOn) phase.SetPower(true);
            if (phase != null && phase.Phase < 3) phase.EnterPhase3();
            foreach (var t in items) if (t != null) t.available = false;
            foreach (var t in new[] { station, kettle, fuseBox }) if (t != null) t.available = false;
            if (fireLight != null) fireLight.enabled = false;
            if (loop != null) loop.Stop();
            Current = Step.Done;
            base.DebugComplete();
        }
    }
}
