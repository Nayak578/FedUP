using System.Collections;
using System.Linq;
using FedUp.Frustration;
using FedUp.Ghost;
using FedUp.Ghost.Scares;
using FedUp.Interaction;
using FedUp.Player;
using FedUp.Story;
using FedUp.Tasks;
using FedUp.Tasks.Night;
using FedUp.Tasks.Tv;
using FedUp.UI;
using FedUp.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FedUp.Tests
{
    /// <summary>Gate 3 in the real greybox scene: falling asleep, the Phase 2 house, the director, and three scares.</summary>
    public class Gate3Tests
    {
        FirstPersonMotor motor;
        Interactor interactor;
        FrustrationSystem frustration;
        TaskRunner runner;
        SubtitleUI subtitles;
        NightPhaseController phase;
        GhostDirector director;

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
            phase = Object.FindAnyObjectByType<NightPhaseController>();
            director = Object.FindAnyObjectByType<GhostDirector>();
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
            for (int i = 0; i < 12 && !(runner.Current is T); i++) { runner.DebugSkipCurrent(); yield return null; }
            Assert.IsInstanceOf<T>(runner.Current);
            yield return null;
        }

        IEnumerator Use(string id, float hold = 0f)
        {
            var t = TaskInteractable.Find(id);
            Assert.IsNotNull(t, id);
            Vector3 away = motor.transform.position - t.transform.position; away.y = 0;
            if (away.sqrMagnitude < 0.01f) away = Vector3.right;
            Vector3 feet = t.transform.position + away.normalized * 0.9f;
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

        /// <summary>Skips the evening and the sleep scene: the house is in Phase 2 and the haunting has started.</summary>
        IEnumerator IntoPhase2()
        {
            yield return SkipTo<SleepTask>();
            runner.DebugSkipCurrent();
            yield return null;
            Assert.AreEqual(2, phase.Phase);
        }

        /// <summary>Lets game time pass quickly (the director's grace and quiet are game-time rules).</summary>
        IEnumerator Fast(float gameSeconds)
        {
            Time.timeScale = 8f;
            float until = Time.time + gameSeconds;
            while (Time.time < until) yield return null;
            Time.timeScale = 1f;
        }

        IEnumerator WaitUntilFast(System.Func<bool> done, float gameSeconds)
        {
            Time.timeScale = 6f;
            float until = Time.time + gameSeconds;
            while (!done() && Time.time < until) yield return null;
            Time.timeScale = 1f;
        }

        void Stand(Vector3 xz, float yaw, float pitch = 0f)
        {
            motor.Teleport(new Vector3(xz.x, motor.transform.position.y + 0.3f, xz.z), yaw);
            motor.SetLook(yaw, pitch);
        }

        IEnumerator WalkFor(float seconds)
        {
            motor.VirtualMove = new Vector2(0f, 1f);
            yield return new WaitForSeconds(seconds);
            motor.VirtualMove = Vector2.zero;
        }

        IEnumerator SplashAndLookInTheMirror()
        {
            yield return Use("sink");
            var mirror = GameObject.Find("PREF_Mirror (greybox)").transform.position;
            Stand(new Vector3(mirror.x, 0f, mirror.z + 1.2f), 180f, 4f);
            yield return null;
        }

        // ---------------- sleep and the Phase 2 house ----------------

        [UnityTest]
        public IEnumerator LyingDown_FallsAsleep_AndWakesInTheSickHouse()
        {
            yield return SkipTo<SleepTask>();
            Assert.AreEqual(1, phase.Phase);
            float fog1 = RenderSettings.fogDensity;
            var sleep = Object.FindAnyObjectByType<SleepTask>();
            yield return Use("couch_sleep", hold: 1.4f);
            Assert.IsTrue(sleep.Asleep);
            yield return WaitUntilFast(() => sleep.State == TaskState.Done, 40f);
            Assert.AreEqual(TaskState.Done, sleep.State);
            Assert.AreEqual(2, phase.Phase, "Phase 2");
            Assert.Greater(RenderSettings.fogDensity, fog1, "thicker fog");
            var lit = GameObject.Find("Lights").GetComponentsInChildren<Light>().Where(l => l.enabled).Select(l => l.name).ToList();
            Assert.AreEqual(2, lit.Count, "only the path stays lit: " + string.Join(", ", lit));
            Assert.IsTrue(phase.DronePlaying, "the drone");
            Assert.IsTrue(Object.FindAnyObjectByType<PhoneTorch>().Available);
            Assert.AreEqual(TvChannel.Static, Object.FindAnyObjectByType<TvTask>().ForcedChannel, "the TV turned to static by itself");
            Assert.IsInstanceOf<UseTask>(runner.Current);
            StringAssert.Contains("Splash water", runner.ObjectiveText);
            Assert.IsFalse(motor.Locked, "control is back");
        }

        [UnityTest]
        public IEnumerator Torch_OnlyFromTheWakeUp_AndFToggles()
        {
            yield return SkipTo<SleepTask>();
            var torch = Object.FindAnyObjectByType<PhoneTorch>();
            torch.VirtualToggle();
            yield return null;
            Assert.IsFalse(torch.Lit, "no torch in the evening");
            runner.DebugSkipCurrent();
            yield return null;
            torch.VirtualToggle();
            yield return null;
            Assert.IsTrue(torch.Lit);
            torch.VirtualToggle();
            yield return null;
            Assert.IsFalse(torch.Lit);
        }

        // ---------------- scares ----------------

        [UnityTest]
        public IEnumerator InTheEvening_TheHallwayIsSilent()
        {
            yield return SkipTo<SleepTask>();
            var steps = Object.FindAnyObjectByType<FootstepStalker>();
            Stand(new Vector3(-35.8f, 0f, 1.0f), 90f);
            yield return WalkFor(2f);
            Assert.IsFalse(steps.Fired, "no scares in Phase 1");
        }

        [UnityTest]
        public IEnumerator Hallway_FootstepsFollowYou_AndStopWhenYouTurn()
        {
            yield return IntoPhase2();
            yield return Fast(7f); // the director's grace after waking
            var steps = Object.FindAnyObjectByType<FootstepStalker>();
            Stand(new Vector3(-35.8f, 0f, 1.0f), 90f);
            yield return WalkFor(2.2f);
            Assert.IsTrue(steps.Fired);
            Assert.GreaterOrEqual(steps.StepsPlayed, 2, "a step behind you for your steps");
            Stand(new Vector3(motor.transform.position.x, 0f, motor.transform.position.z), 270f);
            yield return null; yield return null;
            Assert.IsTrue(steps.StoppedByTurning);
            int n = steps.StepsPlayed;
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(n, steps.StepsPlayed, "nothing there");
            StringAssert.Contains("Hello", subtitles.LastText);
        }

        [UnityTest]
        public IEnumerator Mirror_AfterTheSplash_LagsThenAFigure_ThenItCracksSilently()
        {
            yield return IntoPhase2();
            yield return Fast(7f);
            var scare = Object.FindAnyObjectByType<MirrorScare>();
            yield return SplashAndLookInTheMirror();
            Assert.IsTrue(scare.Armed);
            Assert.IsInstanceOf<ReachTask>(runner.Current, "next: back to the living room");
            float waited = 0f;
            while (!scare.Finished && waited < 15f) { waited += Time.deltaTime; yield return null; }
            Assert.IsTrue(scare.FigureWasShown, "something behind you");
            Assert.IsTrue(scare.Cracked, "the mirror cracked");
            Assert.AreEqual("bathroom_mirror", director.LastAllowed);
        }

        [UnityTest]
        public IEnumerator BathroomDoor_SlamsBehindYou_ThenReopensWithFog_Once()
        {
            yield return IntoPhase2();
            yield return Fast(7f);
            var mirror = Object.FindAnyObjectByType<MirrorScare>();
            var door = Object.FindAnyObjectByType<DoorSlamScare>();
            yield return SplashAndLookInTheMirror();
            float waited = 0f;
            while (!mirror.Finished && waited < 15f) { waited += Time.deltaTime; yield return null; }
            Assert.AreEqual(DoorSlamScare.DoorState.Open, door.State);
            Stand(new Vector3(-30.6f, 0f, 1.3f), 270f); // in the hallway, walking away, the door behind you
            yield return WaitUntilFast(() => door.State == DoorSlamScare.DoorState.Slammed, 20f);
            Assert.AreEqual(DoorSlamScare.DoorState.Slammed, door.State);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(0f, door.Angle, 1f, "shut");
            yield return Fast(4.5f);
            Stand(new Vector3(-29.2f, 0f, 0.9f), 180f); // come back to it
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(DoorSlamScare.DoorState.Reopening, door.State);
            Assert.IsTrue(door.GetComponentInChildren<ParticleSystem>().isPlaying, "fog seeping out");
            yield return WaitUntilFast(() => door.State == DoorSlamScare.DoorState.Reopened, 10f);
            Stand(new Vector3(-30.6f, 0f, 1.3f), 270f);
            yield return Fast(20f);
            Assert.AreEqual(1, door.Slams, "it only happens once");
        }

        [UnityTest]
        public IEnumerator Director_KeepsTheHouseQuiet_BetweenScares()
        {
            yield return IntoPhase2();
            yield return Fast(7f);
            frustration.SetValue(0f);
            Stand(new Vector3(-35.8f, 0f, 1.0f), 90f);
            yield return WalkFor(2.2f);
            Assert.AreEqual("hallway_steps", director.LastAllowed);
            float stepsAt = director.LastAllowedAt;
            var mirror = Object.FindAnyObjectByType<MirrorScare>();
            yield return SplashAndLookInTheMirror();
            float quiet = director.Rules.QuietAfter(ScareSize.Small, 0f);
            while (Time.time < stepsAt + quiet - 0.5f) { Assert.IsFalse(mirror.Fired, "too soon after the footsteps"); yield return null; }
            float waited = 0f;
            while (!mirror.Fired && waited < 5f) { waited += Time.deltaTime; yield return null; }
            Assert.IsTrue(mirror.Fired, "allowed once the quiet is over");
            Assert.GreaterOrEqual(director.LastAllowedAt - stepsAt, quiet - 0.05f);
        }

        [UnityTest]
        public IEnumerator BackInTheLivingRoom_TheGateEnds()
        {
            yield return IntoPhase2();
            runner.DebugSkipCurrent(); // the splash
            yield return null;
            Assert.IsInstanceOf<ReachTask>(runner.Current);
            Stand(GameObject.Find("ROOM_LivingRoom").transform.position, 0f);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(runner.AllDone);
            StringAssert.Contains("not going back to sleep", subtitles.LastText);
        }

        // ---------------- design reasons (DESIGN_LOGIC.md #32, #35) ----------------

        [Test]
        public void DesignLogic_TheMirrorFacesTheOnlyWayIn()
        {
            var mirror = GameObject.Find("PREF_Mirror (greybox)").transform;
            var door = GameObject.Find("CTRL_BathroomDoorway").transform.position + Vector3.up * 1.4f;
            Vector3 to = door - mirror.position; to.y = 0f;
            Assert.Less(Vector3.Angle(mirror.forward, to), 15f, "the reflection is centred on the doorway");
            Assert.IsFalse(Physics.Linecast(mirror.position + mirror.forward * 0.3f, door), "nothing blocks the view of the doorway");
        }

        [Test]
        public void DesignLogic_ThePhase2LightsAreThePath()
        {
            var keep = new[] { "LGT_BathroomDownstairs", "LGT_BackHall" };
            var names = phase.KeepLit.Select(l => l.name).OrderBy(n => n).ToArray();
            CollectionAssert.AreEqual(keep.OrderBy(n => n).ToArray(), names);
        }
    }
}
