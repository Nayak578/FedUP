using FedUp.Tasks.Appliances;
using FedUp.Tasks.Calm;
using NUnit.Framework;

namespace FedUp.Tests
{
    /// <summary>Appliance chaos (GDD Task 6) and the radio (GDD rebalance calm extra).</summary>
    public class ApplianceAndRadioRulesTests
    {
        static ApplianceSession Running(float frustration, float roll)
        {
            var a = new ApplianceSession(timerSeconds: 10f, blockAbove: 50f, blockChance: 0.5f);
            a.Start(frustration, roll);
            return a;
        }

        [Test]
        public void ShutItOffInTime_NothingHappens()
        {
            var a = Running(20f, 0f);
            a.Tick(6f);
            Assert.IsTrue(a.HoldOff());
            Assert.IsTrue(a.ShutOff);
            Assert.IsFalse(a.Running);
            a.Tick(10f);
            Assert.IsFalse(a.Failed);
        }

        [Test]
        public void TenSecondsIgnored_ItOverloads_Once()
        {
            var a = Running(20f, 0f);
            a.Tick(9.9f);
            Assert.IsFalse(a.Failed);
            a.Tick(0.2f);
            Assert.IsTrue(a.FailedThisTick, "+10");
            Assert.IsFalse(a.Running);
            a.Tick(1f);
            Assert.IsFalse(a.FailedThisTick);
        }

        [Test]
        public void Above50_HalfTheTime_TheGhostCoversTheSwitch()
        {
            Assert.IsTrue(Running(60f, 0.4f).Blocked);
            Assert.IsFalse(Running(60f, 0.6f).Blocked);
            Assert.IsFalse(Running(50f, 0f).Blocked, "only above 50%");
        }

        [Test]
        public void ACoveredSwitch_CantBeHeldOff_UntilYouScareItAway()
        {
            var a = Running(70f, 0f);
            Assert.IsFalse(a.HoldOff());
            Assert.IsTrue(a.Running);
            a.ScareOff();
            Assert.IsFalse(a.Blocked);
            Assert.IsTrue(a.HoldOff());
        }

        [Test]
        public void TheClockKeepsRunning_WhileTheGhostCoversIt()
        {
            var a = Running(70f, 0f);
            a.Tick(10.1f);
            Assert.IsTrue(a.Failed);
        }

        // ---- radio ----

        [Test]
        public void Radio_15UninterruptedSeconds_Calms_Once()
        {
            var r = new RadioSession(15f);
            r.Tick(14.9f, true, true);
            Assert.IsFalse(r.Calmed);
            r.Tick(0.2f, true, true);
            Assert.IsTrue(r.CalmedThisTick);
            r.Tick(30f, true, true);
            Assert.IsFalse(r.CalmedThisTick, "once a night");
        }

        [Test]
        public void Radio_WalkingAway_StartsTheCountAgain()
        {
            var r = new RadioSession(15f);
            r.Tick(10f, true, true);
            r.Tick(0.1f, true, false);
            Assert.AreEqual(0f, r.Heard, 1e-4f);
            r.Tick(10f, true, true);
            Assert.IsFalse(r.Calmed);
        }

        [Test]
        public void Radio_AnInterruption_StartsTheCountAgain()
        {
            var r = new RadioSession(15f);
            r.Tick(10f, true, true);
            r.Interrupt();
            r.Tick(10f, true, true);
            Assert.IsFalse(r.Calmed);
        }

        [Test]
        public void Radio_Off_CountsNothing()
        {
            var r = new RadioSession(15f);
            r.Tick(20f, false, true);
            Assert.IsFalse(r.Calmed);
        }
    }
}
