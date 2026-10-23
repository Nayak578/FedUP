using System.Collections;
using FedUp.Audio;
using FedUp.Story;
using FedUp.Tasks;
using UnityEngine;

namespace FedUp.Ghost.Scares
{
    /// <summary>
    /// GDD "Mirror Flicker": the reflection is slightly delayed; then, for a split second, something stands right
    /// behind you, between you and the only way out (only in the mirror); then the mirror cracks, silently.
    /// Armed once you've splashed your face at the sink; fires when you look at the mirror and the director allows.
    /// </summary>
    public class MirrorScare : MonoBehaviour
    {
        [SerializeField] string scareId = "bathroom_mirror";
        [SerializeField] GhostDirector director;
        [SerializeField] TaskInteractable sink;
        [SerializeField] MirrorView mirror;
        [SerializeField] Transform eye;
        [SerializeField] GameObject figure;
        [Tooltip("The doorway: the figure appears between you and it.")]
        [SerializeField] Transform wayOut;
        [SerializeField] GameObject crack;
        [SerializeField, Min(0.5f)] float lookWithin = 2.8f;
        [SerializeField, Range(5f, 60f)] float lookAngle = 35f;
        [SerializeField] float lagSeconds = 0.45f;
        [SerializeField] float figureSeconds = 0.22f;
        [SerializeField] MonologueLibrarySO lines;

        public bool Armed { get; private set; }
        public bool Fired { get; private set; }
        public bool Finished { get; private set; }
        public bool FigureWasShown { get; private set; }
        public bool Cracked => crack != null && crack.activeSelf;

        AudioSource sfx;

        void Awake()
        {
            if (figure != null) figure.SetActive(false);
            if (crack != null) crack.SetActive(false);
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0.5f;
            if (sink != null) sink.Used += _ => Armed = true;
        }

        bool LookingAtMirror()
        {
            Vector3 to = mirror.transform.position - eye.position;
            return to.magnitude <= lookWithin && Vector3.Angle(eye.forward, to) <= lookAngle;
        }

        void Update()
        {
            if (!Armed || Fired || !LookingAtMirror()) return;
            if (!director.TryScare(scareId, 2, ScareSize.Big)) return;
            Fired = true;
            StartCoroutine(Play());
        }

        IEnumerator Play()
        {
            // the reflection starts to lag behind you
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) { mirror.Delay = lagSeconds * t / 0.6f; yield return null; }
            mirror.Delay = lagSeconds;
            yield return new WaitForSeconds(2.2f);
            // something right behind you, over your shoulder, for a split second
            Vector3 feet = eye.position - Vector3.up * 1.6f;
            Vector3 toDoor = wayOut.position - feet; toDoor.y = 0f;
            Vector3 side = Vector3.Cross(Vector3.up, toDoor.normalized);
            figure.transform.position = feet + toDoor.normalized * 0.9f + side * 0.5f + Vector3.up * 0.9f;
            figure.SetActive(true);
            FigureWasShown = true;
            sfx.PlayOneShot(PlaceholderTones.Get("sting"));
            yield return new WaitForSeconds(figureSeconds);
            figure.SetActive(false);
            yield return new WaitForSeconds(1.1f);
            // the mirror cracks, silently (the silence is the point)
            crack.SetActive(true);
            for (float t = 0f; t < 1f; t += Time.deltaTime) { mirror.Delay = lagSeconds * (1f - t); yield return null; }
            mirror.Delay = 0f;
            yield return new WaitForSeconds(1.2f);
            lines?.Say("mirror_after");
            Finished = true;
        }
    }
}
