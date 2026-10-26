using FedUp.Story;
using FedUp.Tasks.Night;
using NUnit.Framework;

namespace FedUp.Tests
{
    /// <summary>The final sleep (GDD Final Sleep Task, Task 7 and the rebalance; Appendix 1: +15 / -20 / +10 once).</summary>
    public class FinalSleepRulesTests
    {
        static FinalSleepSession New() => new FinalSleepSession(qteAbove: 50f, beats: 3, maxFails: 3, failCost: 15f,
            retryCost: 10f, cleanSleep: -20f, choiceSeconds: 8f);

        [Test]
        public void Calm_NoDisruption_ACleanSleep_Minus20_ThenTheChoice()
        {
            var s = New();
            Assert.AreEqual(-20f, s.LieDown(30f), 1e-4f);
            Assert.AreEqual(FinalSleepStage.Choice, s.Stage);
        }

        [Test]
        public void Exactly50_StillCalm()
        {
            var s = New();
            Assert.AreEqual(-20f, s.LieDown(50f), 1e-4f);
            Assert.AreEqual(FinalSleepStage.Choice, s.Stage);
        }

        [Test]
        public void Above50_YouMustBreatheFirst()
        {
            var s = New();
            Assert.AreEqual(0f, s.LieDown(60f), 1e-4f);
            Assert.AreEqual(FinalSleepStage.Breathing, s.Stage);
        }

        [Test]
        public void ThreeBeatsInARow_ACleanSleep_Minus20()
        {
            var s = New();
            s.LieDown(70f);
            Assert.AreEqual(0f, s.Beat(true), 1e-4f);
            Assert.AreEqual(0f, s.Beat(true), 1e-4f);
            Assert.AreEqual(-20f, s.Beat(true), 1e-4f);
            Assert.AreEqual(FinalSleepStage.Choice, s.Stage);
            Assert.IsFalse(s.Nightmare);
        }

        [Test]
        public void AMiss_Scream15_PlusTheOneRetryPenalty_AndYouStartAgain()
        {
            var s = New();
            s.LieDown(70f);
            s.Beat(true); s.Beat(true);
            Assert.AreEqual(25f, s.Beat(false), 1e-4f); // +15 scream, +10 for the (first) retry
            Assert.AreEqual(0, s.BeatsHit);
            Assert.AreEqual(FinalSleepStage.Breathing, s.Stage);
        }

        [Test]
        public void TheRetryPenalty_OnlyOnce()
        {
            var s = New();
            s.LieDown(70f);
            s.Beat(false);
            Assert.AreEqual(15f, s.Beat(false), 1e-4f);
        }

        [Test]
        public void ThreeMisses_TheNightmareHappens_NoCleanSleep()
        {
            var s = New();
            s.LieDown(70f);
            s.Beat(false); s.Beat(false);
            Assert.AreEqual(15f, s.Beat(false), 1e-4f);
            Assert.IsTrue(s.Nightmare);
            Assert.AreEqual(FinalSleepStage.Choice, s.Stage);
        }

        [Test]
        public void DoingNothing_IsStayingAsleep()
        {
            var s = New();
            s.LieDown(20f);
            s.Tick(7.9f);
            Assert.AreEqual(FinalSleepStage.Choice, s.Stage);
            s.Tick(0.2f);
            Assert.AreEqual(FinalSleepStage.Chosen, s.Stage);
            Assert.AreEqual(FinalChoice.StayAsleep, s.Chosen);
        }

        [Test]
        public void PressingInTime_WakesYouUp()
        {
            var s = New();
            s.LieDown(20f);
            s.Tick(5f);
            s.WakeUp();
            Assert.AreEqual(FinalSleepStage.Chosen, s.Stage);
            Assert.AreEqual(FinalChoice.WakeUp, s.Chosen);
            s.Tick(10f);
            Assert.AreEqual(FinalChoice.WakeUp, s.Chosen);
        }

        [Test]
        public void YouCantWake_BeforeTheBlanketLifts()
        {
            var s = New();
            s.LieDown(70f);
            s.WakeUp();
            Assert.AreEqual(FinalSleepStage.Breathing, s.Stage);
        }
    }
}
