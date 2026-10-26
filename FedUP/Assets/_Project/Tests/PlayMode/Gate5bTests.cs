using System.Collections;
using System.Linq;
using FedUp.Frustration;
using FedUp.Interaction;
using FedUp.Player;
using FedUp.Story;
using FedUp.Tasks;
using FedUp.Tasks.Night;
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
    /// <summary>
    /// Gate 5b in the real greybox scene: Mom's voicemail, the tea upstairs and the Storeroom blackout, the scenes for the
    /// loop, unemployed, dying in your sleep and the Burial in the house. Plus the design reasons (DESIGN_LOGIC #53-60, #77-86).
    /// </summary>
    public class Gate5bTests
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
            for (int i = 0; i < 24 && !(runner.Current is T); i++) { runner.DebugSkipCurrent(); yield return null; }
            Assert.IsInstanceOf<T>(runner.Current);
            yield return null;
        }

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

        IEnumerator Use(string id, float hold = 0f, float distance = 0.9f)
        {
            var t = TaskInteractable.Find(id);
            Assert.IsNotNull(t, id);
            Vector3 away = motor.transform.position - t.transform.position; away.y = 0;
            if (away.sqrMagnitude < 0.01f) away = Vector3.right;
            motor.Teleport(StandSpot(t.transform, away.normalized, distance) + Vector3.up * 0.2f, 0f);
            yield return null;
            Aim(t.transform.position);
            yield return null; yield return null;
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

        static bool Inside(Vector3 p, float x0, float x1, float z0, float z1) => p.x >= x0 && p.x <= x1 && p.z >= z0 && p.z <= z1;

        // ---------------- Mom's voicemail (#53, #77) ----------------

        [UnityTest]
        public IEnumerator Voicemail_OnTheEntranceTable_PiecesThenAClick_NeverWhole()
        {
            var vm = Object.FindAnyObjectByType<MomsVoicemail>();
            Assert.IsTrue(TaskInteractable.Find("his_phone").available, "there from the start of the evening");
            yield return Use("his_phone");
            bool heardPiece = false;
            Time.timeScale = 3f;
            for (float t = 0f; t < 20f && !vm.Listened; t += Time.deltaTime)
            {
                heardPiece |= subtitles.LastText != null && subtitles.LastText.Contains("I see you");
                StringAssert.DoesNotContain("I see you. I'm here. Stay with me.", subtitles.LastText ?? "", "never the whole mantra");
                yield return null;
            }
            Time.timeScale = 1f;
            Assert.IsTrue(vm.Listened);
            Assert.IsTrue(heardPiece, "you hear a piece of it");
            StringAssert.Contains("Later, Mom", subtitles.LastText);
        }

        [UnityTest]
        public IEnumerator Voicemail_HeTakesThePhone_WhenTheHouseTurns()
        {
            yield return SkipTo<ChargerTask>();
            yield return null;
            Assert.IsTrue(Object.FindAnyObjectByType<MomsVoicemail>().Pocketed);
            Assert.IsFalse(GameObject.Find("PROP_HisPhone (greybox)") != null, "it's his torch now: gone from the table");
        }

        // ---------------- the tea upstairs and the blackout (#56, #78-80) ----------------

        IEnumerator ToTheHunt()
        {
            yield return SkipTo<TeaTask>();
            var tea = (TeaTask)runner.Current;
            foreach (var id in new[] { "tea_milk", "tea_sugar", "tea_leaves" }) yield return Use(id);
            yield return WaitUntilFast(() => tea.Current == TeaTask.Step.Hunt, 4f, 2f);
            Assert.AreEqual(TeaTask.Step.Hunt, tea.Current);
        }

        [UnityTest]
        public IEnumerator TheTea_IsUpstairs_AndYouHearItFall()
        {
            yield return ToTheHunt();
            var teaItem = TaskInteractable.Find("tea_leaves").transform.position;
            Assert.IsTrue(Inside(teaItem, -36.8f, -29.5f, -5.2f, -0.5f) && teaItem.y > 8f, $"the tea is in the Storeroom: {teaItem:F2}");
            bool heard = false;
            for (float t = 0f; t < 5f && !heard; t += Time.deltaTime) { heard = subtitles.LastText != null && subtitles.LastText.Contains("upstairs"); yield return null; }
            Assert.IsTrue(heard, "*…That came from upstairs.*");
        }

        [UnityTest]
        public IEnumerator Blackout_TakeTheTea_TheLockedRoom_ThenTheTub_YouKeepIt()
        {
            yield return ToTheHunt();
            var blackout = Object.FindAnyObjectByType<StoreroomBlackout>();
            var tea = Object.FindAnyObjectByType<TeaTask>();
            yield return Use("tea_leaves");
            Assert.IsTrue(blackout.Happened, "taking the tea from under the axe");
            yield return WaitUntilFast(() => blackout.InDream, 6f, 2f);
            Assert.IsTrue(blackout.InDream);
            var locked = GameObject.Find("ROOM_LockedRoom").transform.position;
            Assert.Less(Vector2.Distance(new Vector2(motor.transform.position.x, motor.transform.position.z), new Vector2(locked.x, locked.z)), 4f, "in the Locked Room");
            Assert.IsTrue(motor.FeetFrozen, "you can look, not walk");
            yield return WaitUntilFast(() => blackout.InTub, 12f, 2f);
            Assert.IsTrue(blackout.GlimpseShown);
            Assert.That(blackout.GlimpseVisibleFor, Is.LessThan(0.32f), "a glimpse, under 0.3 s");
            Assert.IsTrue(blackout.MilkWasBeside, "the milk was still hidden in the tub");
            var tub = GameObject.Find("PREF_Bathtub (greybox)").GetComponent<Renderer>().bounds;
            Assert.Less(Vector2.Distance(new Vector2(motor.transform.position.x, motor.transform.position.z), new Vector2(tub.center.x, tub.center.z)), 1.5f, "in the bath");
            yield return WaitUntilFast(() => !blackout.Running, 8f, 2f);
            Assert.IsFalse(motor.Locked);
            Assert.IsFalse(motor.FeetFrozen);
            Assert.AreEqual(TeaItemState.Carried, tea.Session.StateOf(2), "you're still holding the tea");
            Assert.AreEqual(TeaTask.Step.Hunt, tea.Current, "and the hunt carries on");
        }

        [UnityTest]
        public IEnumerator Blackout_NotFromTheMilkOrSugar()
        {
            yield return ToTheHunt();
            yield return Use("tea_milk");
            yield return null;
            Assert.IsFalse(Object.FindAnyObjectByType<StoreroomBlackout>().Happened);
        }

        // ---------------- the ending scenes (#81-84) ----------------

        IEnumerator PlayEnding(Ending e)
        {
            yield return SkipTo<FinalSleepTask>();
            var endings = Object.FindAnyObjectByType<EndingPlayer>();
            endings.Play(e);
        }

        [UnityTest]
        public IEnumerator Loop_TheAlarm_ThenThePorchAgain()
        {
            yield return PlayEnding(Ending.Loop);
            var endings = Object.FindAnyObjectByType<EndingPlayer>();
            bool again = false;
            Time.timeScale = 3f;
            for (float t = 0f; t < 25f && !endings.CardShown; t += Time.deltaTime) { again |= subtitles.LastText == "…Again."; yield return null; }
            Time.timeScale = 1f;
            Assert.IsTrue(again, "*…Again.*");
            var spawn = Object.FindAnyObjectByType<SpawnPoint>().transform.position;
            Assert.Less(Vector3.Distance(motor.transform.position, spawn), 1f, "the porch, like the start");
            Assert.IsFalse(Object.FindAnyObjectByType<FedUp.Ghost.NightPhaseController>().DronePlaying, "the evening: no drone of the night");
            Assert.AreEqual("THE LOOP", endings.CardTitle);
            StringAssert.Contains("Job: safe", endings.CardOutcome);
        }

        [UnityTest]
        public IEnumerator Unemployed_TheVoicemail_ThenTheHouseRages()
        {
            yield return PlayEnding(Ending.UnemployedGhostAngry);
            var endings = Object.FindAnyObjectByType<EndingPlayer>();
            bool vm = false, after = false;
            Time.timeScale = 3f;
            for (float t = 0f; t < 25f && !endings.CardShown; t += Time.deltaTime)
            {
                vm |= subtitles.LastText != null && subtitles.LastText.Contains("don't bother coming in");
                after |= subtitles.LastText != null && subtitles.LastText.Contains("Me too");
                yield return null;
            }
            Time.timeScale = 1f;
            Assert.IsTrue(vm && after);
            StringAssert.Contains("Job: gone", endings.CardOutcome);
            StringAssert.Contains("furious", endings.CardOutcome);
        }

        [UnityTest]
        public IEnumerator DieInSleep_TheMorning_HisPhoneRings_Mom()
        {
            yield return PlayEnding(Ending.DieInSleep);
            var endings = Object.FindAnyObjectByType<EndingPlayer>();
            bool mom = false;
            Time.timeScale = 3f;
            for (float t = 0f; t < 30f && !endings.CardShown; t += Time.deltaTime) { mom |= subtitles.LastText != null && subtitles.LastText.Contains("Mom"); yield return null; }
            Time.timeScale = 1f;
            Assert.IsTrue(mom, "someone who loved him, unanswered");
            StringAssert.Contains("his mother", endings.CardLine, "a loss, not an escape");
        }

        [UnityTest]
        public IEnumerator Burial_TheMantra_TheChaseThroughTheStoreroom_TheWall_TheGap_TheVent()
        {
            yield return PlayEnding(Ending.BurialAnswered);
            var burial = Object.FindAnyObjectByType<BurialSequence>();
            var endings = Object.FindAnyObjectByType<EndingPlayer>();
            bool mantra = false;
            for (float t = 0f; t < 8f && burial.Current != BurialSequence.Stage.Chase; t += Time.deltaTime) { mantra |= subtitles.LastText == "I see you. I'm here. Stay with me."; yield return null; }
            Assert.IsTrue(mantra, "his mother's words, whole");
            Assert.AreEqual(BurialSequence.Stage.Chase, burial.Current);
            Assert.IsTrue(burial.Stops.Any(p => Inside(p, -36.8f, -29.5f, -5.2f, -0.5f) && p.y > 8f), "the chase passes through the Storeroom, past the axe");
            // follow it: it never lets you closer than a few metres
            for (int guard = 0; guard < 20 && burial.Current == BurialSequence.Stage.Chase; guard++)
            {
                var f = burial.Figure.position;
                Vector3 near = NavMesh.SamplePosition(f - Vector3.up * 0.9f, out var h, 2f, NavMesh.AllAreas) ? h.position : f;
                motor.Teleport(near + Vector3.up * 0.2f, 0f);
                yield return new WaitForSeconds(0.2f);
            }
            yield return WaitUntilFast(() => burial.Current == BurialSequence.Stage.Crack, 6f, 2f);
            Assert.AreEqual(BurialSequence.Stage.Crack, burial.Current, "it went into the wall");
            yield return new WaitForSeconds(3.2f);
            StringAssert.Contains("axe", subtitles.LastText);
            yield return Use("axe", hold: 1.0f, distance: 1.1f);
            Assert.IsTrue(burial.HasAxe);
            for (int i = 0; i < 3; i++) yield return Use("burial_wall", 0f, 1.0f);
            Assert.AreEqual(3, burial.Hits);
            Assert.IsTrue(GameObject.Find("GB_WallHole").activeSelf);
            yield return Use("burial_wall", 0f, 1.0f); // squeeze in
            yield return WaitUntilFast(() => burial.Current == BurialSequence.Stage.Squeeze && !motor.Locked, 3f, 2f);
            Assert.AreEqual(0.07f, motor.RadiusOverride ?? 0f, 1e-3f, "sideways");
            // shuffle south the length of the house, on foot
            motor.SetLook(180f, 0f);
            motor.VirtualMove = Vector2.up;
            yield return WaitUntilFast(() => burial.Current == BurialSequence.Stage.Triangle, 40f, 4f);
            motor.VirtualMove = Vector2.zero;
            Assert.AreEqual(BurialSequence.Stage.Triangle, burial.Current, $"reached the triangle on foot (stopped at {motor.transform.position:F2})");
            yield return Use("vent", 0f, 0.8f);
            yield return WaitUntilFast(() => endings.CardShown, 15f, 3f);
            Assert.AreEqual("GHOST BURIAL", endings.CardTitle);
            StringAssert.Contains("take a few days off", endings.CardLine);
            StringAssert.Contains("at peace", endings.CardOutcome);
            Assert.IsNull(motor.RadiusOverride, "back to normal size");
        }

        // ---------------- design reasons ----------------

        [Test]
        public void DesignLogic_TheAxe_FacesTheLandingDoor_TheTeaUnderIt()
        {
            var axe = GameObject.Find("PROP_Axe (greybox)").transform.position;
            Assert.IsTrue(Inside(axe, -36.8f, -29.5f, -5.2f, -0.5f) && axe.y > 9f, "in the Storeroom, on the wall");
            var tea = GameObject.Find("CTRL_HideTea").transform.position;
            Assert.Less(Vector2.Distance(new Vector2(axe.x, axe.z), new Vector2(tea.x, tea.z)), 0.4f, "the tea is right under the axe");
            Assert.Less(tea.y, axe.y);
            // from the landing doorway (west), the axe is straight ahead
            var eye = new Vector3(-36.3f, 8.28f + 1.6f, -3.8f);
            Vector3 to = axe - eye;
            var blockers = Physics.RaycastAll(eye, to.normalized, to.magnitude - 0.1f)
                .Where(h => h.collider.name != "PROP_Axe (greybox)" && h.collider.GetComponent<Renderer>() != null && h.collider.GetComponent<Renderer>().enabled)
                .Select(h => h.collider.name).ToArray();
            Assert.IsEmpty(blockers, "the first thing in your torch beam as you come in");
        }

        [Test]
        public void DesignLogic_TheHiddenDoor_ShowsAboveTheShelf_OnTheLockedRoomWall()
        {
            var shelf = GameObject.Find("GB_TallShelf").GetComponent<Renderer>().bounds;
            var frame = GameObject.Find("GB_HiddenDoorFrame_Top").GetComponent<Renderer>().bounds;
            Assert.Greater(frame.min.y, shelf.max.y, "the top of the frame shows above the shelf");
            var locked = GameObject.Find("ROOM_LockedRoom").transform.position;
            Assert.Greater(locked.z, frame.center.z, "the Locked Room is on the other side of that wall");
        }

        [Test]
        public void DesignLogic_HisPhone_BesideMomsCard()
        {
            var phone = GameObject.Find("PROP_HisPhone (greybox)").transform.position;
            var card = GameObject.Find("PROP_MomsCard (greybox)").transform.position;
            Assert.Less(Vector3.Distance(phone, card), 0.4f);
        }

        [Test]
        public void DesignLogic_TheDreamRoom_NothingSuggestsAMethod()
        {
            // research rule 10: the chair is upright, nothing hangs, nothing is knocked over
            var back = GameObject.Find("GB_DreamChair_Back").transform;
            Assert.Less(Vector3.Angle(back.up, Vector3.up), 1f, "the chair stands upright");
            foreach (var t in GameObject.Find("ROOMFX_LockedRoomDream").GetComponentsInChildren<Transform>())
                StringAssert.DoesNotContain("rope", t.name.ToLowerInvariant());
        }
    }
}
