using System;
using FedUp.Audio;
using FedUp.Frustration;
using FedUp.Story;
using UnityEngine;

namespace FedUp.Tasks.Toast
{
    /// <summary>GDD: two burns in a row set it off; it costs +3% every second until you reach up and silence it.</summary>
    public class SmokeAlarm : MonoBehaviour
    {
        [SerializeField] TaskInteractable button;
        [SerializeField] FrustrationRequestChannelSO frustration;
        [SerializeField] FrustrationEventSO perSecond;
        [SerializeField] Light blinkLight;
        [SerializeField] MonologueLibrarySO lines;

        public bool Ringing { get; private set; }
        public event Action Silenced;

        AudioSource audioSource;

        void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.spatialBlend = 0.7f;
            audioSource.playOnAwake = false;
            if (button != null) { button.available = false; button.Used += _ => Silence(); }
            if (blinkLight != null) blinkLight.enabled = false;
        }

        public void StartRinging()
        {
            if (Ringing) return;
            Ringing = true;
            button.available = true;
            audioSource.clip = PlaceholderTones.Get("beep");
            audioSource.Play();
            lines?.Say("alarm");
        }

        public void Silence()
        {
            if (!Ringing) return;
            Ringing = false;
            button.available = false;
            audioSource.Stop();
            if (blinkLight != null) blinkLight.enabled = false;
            lines?.Say("alarm_silenced");
            Silenced?.Invoke();
        }

        void Update()
        {
            if (!Ringing) return;
            if (frustration != null && perSecond != null) frustration.Request(perSecond, Time.deltaTime);
            if (blinkLight != null) blinkLight.enabled = Mathf.Repeat(Time.time, 0.5f) < 0.25f;
        }
    }
}
