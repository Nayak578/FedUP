using System.Collections;
using System.Collections.Generic;
using FedUp.Audio;
using FedUp.Frustration;
using FedUp.Interaction;
using FedUp.Story;
using UnityEngine;

namespace FedUp.Tasks.Cleanup
{
    /// <summary>
    /// GDD Phase 1, Clean Up Mess. Pick up one piece of mess at a time and drop it in the right container
    /// (trash bin or laundry basket). Each object says something about his life. Little ghost effects: clutter
    /// twitches when you're not looking, the light flickers. Rules live in <see cref="CleanupSession"/>.
    /// </summary>
    public class CleanupTask : TaskBehaviour
    {
        [SerializeField] CleanupTuningSO tuning;
        [SerializeField] List<ClutterItem> items = new List<ClutterItem>();
        [SerializeField] List<ClutterBin> bins = new List<ClutterBin>();
        [SerializeField] Transform holdPoint;
        [SerializeField] Transform viewer;
        [SerializeField] Light flickerLight;
        [SerializeField] FrustrationRequestChannelSO frustration;
        [SerializeField] FrustrationChannelSO frustrationState;
        [SerializeField] MonologueLibrarySO lines;

        public CleanupSession Session { get; private set; }
        public ClutterItem Carried { get; private set; }
        public int Rejections { get; private set; }
        public int ComedyFlybacks { get; private set; }
        /// <summary>Tests: force the ghost's random roll (0..1). Null = random.</summary>
        public float? ForcedRoll { get; set; }

        public override string Objective => Session == null ? objective : $"{objective}   ({Session.Binned}/{Session.Total} cleaned)";

        AudioSource sfx;
        float twitchTimer, flickerTimer;

        void Awake()
        {
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            foreach (var i in items) i.Pickup.available = false;
            foreach (var b in bins) b.Target.available = false;
        }

        protected override void OnBegin()
        {
            Session = new CleanupSession(items.Count, tuning.interferenceAbove, tuning.rejectChance, tuning.streakForBreak,
                tuning.ghostBreakSeconds, tuning.cleanRoomSeconds, tuning.comedyFlybackOnce);
            foreach (var i in items) { var item = i; item.Pickup.available = true; item.Pickup.Used += _ => PickUp(item); }
            foreach (var b in bins) { var bin = b; bin.Target.Used += _ => DropInto(bin); }
        }

        void PickUp(ClutterItem item)
        {
            if (Carried != null || item.InBin) return;
            Carried = item;
            foreach (var i in items) i.Pickup.available = false;
            foreach (var b in bins) b.Target.available = true;
            var col = item.GetComponent<Collider>(); if (col) col.enabled = false;
            item.transform.SetParent(holdPoint, true);
            item.transform.localPosition = Vector3.zero;
            sfx.PlayOneShot(PlaceholderTones.Get("click"));
            lines?.Say(item.pickupLineId);
        }

        void DropInto(ClutterBin bin)
        {
            if (Carried == null) return;
            if (bin.accepts != Carried.kind) { lines?.Say("wrong_bin"); return; }

            var item = Carried;
            Carried = null;
            item.transform.SetParent(null, true);
            float f = frustrationState != null ? frustrationState.Last.Hidden : 0f;
            var result = Session.Drop(f, ForcedRoll ?? Random.value);
            switch (result)
            {
                case DropResult.Binned:
                    item.InBin = true;
                    item.transform.position = bin.transform.position + Vector3.up * 0.15f;
                    item.gameObject.SetActive(false);
                    sfx.PlayOneShot(PlaceholderTones.Get("thunk"));
                    break;
                case DropResult.Rejected:
                    Rejections++;
                    Reject(item, bin);
                    break;
                case DropResult.ComedyFlyback:
                    ComedyFlybacks++;
                    Reject(item, bin);
                    break;
            }

            foreach (var b in bins) b.Target.available = false;
            foreach (var i in items) i.Pickup.available = !i.InBin;
            if (Session.Done) Complete();
        }

        void Reject(ClutterItem item, ClutterBin bin)
        {
            frustration?.Request(tuning.flyback);
            frustration?.Request(tuning.giggle);
            sfx.PlayOneShot(PlaceholderTones.Get("giggle"));
            lines?.Say("flyback");
            Vector3 land = item.Home + new Vector3(Random.Range(-0.6f, 0.6f), 0f, Random.Range(-0.6f, 0.6f));
            StartCoroutine(Fly(item, bin.transform.position + Vector3.up * 0.6f, land));
        }

        IEnumerator Fly(ClutterItem item, Vector3 from, Vector3 to)
        {
            var col = item.GetComponent<Collider>();
            for (float t = 0; t < 0.6f; t += Time.deltaTime)
            {
                float k = t / 0.6f;
                Vector3 p = Vector3.Lerp(from, to, k);
                p.y += Mathf.Sin(k * Mathf.PI) * 0.9f;
                item.transform.position = p;
                item.transform.Rotate(400f * Time.deltaTime, 250f * Time.deltaTime, 0f);
                yield return null;
            }
            item.transform.position = to;
            if (col) col.enabled = true;
        }

        void Update()
        {
            if (Session == null) return;
            Session.Tick(Time.deltaTime);
            if (Session.CleanBonusThisTick) { frustration?.Request(tuning.roomClean); lines?.Say("clean_bonus"); }
            if (State != TaskState.Active) return;

            // small ghost effects: a twitch when you're not looking, an occasional light flicker
            twitchTimer += Time.deltaTime;
            if (twitchTimer >= tuning.twitchEverySeconds)
            {
                twitchTimer = 0f;
                foreach (var i in items)
                {
                    if (i.InBin || i == Carried || viewer == null) continue;
                    if (Vector3.Angle(viewer.forward, i.transform.position - viewer.position) > 70f) { StartCoroutine(Twitch(i.transform)); break; }
                }
            }
            flickerTimer += Time.deltaTime;
            if (flickerLight != null && flickerTimer > 11f) { flickerTimer = 0f; StartCoroutine(Flicker()); }
        }

        IEnumerator Twitch(Transform t)
        {
            Quaternion r = t.rotation;
            for (float k = 0; k < 0.35f; k += Time.deltaTime)
            {
                if (t == null || Carried != null && t == Carried.transform) yield break;
                t.rotation = r * Quaternion.Euler(0f, Mathf.Sin(k * 70f) * 12f, 0f);
                yield return null;
            }
            if (t != null) t.rotation = r;
        }

        IEnumerator Flicker()
        {
            bool was = flickerLight.enabled;
            for (int i = 0; i < 3; i++) { flickerLight.enabled = !was; yield return new WaitForSeconds(0.05f); flickerLight.enabled = was; yield return new WaitForSeconds(0.08f); }
        }

        public override void DebugComplete()
        {
            foreach (var i in items) { i.InBin = true; i.gameObject.SetActive(false); i.Pickup.available = false; }
            foreach (var b in bins) b.Target.available = false;
            base.DebugComplete();
        }
    }
}
