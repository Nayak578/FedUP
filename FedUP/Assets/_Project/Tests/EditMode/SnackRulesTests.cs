using FedUp.Tasks.Snack;
using NUnit.Framework;

namespace FedUp.Tests
{
    /// <summary>Grab a Snack rules from the GDD (Phase 3 continuing tasks + Task 3).</summary>
    public class SnackRulesTests
    {
        static SnackSession OpenFridge()
        {
            var s = new SnackSession(slideAbove: 30f, grabWindow: 0.6f, slideSeconds: 0.4f, sarcasmAfter: 3f, handAfterOpen: 10f, slamAfter: 15f);
            s.Open();
            return s;
        }

        [Test]
        public void ClosedFridge_NothingToGrab()
        {
            var s = new SnackSession(30f, 0.6f, 0.4f, 3f, 10f, 15f);
            Assert.AreEqual(GrabResult.NotOpen, s.Grab());
        }

        [Test]
        public void Calm_LookAndGrab_GetsTheSnack()
        {
            var s = OpenFridge();
            s.Tick(0.3f, true, 10f);
            Assert.AreEqual(GrabResult.Grabbed, s.Grab());
        }

        [Test]
        public void Above30_StaringMakesItSlideAway()
        {
            var s = OpenFridge();
            s.Tick(0.5f, true, 40f);
            Assert.IsFalse(s.SlidAwayThisTick);
            s.Tick(0.2f, true, 40f);
            Assert.IsTrue(s.SlidAwayThisTick);
            Assert.AreEqual(1, s.Slides);
            Assert.IsTrue(s.Sliding);
        }

        [Test]
        public void At30_ItNeverSlides()
        {
            var s = OpenFridge();
            for (int i = 0; i < 20; i++) s.Tick(0.1f, true, 30f);
            Assert.AreEqual(0, s.Slides);
        }

        [Test]
        public void GrabbingWhileItSlides_IsAFailedGrab()
        {
            var s = OpenFridge();
            s.Tick(0.7f, true, 40f);
            Assert.AreEqual(GrabResult.FailedGrab, s.Grab());
        }

        [Test]
        public void OnceItSettles_AQuickGrabWorks()
        {
            var s = OpenFridge();
            s.Tick(0.7f, true, 40f);
            s.Tick(0.45f, false, 40f); // it settles; you look again
            Assert.IsFalse(s.Sliding);
            s.Tick(0.2f, true, 40f);
            Assert.AreEqual(GrabResult.Grabbed, s.Grab());
        }

        [Test]
        public void HoveringThreeSeconds_TheFridgeMocksYou_Once()
        {
            var s = OpenFridge();
            bool mocked = false;
            for (int i = 0; i < 31; i++) { s.Tick(0.1f, true, 10f); mocked |= s.SarcasmThisTick; }
            Assert.IsTrue(mocked);
            bool again = false;
            for (int i = 0; i < 40; i++) { s.Tick(0.1f, true, 10f); again |= s.SarcasmThisTick; }
            Assert.IsFalse(again, "once per open");
        }

        [Test]
        public void TheSarcasmCounts_EvenWhileItKeepsSliding()
        {
            var s = OpenFridge();
            bool mocked = false;
            for (int i = 0; i < 40; i++) { s.Tick(0.1f, true, 50f); mocked |= s.SarcasmThisTick; }
            Assert.IsTrue(mocked, "3 s of hovering in total, slides or not");
        }

        [Test]
        public void After10SecondsOpen_AGhostHandYanksIt()
        {
            var s = OpenFridge();
            s.Tick(10.1f, false, 10f);
            s.Tick(0.1f, true, 10f);
            Assert.AreEqual(GrabResult.GhostHand, s.Grab());
        }

        [Test]
        public void Open15Seconds_TheDoorSlams_ThenYouReopen()
        {
            var s = OpenFridge();
            s.Tick(14.9f, false, 10f);
            Assert.IsTrue(s.IsOpen);
            s.Tick(0.2f, false, 10f);
            Assert.IsTrue(s.SlammedThisTick);
            Assert.IsFalse(s.IsOpen);
            Assert.AreEqual(GrabResult.NotOpen, s.Grab());
            s.Open();
            Assert.AreEqual(0f, s.OpenFor, 1e-4f);
            s.Tick(0.2f, true, 10f);
            Assert.AreEqual(GrabResult.Grabbed, s.Grab());
        }
    }
}
