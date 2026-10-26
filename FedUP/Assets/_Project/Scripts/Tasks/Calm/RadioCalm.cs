using FedUp.Audio;
using FedUp.Frustration;
using FedUp.Ghost;
using FedUp.Story;
using UnityEngine;

namespace FedUp.Tasks.Calm
{
    /// <summary>
    /// The radio on the dining table (GDD rebalance calm extra; STORY_FLOW: the song is the one from his old
    /// relationship). Optional, from the wake-up on: switch it on and let it play 15 s uninterrupted within earshot
    /// for -5 (once a night). Walking out of earshot starts the count again.
    /// </summary>
    public class RadioCalm : MonoBehaviour
    {
        [SerializeField] TaskInteractable radio;
        [SerializeField, Min(1f)] float secondsNeeded = 15f;
        [SerializeField, Min(1f)] float earshot = 4.5f;
        [SerializeField] Transform listener;
        [SerializeField] NightPhaseController phase;
        [SerializeField] FrustrationEventSO calm; // -5
        [SerializeField] FrustrationRequestChannelSO frustration;
        [SerializeField] SubtitleChannelSO subtitles;
        [SerializeField] MonologueLibrarySO lines;

        public RadioSession Session { get; private set; }
        public bool Playing { get; private set; }

        AudioSource song;

        void Awake()
        {
            Session = new RadioSession(secondsNeeded);
            song = radio.gameObject.AddComponent<AudioSource>();
            song.playOnAwake = false; song.loop = true; song.spatialBlend = 0.85f; song.maxDistance = 12f; song.volume = 0.6f;
            radio.prompt = "Turn on the radio";
            radio.available = false;
            radio.Used += _ => Toggle();
        }

        void Update()
        {
            // a calm extra for the night: only after the wake-up
            radio.available = phase == null || phase.Phase >= 2;
            if (!radio.available) return;
            bool near = (listener.position - radio.transform.position).sqrMagnitude <= earshot * earshot;
            Session.Tick(Time.deltaTime, Playing, near);
            if (Session.CalmedThisTick)
            {
                frustration?.Request(calm);
                lines?.Say("radio_song");
            }
        }

        void Toggle()
        {
            Playing = !Playing;
            radio.prompt = Playing ? "Turn off the radio" : "Turn on the radio";
            if (Playing)
            {
                song.clip = PlaceholderTones.Get("lofi");
                song.Play();
                if (!Session.Calmed) subtitles?.Say("[ a slow lo-fi song… the one from back then ]", 3f, SubtitleStyle.Caption);
            }
            else { song.Stop(); Session.Interrupt(); }
        }
    }
}
