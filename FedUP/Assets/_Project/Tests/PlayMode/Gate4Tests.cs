using System.Collections;
using FedUp.Frustration;
using FedUp.Ghost;
using FedUp.Interaction;
using FedUp.Player;
using FedUp.Story;
using FedUp.Tasks;
using FedUp.Tasks.Appliances;
using FedUp.Tasks.Calm;
using FedUp.Tasks.Snack;
using FedUp.Tasks.Tea;
using FedUp.UI;
using FedUp.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FedUp.Tests
{
    /// <summary>Gate 4 in the real greybox scene: the snack, the juice box, the radio, Make Tea and appliance chaos.</summary>
    public class Gate4Tests
    {
        FirstPersonMotor motor;
        Interactor interactor;
        FrustrationSystem frustration;
        TaskRunner runner;
        SubtitleUI subtitles;
        NightPhaseController phase;

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
            // Gate 5a rings calls #3-4 over the chaos; these tests measure Gate 4's systems on their own
            Object.FindAnyObjectByType<FedUp.Tasks.Phone.MidTaskCalls>().enabled = false;
            // Gate 5b blacks you out when you take the tea upstairs; these tests measure the tea task on its own
            Object.FindAnyObjectByType<FedUp.Tasks.Tea.StoreroomBlackout>().enabled = false;
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
            for (int i = 0; i < 16 && !(runner.Current is T); i++) { runner.DebugSkipCurrent(); yield return null; }
            Assert.IsInstanceOf<T>(runner.Current);
            yield return null;
        }

        /// <summary>0.9 m from the target on open floor with a clear view of it.</summary>
        static Vector3 StandSpot(Transform target, Vector3 preferred)
        {
            for (int i = 0; i < 16; i++)
            {
                float a = (i % 2 == 0 ? 1 : -1) * ((i + 1) / 2) * 22.5f;
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * preferred;
                Vector3 feet = target.position + dir * 0.9f;
                // either storey: the tea moved upstairs in Gate 5b
                if (!NavMesh.SamplePosition(new Vector3(feet.x, Floor(target), feet.z), out var hit, 0.15f, NavMesh.AllAreas)) continue;
                Vector3 eye = new Vector3(feet.x, hit.position.y + 1.6f, feet.z);
                if (!ClearView(eye, target)) continue;
                return feet;
            }
            return target.position + preferred * 0.9f;
        }

        /// <summary>Like the interactor sees it: things you can't use right now don't block the view.</summary>
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

        void Aim(Transform t)
        {
            Vector3 d = t.position - interactor.transform.position;
            motor.SetLook(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg);
        }

        static float Floor(Transform t) => t.position.y > 7.5f ? 8.28f : 4.83f;

        IEnumerator GoTo(TaskInteractable t)
        {
            Vector3 away = motor.transform.position - t.transform.position; away.y = 0;
            if (away.sqrMagnitude < 0.01f) away = Vector3.right;
            Vector3 feet = StandSpot(t.transform, away.normalized);
            feet.y = Floor(t.transform) + 0.3f;
            motor.Teleport(feet, 0f);
            yield return null;
            Aim(t.transform);
            yield return null; yield return null;
        }

        IEnumerator Use(string id, float hold = 0f)
        {
            var t = TaskInteractable.Find(id);
            Assert.IsNotNull(t, id);
            yield return GoTo(t);
            if (!ReferenceEquals(t, interactor.Current))
            {
                var eye = interactor.transform;
                var hits = Physics.RaycastAll(eye.position, eye.forward, 3f, ~0, QueryTriggerInteraction.Collide);
                System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
                var seen = string.Join(", ", System.Array.ConvertAll(hits, h => $"{h.collider.name}@{h.distance:F2}"));
                Assert.Fail($"looking at {id}: available={t.available}, eye {eye.position:F2} -> {t.transform.position:F2}; ray hits: {seen}");
            }
            if (hold > 0f) { interactor.VirtualHeld = true; yield return new WaitForSeconds(hold); interactor.VirtualHeld = false; }
            else interactor.VirtualPress();
            yield return null; yield return null;
        }

        IEnumerator Fast(float gameSeconds, float scale = 6f)
        {
            Time.timeScale = scale;
            float until = Time.time + gameSeconds;
            while (Time.time < until) yield return null;
            Time.timeScale = 1f;
        }

        IEnumerator WaitUntilFast(System.Func<bool> done, float gameSeconds, float scale = 4f)
        {
            Time.timeScale = scale;
            float until = Time.time + gameSeconds;
            while (!done() && Time.time < until) yield return null;
            Time.timeScale = 1f;
        }

        void StandIn(string marker, float yaw = 0f)
        {
            var p = GameObject.Find(marker).transform.position;
            motor.Teleport(new Vector3(p.x, 5.13f, p.z), yaw);
        }

        // ---------------- the snack ----------------

        [UnityTest]
        public IEnumerator Snack_Calm_OpenAndGrab_Minus5_ThenTea()
        {
            yield return SkipTo<SnackTask>();
            var snack = Object.FindAnyObjectByType<SnackTask>();
            snack.ForcedJuice = false;
            frustration.SetValue(20f);
            yield return Use("fridge");
            Assert.IsTrue(snack.Session.IsOpen);
            StringAssert.Contains("Back so soon", subtitles.LastText);
            yield return Use("snack");
            Assert.IsTrue(snack.Got);
            Assert.AreEqual(15f, frustration.Value, 1e-3f, "-5 for the snack");
            yield return new WaitForSeconds(1.6f);
            Assert.IsInstanceOf<FedUp.Tasks.Phone.PhoneCallTask>(runner.Current, "then: call #2 (Gate 5a), before the tea");
        }

        [UnityTest]
        public IEnumerator Snack_Above30_ItSlidesAway_AndAGrabMidSlideFails()
        {
            yield return SkipTo<SnackTask>();
            var snack = Object.FindAnyObjectByType<SnackTask>();
            snack.ForcedJuice = false;
            frustration.SetValue(40f);
            yield return Use("fridge");
            var bar = TaskInteractable.Find("snack").transform;
            yield return GoTo(TaskInteractable.Find("snack"));
            for (float t = 0f; t < 2f && snack.Session.Slides == 0; t += Time.deltaTime) { Aim(bar); yield return null; }
            Assert.AreEqual(1, snack.Session.Slides, "it slid away from your look");
            Assert.IsTrue(snack.Session.Sliding);
            Aim(bar);
            yield return null;
            if (ReferenceEquals(interactor.Current, TaskInteractable.Find("snack"))) interactor.VirtualPress();
            else TaskInteractable.Find("snack").Interact(interactor); // grabbing at it as it moves
            yield return null; yield return null;
            Assert.AreEqual(1, snack.FailedGrabs);
            Assert.AreEqual(45f, frustration.Value, 1e-3f, "+5 failed grab");
        }

        [UnityTest]
        public IEnumerator Snack_HoverThreeSeconds_TheFridgeMocksYou_Plus3()
        {
            yield return SkipTo<SnackTask>();
            var snack = Object.FindAnyObjectByType<SnackTask>();
            snack.ForcedJuice = false;
            frustration.SetValue(10f);
            yield return Use("fridge");
            var bar = TaskInteractable.Find("snack");
            yield return GoTo(bar);
            for (float t = 0f; t < 3.4f; t += Time.deltaTime) { Aim(bar.transform); yield return null; }
            Assert.AreEqual(13f, frustration.Value, 1e-3f, "+3 sarcasm");
            StringAssert.Contains("Again?", subtitles.LastText);
        }

        [UnityTest]
        public IEnumerator Snack_After10s_TheGhostHandWins_Plus10()
        {
            yield return SkipTo<SnackTask>();
            var snack = Object.FindAnyObjectByType<SnackTask>();
            snack.ForcedJuice = false;
            frustration.SetValue(10f);
            yield return Use("fridge");
            motor.SetLook(motor.Yaw + 120f, 0f); // look away
            yield return Fast(10.3f, 4f);
            yield return Use("snack");
            Assert.IsTrue(snack.Tugging, "a hand at the back of the fridge");
            yield return new WaitForSeconds(3f); // no mashing
            Assert.IsTrue(snack.Lost);
            Assert.AreEqual(20f, frustration.Value, 1e-3f, "+10 QTE lost");
        }

        [UnityTest]
        public IEnumerator JuiceBox_Minus5()
        {
            yield return SkipTo<SnackTask>();
            var snack = Object.FindAnyObjectByType<SnackTask>();
            snack.ForcedJuice = true;
            frustration.SetValue(20f);
            yield return Use("fridge");
            yield return Use("juice_box");
            Assert.IsTrue(snack.JuiceDrunk);
            Assert.AreEqual(15f, frustration.Value, 1e-3f);
        }

        [UnityTest]
        public IEnumerator Radio_15SecondsNearby_Minus5()
        {
            yield return SkipTo<SnackTask>();
            frustration.SetValue(20f);
            yield return Use("radio");
            var radio = Object.FindAnyObjectByType<RadioCalm>();
            Assert.IsTrue(radio.Playing);
            yield return Fast(15.5f, 5f);
            Assert.IsTrue(radio.Session.Calmed);
            Assert.AreEqual(15f, frustration.Value, 1e-3f);
        }

        // ---------------- tea ----------------

        IEnumerator GatherAll(TeaTask tea)
        {
            foreach (var id in new[] { "tea_milk", "tea_sugar", "tea_leaves" }) yield return Use(id);
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(TeaTask.Step.Hunt, tea.Current, "they vanished");
        }

        IEnumerator IntoKitchen()
        {
            StandIn("ROOM_Kitchen", 90f);
            yield return null; yield return null;
        }

        /// <summary>Find all three, bring two, then the third (the power cuts), fix it, place it: kettle ready.</summary>
        IEnumerator HuntToPowerCut(TeaTask tea)
        {
            yield return GatherAll(tea);
            yield return Use("tea_milk");
            yield return Use("tea_sugar");
            yield return IntoKitchen();
            yield return Use("tea_station");
            Assert.AreEqual(2, tea.Session.Placed);
            yield return Use("tea_leaves");
            StandIn("ROOM_LivingRoom");
            yield return null;
            yield return IntoKitchen();
            Assert.AreEqual(TeaStage.PowerCut, tea.Session.Stage);
            Assert.IsFalse(phase.PowerOn, "the power cut out");
        }

        IEnumerator HuntToKettle(TeaTask tea)
        {
            yield return HuntToPowerCut(tea);
            yield return Use("fuse_box", hold: 1.3f);
            Assert.IsTrue(phase.PowerOn);
            yield return Use("tea_station");
            Assert.AreEqual(TeaStage.Brew, tea.Session.Stage);
        }

        [UnityTest]
        public IEnumerator Tea_StartsPhase3_ThingsVanishIntoTheHouse_YouCarryTwo()
        {
            yield return SkipTo<TeaTask>();
            var tea = Object.FindAnyObjectByType<TeaTask>();
            Assert.AreEqual(3, phase.Phase, "Phase 3: the haunted night");
            yield return GatherAll(tea);
            Assert.Less(Vector3.Distance(TaskInteractable.Find("tea_milk").transform.position, GameObject.Find("CTRL_HideMilk").transform.position), 0.05f);
            yield return Use("tea_milk");
            yield return Use("tea_sugar");
            Assert.AreEqual(2, tea.Session.Carrying);
            var leaves = TaskInteractable.Find("tea_leaves");
            yield return GoTo(leaves);
            interactor.VirtualPress();
            yield return null; yield return null;
            Assert.AreEqual(TeaItemState.Hidden, tea.Session.StateOf(2), "hands full");
            StringAssert.Contains("Two hands", subtitles.LastText);
        }

        [UnityTest]
        public IEnumerator Tea_TheLastOneCutsThePower_TheFuseBoxFixesIt()
        {
            yield return SkipTo<TeaTask>();
            var tea = Object.FindAnyObjectByType<TeaTask>();
            yield return HuntToPowerCut(tea);
            var station = TaskInteractable.Find("tea_station");
            yield return GoTo(station);
            station.Interact(interactor);
            yield return null;
            Assert.AreEqual(2, tea.Session.Placed, "can't place it in the dark");
            yield return Use("fuse_box", hold: 1.3f);
            Assert.IsTrue(phase.PowerOn);
            yield return Use("tea_station");
            Assert.AreEqual(TeaStage.Brew, tea.Session.Stage);
        }

        [UnityTest]
        public IEnumerator Tea_TooSlowOnThePower_EverythingRescatters_Plus10_Once()
        {
            yield return SkipTo<TeaTask>();
            var tea = Object.FindAnyObjectByType<TeaTask>();
            frustration.SetValue(20f);
            yield return HuntToPowerCut(tea);
            yield return Fast(31f, 8f);
            Assert.AreEqual(1, tea.Session.Rescatters);
            Assert.AreEqual(30f, frustration.Value, 1e-3f, "+10");
            Assert.IsTrue(phase.PowerOn, "the lights come back");
            Assert.AreEqual(0, tea.Session.Placed);
            Assert.Less(Vector3.Distance(TaskInteractable.Find("tea_sugar").transform.position, GameObject.Find("CTRL_HideSugar").transform.position), 0.05f, "back in its hiding place");
        }

        [UnityTest]
        public IEnumerator Tea_StandingStill_YouGetHit_ThreeHitsAndItsFire()
        {
            yield return SkipTo<TeaTask>();
            var tea = Object.FindAnyObjectByType<TeaTask>();
            yield return HuntToKettle(tea);
            yield return Use("kettle");
            yield return WaitUntilFast(() => tea.Current == TeaTask.Step.Dodge, 15f);
            frustration.SetValue(20f);
            yield return IntoKitchen();
            yield return WaitUntilFast(() => tea.Current == TeaTask.Step.Fire, 20f, 2f);
            Assert.AreEqual(TeaTask.Step.Fire, tea.Current);
            Assert.AreEqual(3, tea.Dodge.Hits);
            Assert.AreEqual(20f + 7f * 3 + 15f, frustration.Value, 1e-3f, "+7 a hit, +15 fire");
            Object.FindAnyObjectByType<ApplianceChaosTask>().ForcedRoll = 0.99f; // the next task starts at once; keep its dice out of this count
            yield return Use("kettle", hold: 2.3f);
            Assert.AreEqual(TaskState.Done, tea.State);
            Assert.AreEqual(56f, frustration.Value, 1e-3f, "no tea, no -10");
        }

        [UnityTest]
        public IEnumerator Tea_LeavingTheKitchen_PausesTheDodge()
        {
            yield return SkipTo<TeaTask>();
            var tea = Object.FindAnyObjectByType<TeaTask>();
            yield return HuntToKettle(tea);
            yield return Use("kettle");
            yield return WaitUntilFast(() => tea.Current == TeaTask.Step.Dodge, 15f);
            StandIn("ROOM_LivingRoom");
            yield return new WaitForSeconds(0.2f);
            float elapsed = tea.Dodge.Elapsed;
            int throws = tea.Throws;
            yield return new WaitForSeconds(3f);
            Assert.AreEqual(elapsed, tea.Dodge.Elapsed, 1e-3f);
            Assert.AreEqual(throws, tea.Throws, "nothing thrown at an empty kitchen");
        }

        [UnityTest]
        public IEnumerator Tea_DodgeEveryThrow_ThenTheStoveOff_Minus10()
        {
            yield return SkipTo<TeaTask>();
            var tea = Object.FindAnyObjectByType<TeaTask>();
            yield return HuntToKettle(tea);
            yield return Use("kettle");
            yield return WaitUntilFast(() => tea.Current == TeaTask.Step.Dodge, 15f);
            frustration.SetValue(20f);
            var zone = GameObject.Find("ROOM_Kitchen").transform.position;
            Vector3 a = new Vector3(zone.x, 5.13f, zone.z - 1f), b = new Vector3(zone.x, 5.13f, zone.z + 1f);
            int seen = tea.Launched;
            bool atA = true;
            motor.Teleport(a, 90f);
            Time.timeScale = 2f;
            float until = Time.time + 25f;
            while (tea.Current == TeaTask.Step.Dodge && Time.time < until)
            {
                if (tea.Launched != seen) { seen = tea.Launched; atA = !atA; motor.Teleport(atA ? a : b, 90f); } // step aside once it's flying
                yield return null;
            }
            Time.timeScale = 1f;
            Assert.AreEqual(TeaTask.Step.StoveOff, tea.Current, "survived the dodge");
            Assert.AreEqual(0, tea.Dodge.Hits);
            yield return Use("kettle", hold: 1.3f);
            Assert.AreEqual(TaskState.Done, tea.State);
            Assert.AreEqual(10f, frustration.Value, 1e-3f, "-10 tea made");
            Assert.IsInstanceOf<ApplianceChaosTask>(runner.Current);
        }

        // ---------------- appliances ----------------

        [UnityTest]
        public IEnumerator Appliances_ShutOneOff_IgnoreOne_Plus10()
        {
            yield return SkipTo<ApplianceChaosTask>();
            var chaos = Object.FindAnyObjectByType<ApplianceChaosTask>();
            frustration.SetValue(20f);
            yield return WaitUntilFast(() => chaos.Session != null, 30f);
            Assert.AreEqual(0, chaos.Index, "the microwave first");
            yield return Use("microwave", hold: 1.3f);
            Assert.AreEqual(1, chaos.ShutOffCount);
            yield return WaitUntilFast(() => chaos.Session != null, 30f);
            Assert.AreEqual(1, chaos.Index, "then the blender");
            yield return Fast(10.5f, 5f);
            Assert.AreEqual(1, chaos.Overloads);
            Assert.AreEqual(30f, frustration.Value, 1e-3f, "+10 not shut off");
        }

        [UnityTest]
        public IEnumerator Appliances_Above50_TheGhostCoversIt_TheTorchScaresItOff()
        {
            yield return SkipTo<ApplianceChaosTask>();
            var chaos = Object.FindAnyObjectByType<ApplianceChaosTask>();
            frustration.SetValue(60f);
            chaos.ForcedRoll = 0f;
            yield return WaitUntilFast(() => chaos.Session != null, 30f);
            Assert.IsTrue(chaos.Session.Blocked);
            Assert.AreEqual(67f, frustration.Value, 1e-3f, "+7 blocked");
            yield return Use("microwave");
            StringAssert.Contains("Torch", subtitles.LastText, "no light, no scare");
            Object.FindAnyObjectByType<PhoneTorch>().VirtualToggle();
            yield return null;
            for (int i = 0; i < 5; i++) { interactor.VirtualPress(); yield return null; yield return null; }
            Assert.IsFalse(chaos.Session.Blocked);
            Assert.AreEqual(62f, frustration.Value, 1e-3f, "-5 scared it off");
            yield return Use("microwave", hold: 1.3f);
            Assert.AreEqual(1, chaos.ShutOffCount);
        }

        [UnityTest]
        public IEnumerator Appliances_AllThree_ThenTheGateEnds()
        {
            yield return SkipTo<ApplianceChaosTask>();
            var chaos = Object.FindAnyObjectByType<ApplianceChaosTask>();
            foreach (var id in new[] { "microwave", "blender", "sink_tap" })
            {
                yield return WaitUntilFast(() => chaos.Session != null, 30f);
                yield return Use(id, hold: 1.3f);
            }
            Assert.AreEqual(3, chaos.ShutOffCount);
            yield return null;
            Assert.IsInstanceOf<FedUp.Tasks.Night.FinalSleepTask>(runner.Current, "then: bed (Gate 5a)");
        }

        // ---------------- design reasons (DESIGN_LOGIC.md #43-52) ----------------

        static bool Inside(Vector3 p, float x0, float x1, float z0, float z1) => p.x >= x0 && p.x <= x1 && p.z >= z0 && p.z <= z1;

        [Test]
        public void DesignLogic_TheHidingPlaces()
        {
            var milk = GameObject.Find("CTRL_HideMilk").transform.position;
            var tub = GameObject.Find("PREF_Bathtub (greybox)").GetComponent<Renderer>().bounds;
            Assert.Less(Vector3.Distance(new Vector3(milk.x, 0, milk.z), new Vector3(tub.center.x, 0, tub.center.z)), 0.5f, "milk in the bathtub");
            var sugar = GameObject.Find("CTRL_HideSugar").transform.position;
            Assert.Less(Vector3.Distance(sugar, GameObject.Find("PREF_Phone (greybox)").transform.position), 0.6f, "sugar beside the silent phone");
            // the tea moved to the Storeroom shelf under the axe in Gate 5b (designer-approved P1; DESIGN_LOGIC #56)
            var tea = GameObject.Find("CTRL_HideTea").transform.position;
            var axe = GameObject.Find("PROP_Axe (greybox)").transform.position;
            Assert.Less(Vector2.Distance(new Vector2(tea.x, tea.z), new Vector2(axe.x, axe.z)), 0.4f, "tea under the axe in the Storeroom");
        }

        [Test]
        public void DesignLogic_TheGuestBedStandsAgainstAWall_FacingTheDoor()
        {
            // #47: a made bed pushed against the wall facing the doorway, the first thing you see when you look in;
            // never in the middle of the room, in the way
            var bed = GameObject.Find("GB_GuestBed").GetComponent<Renderer>().bounds;
            Vector3 from = bed.center + Vector3.up * (bed.extents.y + 0.6f);
            bool againstWall = false;
            foreach (var d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
            {
                float half = Mathf.Abs(Vector3.Dot(bed.extents, d));
                if (Physics.Raycast(from, d, out var hit, half + 0.15f) && hit.collider.GetComponentInParent<TaskInteractable>() == null) againstWall = true;
            }
            Assert.IsTrue(againstWall, "the bed touches a wall");
            var door = GameObject.Find("ROOM_GuestBedroom").transform.position;
            var path = new UnityEngine.AI.NavMeshPath();
            Assert.IsTrue(UnityEngine.AI.NavMesh.CalculatePath(GameObject.Find("ROOM_LivingRoom").transform.position, door, UnityEngine.AI.NavMesh.AllAreas, path)
                          && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete, "you can still walk into the middle of the room");
        }

        [Test]
        public void DesignLogic_TheFuseBoxIsInTheBackHall()
        {
            var fuse = GameObject.Find("PREF_FuseBox (greybox)").transform.position;
            var hall = GameObject.Find("ROOM_BackHall").transform.position;
            Assert.Less(Vector3.Distance(new Vector3(fuse.x, 0, fuse.z), new Vector3(hall.x, 0, hall.z)), 1.5f);
        }

        [Test]
        public void DesignLogic_TheRadioIsOnTheDiningTable()
        {
            var radio = GameObject.Find("PROP_Radio (greybox)").transform.position;
            var table = GameObject.Find("GB_Dining_Table").GetComponent<Renderer>().bounds;
            Assert.IsTrue(Inside(radio, table.min.x, table.max.x, table.min.z, table.max.z) && radio.y > table.max.y);
        }

        [Test]
        public void DesignLogic_ThrowsComeFromAboveTheStove_WhereYouAreLooking()
        {
            var stove = GameObject.Find("GB_Stove").transform.position;
            foreach (var n in new[] { "CTRL_Throw0", "CTRL_Throw1", "CTRL_Throw2" })
            {
                var p = GameObject.Find(n).transform.position;
                Assert.Greater(p.y, stove.y + 0.8f, n + " is above the stove");
                Assert.Less(new Vector2(p.x - stove.x, p.z - stove.z).magnitude, 3f, n + " is right over the counter");
            }
        }
    }
}
