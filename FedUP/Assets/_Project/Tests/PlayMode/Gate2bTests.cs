using System.Collections;
using System.Linq;
using FedUp.Frustration;
using FedUp.Interaction;
using FedUp.Player;
using FedUp.Story;
using FedUp.Tasks;
using FedUp.Tasks.Cat;
using FedUp.Tasks.Phone;
using FedUp.Tasks.Tv;
using FedUp.Fx;
using FedUp.UI;
using FedUp.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FedUp.Tests
{
    /// <summary>Gate 2b in the real greybox scene: Watch TV, Cat Visit #1, and the design reasons for where things are.</summary>
    public class Gate2bTests
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

        IEnumerator SitOnCouch(TvTask tv)
        {
            motor.Teleport(GameObject.Find("CTRL_CouchStand").transform.position + Vector3.up * 0.3f, 0f);
            yield return null;
            yield return Use("couch", hold: 1.3f);
            Assert.IsTrue(tv.Seated, "sitting on the couch");
            Assert.IsTrue(motor.Locked);
        }

        IEnumerator Flip(TvTask tv, float channelRoll, float stealRoll = 0.9f)
        {
            tv.ForcedChannelRoll = channelRoll;
            tv.ForcedStealRoll = stealRoll;
            tv.VirtualPress();
            yield return null; yield return null;
        }

        // ---------------- TV ----------------

        [UnityTest]
        public IEnumerator Tv_BadChannelsCost_ButTheTvAddsAtMost10()
        {
            yield return SkipTo<TvTask>();
            var tv = Object.FindAnyObjectByType<TvTask>();
            frustration.SetValue(0f);
            yield return SitOnCouch(tv);
            yield return Flip(tv, 0.1f);
            Assert.AreEqual(TvChannel.Static, tv.Session.Channel);
            Assert.AreEqual(7f, frustration.Value, 1e-3f, "static +7");
            var staticColour = tv.ScreenColor;
            yield return new WaitForSeconds(0.3f);
            yield return Flip(tv, 0.6f);
            Assert.AreEqual(TvChannel.Ad, tv.Session.Channel);
            Assert.AreNotEqual(staticColour, tv.ScreenColor, "you can see the channel change");
            Assert.AreEqual(10f, frustration.Value, 1e-3f, "the ad only adds up to the +10 cap");
            yield return new WaitForSeconds(0.3f);
            yield return Flip(tv, 0.85f);
            Assert.AreEqual(TvChannel.Horror, tv.Session.Channel);
            Assert.AreEqual(10f, frustration.Value, 1e-3f, "capped");
        }

        [UnityTest]
        public IEnumerator Tv_SpammingTheRemote_SendsTheBatteriesUnderTheCouch_ThenYouFindThem()
        {
            yield return SkipTo<TvTask>();
            var tv = Object.FindAnyObjectByType<TvTask>();
            frustration.SetValue(0f);
            yield return SitOnCouch(tv);
            for (int i = 0; i < 10; i++) yield return Flip(tv, 0.1f);
            Assert.AreEqual(1, tv.BatteryLosses);
            Assert.IsFalse(tv.Session.HasRemote);
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(tv.Seated, "he gets up to look");
            StringAssert.Contains("Under the couch", subtitles.LastText);
            Assert.AreEqual(10f, frustration.Value, 1e-3f, "static +7, then the cap");
            yield return Use("under_couch", hold: 1.5f);
            Assert.IsTrue(tv.Session.HasRemote, "batteries back in");
        }

        [UnityTest]
        public IEnumerator Tv_Above50_TheGhostGrabsTheRemote_LoseTheTug_ItsAtTheFootOfTheTv()
        {
            yield return SkipTo<TvTask>();
            var tv = Object.FindAnyObjectByType<TvTask>();
            frustration.SetValue(55f);
            yield return null;
            yield return SitOnCouch(tv);
            yield return Flip(tv, 0.1f, stealRoll: 0f);
            Assert.IsTrue(tv.Tugging, "tug of war");
            yield return new WaitForSeconds(3.5f); // no mashing
            Assert.AreEqual(1, tv.RemoteThefts);
            Assert.AreEqual(60f, frustration.Value, 1e-3f, "+5 stolen");
            Assert.IsFalse(tv.Seated);
            var floor = TaskInteractable.Find("remote_floor");
            var screen = GameObject.Find("PREF_TVScreen (greybox)").transform.position;
            Assert.Less(Vector3.Distance(floor.transform.position, screen), 2f, "left at the foot of the TV");
            yield return Use("remote_floor");
            Assert.IsTrue(tv.Session.HasRemote);
        }

        [UnityTest]
        public IEnumerator Tv_GhostGrab_MashToKeepTheRemote()
        {
            yield return SkipTo<TvTask>();
            var tv = Object.FindAnyObjectByType<TvTask>();
            frustration.SetValue(55f);
            yield return null;
            yield return SitOnCouch(tv);
            yield return Flip(tv, 0.1f, stealRoll: 0f);
            Assert.IsTrue(tv.Tugging);
            for (int i = 0; i < 16 && tv.Tugging; i++) { tv.VirtualPress(); yield return null; yield return null; }
            Assert.IsFalse(tv.Tugging);
            Assert.AreEqual(0, tv.RemoteThefts);
            Assert.IsTrue(tv.Seated, "still on the couch");
            Assert.AreEqual(55f, frustration.Value, 1e-3f);
        }

        [UnityTest]
        public IEnumerator Tv_CalmFor10Seconds_Minus10_ThenTheBossCalls()
        {
            yield return SkipTo<TvTask>();
            var tv = Object.FindAnyObjectByType<TvTask>();
            frustration.SetValue(30f);
            yield return SitOnCouch(tv);
            yield return Flip(tv, 0.97f);
            Assert.AreEqual(TvChannel.Calm, tv.Session.Channel);
            Time.timeScale = 3f;
            float waited = 0f;
            while (tv.State != TaskState.Done && waited < 20f) { waited += Time.unscaledDeltaTime; yield return null; }
            Time.timeScale = 1f;
            Assert.AreEqual(TaskState.Done, tv.State);
            Assert.AreEqual(20f, frustration.Value, 1e-3f, "-10 for the calm channel");
            Assert.IsInstanceOf<PhoneCallTask>(runner.Current, "the boss calls next");
            Assert.AreEqual(TvChannel.Calm, tv.Session.Channel, "the home video keeps playing");
        }

        // ---------------- cat ----------------

        IEnumerator CatArrives(CatVisitTask visit)
        {
            visit.DebugArriveNow();
            Time.timeScale = 4f;
            float waited = 0f;
            while (!visit.Arrived && waited < 20f) { waited += Time.unscaledDeltaTime; yield return null; }
            Time.timeScale = 1f;
            Assert.IsTrue(visit.Arrived, "the cat walked up and sat down");
            Assert.Less(Vector3.Distance(visit.Cat.transform.position, motor.transform.position), 2.5f, "near you");
        }

        [UnityTest]
        public IEnumerator Cat_Petted_Minus20_Recorded_AndTheScreenSoftens()
        {
            yield return SkipTo<CatVisitTask>();
            var visit = Object.FindAnyObjectByType<CatVisitTask>();
            frustration.SetValue(30f);
            yield return CatArrives(visit);
            StringAssert.Contains("don't have a cat", subtitles.LastText);
            yield return Use("cat", hold: 1.8f);
            Assert.IsTrue(visit.Session.Petted);
            Assert.AreEqual(10f, frustration.Value, 1e-3f, "-20 petted");
            Assert.IsTrue(Object.FindAnyObjectByType<ScreenSoften>().Glowing, "the screen softens");
            var run = Resources.FindObjectsOfTypeAll<RunStateSO>().First();
            Assert.IsTrue(run.CatPettedOn(1));
            Assert.IsTrue(run.CatPettedEver);
        }

        [UnityTest]
        public IEnumerator Cat_Ignored_WalksBackIntoTheDark_RecordedAsNotPetted()
        {
            yield return SkipTo<CatVisitTask>();
            var visit = Object.FindAnyObjectByType<CatVisitTask>();
            yield return CatArrives(visit);
            Time.timeScale = 5f;
            float waited = 0f;
            while (!visit.Session.Left && waited < 15f) { waited += Time.unscaledDeltaTime; yield return null; }
            Time.timeScale = 1f;
            yield return null;
            StringAssert.Contains("Cats are smart", subtitles.LastText);
            Time.timeScale = 4f;
            waited = 0f;
            while (visit.State != TaskState.Done && waited < 15f) { waited += Time.unscaledDeltaTime; yield return null; }
            Time.timeScale = 1f;
            Assert.AreEqual(TaskState.Done, visit.State);
            Assert.IsFalse(visit.Cat.gameObject.activeSelf, "gone into the back hall");
            Assert.AreEqual(0f, frustration.Value, 1e-3f, "ignoring costs nothing now");
            var run = Resources.FindObjectsOfTypeAll<RunStateSO>().First();
            Assert.IsTrue(run.CatVisitHappened(1));
            Assert.IsFalse(run.CatPettedOn(1));
            Assert.IsTrue(runner.AllDone);
        }

        [UnityTest]
        public IEnumerator Cat_Above60_ItCanHiss_AndThePetAfter30sCalmsLess()
        {
            yield return SkipTo<CatVisitTask>();
            var visit = Object.FindAnyObjectByType<CatVisitTask>();
            frustration.SetValue(70f);
            yield return CatArrives(visit);
            visit.ForcedRoll = 0f;
            yield return Use("cat", hold: 1.8f);
            Assert.IsTrue(visit.Hissed);
            Assert.AreEqual(70f, frustration.Value, 1e-3f, "a hiss costs nothing");
            yield return new WaitForSeconds(1.2f);
            var couch = GameObject.Find("GB_3seat4(1)").GetComponent<Renderer>().bounds;
            Assert.Less(Vector3.Distance(visit.Cat.transform.position, couch.center), 2.5f, "it reappears on the couch");
            var pet = TaskInteractable.Find("cat");
            Assert.IsFalse(pet.available, "not yet");
            Time.timeScale = 6f;
            float waited = 0f;
            while (!pet.available && waited < 10f) { waited += Time.unscaledDeltaTime; yield return null; }
            Time.timeScale = 1f;
            Assert.IsTrue(pet.available, "after 30 s you can try again");
            motor.Teleport(new Vector3(couch.min.x - 0.5f, motor.transform.position.y, couch.max.z + 0.5f), 0f);
            yield return null;
            visit.ForcedRoll = 0.9f;
            yield return Use("cat", hold: 1.8f);
            Assert.IsTrue(visit.Session.Petted);
            Assert.AreEqual(60f, frustration.Value, 1e-3f, "-10 on the retry");
        }

        [UnityTest]
        public IEnumerator AfterThePhone_TheCatVisits()
        {
            yield return SkipTo<PhoneCallTask>();
            runner.DebugSkipCurrent();
            yield return null;
            Assert.IsInstanceOf<CatVisitTask>(runner.Current);
        }

        // ---------------- design reasons (DESIGN_LOGIC.md) ----------------

        [Test]
        public void DesignLogic_ThePhoneCantBeSeenFromTheCouch_ButCanBeWalkedTo()
        {
            var seat = GameObject.Find("CTRL_CouchSeat").transform.position;
            var phone = GameObject.Find("PREF_Phone (greybox)").transform.position;
            Assert.IsTrue(Physics.Linecast(seat + Vector3.up * 1.15f, phone, out var hit) && hit.collider.name != "PREF_Phone (greybox)",
                "out of sight from the couch: you hear it ring, then have to get up and go to it");
            var path = new NavMeshPath();
            NavMesh.SamplePosition(GameObject.Find("CTRL_CouchStand").transform.position, out var a, 1f, NavMesh.AllAreas);
            NavMesh.SamplePosition(phone + Vector3.down * 1f, out var b, 1.5f, NavMesh.AllAreas);
            NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path);
            Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status);
        }

        [Test]
        public void DesignLogic_FromThePhone_YouLookDownTheCorridorTheCatComesFrom()
        {
            var phone = GameObject.Find("PREF_Phone (greybox)").transform.position;
            var entry = GameObject.Find("CTRL_CatEntry").transform.position;
            Vector3 eye = new Vector3(phone.x, entry.y + 1.5f, phone.z - 0.6f);
            Assert.IsFalse(Physics.Linecast(eye, entry + Vector3.up * 0.3f), "a clear view from the phone to where the cat appears");
        }

        [Test]
        public void DesignLogic_TheCouchFacesTheTv()
        {
            var seat = GameObject.Find("CTRL_CouchSeat").transform;
            var screen = GameObject.Find("PREF_TVScreen (greybox)").transform.position;
            Vector3 to = screen - seat.position; to.y = 0f;
            Assert.Less(Vector3.Angle(seat.forward, to), 10f);
            Assert.IsFalse(Physics.Linecast(seat.position + Vector3.up * 1.15f, screen + Vector3.back * 0.05f), "nothing blocks the screen");
        }
    }
}
