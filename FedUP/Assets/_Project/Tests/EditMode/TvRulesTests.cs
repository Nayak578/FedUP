using FedUp.Tasks.Tv;
using NUnit.Framework;

namespace FedUp.Tests
{
    /// <summary>Watch TV rules from the GDD (Phase 1 + Task 2) and the TV rebalance cap.</summary>
    public class TvRulesTests
    {
        static readonly float[] Odds = { 0.5f, 0.3f, 0.15f, 0.05f }; // static, ad, horror, calm

        static TvSession New() => new TvSession(Odds, spamPresses: 10, spamWindowSeconds: 5f, stealAbove: 50f,
            stealChance: 0.1f, calmSecondsToWin: 10f, positiveCap: 10f);

        [TestCase(0.00f, TvChannel.Static)]
        [TestCase(0.49f, TvChannel.Static)]
        [TestCase(0.50f, TvChannel.Ad)]
        [TestCase(0.79f, TvChannel.Ad)]
        [TestCase(0.80f, TvChannel.Horror)]
        [TestCase(0.94f, TvChannel.Horror)]
        [TestCase(0.95f, TvChannel.Calm)]
        [TestCase(0.999f, TvChannel.Calm)]
        public void ChannelOdds_MatchTheGdd(float roll, TvChannel expected)
        {
            Assert.AreEqual(expected, TvSession.ChannelFor(roll, Odds));
        }

        [Test]
        public void StartsOff_WithTheRemoteInHand()
        {
            var tv = New();
            Assert.AreEqual(TvChannel.Off, tv.Channel);
            Assert.IsTrue(tv.HasRemote);
        }

        [Test]
        public void APress_ChangesTheChannel()
        {
            var tv = New();
            Assert.AreEqual(TvPressResult.Changed, tv.Press(0f, 0.6f, 0f, 0.9f));
            Assert.AreEqual(TvChannel.Ad, tv.Channel);
        }

        [Test]
        public void TenPressesInFiveSeconds_EjectTheBatteries()
        {
            var tv = New();
            for (int i = 0; i < 9; i++) Assert.AreEqual(TvPressResult.Changed, tv.Press(i * 0.5f, 0.1f, 0f, 0.9f), $"press {i + 1}");
            Assert.AreEqual(TvPressResult.BatteriesOut, tv.Press(4.5f, 0.1f, 0f, 0.9f));
            Assert.IsFalse(tv.HasRemote);
            Assert.AreEqual(TvPressResult.Ignored, tv.Press(4.6f, 0.1f, 0f, 0.9f), "no remote, nothing happens");
        }

        [Test]
        public void SteadyFlipping_NeverEjectsTheBatteries()
        {
            var tv = New();
            for (int i = 0; i < 30; i++) Assert.AreEqual(TvPressResult.Changed, tv.Press(i * 0.6f, 0.1f, 0f, 0.9f), $"press {i + 1}");
            Assert.IsTrue(tv.HasRemote);
        }

        [Test]
        public void FindingTheBatteries_GivesAFreshStart()
        {
            var tv = New();
            for (int i = 0; i < 10; i++) tv.Press(i * 0.1f, 0.1f, 0f, 0.9f);
            Assert.IsFalse(tv.HasRemote);
            tv.RemoteRecovered();
            Assert.IsTrue(tv.HasRemote);
            Assert.AreEqual(TvPressResult.Changed, tv.Press(1.1f, 0.1f, 0f, 0.9f), "old presses don't count any more");
        }

        [Test]
        public void Above50Percent_TheGhostMayGrabTheRemote()
        {
            var tv = New();
            tv.Press(0f, 0.6f, 0f, 0.9f); // Ad
            Assert.AreEqual(TvPressResult.Stolen, tv.Press(1f, 0.1f, 55f, 0.05f));
            Assert.AreEqual(TvChannel.Ad, tv.Channel, "a grab doesn't change the channel");
        }

        [Test]
        public void AtOrBelow50Percent_TheGhostNeverGrabs()
        {
            var tv = New();
            Assert.AreEqual(TvPressResult.Changed, tv.Press(0f, 0.1f, 50f, 0.0f));
        }

        [Test]
        public void HighFrustration_UnluckyRollOnly_Grabs()
        {
            var tv = New();
            Assert.AreEqual(TvPressResult.Changed, tv.Press(0f, 0.1f, 80f, 0.5f));
        }

        [Test]
        public void LosingTheTugOfWar_LosesTheRemote_UntilFound()
        {
            var tv = New();
            tv.RemoteLost();
            Assert.IsFalse(tv.HasRemote);
            Assert.AreEqual(TvPressResult.Ignored, tv.Press(0f, 0.1f, 0f, 0.9f));
            tv.RemoteRecovered();
            Assert.AreEqual(TvPressResult.Changed, tv.Press(1f, 0.1f, 0f, 0.9f));
        }

        [Test]
        public void TheTvAddsAtMost10_InTotal()
        {
            var tv = New();
            Assert.AreEqual(7f, tv.Charge(7f), 1e-4f, "first bad channel");
            Assert.AreEqual(3f, tv.Charge(7f), 1e-4f, "second only up to the cap");
            Assert.AreEqual(0f, tv.Charge(5f), 1e-4f);
            Assert.AreEqual(0f, tv.Charge(10f), 1e-4f, "batteries too, once capped");
            Assert.AreEqual(10f, tv.PositiveSpent, 1e-4f);
        }

        [Test]
        public void Calming_IsNeverCapped()
        {
            var tv = New();
            tv.Charge(10f);
            Assert.AreEqual(-10f, tv.Charge(-10f), 1e-4f);
        }

        [Test]
        public void CalmFor10Seconds_WhileSitting_Wins_Once()
        {
            var tv = New();
            tv.Press(0f, 0.97f, 0f, 0.9f);
            Assert.AreEqual(TvChannel.Calm, tv.Channel);
            tv.Tick(9.9f, true);
            Assert.IsFalse(tv.Won);
            tv.Tick(0.2f, true);
            Assert.IsTrue(tv.Won);
            Assert.IsTrue(tv.WonThisTick);
            tv.Tick(1f, true);
            Assert.IsFalse(tv.WonThisTick, "pays out once");
        }

        [Test]
        public void ChangingAwayFromCalm_StartsTheCountAgain()
        {
            var tv = New();
            tv.Press(0f, 0.97f, 0f, 0.9f);
            tv.Tick(6f, true);
            tv.Press(6f, 0.1f, 0f, 0.9f);
            tv.Press(7f, 0.97f, 0f, 0.9f);
            tv.Tick(6f, true);
            Assert.IsFalse(tv.Won);
            Assert.AreEqual(6f, tv.CalmHeld, 1e-4f);
        }

        [Test]
        public void CalmDoesNotCount_WhenHeIsNotSitting()
        {
            var tv = New();
            tv.Press(0f, 0.97f, 0f, 0.9f);
            tv.Tick(20f, false);
            Assert.IsFalse(tv.Won);
        }

        [Test]
        public void AfterWinning_TheRemoteDoesNothing()
        {
            var tv = New();
            tv.Press(0f, 0.97f, 0f, 0.9f);
            tv.Tick(10.5f, true);
            Assert.AreEqual(TvPressResult.Ignored, tv.Press(11f, 0.1f, 0f, 0.9f));
            Assert.AreEqual(TvChannel.Calm, tv.Channel, "the calm channel stays on");
        }
    }
}
