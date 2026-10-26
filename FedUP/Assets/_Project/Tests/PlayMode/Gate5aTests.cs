using System.Collections;
using System.Linq;
using FedUp.Frustration;
using FedUp.Ghost;
using FedUp.Interaction;
using FedUp.Player;
using FedUp.Story;
using FedUp.Tasks;
using FedUp.Tasks.Appliances;
using FedUp.Tasks.Cat;
using FedUp.Tasks.Night;
using FedUp.Tasks.Phone;
using FedUp.Tasks.Snack;
using FedUp.Tasks.Tea;
using FedUp.Tasks.Toast;
using FedUp.UI;
using FedUp.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FedUp.Tests
{
    /// <summary>
    /// Gate 5a in the real greybox scene: the charger trip and the gun, calls #2-4, cat visits #2-3, the final sleep,
    /// the ending resolver at work, the secret ending and the Showdown. Plus the design reasons (DESIGN_LOGIC #61-76).
    /// </summary>
    public class Gate5aTests
    {
        FirstPersonMotor motor;
        Interactor interactor;
        FrustrationSystem frustration;
        TaskRunner runner;
        SubtitleUI subtitles;
        RunStateSO runState;

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
            runState = (RunStateSO)typeof(FinalSleepTask).GetField("runState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(Object.FindAnyObjectByType<FinalSleepTask>());
            Object.FindAnyObjectByType<CutscenePlayer>().SkipAll();
            motor.UseVirtualInput = true;
            Object.FindAnyObjectByType<FrontDoor>().Open();
            motor.Teleport(GameObject.Find("ROOM_Entrance").transform.position, 90f);
            yield return new WaitForSeconds(0.2f);
        }

        [UnityTearDown]
        public IEnumerator ResetTime() { Time.timeScale = 1f; yield return null; }

        IEnumerator SkipTo<T>(System.Func<T, bool> which = null) where T : TaskBehaviour
        {
            for (int i = 0; i < 24 && !(runner.Current is T t && (which == null || which(t))); i++) { runner.DebugSkipCurrent(); yield return null; }
            Assert.IsInstanceOf<T>(runner.Current);
            yield return null;
        }

        /// <summary>Close to the target on open floor (either storey) with a clear view of it.</summary>
        static Vector3 StandSpot(Transform target, Vector3 preferred, float distance = 0.9f)
        {
            for (int i = 0; i < 16; i++)
            {
                float a = (i % 2 == 0 ? 1 : -1) * ((i + 1) / 2) * 22.5f;
                Vector3 want = target.position + Quaternion.Euler(0f, a, 0f) * preferred * distance;
                if (!NavMesh.SamplePosition(want, out var hit, 1.8f, NavMesh.AllAreas)) continue;
                if (new Vector2(hit.position.x - want.x, hit.position.z - want.z).magnitude > 0.35f || hit.position.y > target.position.y + 0.3f) continue;
                if (ClearView(hit.position + Vector3.up * 1.6f, target)) return hit.position;
            }
            return target.position + preferred * distance;
        }

        static bool ClearView(Vector3 eye, Transform target)
        {
            Vector3 to = target.position - eye;
            var hits = Physics.RaycastAll(eye, to.normalized, to.magnitude + 0.05f, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
            foreach (var h in hits)
            {
                if (h.collider.transform.IsChildOf(target)) return true;
                var other = h.collider.GetComponentInParent<IInteractable>();
                if (other != null && !other.CanInteract(null)) continue;
                return false;
            }
            return true;
        }

        void Aim(Vector3 p)
        {
            Vector3 d = p - interactor.transform.position;
            motor.SetLook(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg);
        }

        IEnumerator GoTo(Transform t, float distance = 0.9f)
        {
            Vector3 away = motor.transform.position - t.position; away.y = 0;
            if (away.sqrMagnitude < 0.01f) away = Vector3.right;
            motor.Teleport(StandSpot(t, away.normalized, distance) + Vector3.up * 0.2f, 0f);
            yield return null;
            Aim(t.position);
            yield return null; yield return null;
        }

        IEnumerator Use(string id, float hold = 0f)
        {
            var t = TaskInteractable.Find(id);
            Assert.IsNotNull(t, id);
            yield return GoTo(t.transform);
            Assert.IsTrue(ReferenceEquals(t, interactor.Current), $"looking at {id} (available={t.available}) from {motor.transform.position:F2}");
            if (hold > 0f) { interactor.VirtualHeld = true; yield return new WaitForSeconds(hold); interactor.VirtualHeld = false; }
            else interactor.VirtualPress();
            yield return null; yield return null;
        }

        IEnumerator WaitUntilFast(System.Func<bool> done, float gameSeconds, float scale = 4f)
        {
            Time.timeScale = scale;
            float until = Time.time + gameSeconds;
            while (!done() && Time.time < until) yield return null;
            Time.timeScale = 1f;
        }

        PhoneCallTask Call(int n) => Object.FindObjectsByType<PhoneCallTask>(FindObjectsSortMode.None).First(p => p.CallNumber == n);
        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
        static Transform Inactive(string name) => Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t.name == name);

        // ---------------- the night's order ----------------

        [UnityTest]
        public IEnumerator TheNight_ChargerAfterTheWakeUp_Call2AfterTheSnack_BedLast()
        {
            yield return SkipTo<ReachTask>();
            runner.DebugSkipCurrent(); yield return null;
            Assert.IsInstanceOf<ChargerTask>(runner.Current);
            yield return SkipTo<SnackTask>();
            runner.DebugSkipCurrent(); yield return null;
            Assert.IsInstanceOf<PhoneCallTask>(runner.Current);
            Assert.AreEqual(2, ((PhoneCallTask)runner.Current).CallNumber);
            yield return SkipTo<ApplianceChaosTask>();
            runner.DebugSkipCurrent(); yield return null;
            Assert.IsInstanceOf<FinalSleepTask>(runner.Current, "after the chaos: go to bed");
        }

        [UnityTest]
        public IEnumerator TheStairs_YouCanWalkUpThem_OnFoot()
        {
            // the charger and the final sleep are upstairs: a real walk, not just a line on the walking map
            motor.Teleport(new Vector3(-36.6f, 5.1f, 1.1f), 270f);
            yield return new WaitForSeconds(0.2f);
            motor.VirtualMove = Vector2.up;
            yield return new WaitForSeconds(3f);
            motor.VirtualMove = Vector2.zero;
            Assert.Greater(motor.transform.position.y, 7.5f, $"climbed to {motor.transform.position:F2}");
        }

        // ---------------- the charger and the gun (#61-62) ----------------

        [UnityTest]
        public IEnumerator Charger_TheTorchIsDying_TheLandingIsLit_PluggingInFixesIt()
        {
            yield return SkipTo<ChargerTask>();
            var torch = Object.FindAnyObjectByType<PhoneTorch>();
            Assert.IsTrue(torch.Dying);
            Assert.IsTrue(GameObject.Find("LGT_LandingPath").GetComponent<Light>().enabled, "the one light on upstairs");
            StringAssert.Contains("upstairs", runner.ObjectiveText);
            yield return new WaitForSeconds(3.2f); // after "...I'm not going back to sleep." has landed
            StringAssert.Contains("charger", subtitles.LastText);
            yield return Use("charger", 2.3f);
            Assert.IsTrue(Object.FindAnyObjectByType<ChargerTask>().Charged);
            Assert.IsFalse(torch.Dying);
            yield return null;
            Assert.IsInstanceOf<SnackTask>(runner.Current);
        }

        [UnityTest]
        public IEnumerator Gun_OnceForAHeartbeat_ThenTheDrawerSlidesShut()
        {
            yield return SkipTo<ChargerTask>();
            var cameo = Object.FindAnyObjectByType<GunCameo>();
            Assert.IsFalse(cameo.Gun.activeSelf, "not there before you look");
            yield return GoTo(cameo.Drawer, 1.3f);
            for (float t = 0f; t < 1f && !cameo.Shown; t += Time.deltaTime) { Aim(cameo.Drawer.position); yield return null; }
            if (!cameo.Shown)
            {
                var eye = interactor.transform; Vector3 to = cameo.Drawer.position - eye.position;
                Physics.Raycast(eye.position, to.normalized, out var h, to.magnitude, ~0, QueryTriggerInteraction.Ignore);
                Assert.Fail($"not seen: armed={cameo.Armed} eye {eye.position:F2} d={to.magnitude:F2} angle={Vector3.Angle(eye.forward, to):F1} ray={(h.collider ? h.collider.name : "clear")}");
            }
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(cameo.Shut);
            Assert.IsFalse(cameo.Gun.activeSelf);
            float shown = cameo.GunHiddenAt - cameo.SeenAt;
            Assert.That(shown, Is.InRange(0.15f, 0.3f), $"a heartbeat, not a frame and not a stare: {shown:F3} s");
            float first = cameo.SeenAt;
            Aim(cameo.Drawer.position + Vector3.right * 3f); yield return new WaitForSeconds(0.2f);
            Aim(cameo.Drawer.position); yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(first, cameo.SeenAt, "once");
        }

        [UnityTest]
        public IEnumerator Gun_NothingBeforeTheChargerTrip()
        {
            var cameo = Object.FindAnyObjectByType<GunCameo>();
            yield return GoTo(cameo.Drawer, 1.0f);
            for (float t = 0f; t < 0.5f; t += Time.deltaTime) { Aim(cameo.Drawer.position); yield return null; }
            Assert.IsFalse(cameo.Shown);
        }

        // ---------------- calls #2-4 (#63-66) ----------------

        [UnityTest]
        public IEnumerator Call2_OnTheCouchWhereYouSlept_AnsweredQuickly_Minus5()
        {
            yield return SkipTo<PhoneCallTask>(p => p.CallNumber == 2);
            var call = Call(2);
            yield return WaitUntilFast(() => call.Ringing, 5f, 2f);
            Assert.IsTrue(call.Ringing);
            Assert.Less(Vector3.Distance(call.Phone.transform.position, GameObject.Find("CTRL_Phone2_Couch").transform.position), 0.05f);
            frustration.SetValue(30f);
            yield return Use("phone");
            Assert.IsTrue(call.Answered);
            Assert.AreEqual(25f, frustration.Value, 1e-3f, "-5 for a quick answer");
            Assert.IsTrue(runState.CallAnswered(2));
        }

        [UnityTest]
        public IEnumerator Call2_Ignored_EveryRingCosts_ThenItRingsOut()
        {
            yield return SkipTo<PhoneCallTask>(p => p.CallNumber == 2);
            var call = Call(2);
            yield return WaitUntilFast(() => call.Ringing, 5f, 2f);
            frustration.SetValue(20f);
            yield return WaitUntilFast(() => call.Ignored, 24f, 6f);
            Assert.IsTrue(call.Session.RangOut);
            Assert.AreEqual(38f, frustration.Value, 0.01f, "nine rings after the first, +2 each");
            Assert.IsTrue(runState.CallHappened(2));
            Assert.IsFalse(runState.CallAnswered(2));
        }

        [UnityTest]
        public IEnumerator Call3_RingsOverTheFirstAppliance()
        {
            yield return SkipTo<ApplianceChaosTask>();
            var chaos = (ApplianceChaosTask)runner.Current;
            yield return WaitUntilFast(() => chaos.Session != null, 40f, 4f);
            Assert.IsNotNull(chaos.Session, "the first appliance switched on");
            Assert.AreEqual(0, chaos.Index);
            yield return null;
            Assert.AreNotEqual(TaskState.Waiting, Call(3).State, "call #3 comes mid-task");
        }

        [UnityTest]
        public IEnumerator Call3_TheDistortedOne_Call4_TheAnsweringMachine()
        {
            yield return SkipTo<ApplianceChaosTask>();
            runner.DebugSkipCurrent(); // the chaos is over: any call still to come rings anyway
            var c3 = Call(3);
            yield return WaitUntilFast(() => c3.Ringing, 6f, 2f);
            Assert.Less(Vector3.Distance(c3.Phone.transform.position, GameObject.Find("CTRL_Phone3_BackHallFloor").transform.position), 0.05f);
            frustration.SetValue(40f);
            yield return Use("phone");
            Assert.AreEqual(40f, frustration.Value, 1e-3f, "-5 quick, +5 for the distorted voice");
            StringAssert.Contains("your own voice", subtitles.LastText);
            var c4 = Call(4);
            yield return WaitUntilFast(() => c4.Ringing, 20f, 3f);
            Assert.Less(Vector3.Distance(c4.Phone.transform.position, GameObject.Find("CTRL_Phone4_OnTheFile").transform.position), 0.05f, "on the file");
            bool heard = false;
            Time.timeScale = 6f;
            for (float t = 0f; t < 30f && !heard; t += Time.deltaTime) { heard = subtitles.LastText != null && subtitles.LastText.Contains("coming over"); yield return null; }
            Time.timeScale = 1f;
            Assert.IsTrue(heard, "ignored, the answering machine plays it to the empty room");
            Assert.IsTrue(runState.CallAnswered(3));
            Assert.IsFalse(runState.CallAnswered(4));
        }

        // ---------------- cats #2-3 (#67-69) ----------------

        [UnityTest]
        public IEnumerator Cat2_OnTheCounterAfterTheTea_PettingItHushesTheHouse()
        {
            yield return SkipTo<TeaTask>();
            runner.DebugSkipCurrent();
            var cat2 = Object.FindObjectsByType<CatPerchTask>(FindObjectsSortMode.None).First(c => GetVisit(c) == 2);
            yield return WaitUntilFast(() => cat2.State == TaskState.Active, 4f, 2f);
            Assert.AreEqual("CTRL_Cat2_Counter", cat2.CurrentPerch.name);
            Assert.IsInstanceOf<ApplianceChaosTask>(runner.Current, "the night carries on while it waits");
            frustration.SetValue(30f);
            cat2.ForcedRoll = 0.9f;
            yield return Use("cat", 1.7f);
            Assert.AreEqual(10f, frustration.Value, 1e-3f, "-20");
            Assert.IsTrue(runState.CatPettedOn(2));
            var director = Object.FindAnyObjectByType<GhostDirector>();
            Assert.Greater(director.Rules.QuietUntil, Time.time + 15f, "the house goes quiet for a while");
        }

        [UnityTest]
        public IEnumerator Cat_RunAtIt_AndItJumpsToItsOtherPerch()
        {
            yield return SkipTo<TeaTask>();
            runner.DebugSkipCurrent();
            var cat2 = Object.FindObjectsByType<CatPerchTask>(FindObjectsSortMode.None).First(c => GetVisit(c) == 2);
            yield return WaitUntilFast(() => cat2.State == TaskState.Active, 4f, 2f);
            var perch = cat2.CurrentPerch;
            var catPos = TaskInteractable.Find("cat").transform.position;
            Vector3 from = StandSpot(perch, (Flat(GameObject.Find("ROOM_LivingRoom").transform.position) - Flat(perch.position)).normalized, 3.6f);
            motor.Teleport(from + Vector3.up * 0.2f, 0f);
            yield return null;
            Aim(catPos);
            motor.VirtualSprint = true;
            motor.VirtualMove = Vector2.up;
            for (float t = 0f; t < 1.5f && cat2.Spooked == 0; t += Time.deltaTime) yield return null;
            motor.VirtualMove = Vector2.zero; motor.VirtualSprint = false;
            Assert.AreEqual(1, cat2.Spooked);
            yield return new WaitForSeconds(1f);
            Assert.AreEqual("CTRL_Cat2_Table", cat2.CurrentPerch.name);
            Assert.IsFalse(runState.CatVisitHappened(2), "no penalty: it's still waiting");
        }

        [UnityTest]
        public IEnumerator Cat3_OnTheGuestBed_GoToBedAndItLeavesUnpetted()
        {
            yield return SkipTo<ApplianceChaosTask>();
            runner.DebugSkipCurrent();
            var cat3 = Object.FindObjectsByType<CatPerchTask>(FindObjectsSortMode.None).First(c => GetVisit(c) == 3);
            yield return WaitUntilFast(() => cat3.State == TaskState.Active, 4f, 2f);
            Assert.AreEqual("CTRL_Cat3_GuestBed", cat3.CurrentPerch.name);
            var bed = (FinalSleepTask)runner.Current;
            bed.DebugLieDownNow();
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(TaskState.Done, cat3.State);
            Assert.IsTrue(runState.CatVisitHappened(3));
            Assert.IsFalse(runState.CatPettedOn(3));
        }

        static int GetVisit(CatPerchTask c) => (int)typeof(CatPerchTask).GetField("visit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(c);

        // ---------------- the final sleep and the endings (#70-72, #76) ----------------

        IEnumerator ToBed(bool catPetted, int callsAnswered)
        {
            yield return SkipTo<FinalSleepTask>();
            if (catPetted) runState.RecordCatVisit(1, true);
            for (int i = 1; i <= callsAnswered; i++) runState.RecordCall(i, true);
        }

        [UnityTest]
        public IEnumerator FinalSleep_IsInHisOwnBedUpstairs()
        {
            yield return SkipTo<FinalSleepTask>();
            var bed = TaskInteractable.Find("bed");
            Assert.IsTrue(bed.available);
            Assert.Greater(bed.transform.position.y, 8f, "upstairs");
            StringAssert.Contains("Go to bed", runner.ObjectiveText);
            yield return Use("bed", 1.7f);
            Assert.IsTrue(((FinalSleepTask)runner.Current).Lying);
        }

        [UnityTest]
        public IEnumerator FinalSleep_Calm_ACleanSleep_DoingNothingIsStayingAsleep_TheLoop()
        {
            yield return ToBed(true, 3);
            var fs = (FinalSleepTask)runner.Current;
            frustration.SetValue(30f);
            fs.DebugLieDownNow();
            yield return WaitUntilFast(() => fs.Session.Stage == FinalSleepStage.Choice, 4f, 2f);
            Assert.AreEqual(10f, frustration.Value, 1e-3f, "-20 for a clean sleep");
            yield return WaitUntilFast(() => fs.Resolved, 12f, 4f);
            Assert.AreEqual(FinalChoice.StayAsleep, fs.Session.Chosen);
            Assert.AreEqual(Ending.Loop, fs.Result);
            var endings = Object.FindAnyObjectByType<EndingPlayer>();
            yield return WaitUntilFast(() => endings.CardShown, 30f, 4f); // Gate 5b: the loop's scene plays first
            Assert.AreEqual("THE LOOP", endings.CardTitle);
        }

        [UnityTest]
        public IEnumerator FinalSleep_Above50_BreatheOnTheBeat_AMissScreams_ThenWakeUp_Burial()
        {
            yield return ToBed(true, 3);
            var fs = (FinalSleepTask)runner.Current;
            frustration.SetValue(70f);
            fs.DebugLieDownNow();
            yield return WaitUntilFast(() => fs.Session != null && fs.Session.Stage == FinalSleepStage.Breathing, 4f, 2f);
            Assert.AreEqual(FinalSleepStage.Breathing, fs.Session.Stage, "above 50% the ghost disrupts you");
            yield return new WaitUntil(() => fs.Session.Fails == 1);
            Assert.AreEqual(95f, frustration.Value, 1e-3f, "a missed beat: +15 scream, +10 for the one retry");
            for (int beat = 0; beat < 3; beat++)
            {
                int hit = fs.Session.BeatsHit;
                yield return new WaitUntil(() => fs.InZone || fs.Session.Stage != FinalSleepStage.Breathing);
                interactor.VirtualPress();
                yield return new WaitUntil(() => fs.Session.BeatsHit > hit || fs.Session.Stage != FinalSleepStage.Breathing);
            }
            Assert.AreEqual(FinalSleepStage.Choice, fs.Session.Stage);
            Assert.AreEqual(75f, frustration.Value, 1e-3f, "-20 for the clean sleep");
            yield return new WaitUntil(() => fs.AwaitingWake);
            yield return new WaitForSeconds(0.5f);
            interactor.VirtualPress();
            yield return new WaitUntil(() => fs.Resolved);
            Assert.AreEqual(FinalChoice.WakeUp, fs.Session.Chosen);
            Assert.AreEqual(Ending.BurialAnswered, fs.Result);
            var endings = Object.FindAnyObjectByType<EndingPlayer>();
            Assert.AreEqual(Ending.BurialAnswered, endings.Current);
            yield return new WaitForSeconds(1f);
            // Gate 5b: the Burial now plays in the house first (its card is checked at the end of it, in Gate5bTests)
            Assert.AreNotEqual(BurialSequence.Stage.Idle, Object.FindAnyObjectByType<BurialSequence>().Current);
        }

        [UnityTest]
        public IEnumerator FinalSleep_TheCatWasNeverPetted_YouDieInYourSleep()
        {
            yield return ToBed(false, 4);
            var fs = (FinalSleepTask)runner.Current;
            frustration.SetValue(20f);
            fs.DebugLieDownNow();
            yield return WaitUntilFast(() => fs.AwaitingWake, 5f, 2f);
            interactor.VirtualPress();
            yield return new WaitUntil(() => fs.Resolved);
            Assert.AreEqual(Ending.DieInSleep, fs.Result, "the cat rule beats everything, even waking up");
            var endings = Object.FindAnyObjectByType<EndingPlayer>();
            yield return WaitUntilFast(() => endings.CardShown, 40f, 4f); // Gate 5b: the morning plays first
            Assert.AreEqual("YOU DIE IN YOUR SLEEP", endings.CardTitle);
            StringAssert.Contains("Nobody picks up", endings.CardLine, "it reads as a loss, not an escape");
        }

        // ---------------- the secret ending (#75) ----------------

        [UnityTest]
        public IEnumerator Secret_StraightToBed_BeforeAnyChore()
        {
            var secret = Object.FindAnyObjectByType<SecretSleep>();
            yield return null;
            Assert.IsInstanceOf<DropThingsTask>(runner.Current);
            Assert.IsTrue(secret.Open);
            secret.ForcedRoll = 0.8f;
            yield return Use("bed", 1.7f);
            Assert.IsTrue(secret.Taken);
            StringAssert.Contains("Maybe ignoring it all", subtitles.LastText);
            var endings = Object.FindAnyObjectByType<EndingPlayer>();
            yield return WaitUntilFast(() => endings.CardShown, 8f, 2f);
            Assert.AreEqual(Ending.SecretGhostWins, endings.Current);
            Assert.AreEqual("THE GHOST WINS", endings.CardTitle);
        }

        [UnityTest]
        public IEnumerator Secret_GoneOnceTheFirstChoreIsDone()
        {
            yield return SkipTo<MakeToastTask>();
            yield return null;
            Assert.IsFalse(Object.FindAnyObjectByType<SecretSleep>().Open);
            Assert.IsFalse(TaskInteractable.Find("bed").available);
        }

        // ---------------- the Showdown (#73-74) ----------------

        IEnumerator RunShowdown(int callsAnswered)
        {
            yield return ToBed(true, callsAnswered);
            var fs = (FinalSleepTask)runner.Current;
            frustration.SetValue(40f);
            fs.DebugLieDownNow();
            yield return WaitUntilFast(() => fs.AwaitingWake, 5f, 2f);
            frustration.SetValue(100f); // he wakes fed up
            interactor.VirtualPress();
            yield return new WaitUntil(() => fs.Resolved);
            var sd = Object.FindAnyObjectByType<ShowdownSequence>();
            yield return new WaitUntil(() => sd.Target != null || !sd.Running);
            Assert.IsTrue(Object.FindAnyObjectByType<FrontDoor>().IsOpen, "the door that locked behind him is open: the porch is reachable on foot");
            Assert.IsFalse(Object.FindAnyObjectByType<FrontDoor>().IsLocked);
            // follow the image: step near it, aim, fire
            for (int guard = 0; guard < 12 && sd.Running && !sd.AtGate; guard++)
            {
                var target = sd.Target;
                if (target == null) { yield return null; continue; }
                int left = sd.StopsLeft;
                motor.Teleport(StandSpot(target, (Flat(motor.transform.position) - Flat(target.position)).normalized, 3f) + Vector3.up * 0.2f, 0f);
                yield return null;
                Aim(target.position);
                yield return null;
                interactor.VirtualPress();
                float t0 = Time.time;
                yield return new WaitUntil(() => sd.StopsLeft != left || sd.AtGate || Time.time - t0 > 3f);
                Assert.Less(Time.time - t0, 3f, $"the shot at stop {sd.Stops.Count - left + 1} hit");
                yield return new WaitForSeconds(0.2f);
            }
            // the porch, and the gate
            motor.Teleport(GameObject.Find("CTRL_PorchShoot").transform.position + Vector3.up * 0.2f, 270f);
            float t1 = Time.time;
            yield return new WaitUntil(() => (sd.AtGate && sd.Target != null) || Time.time - t1 > 5f);
            Assert.IsTrue(sd.AtGate, "on the porch, the last one is at the gate");
            for (float t = 0f; t < 1f; t += Time.deltaTime) { Aim(sd.Target.position); yield return null; }
            interactor.VirtualPress();
            float t2 = Time.time;
            yield return new WaitUntil(() => sd.Revealed || Time.time - t2 > 3f);
            Assert.IsTrue(sd.Revealed, "the last shot");
        }

        [UnityTest]
        public IEnumerator Showdown_PhoneAnswered_TheGhostBecomesTheBoss_AtTheGate()
        {
            yield return RunShowdown(3);
            var sd = Object.FindAnyObjectByType<ShowdownSequence>();
            Assert.IsTrue(sd.Transformed, "answered: it turns into the boss before the shot");
            Assert.IsTrue(GameObject.Find("GB_BossAtGate").activeSelf);
            Assert.IsTrue(GameObject.Find("LGT_StreetLamp").GetComponent<Light>().enabled, "the lamp comes up on him");
            Assert.Greater(sd.Wrecked, 0, "the house is wrecked by his shots");
            Assert.Greater(sd.Stops.Count, 1, "the trail has stops through the house");
            var endings = Object.FindAnyObjectByType<EndingPlayer>();
            yield return WaitUntilFast(() => endings.CardShown, 12f, 3f);
            Assert.IsTrue(GameObject.Find("GB_GhostInDoorway").activeSelf, "the ghost is still there");
            Assert.AreEqual(Ending.ShowdownAnswered, endings.Current);
            Assert.AreEqual("HORROR SHOWDOWN", endings.CardTitle);
        }

        [UnityTest]
        public IEnumerator Showdown_PhoneIgnored_NoTransformation_StillTheBoss()
        {
            yield return RunShowdown(2);
            var sd = Object.FindAnyObjectByType<ShowdownSequence>();
            Assert.IsFalse(sd.Transformed);
            Assert.IsTrue(GameObject.Find("GB_BossAtGate").activeSelf, "either way, the body is the boss");
            Assert.AreEqual(Ending.ShowdownIgnored, Object.FindAnyObjectByType<EndingPlayer>().Current);
        }

        [UnityTest]
        public IEnumerator Showdown_TheBurstDoor_StaysOpen_WhenYouWalkBackThroughIt()
        {
            // the entry zone shuts and locks the door behind you at the start of the night; in the Showdown it must not
            var door = Object.FindAnyObjectByType<FrontDoor>();
            yield return null;
            door.BurstOpen();
            motor.Teleport(GameObject.Find("CTRL_PorchShoot").transform.position + Vector3.up * 0.2f, 90f);
            yield return new WaitForSeconds(0.3f);
            motor.Teleport(GameObject.Find("ROOM_Entrance").transform.position + Vector3.up * 0.2f, 90f);
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(door.IsOpen);
            Assert.IsFalse(door.IsLocked);
            // and you can walk out of it on foot
            motor.Teleport(new Vector3(-46.6f, 5.1f, -3.31f), 270f);
            yield return new WaitForSeconds(0.2f);
            motor.VirtualMove = Vector2.up;
            yield return new WaitForSeconds(1.6f);
            motor.VirtualMove = Vector2.zero;
            Assert.Less(motor.transform.position.x, -48.3f, $"out onto the porch: {motor.transform.position:F2}");
        }

        // ---------------- design reasons (DESIGN_LOGIC.md #61-76) ----------------

        [Test]
        public void DesignLogic_TheChargerIsOnTheBedsideCabinet_TheDrawerRightUnderIt()
        {
            var charger = GameObject.Find("PROP_Charger (greybox)").transform.position;
            var cab = GameObject.Find("GB_SmallCloset").GetComponent<Renderer>().bounds;
            var bed = GameObject.Find("GB_SingleBed").GetComponent<Renderer>().bounds;
            Assert.Less(Vector2.Distance(new Vector2(charger.x, charger.z), new Vector2(cab.center.x, cab.center.z)), 0.3f, "on the cabinet");
            Assert.GreaterOrEqual(charger.y, cab.max.y - 0.01f);
            Assert.Less(Vector3.Distance(cab.ClosestPoint(bed.center), bed.ClosestPoint(cab.center)), 1.2f, "at the head of the bed");
            Assert.Greater(charger.y, 8f, "upstairs");
            var drawer = GameObject.Find("PRT_Drawer").transform.position;
            Assert.That(charger.y - drawer.y, Is.InRange(0.1f, 0.5f), "right under it");
            Assert.Less(Vector2.Distance(new Vector2(charger.x, charger.z), new Vector2(drawer.x, drawer.z)), 0.6f);
        }

        [Test]
        public void DesignLogic_ThePhone_NeverInTheSamePlaceTwice()
        {
            var spots = new[]
            {
                Object.FindObjectsByType<PhoneCallTask>(FindObjectsSortMode.None).First(p => p.CallNumber == 1).Phone.transform.position,
                GameObject.Find("CTRL_Phone2_Couch").transform.position,
                GameObject.Find("CTRL_Phone3_BackHallFloor").transform.position,
                GameObject.Find("CTRL_Phone4_OnTheFile").transform.position,
            };
            for (int i = 0; i < spots.Length; i++)
                for (int j = i + 1; j < spots.Length; j++)
                    Assert.Greater(Vector3.Distance(spots[i], spots[j]), 3f, $"call #{i + 1} and #{j + 1}");
            Assert.Less(Vector2.Distance(new Vector2(spots[1].x, spots[1].z), new Vector2(-41.01f, -6.31f)), 0.8f, "#2 on the couch where you slept");
            var fuse = GameObject.Find("PREF_FuseBox (greybox)").transform.position;
            Assert.Less(Vector2.Distance(new Vector2(spots[2].x, spots[2].z), new Vector2(fuse.x, fuse.z)), 0.6f, "#3 under the fuse box");
            Assert.Less(spots[2].y, 5.1f, "#3 on the floor, as if dropped");
            var file = Inactive("GB_BossFile"); // still in the bag at the start of the night
            Assert.Less(Vector2.Distance(new Vector2(spots[3].x, spots[3].z), new Vector2(file.position.x, file.position.z)), 0.1f, "#4 on the file");
            Assert.That(spots[3].y - file.position.y, Is.InRange(0f, 0.15f));
        }

        [Test]
        public void DesignLogic_TheCats_OnTheCounter_AndOnTheGuestBed()
        {
            var counter = GameObject.Find("GB_Kitchen_Set03").GetComponent<Renderer>().bounds;
            var p2 = GameObject.Find("CTRL_Cat2_Counter").transform.position;
            Assert.IsTrue(counter.min.x <= p2.x && p2.x <= counter.max.x && counter.min.z <= p2.z && p2.z <= counter.max.z, "on the counter");
            Assert.AreEqual(counter.max.y, p2.y, 0.05f);
            var bed = GameObject.Find("GB_GuestBed").GetComponent<Renderer>().bounds;
            var p3 = GameObject.Find("CTRL_Cat3_GuestBed").transform.position;
            Assert.IsTrue(bed.min.x <= p3.x && p3.x <= bed.max.x && bed.min.z <= p3.z && p3.z <= bed.max.z, "on the guest bed");
        }

        [Test]
        public void DesignLogic_TheGate_InLineWithTheFrontDoor_InViewFromThePorch_UnderTheLamp()
        {
            var gate = GameObject.Find("CTRL_Gate").transform.position;
            var door = Object.FindAnyObjectByType<FrontDoor>().transform.position;
            var porch = GameObject.Find("CTRL_PorchShoot").transform.position;
            Assert.AreEqual(door.z, gate.z, 0.5f, "straight out of the front door");
            Assert.Less(gate.x, porch.x - 5f, "out past the porch");
            Vector3 eye = porch + Vector3.up * 1.6f, to = gate + Vector3.up * 1f - eye;
            // only things you can see block the view (the porch's invisible bounds don't)
            var blockers = Physics.RaycastAll(eye, to.normalized, to.magnitude - 0.3f)
                .Where(h => h.collider.GetComponent<Renderer>() != null && h.collider.GetComponent<Renderer>().enabled).Select(h => h.collider.name).ToArray();
            Assert.IsEmpty(blockers, "you can see it from the porch");
            var lamp = GameObject.Find("LGT_StreetLamp").GetComponent<Light>();
            Assert.IsTrue(lamp.enabled, "lit all night, so you see the gate at the start");
            Assert.Less(Vector3.Distance(lamp.transform.position, gate), lamp.range * 0.6f);
        }

        [Test]
        public void DesignLogic_TheTrail_RunsFromTheBedToThePorch_TheGhostWaitsInTheDoorwayBehindYou()
        {
            var path = new NavMeshPath();
            Assert.IsTrue(NavMesh.CalculatePath(GameObject.Find("CTRL_ShowdownStart").transform.position, GameObject.Find("CTRL_PorchShoot").transform.position, NavMesh.AllAreas, path));
            Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status);
            var ghost = Inactive("GB_GhostInDoorway").position;
            var door = Object.FindAnyObjectByType<FrontDoor>().transform.position;
            Assert.Less(Vector2.Distance(new Vector2(ghost.x, ghost.z), new Vector2(door.x, door.z)), 1.2f, "in the doorway");
            var porch = GameObject.Find("CTRL_PorchShoot").transform.position + Vector3.up * 1.6f;
            Vector3 to = ghost - porch;
            var blockers = Physics.RaycastAll(porch, to.normalized, to.magnitude - 0.4f)
                .Where(h => h.collider.GetComponent<Renderer>() != null && h.collider.GetComponent<Renderer>().enabled && h.collider.GetComponentInParent<FrontDoor>() == null)
                .Select(h => h.collider.name).ToArray();
            Assert.IsEmpty(blockers, "you can see it from the porch when you turn round");
        }
    }
}
