using System;
using System.Collections;
using FedUp.Audio;
using FedUp.Interaction;
using FedUp.Player;
using UnityEngine;
using UnityEngine.UI;

namespace FedUp.Story
{
    /// <summary>Plays CutsceneSO assets. Takes the controls away while playing and gives them back after.</summary>
    public class CutscenePlayer : MonoBehaviour
    {
        [SerializeField] CutsceneSO prologue;
        [SerializeField] bool playPrologueOnStart = true;
        [SerializeField] FirstPersonMotor motor;
        [SerializeField] Interactor interactor;
        [SerializeField] MonologueLibrarySO lines;
        [SerializeField] SubtitleChannelSO subtitles;
        [SerializeField] Image blackPanel;
        [SerializeField] Text captionText;

        public bool Playing { get; private set; }
        public string CurrentCaption => captionText != null ? captionText.text : null;
        public CutsceneSO Prologue => prologue;

        Coroutine running;
        Action onDone;
        AudioSource tones;

        void Awake()
        {
            tones = gameObject.AddComponent<AudioSource>();
            tones.playOnAwake = false;
        }

        void Start()
        {
            SetBlack(0f);
            if (playPrologueOnStart && prologue != null) Play(prologue);
        }

        public void Play(CutsceneSO cutscene, Action done = null)
        {
            if (cutscene == null) { done?.Invoke(); return; }
            if (running != null) StopCoroutine(running);
            onDone = done;
            running = StartCoroutine(Run(cutscene));
        }

        /// <summary>Ends whatever is playing immediately and hands control back (tests and skipping).</summary>
        public void SkipAll()
        {
            if (running != null) StopCoroutine(running);
            Finish();
        }

        IEnumerator Run(CutsceneSO cs)
        {
            Playing = true;
            if (motor != null) motor.Locked = true;
            if (interactor != null) interactor.enabled = false;

            foreach (var step in cs.steps)
            {
                switch (step.kind)
                {
                    case CutsceneSO.StepKind.BlackCaption:
                        SetBlack(1f); SetCaption(step.text);
                        yield return new WaitForSeconds(step.seconds);
                        SetCaption("");
                        break;
                    case CutsceneSO.StepKind.Caption:
                        SetCaption(step.text);
                        yield return new WaitForSeconds(step.seconds);
                        SetCaption("");
                        break;
                    case CutsceneSO.StepKind.Line:
                        if (lines != null) lines.Say(step.text); else subtitles?.Say(step.text);
                        yield return new WaitForSeconds(step.seconds);
                        break;
                    case CutsceneSO.StepKind.FadeIn:
                        for (float t = 0; t < step.seconds; t += Time.deltaTime)
                        {
                            SetBlack(1f - t / Mathf.Max(0.01f, step.seconds));
                            yield return null;
                        }
                        SetBlack(0f);
                        break;
                    case CutsceneSO.StepKind.LookAt:
                        var target = GameObject.Find(step.text);
                        if (target != null && motor != null) yield return Look(target.transform.position, step.seconds);
                        break;
                    case CutsceneSO.StepKind.Tone:
                        tones.PlayOneShot(PlaceholderTones.Get(step.text));
                        yield return new WaitForSeconds(step.seconds);
                        break;
                    default:
                        yield return new WaitForSeconds(step.seconds);
                        break;
                }
            }
            Finish();
        }

        IEnumerator Look(Vector3 point, float seconds)
        {
            Transform eye = interactor != null ? interactor.transform : motor.transform;
            Vector3 d = point - eye.position;
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
            float y0 = motor.Yaw, p0 = motor.Pitch;
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / Mathf.Max(0.01f, seconds));
                motor.SetLook(Mathf.LerpAngle(y0, yaw, k), Mathf.Lerp(p0, pitch, k));
                yield return null;
            }
            motor.SetLook(yaw, pitch);
        }

        void Finish()
        {
            running = null;
            SetBlack(0f);
            SetCaption("");
            if (motor != null) motor.Locked = false;
            if (interactor != null) interactor.enabled = true;
            Playing = false;
            var done = onDone;
            onDone = null;
            done?.Invoke();
        }

        void SetBlack(float a)
        {
            if (blackPanel == null) return;
            var c = blackPanel.color; c.a = a; blackPanel.color = c;
        }

        void SetCaption(string s)
        {
            if (captionText != null) captionText.text = s ?? "";
        }
    }
}
