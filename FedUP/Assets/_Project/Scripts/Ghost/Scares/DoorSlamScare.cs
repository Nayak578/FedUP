using System.Collections;
using FedUp.Audio;
using FedUp.Story;
using UnityEngine;

namespace FedUp.Ghost.Scares
{
    /// <summary>
    /// GDD "Door Slam": the door behind you slams shut; when you come back, it opens slowly by itself with fog
    /// seeping out. Armed after the mirror; fires once you've walked out of the room with your back to the door
    /// and the director allows.
    /// </summary>
    public class DoorSlamScare : MonoBehaviour
    {
        [SerializeField] string scareId = "bathroom_door";
        [SerializeField] GhostDirector director;
        [SerializeField] MirrorScare armedAfter;
        [Tooltip("The door's hinge; the leaf is its child.")]
        [SerializeField] Transform hinge;
        [SerializeField] float openAngle = -90f;
        [SerializeField] float closedAngle = 0f;
        [Tooltip("Points out of the room, through the doorway.")]
        [SerializeField] Vector3 outward = Vector3.forward;
        [SerializeField] Transform player;
        [SerializeField] Transform eye;
        [SerializeField, Min(0.5f)] float slamWhenOutBy = 1.6f;
        [Tooltip("Further than this and you wouldn't feel it: it waits for a later pass down the hallway.")]
        [SerializeField, Min(1f)] float slamWithin = 9f;
        [SerializeField, Range(60f, 180f)] float behindAngle = 110f;
        [SerializeField, Min(0.5f)] float reopenWithin = 3f;
        [SerializeField, Min(0f)] float reopenAfter = 4f;
        [SerializeField, Min(0.5f)] float reopenSeconds = 5f;
        [SerializeField] ParticleSystem fog;
        [SerializeField] MonologueLibrarySO lines;

        public enum DoorState { Open, Slammed, Reopening, Reopened }
        public DoorState State { get; private set; } = DoorState.Open;
        public int Slams { get; private set; }
        public float Angle => hinge.localEulerAngles.y > 180f ? hinge.localEulerAngles.y - 360f : hinge.localEulerAngles.y;

        AudioSource sfx;
        float slammedAt;

        void Awake()
        {
            hinge.localRotation = Quaternion.Euler(0f, openAngle, 0f);
            sfx = hinge.gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0.9f;
            sfx.maxDistance = 25f;
            if (fog != null) fog.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        void Update()
        {
            Vector3 fromDoor = player.position - transform.position; fromDoor.y = 0f;
            float outBy = Vector3.Dot(fromDoor, outward.normalized);
            switch (State)
            {
                case DoorState.Open:
                    if (armedAfter != null && !armedAfter.Finished) return;
                    if (outBy < slamWhenOutBy || fromDoor.magnitude > slamWithin) return;
                    Vector3 toDoor = transform.position - eye.position; toDoor.y = 0f;
                    Vector3 look = eye.forward; look.y = 0f;
                    if (Vector3.Angle(look, toDoor) < behindAngle) return; // only when it's behind you
                    if (!director.TryScare(scareId, 2, ScareSize.Big)) return;
                    StartCoroutine(Slam());
                    break;
                case DoorState.Slammed:
                    if (Time.time - slammedAt >= reopenAfter && fromDoor.magnitude <= reopenWithin) StartCoroutine(Reopen());
                    break;
            }
        }

        IEnumerator Slam()
        {
            State = DoorState.Slammed;
            Slams++;
            slammedAt = Time.time;
            float from = openAngle;
            for (float t = 0f; t < 0.14f; t += Time.deltaTime)
            {
                hinge.localRotation = Quaternion.Euler(0f, Mathf.Lerp(from, closedAngle, t / 0.14f), 0f);
                yield return null;
            }
            hinge.localRotation = Quaternion.Euler(0f, closedAngle, 0f);
            sfx.PlayOneShot(PlaceholderTones.Get("slam"));
            yield return new WaitForSeconds(1.2f);
            lines?.Say("door_slam");
        }

        IEnumerator Reopen()
        {
            State = DoorState.Reopening;
            sfx.PlayOneShot(PlaceholderTones.Get("creak"));
            if (fog != null) fog.Play();
            for (float t = 0f; t < reopenSeconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / reopenSeconds);
                hinge.localRotation = Quaternion.Euler(0f, Mathf.Lerp(closedAngle, openAngle * 0.6f, k), 0f); // not all the way
                yield return null;
            }
            State = DoorState.Reopened;
            yield return new WaitForSeconds(4f);
            if (fog != null) fog.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
