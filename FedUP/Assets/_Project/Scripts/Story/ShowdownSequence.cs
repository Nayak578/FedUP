using System;
using System.Collections;
using System.Collections.Generic;
using FedUp.Audio;
using FedUp.Ghost;
using FedUp.Interaction;
using FedUp.Player;
using FedUp.World;
using UnityEngine;
using UnityEngine.AI;

namespace FedUp.Story
{
    /// <summary>
    /// Horror Showdown (STORY_BIBLE Part 1; DESIGN_LOGIC #73-74). He wakes fed up and takes the gun from the bedside
    /// drawer. Every light dies. A red trail runs from the bed to the porch, and the ghost's image waits at each turn:
    /// shoot it and it's further on, and every shot breaks something in the house. The last one stands at the gate.
    /// Phone answered: it turns into the boss just before you fire. Either way the street lamp comes up on the boss,
    /// dead, and the ghost is standing in the doorway behind him.
    /// </summary>
    public class ShowdownSequence : MonoBehaviour
    {
        [SerializeField] GunCameo drawer;
        [SerializeField] GameObject heldGun;
        [SerializeField] FirstPersonMotor motor;
        [SerializeField] Interactor interactor;
        [SerializeField] PhoneTorch torch;
        [SerializeField] NightPhaseController phase;
        [SerializeField] FrontDoor frontDoor;
        [SerializeField] Transform start;
        [SerializeField] Transform porch;
        [SerializeField] Transform gate;
        [SerializeField] GameObject figure;
        [SerializeField] GameObject bossAtGate;
        [SerializeField] Transform bossBody;
        [SerializeField] Light lamp;
        [Tooltip("The ghost, still here: in the doorway behind you when you turn round.")]
        [SerializeField] GameObject ghostBehind;
        [SerializeField] LineRenderer trail;
        [SerializeField] Light muzzle;
        [SerializeField] Transform[] wreckables = new Transform[0];
        [SerializeField, Min(2f)] float stopEvery = 7f;
        [SerializeField, Range(1f, 15f)] float hitAngle = 6f;
        [SerializeField] MonologueLibrarySO lines;
        [SerializeField] SubtitleChannelSO subtitles;

        public bool Running { get; private set; }
        public bool Revealed { get; private set; }
        public bool Transformed { get; private set; }
        public int Shots { get; private set; }
        public int Wrecked { get; private set; }
        public int StopsLeft => stops.Count - next;
        /// <summary>Where the ghost's image is now (autopilot/tests).</summary>
        public Transform Target => waitingAt ? figure.transform : null;
        public bool AtGate { get; private set; }
        public IReadOnlyList<Vector3> Stops => stops;

        readonly List<Vector3> stops = new List<Vector3>();
        int next;
        bool waitingAt;
        AudioSource sfx;
        Coroutine stutter;
        float lampFull;
        Color lampColour;

        void Awake()
        {
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            if (lamp != null) { lampFull = lamp.intensity; lampColour = lamp.color; }
            foreach (var g in new[] { heldGun, figure, bossAtGate, ghostBehind }) if (g != null) g.SetActive(false);
            // the street lamp burns all night (you see the gate from the porch at the start); the Showdown kills it
            if (muzzle != null) muzzle.enabled = false;
            if (trail != null) trail.enabled = false;
        }

        public void Play(bool phoneAnswered, Action done) => StartCoroutine(Run(phoneAnswered, done));

        IEnumerator Run(bool answered, Action done)
        {
            Running = true;
            motor.Locked = true;
            motor.EyeHeightOverride = null;
            motor.Teleport(start.position, start.eulerAngles.y);
            lines?.Say("showdown_wake");
            // the drawer he saw at 3 AM
            drawer.OpenForShowdown();
            yield return new WaitForSeconds(1.2f);
            drawer.Gun.SetActive(false);
            heldGun.SetActive(true);
            sfx.PlayOneShot(PlaceholderTones.Get("thunk"));

            // everything turns dark
            phase.SetPower(false);
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l != muzzle && l.type != LightType.Directional) l.enabled = false;
            if (torch != null) { torch.Available = false; }
            // dark, but the trail and the image must read: thin red fog, nothing else lit
            RenderSettings.ambientLight = new Color(0.03f, 0.012f, 0.012f);
            RenderSettings.fogColor = new Color(0.08f, 0.0f, 0.0f);
            RenderSettings.fogDensity = 0.035f;
            frontDoor?.BurstOpen(); // it locked behind him at the start of the night
            BuildTrail();
            Debug.Log($"SHOWDOWN trail from {start.position:F2} to {porch.position:F2}: {stops.Count} stops, answered={answered}");
            yield return new WaitForSeconds(0.8f);
            motor.Locked = false;

            // follow the image down through the house
            for (next = 0; next < stops.Count; next++)
            {
                figure.transform.position = stops[next] + Vector3.up * FigureHalfHeight;
                yield return HoldUntilShot(false);
            }

            // the porch: the last one is at the gate
            while (Vector3.Distance(Flat(motor.transform.position), Flat(porch.position)) > 1.6f) { Flicker(); yield return null; }
            AtGate = true;
            figure.transform.position = gate.position + Vector3.up * FigureHalfHeight;
            yield return HoldUntilShot(answered);

