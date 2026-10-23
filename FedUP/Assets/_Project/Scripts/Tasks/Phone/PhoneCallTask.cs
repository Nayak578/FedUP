using System.Collections;
using FedUp.Audio;
using FedUp.Frustration;
using FedUp.Story;
using UnityEngine;

namespace FedUp.Tasks.Phone
{
    /// <summary>
    /// A boss call (STORY_BIBLE.md: the calls are from the boss, about the file). The phone rings; answer it
    /// (garbled boss lines, -5) or let it ring out (a buzz, "1 new voicemail", +2). The answer is recorded in
    /// RunState for the ending.
    /// </summary>
    public class PhoneCallTask : TaskBehaviour
    {
        [SerializeField, Range(1, 4)] int callNumber = 1;
        [SerializeField] TaskInteractable phone;
        [SerializeField, Min(1f)] float delayBeforeRing = 2f;
        [SerializeField, Min(3f)] float ringSeconds = 15f;
        [Tooltip("Monologue-library ids of the boss's lines, played as captions in order.")]
        [SerializeField] string[] bossLineIds = new string[0];
        [SerializeField] string afterAnswerLineId = "phone_after";
        [SerializeField] string ignoredLineId = "phone_ignored";
        [SerializeField] FrustrationEventSO answered;
        [SerializeField] FrustrationEventSO ignored;
        [SerializeField] FrustrationRequestChannelSO frustration;
        [SerializeField] RunStateSO runState;
        [SerializeField] MonologueLibrarySO lines;
        [SerializeField] SubtitleChannelSO subtitles;

        public bool Ringing { get; private set; }
        public bool Answered { get; private set; }
        public bool Ignored { get; private set; }

        AudioSource ring;

        void Awake()
        {
            ring = gameObject.AddComponent<AudioSource>();
            ring.playOnAwake = false;
            ring.loop = true;
            if (phone != null) { phone.available = false; phone.prompt = "Answer the phone"; }
        }

        protected override void OnBegin()
        {
            phone.Used += _ => Answer();
            StartCoroutine(Ring());
        }

        IEnumerator Ring()
        {
            yield return new WaitForSeconds(delayBeforeRing);
            Ringing = true;
            phone.available = true;
            ring.transform.position = phone.transform.position;
            ring.spatialBlend = 0.8f;
            ring.clip = PlaceholderTones.Get("ring");
            ring.Play();
            subtitles?.Say("[ the phone is ringing ]", 2.5f, SubtitleStyle.Caption);
            float t = 0f;
            while (Ringing && t < ringSeconds) { t += Time.deltaTime; yield return null; }
            if (Ringing) yield return LetItRingOut();
        }

        void Answer()
        {
            if (!Ringing || State != TaskState.Active) return;
            Ringing = false;
            Answered = true;
            phone.available = false;
            ring.Stop();
            frustration?.Request(answered);
            runState?.RecordCall(callNumber, true);
            StartCoroutine(BossTalks());
        }

        IEnumerator BossTalks()
        {
            foreach (var id in bossLineIds)
            {
                if (lines != null && lines.TryGet(id, out var e))
                {
                    subtitles?.Say(e.text, e.seconds, SubtitleStyle.Caption);
                    yield return new WaitForSeconds(e.seconds);
                }
            }
            lines?.Say(afterAnswerLineId);
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
            frustration?.Request(ignored);
            runState?.RecordCall(callNumber, false);
            subtitles?.Say("[ 1 new voicemail ]", 2.2f, SubtitleStyle.Caption);
            yield return new WaitForSeconds(2.3f);
            lines?.Say(ignoredLineId);
            Complete();
        }

        /// <summary>Tests: skip the wait and start ringing now.</summary>
        public void DebugRingNow() => delayBeforeRing = 0.01f;

        public override void DebugComplete()
        {
            Ringing = false;
            if (ring != null) ring.Stop();
            if (phone != null) phone.available = false;
            base.DebugComplete();
        }
    }
}
