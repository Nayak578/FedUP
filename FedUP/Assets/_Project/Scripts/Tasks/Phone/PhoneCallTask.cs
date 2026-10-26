using System.Collections;
using FedUp.Audio;
using FedUp.Frustration;
using FedUp.Story;
using UnityEngine;

namespace FedUp.Tasks.Phone
{
    /// <summary>
    /// A boss call (STORY_BIBLE.md: the calls are from the boss, about the file). The phone rings; answer it
    /// (garbled boss lines) or let it ring out (a buzz, "1 new voicemail"). The answer is recorded in RunState for
    /// the ending. Call #1 uses its own two values (-5 / +2). Calls #2-4 (with call tuning) follow GDD "Phone Rings
    /// #2-4": the phone is somewhere new each time, every ring after the first costs, a quick answer calms, the
    /// distorted call stings, and an ignored call can leave its message on the answering machine.
    /// </summary>
    public class PhoneCallTask : TaskBehaviour
    {
        [SerializeField, Range(1, 4)] int callNumber = 1;
        [SerializeField] TaskInteractable phone;
        [Tooltip("Calls #2-4: where the phone is this time (GDD: never in the same place twice). Empty = where it is.")]
        [SerializeField] Transform spawnAt;
        [SerializeField, Min(0.01f)] float delayBeforeRing = 2f;
        [SerializeField, Min(3f)] float ringSeconds = 15f;
        [Tooltip("Monologue-library ids of the boss's lines, played as captions in order.")]
        [SerializeField] string[] bossLineIds = new string[0];
        [Tooltip("Played when the call rings out (the answering machine), if any.")]
        [SerializeField] string[] voicemailLineIds = new string[0];
        [SerializeField] string afterAnswerLineId = "phone_after";
        [SerializeField] string ignoredLineId = "phone_ignored";
        [SerializeField] FrustrationEventSO answered;
        [SerializeField] FrustrationEventSO ignored;
        [Tooltip("Calls #2-4 only. Empty = call #1's rules.")]
        [SerializeField] CallTuningSO callTuning;
        [SerializeField] bool distorted;
        [SerializeField] FrustrationRequestChannelSO frustration;
        [SerializeField] RunStateSO runState;
        [SerializeField] MonologueLibrarySO lines;
        [SerializeField] SubtitleChannelSO subtitles;

        public int CallNumber => callNumber;
        public bool Ringing { get; private set; }
        public bool Answered { get; private set; }
        public bool Ignored { get; private set; }
        public CallSession Session { get; private set; }
        public TaskInteractable Phone => phone;

        AudioSource ring;

        void Awake()
        {
            ring = gameObject.AddComponent<AudioSource>();
            ring.playOnAwake = false;
            ring.loop = true;
            if (phone != null && callNumber == 1) { phone.available = false; phone.prompt = "Answer the phone"; }
        }

        protected override void OnBegin()
        {
            phone.Used += _ => Answer();
            StartCoroutine(Ring());
        }

        IEnumerator Ring()
        {
            yield return new WaitForSeconds(delayBeforeRing);
            if (spawnAt != null) phone.transform.SetPositionAndRotation(spawnAt.position, spawnAt.rotation);
            Ringing = true;
            phone.prompt = "Answer the phone";
            phone.available = true;
            ring.transform.position = phone.transform.position;
            ring.spatialBlend = 0.8f;
            ring.clip = PlaceholderTones.Get("ring");
            ring.pitch = distorted ? 0.8f : 1f;
            ring.Play();
            subtitles?.Say("[ the phone is ringing ]", 2.5f, SubtitleStyle.Caption);

            if (callTuning == null)
            {
                float t = 0f;
                while (Ringing && t < ringSeconds) { t += Time.deltaTime; yield return null; }
                if (Ringing) yield return LetItRingOut();
                yield break;
            }

            Session = new CallSession(callTuning.ringEverySeconds, callTuning.ringOutSeconds, callTuning.quickWithinSeconds,
                callTuning.ringAfterFirst != null ? callTuning.ringAfterFirst.amount : 2f,
                callTuning.answeredQuickly != null ? callTuning.answeredQuickly.amount : -5f,
                callTuning.distorted != null ? callTuning.distorted.amount : 5f, distorted);
            Session.Start();
            while (Ringing)
            {
                Session.Tick(Time.deltaTime);
                if (Session.RangThisTick)
                {
                    // GDD: "the phone pitch increases if ring escalates"; each extra ring costs (Appendix 1)
                    ring.pitch = (distorted ? 0.8f : 1f) + 0.03f * Session.Rings;
                    if (callTuning.ringAfterFirst != null) frustration?.Request(callTuning.ringAfterFirst);
                }
                if (Session.RangOut) { yield return LetItRingOut(); yield break; }
                yield return null;
            }
        }

        void Answer()
        {
            if (!Ringing || State != TaskState.Active) return;
            Ringing = false;
            Answered = true;
            phone.available = false;
            ring.Stop();
            if (Session != null)
            {
                bool quick = Session.Elapsed <= callTuning.quickWithinSeconds;
                Session.Answer();
                if (quick && callTuning.answeredQuickly != null) frustration?.Request(callTuning.answeredQuickly);
                if (distorted && callTuning.distorted != null) frustration?.Request(callTuning.distorted);
            }
            else frustration?.Request(answered);
            runState?.RecordCall(callNumber, true);
            StartCoroutine(BossTalks(bossLineIds, afterAnswerLineId));
        }

        IEnumerator BossTalks(string[] ids, string after)
        {
            foreach (var id in ids)
            {
                if (lines != null && lines.TryGet(id, out var e))
                {
                    subtitles?.Say(e.text, e.seconds, SubtitleStyle.Caption);
                    yield return new WaitForSeconds(e.seconds);
                }
            }
            lines?.Say(after);
            yield return new WaitForSeconds(0.5f);
            Complete();
        }

        IEnumerator LetItRingOut()
        {
            Ringing = false;
            Ignored = true;
            phone.available = false;
            ring.Stop();
            ring.PlayOneShot(PlaceholderTones.Get("buzz"));
            if (ignored != null) frustration?.Request(ignored);
            runState?.RecordCall(callNumber, false);
            subtitles?.Say("[ 1 new voicemail ]", 2.2f, SubtitleStyle.Caption);
            yield return new WaitForSeconds(2.3f);
            if (voicemailLineIds.Length > 0) { yield return BossTalks(voicemailLineIds, ignoredLineId); yield break; }
            lines?.Say(ignoredLineId);
            Complete();
        }

        /// <summary>
        /// You went to bed: a ringing call goes to the answering machine now (ignored), and a call that never got to ring
        /// never happens. Either way it's over and costs nothing more.
        /// </summary>
        public void RingOutNow()
        {
            if (State == TaskState.Waiting) { base.DebugComplete(); return; }
            if (State != TaskState.Active || Answered || Ignored) return;
            if (!Ringing) { StopAllCoroutines(); base.DebugComplete(); return; } // about to ring: it never does
            StopAllCoroutines();
            StartCoroutine(LetItRingOut());
        }

        /// <summary>Tests: skip the wait and start ringing now.</summary>
        public void DebugRingNow() => delayBeforeRing = 0.01f;

        public override void DebugComplete()
        {
            StopAllCoroutines(); // a skipped call must not start ringing (and ring out) later
            Ringing = false;
            if (ring != null) ring.Stop();
            if (phone != null) phone.available = false;
            base.DebugComplete();
        }
    }
}