            // the lamp comes up
            Revealed = true;
            motor.Locked = true;
            figure.SetActive(false);
            bossAtGate.SetActive(true);
            bossBody.localRotation = Quaternion.Euler(90f, 0f, 0f); // he's down
            bossBody.localPosition = new Vector3(0f, 0.25f, 0.6f);
            if (stutter != null) StopCoroutine(stutter);
            lamp.enabled = true;
            lamp.color = lampColour;
            lamp.intensity = lampFull * 2f;
            lamp.range *= 1.4f;
            RenderSettings.fogDensity = 0.015f; // the dark lifts: you see who it was
            RenderSettings.ambientLight = new Color(0.07f, 0.06f, 0.06f);
            heldGun.SetActive(false);
            subtitles?.Say("[ under the street lamp: the boss. The file's still in his hand. ]", 3.5f, SubtitleStyle.Caption);
            yield return new WaitForSeconds(3.6f);
            lines?.Say("showdown_after");
            ghostBehind.SetActive(true);
            yield return Look(ghostBehind.transform.position, 1.6f);
            yield return new WaitForSeconds(2.2f);
            Running = false;
            done?.Invoke();
        }

        IEnumerator HoldUntilShot(bool transformsFirst)
        {
            waitingAt = true;
            figure.SetActive(true);
            float aimed = 0f;
            while (true)
            {
                Flicker();
                Vector3 eye = interactor.transform.position;
                Vector3 to = figure.transform.position - eye;
                bool onIt = Vector3.Angle(interactor.transform.forward, to) < hitAngle;
                if (transformsFirst && !Transformed)
                {
                    // phone answered: the ghost visibly turns into the boss while you aim at it
                    aimed = onIt ? aimed + Time.deltaTime : 0f;
                    if (aimed > 0.6f)
                    {
                        Transformed = true;
                        bossAtGate.SetActive(true);
                        figure.GetComponent<Renderer>().enabled = false;
                        stutter = StartCoroutine(LampStutters()); // you see a suit, a tie, a face you know
                    }
                }
                if (interactor.InteractPressedThisFrame)
                {
                    yield return Shoot();
                    if (onIt && (!transformsFirst || Transformed)) break;
                }
                yield return null;
            }
            figure.SetActive(false);
            figure.GetComponent<Renderer>().enabled = true;
            waitingAt = false;
            sfx.PlayOneShot(PlaceholderTones.Get("whoosh"));
        }

        /// <summary>The street lamp stutters on, dim, while the ghost becomes the boss: just enough to see him.</summary>
        IEnumerator LampStutters()
        {
            lamp.color = lampColour; // the bed's red light tinted every lamp in the house
            lamp.intensity = lampFull * 0.45f;
            for (int i = 0; i < 6 && !Revealed; i++)
            {
                lamp.enabled = !lamp.enabled;
                yield return new WaitForSeconds(i % 2 == 0 ? 0.12f : 0.25f);
            }
            if (!Revealed) lamp.enabled = true;
        }

        IEnumerator Shoot()
        {
            Shots++;
            sfx.PlayOneShot(PlaceholderTones.Get("slam"));
            muzzle.enabled = true;
            // every shot breaks something: the house is wrecked by him
            if (Wrecked < wreckables.Length && wreckables[Wrecked] != null)
            {
                var w = wreckables[Wrecked];
                w.rotation = Quaternion.Euler(w.eulerAngles.x + 75f, w.eulerAngles.y + 20f, w.eulerAngles.z);
                Wrecked++;
            }
            yield return new WaitForSeconds(0.06f);
            muzzle.enabled = false;
        }

        void Flicker()
        {
            // the image flickers ahead of you, never quite still
            var r = figure.GetComponent<Renderer>();
            if (r != null && !Transformed) r.enabled = Mathf.PerlinNoise(Time.time * 6f, 0.3f) > 0.25f;
        }

        void BuildTrail()
        {
            stops.Clear();
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(start.position, porch.position, NavMesh.AllAreas, path) || path.corners.Length < 2)
            {
                stops.Add(porch.position);
                return;
            }
            var pts = new List<Vector3>();
            foreach (var c in path.corners) pts.Add(c + Vector3.up * 0.04f);
            trail.positionCount = pts.Count;
            trail.SetPositions(pts.ToArray());
            trail.enabled = true;
            // a stop every few metres along the trail, the image a step past each one
            float walked = 0f, nextStop = stopEvery;
            for (int i = 1; i < path.corners.Length; i++)
            {
                Vector3 a = path.corners[i - 1], b = path.corners[i];
                float seg = Vector3.Distance(a, b);
                while (walked + seg >= nextStop)
                {
                    stops.Add(Vector3.Lerp(a, b, (nextStop - walked) / seg));
                    nextStop += stopEvery;
                }
                walked += seg;
            }
            // never one on the porch itself: the porch is where you shoot the last one from
            stops.RemoveAll(s => Vector3.Distance(Flat(s), Flat(porch.position)) < 2.5f);
        }

        IEnumerator Look(Vector3 point, float seconds)
        {
            Vector3 d = point - interactor.transform.position;
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, pitch = -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
            float y0 = motor.Yaw, p0 = motor.Pitch;
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                motor.SetLook(Mathf.LerpAngle(y0, yaw, k), Mathf.Lerp(p0, pitch, k));
                yield return null;
            }
            motor.SetLook(yaw, pitch);
        }

        const float FigureHalfHeight = 0.9f; // the figure is a capsule centred on its middle

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
