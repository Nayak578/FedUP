using System.Collections;
using FedUp.Audio;
using FedUp.Player;
using UnityEngine;

namespace FedUp.World
{
    /// <summary>
    /// The gun, once (STORY_BIBLE approved additions; DESIGN_LOGIC #62). The bedside drawer is ajar. The first time it's
    /// in front of you, close and lit, the gun shows for about 0.2 s, then the drawer slides shut by itself. No line,
    /// no sting: you're not sure you saw it. The Showdown opens the same drawer.
    /// </summary>
    public class GunCameo : MonoBehaviour
    {
        [SerializeField] Transform drawer;
        [SerializeField] GameObject gun;
        [SerializeField] Transform eye;
        [SerializeField] PhoneTorch torch;
        [Tooltip("How the drawer moves when it shuts, in its parent's space.")]
        [SerializeField] Vector3 shutOffset = new Vector3(0.25f, 0f, 0f);
        [SerializeField, Min(0.5f)] float seeWithin = 3.5f;
        [SerializeField, Range(5f, 60f)] float seeAngle = 22f;
        [Tooltip("Without the torch, only this close.")]
        [SerializeField, Min(0.3f)] float darkWithin = 1.8f;
        [SerializeField, Range(0.05f, 0.3f)] float showSeconds = 0.2f;

        public bool Armed { get; private set; }
        public bool Shown { get; private set; }
        public bool Shut { get; private set; }
        public float SeenAt { get; private set; } = -1f;
        public float ShutAt { get; private set; } = -1f;
        /// <summary>When the gun stopped being visible (for the "a heartbeat, not a frame" check).</summary>
        public float GunHiddenAt { get; private set; } = -1f;
        public Transform Drawer => drawer;
        public GameObject Gun => gun;

        Vector3 openPos;
        AudioSource sfx;

        void Awake()
        {
            openPos = drawer.localPosition;
            gun.SetActive(false); // only ever there for the heartbeat
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 1f;
        }

        /// <summary>From the charger trip on (Phase 2): before that he's never upstairs at night.</summary>
        public void Arm() => Armed = true;

        void Update()
        {
            if (!Armed || Shown || eye == null) return;
            Vector3 to = drawer.position - eye.position;
            float d = to.magnitude;
            bool lit = torch != null && torch.Lit;
            if (d > (lit ? seeWithin : darkWithin) || Vector3.Angle(eye.forward, to) > seeAngle) return;
            // nothing between you and the drawer (the cabinet it's in, and your own body, don't count)
            foreach (var hit in Physics.RaycastAll(eye.position, to / d, d - 0.05f, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.collider.transform.IsChildOf(drawer.parent) && !eye.IsChildOf(hit.collider.transform.root)) return;
            Shown = true;
            SeenAt = Time.time;
            gun.SetActive(true);
            StartCoroutine(SlideShut());
        }

        IEnumerator SlideShut()
        {
            yield return new WaitForSeconds(showSeconds);
            gun.SetActive(false);
            GunHiddenAt = Time.time;
            Vector3 from = drawer.localPosition, to = openPos + shutOffset;
            for (float t = 0f; t < 0.12f; t += Time.deltaTime) { drawer.localPosition = Vector3.Lerp(from, to, t / 0.12f); yield return null; }
            drawer.localPosition = to;
            Shut = true;
            ShutAt = Time.time;
            sfx.PlayOneShot(PlaceholderTones.Get("thunk"), 0.6f);
        }

        /// <summary>The Showdown: he opens it and takes the gun.</summary>
        public void OpenForShowdown()
        {
            StopAllCoroutines();
            drawer.localPosition = openPos;
            gun.SetActive(true);
        }
    }
}
