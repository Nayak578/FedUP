using System;
using System.Collections;
using FedUp.Audio;
using FedUp.Ghost;
using FedUp.Interaction;
using FedUp.Player;
using FedUp.Tasks.Tv;
using FedUp.World;
using UnityEngine;
using UnityEngine.UI;

namespace FedUp.Story
{
    /// <summary>
    /// The short ending scenes before their cards (DESIGN_LOGIC #81-83). In-engine and first person; placeholder captions
    /// until art pass A6 makes them cutscenes.
    /// - The loop: the alarm, then the porch at dusk again, the door in front of you. "…Again."
    /// - Unemployed, ghost angry: the boss's voicemail in the morning, then the house rages around him.
    /// - You die in your sleep: the blanket over your eyes, silence, and in the morning his phone rings: Mom.
    /// </summary>
    public class EndingScenes : MonoBehaviour
    {
        [SerializeField] NightPhaseController phase;
        [SerializeField] FirstPersonMotor motor;
        [SerializeField] Interactor interactor;
        [SerializeField] Image black;
        [SerializeField] Transform porch;
        [SerializeField] FrontDoor frontDoor;
        [SerializeField] TvTask tv;
        [Tooltip("Where the doors slam upstairs, in turn (the ghost, furious).")]
        [SerializeField] Transform[] slams = new Transform[0];
        [Tooltip("His phone on the bedside cabinet (it rings in the morning).")]
        [SerializeField] Transform bedsidePhone;
        [SerializeField] MonologueLibrarySO lines;
        [SerializeField] SubtitleChannelSO subtitles;

        public string Last { get; private set; }
        AudioSource sfx;

        void Awake()
        {
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            if (black != null) SetBlack(0f);
        }

        public void Play(Ending e, Action done)
        {
            switch (e)
            {
                case Ending.Loop: StartCoroutine(Loop(done)); break;
                case Ending.UnemployedGhostAngry: StartCoroutine(Unemployed(done)); break;
                case Ending.DieInSleep: StartCoroutine(Die(done)); break;
                default: done?.Invoke(); break;
            }
        }

        IEnumerator Loop(Action done)
        {
            Last = "loop";
            motor.Locked = true;
            yield return Fade(0f, 1f, 1.5f);
            Caption("[ 7:00 AM ]", 2f);
            for (int i = 0; i < 3; i++) { sfx.PlayOneShot(PlaceholderTones.Get("beep")); yield return new WaitForSeconds(0.7f); }
            yield return new WaitForSeconds(1f);
            Caption("[ the day. the office. the drive home. ]", 2.4f);
            yield return new WaitForSeconds(2.6f);
            // the exact first shot of the game: the porch at dusk, the door in front of you
            phase.RestoreEvening();
            motor.EyeHeightOverride = null;
            motor.Teleport(porch.position, porch.eulerAngles.y);
            yield return Fade(1f, 0f, 1.6f);
            lines?.Say("loop_again");
            yield return new WaitForSeconds(3f);
            done?.Invoke();
        }

        IEnumerator Unemployed(Action done)
        {
            Last = "unemployed";
            motor.Locked = true;
            yield return Fade(0f, 1f, 1.5f);
            Caption("[ 7:02 AM — 1 new voicemail ]", 2.2f);
            yield return new WaitForSeconds(2.4f);
            Say("unemployed_vm");
            yield return new WaitForSeconds(3.4f);
            yield return Fade(1f, 0.15f, 1.2f);
            // the house answers for the ghost: every door, the TV, the lights
            tv?.ForceChannel(TvChannel.Static);
            phase.Flicker(4f);
            foreach (var s in slams)
            {
                if (s != null) AudioSource.PlayClipAtPoint(PlaceholderTones.Get("slam"), s.position, 1f);
                yield return new WaitForSeconds(0.6f);
            }
            yield return new WaitForSeconds(1f);
            lines?.Say("unemployed_after");
            yield return new WaitForSeconds(3f);
            done?.Invoke();
        }

        IEnumerator Die(Action done)
        {
            Last = "die";
            motor.Locked = true;
            // the blanket over his eyes; then nothing at all
            yield return Fade(0f, 1f, 3.5f);
            phase.Silence();
            yield return new WaitForSeconds(2.5f);
            Caption("[ morning ]", 2f);
            yield return new WaitForSeconds(2.4f);
            // his phone, on the bedside cabinet: someone who loved him, unanswered
            for (int i = 0; i < 4; i++)
            {
                if (bedsidePhone != null) AudioSource.PlayClipAtPoint(PlaceholderTones.Get("ring"), bedsidePhone.position, 0.8f);
                if (i == 0) Caption("[ his phone: Mom ]", 2.8f);
                if (i == 2) Caption("[ it rings, and rings ]", 2.8f);
                yield return new WaitForSeconds(3f);
            }
            done?.Invoke();
        }

        void Caption(string s, float seconds) => subtitles?.Say(s, seconds, SubtitleStyle.Caption);

        void Say(string id)
        {
            if (lines != null && lines.TryGet(id, out var e)) subtitles?.Say(e.text, e.seconds, SubtitleStyle.Caption);
        }

        IEnumerator Fade(float from, float to, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime) { SetBlack(Mathf.Lerp(from, to, t / seconds)); yield return null; }
            SetBlack(to);
        }

        void SetBlack(float a)
        {
            if (black == null) return;
            var c = black.color; c.a = a; black.color = c;
        }
    }
}
