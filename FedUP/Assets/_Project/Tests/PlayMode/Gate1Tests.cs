using System.Collections;
using FedUp.Frustration;
using FedUp.Interaction;
using FedUp.Player;
using FedUp.Story;
using FedUp.Tasks;
using FedUp.Tasks.Toast;
using FedUp.UI;
using FedUp.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FedUp.Tests
{
    /// <summary>Gate 1 in the real greybox scene: porch door, Drop Your Things + the file, Make Toast, alarm, bar.</summary>
    public class Gate1Tests
    {
        FirstPersonMotor motor;
        Interactor interactor;
        FrustrationSystem frustration;
        FrustrationBarUI bar;
        CutscenePlayer cutscenes;
        SubtitleUI subtitles;

        [UnitySetUp]
        public IEnumerator LoadGreybox()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Greybox");
            yield return null;
            yield return new WaitForSeconds(0.3f);
            motor = Object.FindAnyObjectByType<FirstPersonMotor>();
            interactor = Object.FindAnyObjectByType<Interactor>();
            frustration = Object.FindAnyObjectByType<FrustrationSystem>();
            bar = Object.FindAnyObjectByType<FrustrationBarUI>();
            cutscenes = Object.FindAnyObjectByType<CutscenePlayer>();
            subtitles = Object.FindAnyObjectByType<SubtitleUI>();
            motor.UseVirtualInput = true;
        }

        [UnityTearDown]
        public IEnumerator ResetTime()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        IEnumerator Use(string id, float hold = 0f)
        {
            var t = TaskInteractable.Find(id);
            Assert.IsNotNull(t, id);
            Vector3 away = motor.transform.position - t.transform.position; away.y = 0;
            if (away.sqrMagnitude < 0.01f) away = Vector3.right;
            Vector3 feet = t.transform.position + away.normalized * 0.9f;
            feet.y = motor.transform.position.y + 0.3f; // stand on the floor, even for things on the ceiling
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

        [UnityTest]
        public IEnumerator Prologue_PlaysThenHandsBackControl()
        {
            Assert.IsTrue(cutscenes.Playing, "prologue plays at the start");
            Assert.IsTrue(motor.Locked, "no walking during the prologue");
            float total = 0f;
            foreach (var s in cutscenes.Prologue.steps) total += s.seconds;
            yield return new WaitForSeconds(total + 1f);
            Assert.IsFalse(cutscenes.Playing);
            Assert.IsFalse(motor.Locked, "control is back");
        }

        [UnityTest]
        public IEnumerator FrontDoor_OpensFromThePorch_AndLocksBehindYou()
        {
            cutscenes.SkipAll();
            var door = Object.FindAnyObjectByType<FrontDoor>();
            Assert.IsFalse(door.IsOpen);
            yield return Use("front_door");
            Assert.IsTrue(door.IsOpen, "door opened");
            motor.Teleport(new Vector3(-46.3f, 4.85f, -3.3f), 90f); // step inside
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(door.IsLocked, "door closed and locked behind you");
        }

        [UnityTest]
        public IEnumerator DropYourThings_CompletesAndRevealsTheBossFile()
        {
            yield return EnterHouse();
            var drop = Object.FindAnyObjectByType<DropThingsTask>();
            Assert.AreEqual(TaskState.Active, drop.State);
            yield return Use("coat_hook");
            yield return Use("shoe_rack");
            yield return Use("bag_table", hold: 1.5f);
            Assert.IsTrue(drop.FileFound, "the boss's file is on the table");
            Assert.IsTrue(cutscenes.Playing, "the file discovery scene plays");
            yield return new WaitUntil(() => !cutscenes.Playing);
            StringAssert.Contains("not mine", subtitles.LastText);
            Assert.AreEqual(TaskState.Done, drop.State);
            Assert.AreEqual(0f, frustration.Value, 1e-3f, "dropping your things costs no frustration");
            Assert.AreEqual(TaskState.Active, Object.FindAnyObjectByType<MakeToastTask>().State, "toast is next");
        }

        [UnityTest]
        public IEnumerator Jacket_FallsAndCanBeHungBackUp()
        {
            yield return EnterHouse();
            yield return Use("coat_hook");
            yield return Use("shoe_rack");
            yield return Use("bag_table", hold: 1.5f);
            cutscenes.SkipAll();
            var jacket = Object.FindAnyObjectByType<JacketHaunt>();
            Assert.IsTrue(jacket.Armed);
            jacket.Fall();
            Assert.IsTrue(jacket.Fallen);
            yield return Use("coat_hook");
            Assert.IsFalse(jacket.Fallen, "hung back up");
        }

        IEnumerator EnterHouse()
        {
            cutscenes.SkipAll();
            Object.FindAnyObjectByType<FrontDoor>().Open();
            motor.Teleport(GameObject.Find("ROOM_Entrance").transform.position, 90f);
            yield return new WaitForSeconds(0.2f);
        }

        IEnumerator GoToToast()
        {
            yield return EnterHouse();
            yield return Use("coat_hook");
            yield return Use("shoe_rack");
            yield return Use("bag_table", hold: 1.5f);
            cutscenes.SkipAll();
            yield return null;
        }

        [UnityTest]
        public IEnumerator GoldenToast_TakenInTime_CompletesTask_AndCalms5()
        {
            yield return GoToToast();
            frustration.SetValue(20f);
            var toast = Object.FindAnyObjectByType<MakeToastTask>();
            yield return Use("toaster");
            Assert.AreEqual(ToasterState.Toasting, toast.Toaster);
            toast.DebugPressAt(0.68f);
            Assert.AreEqual(ToastOutcome.Golden, toast.LastOutcome);
            yield return Use("toast_pickup");
            Assert.AreEqual(TaskState.Done, toast.State);
            Assert.AreEqual(15f, frustration.Value, 1e-3f, "-5 for golden toast");
            yield return null;
            Assert.AreEqual(0.15f, bar.TargetFraction, 1e-3f, "bar shows 15%");
        }

        [UnityTest]
        public IEnumerator TwoFailures_RingTheAlarm_WhichCostsAbout3PerSecond_UntilSilenced()
        {
            yield return GoToToast();
            var toast = Object.FindAnyObjectByType<MakeToastTask>();
            var alarm = Object.FindAnyObjectByType<SmokeAlarm>();
            yield return Use("toaster");
            toast.DebugPressAt(0.95f);                       // late: burnt
            Assert.AreEqual(ToastOutcome.Burnt, toast.LastOutcome);
            Assert.AreEqual(10f, frustration.Value, 1e-3f, "+10 burnt");
            Assert.IsFalse(alarm.Ringing);
            yield return new WaitUntil(() => toast.Toaster == ToasterState.Idle);
            yield return Use("toaster");
            toast.DebugPressAt(0.2f);                        // early: launched
            Assert.AreEqual(ToastOutcome.Launched, toast.LastOutcome);
            Assert.IsTrue(alarm.Ringing, "two failures in a row set off the alarm");
            float before = frustration.Value;
            yield return new WaitForSeconds(2f);
            float gained = frustration.Value - before;
            Assert.That(gained, Is.InRange(5.0f, 7.0f), $"about +3 per second for 2 s (got {gained:F2})");
            yield return Use("smoke_alarm");
            Assert.IsFalse(alarm.Ringing, "silenced");
            float after = frustration.Value;
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(after, frustration.Value, 1e-3f, "no more cost once silenced");
        }

        [UnityTest]
        public IEnumerator Above30Percent_TheToasterJams_MashingUnjamsIt()
        {
            yield return GoToToast();
            frustration.SetValue(35f);
            yield return null;
            var toast = Object.FindAnyObjectByType<MakeToastTask>();
            yield return Use("toaster");
            Assert.AreEqual(ToasterState.Jammed, toast.Toaster);
            for (int i = 0; i < toast.Tuning.mashPresses && toast.Toaster == ToasterState.Jammed; i++)
            {
                interactor.VirtualPress();
                yield return null; yield return null;
            }
            yield return null;
            Assert.AreEqual(ToasterState.Toasting, toast.Toaster, "unjammed and toasting");
        }

        [UnityTest]
        public IEnumerator GoldenToast_LeftTooLong_GetsEaten_Plus10()
        {
            yield return GoToToast();
            var toast = Object.FindAnyObjectByType<MakeToastTask>();
            yield return Use("toaster");
            toast.DebugPressAt(0.68f);
            Assert.AreEqual(ToasterState.Ready, toast.Toaster);
            Time.timeScale = 8f;                              // 15 game-seconds in about 2 real seconds
            yield return new WaitUntil(() => toast.ToastsEaten > 0);
            Time.timeScale = 1f;
            Assert.AreEqual(10f, frustration.Value, 1e-3f, "+10 eaten by something");
            Assert.AreNotEqual(TaskState.Done, toast.State, "you still have to make toast");
        }

        [UnityTest]
        public IEnumerator SessionLog_IsWritten()
        {
            cutscenes.SkipAll();
            yield return null;
            Assert.IsNotNull(frustration.LogPath);
            Assert.IsTrue(System.IO.File.Exists(frustration.LogPath), frustration.LogPath);
            StringAssert.StartsWith("time_s,source,delta", System.IO.File.ReadAllText(frustration.LogPath));
        }
    }
}
