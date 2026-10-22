using System.Collections;
using System.Linq;
using FedUp.Interaction;
using FedUp.Player;
using FedUp.World;
using UnityEngine;
using UnityEngine.AI;

namespace FedUp.DebugTools
{
    /// <summary>
    /// Hands-free tour for evidence videos and build checks. Runs only when the game is launched with
    /// "-autopilot" (or when a test sets <see cref="forceRun"/>). Walks to the light switch, flips it,
    /// then visits every room in tour order, logs results and quits.
    /// </summary>
    public class Autopilot : MonoBehaviour
    {
        [SerializeField] FirstPersonMotor motor;
        [SerializeField] Interactor interactor;
        [SerializeField] LightSwitch firstSwitch;
        [SerializeField] float turnSpeed = 220f;
        [SerializeField] float legTimeout = 25f;
        public bool forceRun;
        public bool quitWhenDone = true;

        public bool Finished { get; private set; }
        public int RoomsVisited { get; private set; }
        public int RoomsTotal { get; private set; }
        public int SwitchToggles { get; private set; }

        float frameTimeSum; int frameCount; float worstFrame;

        void Start()
        {
            bool cli = System.Environment.GetCommandLineArgs().Any(a => a == "-autopilot");
            if (!cli && !forceRun) { enabled = false; return; }
            motor.UseVirtualInput = true;
            if (firstSwitch != null) firstSwitch.Toggled += _ => SwitchToggles++;
            StartCoroutine(Tour());
        }

        void Update()
        {
            if (Time.timeSinceLevelLoad < 2f || Finished) return;
            float ms = Time.unscaledDeltaTime * 1000f;
            frameTimeSum += ms; frameCount++; worstFrame = Mathf.Max(worstFrame, ms);
        }

        IEnumerator Tour()
        {
            yield return new WaitForSeconds(1.5f);
            var rooms = FindObjectsByType<RoomMarker>(FindObjectsSortMode.None).Where(r => r.tourOrder > 0).OrderBy(r => r.tourOrder).ToList();
            RoomsTotal = rooms.Count;
            Debug.Log($"AUTOPILOT START rooms={RoomsTotal}");

            if (firstSwitch != null)
            {
                var stand = firstSwitch.transform.position + firstSwitch.transform.forward * 0.8f;
                yield return WalkTo(stand, "light switch");
                yield return FaceTowards(firstSwitch.transform.position);
                yield return new WaitForSeconds(0.4f);
                interactor.VirtualPress();
                yield return new WaitForSeconds(1.2f);
            }

            foreach (var room in rooms)
            {
                bool ok = false;
                yield return WalkTo(room.transform.position, room.roomName, r => ok = r);
                if (ok) { RoomsVisited++; Debug.Log($"AUTOPILOT VISITED {room.roomName}"); }
                yield return LookAround();
            }

            Finished = true;
            motor.VirtualMove = Vector2.zero;
            float avg = frameCount > 0 ? frameTimeSum / frameCount : 0f;
            Debug.Log($"AUTOPILOT DONE visited={RoomsVisited}/{RoomsTotal} switchToggles={SwitchToggles} avgFrameMs={avg:F2} worstFrameMs={worstFrame:F2} frames={frameCount}");
            if (quitWhenDone && !Application.isEditor) { yield return new WaitForSeconds(1f); Application.Quit(); }
        }

        IEnumerator WalkTo(Vector3 target, string label, System.Action<bool> done = null)
        {
            var path = new NavMeshPath();
            if (!NavMesh.SamplePosition(target, out var hit, 1.5f, NavMesh.AllAreas) ||
                !NavMesh.CalculatePath(motor.transform.position, hit.position, NavMesh.AllAreas, path) ||
                path.status != NavMeshPathStatus.PathComplete)
            {
                Debug.LogWarning($"AUTOPILOT NO PATH to {label}");
                done?.Invoke(false);
                yield break;
            }

            float started = Time.time;
            foreach (var corner in path.corners.Skip(1))
            {
                while (Flat(corner - motor.transform.position).magnitude > 0.35f)
                {
                    if (Time.time - started > legTimeout)
                    {
                        Debug.LogWarning($"AUTOPILOT STUCK on the way to {label} at {motor.transform.position}");
                        motor.VirtualMove = Vector2.zero;
                        done?.Invoke(false);
                        yield break;
                    }
                    Vector3 to = Flat(corner - motor.transform.position);
                    float targetYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                    float yaw = Mathf.MoveTowardsAngle(motor.Yaw, targetYaw, turnSpeed * Time.deltaTime);
                    motor.SetLook(yaw, Mathf.MoveTowards(motor.Pitch, 0f, 60f * Time.deltaTime));
                    float facing = Mathf.Abs(Mathf.DeltaAngle(yaw, targetYaw));
                    motor.VirtualMove = new Vector2(0f, facing < 50f ? 1f : 0.2f);
                    yield return null;
                }
            }
            motor.VirtualMove = Vector2.zero;
            done?.Invoke(true);
        }

        IEnumerator FaceTowards(Vector3 point)
        {
            Vector3 eye = interactor.transform.position;
            Vector3 d = point - eye;
            float targetYaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            float targetPitch = -Mathf.Atan2(d.y, Flat(d).magnitude) * Mathf.Rad2Deg;
            for (float t = 0; t < 1.2f; t += Time.deltaTime)
            {
                motor.SetLook(Mathf.MoveTowardsAngle(motor.Yaw, targetYaw, turnSpeed * Time.deltaTime),
                              Mathf.MoveTowards(motor.Pitch, targetPitch, 90f * Time.deltaTime));
                yield return null;
            }
        }

        IEnumerator LookAround()
        {
            float start = motor.Yaw;
            for (float t = 0; t < 0.8f; t += Time.deltaTime)
            {
                motor.SetLook(start + Mathf.Sin(t / 0.8f * Mathf.PI * 2f) * 35f, 0f);
                yield return null;
            }
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
