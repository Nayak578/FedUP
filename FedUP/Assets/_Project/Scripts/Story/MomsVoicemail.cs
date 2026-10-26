using System.Collections;
using FedUp.Audio;
using FedUp.Tasks;
using FedUp.Tasks.Night;
using UnityEngine;

namespace FedUp.Story
{
    /// <summary>
    /// Mom's voicemail (STORY_BIBLE B1; DESIGN_LOGIC #53, #77). His phone lies on the entrance table where he emptied
    /// his pockets, screen lit: "1 new voicemail – Mom". Listening is optional. It's an accidental call: her voice in
    /// pieces, mid-prayer, then her noticing, then a click. You never hear the mantra whole. When he lies down on the
    /// couch the phone goes with him (it's his torch in Phase 2).
    /// </summary>
    public class MomsVoicemail : MonoBehaviour
    {
        [SerializeField] TaskInteractable phone;
        [SerializeField] Light screen;
        [SerializeField] TaskRunner runner;
        [SerializeField] Ghost.NightPhaseController phase;
        [Tooltip("Monologue ids of her fragments, played as captions in order.")]
        [SerializeField] string[] fragmentIds = { "mom_vm_1", "mom_vm_2", "mom_vm_3" };
        [SerializeField] string afterLineId = "mom_vm_after";
        [SerializeField] MonologueLibrarySO lines;
        [SerializeField] SubtitleChannelSO subtitles;

        public bool Listened { get; private set; }
        public bool Playing { get; private set; }
        public bool Pocketed { get; private set; }

        AudioSource sfx;
        float baseGlow;

        void Awake()
        {
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            if (screen != null) baseGlow = screen.intensity;
            phone.prompt = "Listen to the voicemail (Mom)";
            phone.available = true;
            phone.Used += _ => { if (!Playing && !Pocketed) StartCoroutine(Play()); };
        }

        void Update()
        {
            if (Pocketed) return;
            // he takes it with him when he lies down on the couch
            if ((runner != null && runner.Current is SleepTask sleep && sleep.Asleep) || (phase != null && phase.Phase >= 2)) { Pocket(); return; }
            // the screen glows on and off: a nag he can ignore, the way he ignores her
            if (screen != null) screen.intensity = Listened ? baseGlow * 0.3f : baseGlow * (0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 1.3f)));
        }

        IEnumerator Play()
        {
            Playing = true;
            phone.available = false;
            subtitles?.Say("[ 1 voicemail — Mom — 0:41 ]", 2f, SubtitleStyle.Caption);
            yield return new WaitForSeconds(2f);
            foreach (var id in fragmentIds)
            {
                if (lines == null || !lines.TryGet(id, out var e)) continue;
                subtitles?.Say(e.text, e.seconds, SubtitleStyle.Caption);
                yield return new WaitForSeconds(e.seconds);
            }
            sfx.PlayOneShot(PlaceholderTones.Get("click"));
            yield return new WaitForSeconds(0.8f);
            lines?.Say(afterLineId);
            Listened = true;
            Playing = false;
            phone.prompt = "Listen again";
            phone.available = true;
        }

        void Pocket()
        {
            Pocketed = true;
            StopAllCoroutines();
            phone.available = false;
            phone.gameObject.SetActive(false);
        }
    }
}
