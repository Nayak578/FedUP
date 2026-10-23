using FedUp.Audio;
using FedUp.Story;
using UnityEngine;

namespace FedUp.Ghost.Scares
{
    /// <summary>
    /// GDD "Stalker Audio": footsteps echo behind you in the hallway and vanish when you turn around. Walk the
    /// hallway towards the back hall in Phase 2 and, if the director allows, a step lands behind you for every
    /// step you take, a few metres back. Stop and it takes one more. Turn around and there's nothing.
    /// </summary>
    public class FootstepStalker : MonoBehaviour
    {
        [SerializeField] string scareId = "hallway_steps";
        [SerializeField] GhostDirector director;
        [Tooltip("The hallway, as a box (this object's position is the centre).")]
        [SerializeField] Vector3 zoneSize = new Vector3(6f, 3f, 2f);
        [Tooltip("The way you walk for it to start (along the hallway, away from the stairs).")]
        [SerializeField] Vector3 walkDirection = Vector3.right;
        [SerializeField] Transform player;
        [SerializeField] Transform eye;
        [SerializeField, Min(1f)] float followDistance = 3.2f;
        [SerializeField, Min(0.2f)] float strideMetres = 0.75f;
        [SerializeField, Min(1f)] float maxSeconds = 9f;
        [SerializeField] MonologueLibrarySO lines;

        public bool Fired { get; private set; }
        public bool Running { get; private set; }
        public int StepsPlayed { get; private set; }
        public bool StoppedByTurning { get; private set; }

        AudioSource source;
        Vector3 lastPos, lastStepPos;
        float startedAt, stillFor;
        bool extraStepDone;

        void Awake()
        {
            source = new GameObject("SFX_Stalker").AddComponent<AudioSource>();
            source.transform.SetParent(transform, false);
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.minDistance = 1f;
            source.maxDistance = 12f;
            source.rolloffMode = AudioRolloffMode.Linear;
        }

        void Start() => lastPos = player.position;

        bool InZone(Vector3 p)
        {
            Vector3 d = p - transform.position;
            return Mathf.Abs(d.x) <= zoneSize.x / 2f && Mathf.Abs(d.y) <= zoneSize.y / 2f && Mathf.Abs(d.z) <= zoneSize.z / 2f;
        }

        void Update()
        {
            Vector3 p = player.position;
            Vector3 moved = p - lastPos; moved.y = 0f;
            lastPos = p;
            if (Fired && !Running) return;

            if (!Running)
            {
                bool walkingTheRightWay = Vector3.Dot(moved, walkDirection.normalized) > 0.5f * moved.magnitude && moved.sqrMagnitude > 1e-6f;
                if (InZone(p) && walkingTheRightWay && director.TryScare(scareId, 2, ScareSize.Small))
                {
                    Fired = true;
                    Running = true;
                    startedAt = Time.time;
                    lastStepPos = p;
                }
                return;
            }

            // turned around: they stop dead
            Vector3 behind = BehindPoint(p);
            Vector3 look = eye.forward; look.y = 0f;
            Vector3 toIt = behind - eye.position; toIt.y = 0f;
            if (Vector3.Dot(look.normalized, toIt.normalized) > 0.2f)
            {
                StoppedByTurning = true;
                Stop();
                lines?.Say("steps_turn");
                return;
            }
            if (Time.time - startedAt > maxSeconds) { Stop(); return; }

            if (moved.sqrMagnitude > 1e-6f) { stillFor = 0f; extraStepDone = false; }
            else stillFor += Time.deltaTime;
            Vector3 since = p - lastStepPos; since.y = 0f;
            if (since.magnitude >= strideMetres) Step(behind, p);
            else if (stillFor > 0.6f && !extraStepDone) { extraStepDone = true; Step(behind, p); } // you stop; it takes one more
        }

        Vector3 BehindPoint(Vector3 p) => p - walkDirection.normalized * followDistance;

        void Step(Vector3 at, Vector3 playerPos)
        {
            lastStepPos = playerPos;
            source.transform.position = at + Vector3.up * 0.1f;
            source.pitch = Random.Range(0.85f, 0.95f);
            source.PlayOneShot(PlaceholderTones.Get("step"));
            StepsPlayed++;
        }

        void Stop()
        {
            Running = false;
            source.Stop();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.6f, 0.2f, 0.9f, 0.4f);
            Gizmos.DrawWireCube(transform.position, zoneSize);
        }
    }
}
