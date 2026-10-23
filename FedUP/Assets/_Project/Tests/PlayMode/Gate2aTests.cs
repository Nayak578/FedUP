using System.Collections;
using System.Linq;
using FedUp.Frustration;
using FedUp.Interaction;
using FedUp.Player;
using FedUp.Story;
using FedUp.Tasks;
using FedUp.Tasks.Bath;
using FedUp.Tasks.Cleanup;
using FedUp.Tasks.Phone;
using FedUp.Tasks.Toast;
using FedUp.Tasks.Tv;
using FedUp.UI;
using FedUp.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FedUp.Tests
{
    /// <summary>Gate 2a in the real greybox scene: Take a Bath, Clean Up Mess, boss call #1, room labels.</summary>
    public class Gate2aTests
    {
        FirstPersonMotor motor;
        Interactor interactor;
        FrustrationSystem frustration;
        TaskRunner runner;
        SubtitleUI subtitles;

        [UnitySetUp]
        public IEnumerator Load()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Greybox");
            yield return null;
            yield return new WaitForSeconds(0.3f);
            motor = Object.FindAnyObjectByType<FirstPersonMotor>();
            interactor = Object.FindAnyObjectByType<Interactor>();
            frustration = Object.FindAnyObjectByType<FrustrationSystem>();
            runner = Object.FindAnyObjectByType<TaskRunner>();
            subtitles = Object.FindAnyObjectByType<SubtitleUI>();
            Object.FindAnyObjectByType<CutscenePlayer>().SkipAll();
            motor.UseVirtualInput = true;
            Object.FindAnyObjectByType<FrontDoor>().Open();
            motor.Teleport(GameObject.Find("ROOM_Entrance").transform.position, 90f);
            yield return new WaitForSeconds(0.2f);
        }

        [UnityTearDown]
        public IEnumerator ResetTime() { Time.timeScale = 1f; yield return null; }

        IEnumerator SkipTo<T>() where T : TaskBehaviour
        {
            for (int i = 0; i < 10 && !(runner.Current is T); i++) { runner.DebugSkipCurrent(); yield return null; }
            Assert.IsInstanceOf<T>(runner.Current);
            yield return null;
        }

        IEnumerator Use(string id, float hold = 0f)
        {
            var t = TaskInteractable.Find(id);
            Assert.IsNotNull(t, id);
            Vector3 away = motor.transform.position - t.transform.position; away.y = 0;
            if (away.sqrMagnitude < 0.01f) away = Vector3.right;
            Vector3 feet = StandSpot(t.transform, away.normalized);
            feet.y = motor.transform.position.y + 0.3f;
            motor.Teleport(feet, 0f);
            yield return null;
            Vector3 d = t.transform.position - interactor.transform.position;
            motor.SetLook(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg);
            yield return null; yield return null;
            Assert.AreSame(t, interactor.Current as TaskInteractable, $"looking at {id}");
            if (hold > 0f) { interactor.VirtualHeld = true; yield return new WaitForSeconds(hold); interactor.VirtualHeld = false; }
            else interactor.VirtualPress();
            yield return null; yield return null;
        }

        /// <summary>
        /// 0.9 m from the target on open floor with a clear view of it: the way we came if that works, otherwise the
        /// nearest direction around it that does (coming from the bin, "the way we came" can be inside the couch).
        /// </summary>
        static Vector3 StandSpot(Transform target, Vector3 preferred)
        {
            for (int i = 0; i < 16; i++)
            {
                float a = (i % 2 == 0 ? 1 : -1) * ((i + 1) / 2) * 22.5f;
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * preferred;
                Vector3 feet = target.position + dir * 0.9f;
                if (!NavMesh.SamplePosition(new Vector3(feet.x, 4.85f, feet.z), out var hit, 0.15f, NavMesh.AllAreas)) continue;
                Vector3 eye = new Vector3(feet.x, hit.position.y + 1.6f, feet.z);
                if (Physics.Linecast(eye, target.position, out var block) && !block.collider.transform.IsChildOf(target)) continue;
                return feet;
            }
            return target.position + preferred * 0.9f;
        }

        // ---------------- bath ----------------

        [UnityTest]
        public IEnumerator Bath_NothingLost_Calms10_AndGivesControlBack()
        {
            yield return SkipTo<BathTask>();
            frustration.SetValue(20f);
            var bath = Object.FindAnyObjectByType<BathTask>();
            yield return Use("bathtub", hold: 1.4f);
            Assert.IsTrue(bath.Bathing);
            Assert.IsTrue(motor.Locked, "lying in the bath");
            Time.timeScale = 3f;
            bath.VirtualRelaxHeld = true;
            while (bath.Bathing)
            {
                if (bath.Session.Prompt.HasValue) bath.VirtualPress(bath.Session.Prompt.Value.Direction);
                yield return null;
            }
            Time.timeScale = 1f;
            Assert.AreEqual(3, bath.Session.Saved);
            Assert.AreEqual(10f, frustration.Value, 1e-3f, "-10 for a perfect bath");
            Assert.AreEqual(TaskState.Done, bath.State);
            Assert.IsFalse(motor.Locked, "out of the bath");
            Assert.IsInstanceOf<CleanupTask>(runner.Current, "clean-up is next");
        }

        [UnityTest]
        public IEnumerator Bath_LosingEverything_Costs6Each()
        {
            yield return SkipTo<BathTask>();
            var bath = Object.FindAnyObjectByType<BathTask>();
            yield return Use("bathtub", hold: 1.4f);
            Time.timeScale = 4f;
            bath.VirtualRelaxHeld = true;
            yield return new WaitUntil(() => !bath.Bathing);
            Time.timeScale = 1f;
            Assert.AreEqual(3, bath.Session.Lost);
            Assert.AreEqual(18f, frustration.Value, 1e-3f, "+6 x 3");
            StringAssert.Contains("didn't need it", subtitles.LastText);
        }

        // ---------------- clean up ----------------

        IEnumerator BinItem(ClutterItem item)
        {
            yield return Use(item.Pickup.id);
            Assert.AreSame(item, Object.FindAnyObjectByType<CleanupTask>().Carried);
            yield return Use(item.kind == ClutterKind.Laundry ? "bin_laundry" : "bin_trash");
        }

        [UnityTest]
        public IEnumerator Cleanup_EverythingBinned_WithTheOneComedyFlyback()
        {
            yield return SkipTo<CleanupTask>();
            var clean = Object.FindAnyObjectByType<CleanupTask>();
            for (int guard = 0; guard < 10 && clean.State == TaskState.Active; guard++)
            {
                var item = Object.FindObjectsByType<ClutterItem>(FindObjectsSortMode.None).First(i => !i.InBin);
                yield return new WaitForSeconds(0.7f); // let a flown-back item land
                yield return BinItem(item);
            }
            Assert.AreEqual(TaskState.Done, clean.State);
            Assert.AreEqual(1, clean.ComedyFlybacks, "the joke happens once");
            Assert.AreEqual(8f, frustration.Value, 1e-3f, "flyback +6 and giggle +2");
            Assert.IsInstanceOf<TvTask>(runner.Current, "TV is next (GDD order: clean-up, TV, then the boss calls)");
        }

        [UnityTest]
        public IEnumerator Cleanup_WrongContainer_IsRefused()
        {
            yield return SkipTo<CleanupTask>();
            var clean = Object.FindAnyObjectByType<CleanupTask>();
            var shirt = Object.FindObjectsByType<ClutterItem>(FindObjectsSortMode.None).First(i => i.kind == ClutterKind.Laundry);
            yield return Use(shirt.Pickup.id);
            yield return Use("bin_trash");
            Assert.AreSame(shirt, clean.Carried, "still holding it");
            StringAssert.Contains("Not in there", subtitles.LastText);
        }

        [UnityTest]
        public IEnumerator Cleanup_Above40Percent_TheGhostRejectsADrop()
        {
            yield return SkipTo<CleanupTask>();
            frustration.SetValue(50f);
            yield return null;
            var clean = Object.FindAnyObjectByType<CleanupTask>();
            clean.ForcedRoll = 0f;
            var item = Object.FindObjectsByType<ClutterItem>(FindObjectsSortMode.None).First();
            yield return BinItem(item);
            Assert.AreEqual(1, clean.Rejections);
            Assert.IsFalse(item.InBin);
            Assert.AreEqual(58f, frustration.Value, 1e-3f, "+6 +2");
        }

        [UnityTest]
        public IEnumerator StoryProps_SayTheirLine()
        {
            yield return Use("story_prescription");
            StringAssert.Contains("Haven't opened it", subtitles.LastText);
            yield return Use("story_card");
            StringAssert.Contains("from Mom", subtitles.LastText);
        }

        // ---------------- phone ----------------

        [UnityTest]
        public IEnumerator Phone1_Answered_Minus5_AndRecorded()
        {
            yield return SkipTo<PhoneCallTask>();
            frustration.SetValue(20f);
            var call = Object.FindAnyObjectByType<PhoneCallTask>();
            yield return new WaitUntil(() => call.Ringing);
            yield return Use("phone");
            Assert.IsTrue(call.Answered);
            Assert.AreEqual(15f, frustration.Value, 1e-3f, "-5 answered");
            var run = Resources.FindObjectsOfTypeAll<RunStateSO>().First();
            Assert.IsTrue(run.CallAnswered(1));
            yield return new WaitUntil(() => call.State == TaskState.Done);
        }

        [UnityTest]
        public IEnumerator Phone1_Ignored_Plus2_AndRecordedAsMissed()
        {
            yield return SkipTo<PhoneCallTask>();
            var call = Object.FindAnyObjectByType<PhoneCallTask>();
            Time.timeScale = 6f;
            yield return new WaitUntil(() => call.State == TaskState.Done);
            Time.timeScale = 1f;
            Assert.IsTrue(call.Ignored);
            Assert.AreEqual(2f, frustration.Value, 1e-3f, "+2 ignored");
            var run = Resources.FindObjectsOfTypeAll<RunStateSO>().First();
            Assert.IsTrue(run.CallHappened(1));
            Assert.IsFalse(run.CallAnswered(1));
        }

        // ---------------- house ----------------

        [Test]
        public void LockedRoom_CannotBeEntered()
        {
            var locked = Object.FindObjectsByType<RoomMarker>(FindObjectsSortMode.None).First(m => m.roomName == "Locked Room");
            var path = new NavMeshPath();
            NavMesh.CalculatePath(motor.transform.position, locked.transform.position, NavMesh.AllAreas, path);
            Assert.AreNotEqual(NavMeshPathStatus.PathComplete, path.status);
        }

        [Test]
        public void DesignerRoomNames_AreAllPresent()
        {
            var names = Object.FindObjectsByType<RoomMarker>(FindObjectsSortMode.None).Select(m => m.roomName).ToList();
            foreach (var n in new[] { "Living Room", "Kitchen", "Bathroom Downstairs", "Guest Bedroom", "Garage", "Bedroom", "Bathroom Upstairs",
                                      "Work Room", "Balcony", "Bathroom Middle", "Wardrobe", "Locked Room", "Storeroom", "Hallway" })
                CollectionAssert.Contains(names, n);
        }
    }
}
