using System.Collections;
using FedUp.Ghost;
using FedUp.Interaction;
using FedUp.Player;
using FedUp.Story;
using FedUp.Tasks.Tv;
using FedUp.World;
using UnityEngine;

namespace FedUp.Tasks.Night
{
    /// <summary>
    /// End of Phase 1 (GDD: "the protagonist slowly falls asleep during channel cycling"; STORY_FLOW: the lights dim,
    /// the sound warps, the TV turns to static). Hold E on the couch to lie down; the room dims, the home video
    /// slows, black, "3:07 AM", and he wakes into Phase 2 on the same couch.
    /// </summary>
    public class SleepTask : TaskBehaviour
    {
        [SerializeField] TaskInteractable couch;
        [SerializeField] Transform lieSpot;
        [SerializeField] Transform standSpot;
        [SerializeField] Transform lookAt;
        [SerializeField] float lyingEyeHeight = 0.75f;
        [SerializeField, Min(0.5f)] float dozeSeconds = 4f;
        [SerializeField] TvTask tv;
        [SerializeField] NightPhaseController phase;
        [SerializeField] CutscenePlayer cutscenes;
        [SerializeField] CutsceneSO wakeUp;
        [SerializeField] FirstPersonMotor motor;
        [SerializeField] Interactor interactor;
        [SerializeField] MonologueLibrarySO lines;
        [SerializeField] SubtitleChannelSO subtitles;

        public bool Asleep { get; private set; }

        void Awake()
        {
            if (couch != null) couch.available = false;
        }

        protected override void OnBegin()
        {
            couch.available = true;
            couch.Used += _ => LieDown();
        }

        void LieDown()
        {
            if (Asleep || State != TaskState.Active) return;
            couch.available = false;
            StartCoroutine(Sleep());
        }

        IEnumerator Sleep()
        {
            Asleep = true;
            motor.Locked = true;
            interactor.enabled = false;
            motor.Teleport(lieSpot.position, lieSpot.eulerAngles.y);
            motor.EyeHeightOverride = lyingEyeHeight;
            Look(lookAt.position);
            lines?.Say("sleep_lie");

            // the room dims and the home video slows down as he drifts off
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            var start = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++) start[i] = lights[i].intensity;
            for (float t = 0f; t < dozeSeconds; t += Time.deltaTime)
            {
                float k = t / dozeSeconds;
                for (int i = 0; i < lights.Length; i++) if (lights[i] != null && lights[i].type != LightType.Directional) lights[i].intensity = start[i] * (1f - 0.8f * k);
                tv?.SetSoundPitch(1f - 0.45f * k);
                yield return null;
            }

            // black: the house turns while he sleeps; the static he dozed off to is the static he wakes to
            bool done = false;
            cutscenes.Play(wakeUp, () => done = true);
            yield return null;
            Turn();
            yield return new WaitUntil(() => done);
            WakeUp();
        }

        void Turn()
        {
            tv?.SetSoundPitch(1f);
            tv?.ForceChannel(TvChannel.Static);
            phase.EnterPhase2();
            if (tv != null && tv.ScreenLight != null)
            {
                var flicker = tv.ScreenLight.GetComponent<LightFlicker>();
                if (flicker == null) flicker = tv.ScreenLight.gameObject.AddComponent<LightFlicker>();
                flicker.SetBase(tv.ScreenLight.intensity * 0.8f);
            }
        }

        void WakeUp()
        {
            motor.EyeHeightOverride = null;
            motor.Teleport(standSpot.position, standSpot.eulerAngles.y);
            motor.Locked = false;
            interactor.enabled = true;
            Asleep = false;
            phase.StartHaunting();
            subtitles?.Say("[ F — phone torch ]", 3f, SubtitleStyle.Caption);
            Complete();
        }

        void Look(Vector3 p)
        {
            Vector3 d = p - (lieSpot.position + Vector3.up * lyingEyeHeight);
            motor.SetLook(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg);
        }

        public override void DebugComplete()
        {
            StopAllCoroutines();
            if (couch != null) couch.available = false;
            if (phase.Phase < 2) Turn();
            motor.EyeHeightOverride = null;
            motor.Locked = false;
            interactor.enabled = true;
            Asleep = false;
            phase.StartHaunting();
            base.DebugComplete();
        }
    }
}
