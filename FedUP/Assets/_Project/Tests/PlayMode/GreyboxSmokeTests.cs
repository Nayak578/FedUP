using System.Collections;
using System.Linq;
using FedUp.Player;
using FedUp.Story;
using FedUp.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FedUp.Tests
{
    /// <summary>Slice 0 gate: the greybox house loads cleanly and every room can be walked to, but not the outside.</summary>
    public class GreyboxSmokeTests
    {
        FirstPersonMotor motor;
        SpawnPoint spawn;

        [UnitySetUp]
        public IEnumerator LoadGreybox()
        {
            SceneManager.LoadScene("Greybox");
            yield return null;
            yield return new WaitForSeconds(0.5f);
            motor = Object.FindAnyObjectByType<FirstPersonMotor>();
            spawn = Object.FindAnyObjectByType<SpawnPoint>();
            Object.FindAnyObjectByType<CutscenePlayer>()?.SkipAll(); // the prologue takes control away
            motor.UseVirtualInput = true;
            motor.VirtualMove = Vector2.zero;
        }

        [UnityTest]
        public IEnumerator SceneLoads_WithPlayerAndRooms_AndNoErrors()
        {
            yield return new WaitForSeconds(0.5f);
            Assert.IsNotNull(motor, "player");
            Assert.IsNotNull(spawn, "spawn point");
            Assert.GreaterOrEqual(Object.FindObjectsByType<RoomMarker>(FindObjectsSortMode.None).Length, 10, "room markers");
            // Any Debug.LogError or exception during load fails this test automatically.
        }

        [UnityTest]
        public IEnumerator Player_StartsAtSpawn_StandingOnTheFloor()
        {
            yield return new WaitForSeconds(0.3f);
            Vector3 d = motor.transform.position - spawn.transform.position;
            d.y = 0;
            Assert.Less(d.magnitude, 0.5f, "distance from spawn");
            Assert.IsTrue(motor.IsGrounded, "grounded");
            Assert.Less(Mathf.Abs(motor.transform.position.y - spawn.transform.position.y), 0.3f, "feet at floor height");
        }

        [UnityTest]
        public IEnumerator WalkingForward_TwoSeconds_CoversTiredWalkDistance()
        {
            // Since gate 1 you start on the small porch facing a closed door, so measure the walk somewhere roomy:
            // the upper-floor hallway, facing its longest clear direction.
            var hall = Object.FindObjectsByType<RoomMarker>(FindObjectsSortMode.None).First(m => m.roomName == "Hallway");
            motor.Teleport(hall.transform.position, 0f);
            yield return null;
            float bestYaw = 0f, bestClear = 0f;
            for (int a = 0; a < 360; a += 15)
            {
                var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                float clear = Physics.SphereCast(hall.transform.position + Vector3.up * 1f, 0.35f, dir, out var h, 20f) ? h.distance : 20f;
                if (clear > bestClear) { bestClear = clear; bestYaw = a; }
            }
            Assert.Greater(bestClear, 6f, "need 6 m of clear floor to measure the walk");
            motor.SetLook(bestYaw, 0f);
            yield return new WaitForSeconds(0.3f);
            Vector3 start = motor.transform.position;
            motor.VirtualMove = Vector2.up;
            yield return new WaitForSeconds(2f);
            motor.VirtualMove = Vector2.zero;
            Vector3 d = motor.transform.position - start;
            d.y = 0;
            Assert.That(d.magnitude, Is.InRange(3.5f, 5.5f), $"walked {d.magnitude:F2} m in 2 s");
        }

        [Test]
        public void EveryRoom_IsReachableOnFoot()
        {
            var path = new NavMeshPath();
            var unreachable = Object.FindObjectsByType<RoomMarker>(FindObjectsSortMode.None)
                .Where(m => m.expectReachable)
                .Where(m => !NavMesh.CalculatePath(spawn.transform.position, m.transform.position, NavMesh.AllAreas, path)
                            || path.status != NavMeshPathStatus.PathComplete)
                .Select(m => m.roomName).ToList();
            Assert.IsEmpty(unreachable, "rooms that can't be reached: " + string.Join(", ", unreachable));
        }

        [Test]
        public void EveryRoom_HasACeiling()
        {
            var open = Object.FindObjectsByType<RoomMarker>(FindObjectsSortMode.None)
                .Where(m => m.expectCeiling)
                .Where(m => !Physics.Raycast(m.transform.position + Vector3.up * 1.5f, Vector3.up, 4f, ~0, QueryTriggerInteraction.Ignore))
                .Select(m => m.roomName).ToList();
            Assert.IsEmpty(open, "rooms open to the sky: " + string.Join(", ", open));
        }

        [Test]
        public void Outside_CannotBeReached()
        {
            var house = GameObject.Find("Main").GetComponent<Renderer>().bounds;
            var path = new NavMeshPath();
            var outside = new[]
            {
                new Vector3(house.min.x - 3, house.min.y, house.center.z), new Vector3(house.max.x + 3, house.min.y, house.center.z),
                new Vector3(house.center.x, house.min.y, house.min.z - 3), new Vector3(house.center.x, house.min.y, house.max.z + 3),
            };
            foreach (var o in outside)
            {
                if (!NavMesh.SamplePosition(o, out var hit, 3f, NavMesh.AllAreas)) continue;
                NavMesh.CalculatePath(spawn.transform.position, hit.position, NavMesh.AllAreas, path);
                Assert.AreNotEqual(NavMeshPathStatus.PathComplete, path.status, $"walked out of the house to {hit.position}");
            }
        }
    }
}
