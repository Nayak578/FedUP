using System;
using System.Collections;
using System.Collections.Generic;
using FedUp.Audio;
using FedUp.Interaction;
using FedUp.Player;
using FedUp.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace FedUp.Story
{
    /// <summary>
    /// Ghost Burial, the part in the house (STORY_BIBLE Burial steps 1-6; DESIGN_LOGIC #84-85). He wakes calm and says
    /// his mother's mantra aloud; the ghost flees. Follow it (it keeps its distance) through the Storeroom to the north
    /// end of the east hallway, where it walks into the wall and leaves a crack. The axe is in the Storeroom. Three swings,
    /// then squeeze sideways into the cavity between the double walls and shuffle the length of the house to the
    /// triangle, where darkness rises from a vent. Crawl in. The other world is art pass A4.
    /// </summary>
    public class BurialSequence : MonoBehaviour
    {
        public enum Stage { Idle, Mantra, Chase, Crack, Wall, Squeeze, Triangle, Done }

        [SerializeField] FirstPersonMotor motor;
        [SerializeField] Interactor interactor;
        [SerializeField] PhoneTorch torch;
        [SerializeField] Image black;
        [SerializeField] Transform start;
        [Tooltip("Where you stand in the hallway, facing the wall the ghost walks into.")]
        [SerializeField] Transform wallSpot;
        [SerializeField] Transform wallFace;
        [Tooltip("The chase goes through here on purpose: the Storeroom, past the axe (DESIGN_LOGIC #85).")]
        [SerializeField] Transform via;
        [SerializeField] GameObject figure;
        [SerializeField] GameObject crack;
        [SerializeField] GameObject hole;
        [SerializeField] TaskInteractable wall;
        [SerializeField] TaskInteractable axe;
        [SerializeField] GameObject heldAxe;
        [SerializeField] Transform squeezeIn;
        [Tooltip("The south end of the cavity: past here you're in the triangle.")]
        [SerializeField] Transform triangle;
        [SerializeField] TaskInteractable vent;
        [SerializeField] ParticleSystem darkness;
        [SerializeField, Min(1f)] float keepAway = 3.5f;
        [SerializeField, Min(2f)] float stopEvery = 6f;
        [SerializeField, Range(0.04f, 0.2f)] float squeezeRadius = 0.07f;
        [SerializeField, Range(0.1f, 1f)] float squeezeSpeed = 0.35f;
        [SerializeField, Min(1)] int swings = 3;
        [SerializeField] MonologueLibrarySO lines;
        [SerializeField] SubtitleChannelSO subtitles;

        public Stage Current { get; private set; }
        public BurialChase Chase { get; private set; }
        public int Hits { get; private set; }
        public bool HasAxe { get; private set; }
        public IReadOnlyList<Vector3> Stops => stops;
        public Transform Figure => figure != null ? figure.transform : null;

        readonly List<Vector3> stops = new List<Vector3>();
        AudioSource sfx;
        Action onDone;

        void Awake()
        {
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            foreach (var g in new[] { figure, crack, hole, heldAxe }) if (g != null) g.SetActive(false);
            foreach (var t in new[] { wall, vent }) if (t != null) t.available = false;
            if (darkness != null) darkness.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (black != null) SetBlack(0f);
        }

        void Start()
        {
            // the axe hangs in the Storeroom all night; it's only something to take now
            if (axe != null) { axe.available = false; axe.Used += _ => TakeAxe(); }
            if (wall != null) wall.Used += _ => Swing();
            if (vent != null) vent.Used += _ => { if (Current == Stage.Triangle) StartCoroutine(CrawlIn()); };
        }

        public void Play(Action done)
        {
            onDone = done;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            Current = Stage.Mantra;
            motor.Locked = true;
            motor.EyeHeightOverride = null;
            motor.Teleport(start.position, start.eulerAngles.y);
            BuildStops();
            figure.transform.position = stops[0] + Vector3.up * 0.9f;
            figure.SetActive(true);
            Look(figure.transform.position, 0.8f);
            yield return new WaitForSeconds(0.6f);
            // his mother's words, whole, for the first time
            lines?.Say("burial_mantra");
            yield return new WaitForSeconds(3.6f);
            lines?.Say("burial_mantra_2");
            yield return new WaitForSeconds(2.6f);
            sfx.PlayOneShot(PlaceholderTones.Get("whoosh"));

            // it runs; it never lets you near
            Current = Stage.Chase;
            Chase = new BurialChase(stops.Count, keepAway);
            if (torch != null) torch.Available = true;
            motor.Locked = false;
            while (!Chase.Gone)
            {
                Chase.Tick(Vector3.Distance(Flat(motor.transform.position), Flat(figure.transform.position)));
                if (Chase.MovedThisTick && !Chase.Gone)
                {
                    sfx.PlayOneShot(PlaceholderTones.Get("whoosh"), 0.6f);
                    figure.transform.position = stops[Chase.Stop] + Vector3.up * 0.9f;
                }
                yield return null;
            }

            // into the wall
            Vector3 from = figure.transform.position, into = new Vector3(wallFace.position.x, from.y, wallFace.position.z);
            for (float t = 0f; t < 0.8f; t += Time.deltaTime) { figure.transform.position = Vector3.Lerp(from, into, t / 0.8f); yield return null; }
            figure.SetActive(false);
            crack.SetActive(true);
            Current = Stage.Crack;
            lines?.Say("burial_crack");
            yield return new WaitForSeconds(3f);
            lines?.Say("burial_axe");
            axe.prompt = "Take the axe";
            axe.mode = InteractionMode.Hold;
            axe.holdSeconds = 0.8f;
            axe.available = true;
        }

        void TakeAxe()
        {
            if (Current != Stage.Crack || HasAxe) return;
            HasAxe = true;
            axe.available = false;
            axe.gameObject.SetActive(false);
            heldAxe.SetActive(true);
            Current = Stage.Wall;
            wall.prompt = "Break the wall";
            wall.mode = InteractionMode.Press;
            wall.available = true;
        }

        void Swing()
        {
            if (Current == Stage.Wall && HasAxe && Hits < swings)
            {
                Hits++;
                sfx.PlayOneShot(PlaceholderTones.Get("slam"));
                crack.transform.localScale = Vector3.Scale(crack.transform.localScale, new Vector3(1f, 1.15f, 1.25f));
                if (Hits < swings) return;
                crack.SetActive(false);
                hole.SetActive(true);
                lines?.Say("burial_hole");
                wall.prompt = "Squeeze in";
                return;
            }
            if (Current == Stage.Wall && Hits >= swings) StartCoroutine(SqueezeIn());
        }

        IEnumerator SqueezeIn()
        {
            Current = Stage.Squeeze;
            wall.available = false;
            motor.Locked = true;
            yield return Fade(0f, 1f, 0.5f);
            heldAxe.SetActive(false); // left behind in the hallway: no room for it in there
            motor.RadiusOverride = squeezeRadius;
            motor.SpeedScale = squeezeSpeed;
            motor.Teleport(squeezeIn.position, squeezeIn.eulerAngles.y);
            if (torch != null && !torch.On) torch.VirtualToggle();
            yield return Fade(1f, 0f, 0.6f);
            motor.Locked = false;
            lines?.Say("burial_squeeze");
            // shuffle the length of the house, sideways, to the triangle
            while (motor.transform.position.z > triangle.position.z) yield return null;
            Current = Stage.Triangle;
            darkness?.Play();
            lines?.Say("burial_triangle");
            vent.prompt = "Crawl in";
            vent.available = true;
        }

        IEnumerator CrawlIn()
        {
            Current = Stage.Done;
            vent.available = false;
            motor.Locked = true;
            yield return Fade(0f, 1f, 2.2f);
            motor.RadiusOverride = null;
            motor.SpeedScale = 1f;
            subtitles?.Say("[ the tunnel turns 90 degrees, and the house turns with it ]", 3.2f, SubtitleStyle.Caption);
            yield return new WaitForSeconds(3.4f);
            subtitles?.Say("[ the other world: art pass A4, after the sensitivity read ]", 3f, SubtitleStyle.Caption);
            yield return new WaitForSeconds(3.2f);
            onDone?.Invoke();
        }

        void BuildStops()
        {
            stops.Clear();
            var corners = new List<Vector3>();
            var path = new NavMeshPath();
            Vector3 from = start.position;
            foreach (var to in via != null ? new[] { via.position, wallSpot.position } : new[] { wallSpot.position })
            {
                if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path) || path.corners.Length < 2) continue;
                foreach (var p in path.corners) if (corners.Count == 0 || (corners[corners.Count - 1] - p).sqrMagnitude > 0.01f) corners.Add(p);
                from = to;
            }
            if (corners.Count < 2) { stops.Add(wallSpot.position); return; }
            // the first stop at the foot of the bed, then one every few metres, the last right at the wall
            float walked = 0f, next = keepAway + 0.8f;
            for (int i = 1; i < corners.Count; i++)
            {
                Vector3 a = corners[i - 1], b = corners[i];
                float seg = Vector3.Distance(a, b);
                while (walked + seg >= next) { stops.Add(Vector3.Lerp(a, b, (next - walked) / seg)); next += stopEvery; }
                walked += seg;
            }
            if (stops.Count == 0 || Vector3.Distance(stops[stops.Count - 1], wallSpot.position) > 1.5f) stops.Add(wallSpot.position);
            else stops[stops.Count - 1] = wallSpot.position;
        }

        void Look(Vector3 p, float _)
        {
            Vector3 d = p - interactor.transform.position;
            motor.SetLook(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg);
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

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
