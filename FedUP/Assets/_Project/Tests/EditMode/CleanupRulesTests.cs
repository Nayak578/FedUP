using FedUp.Tasks.Cleanup;
using NUnit.Framework;

namespace FedUp.Tests
{
    /// <summary>
    /// Clean Up Mess (GDD Phase 1 + Task 5, with the rebalance "minor glitches over repeated flybacks"):
    /// - below 40% frustration drops always work; above 40% the ghost may reject one (+6, giggle +2)
    /// - the first time you bin 3 in a row, that third item flies back out once (comedy beat, +6 and giggle +2)
    /// - after that, 3 in a row makes the ghost back off for 60 s and starts a 30 s clean-room timer (-10, once)
    /// </summary>
    public class CleanupRulesTests
    {
        static CleanupSession New(int items = 6) => new CleanupSession(items, interferenceAbove: 40f, rejectChance: 0.4f,
            streakForBreak: 3, ghostBreakSeconds: 60f, cleanRoomSeconds: 30f, comedyFlybackOnce: true);

        [Test]
        public void Below40Percent_DropsAlwaysWork()
        {
            var s = New();
            Assert.AreEqual(DropResult.Binned, s.Drop(frustration: 20f, roll: 0.0f));
            Assert.AreEqual(1, s.Binned);
        }

        [Test]
        public void Above40Percent_TheGhostMayRejectADrop()
        {
            var s = New();
            Assert.AreEqual(DropResult.Rejected, s.Drop(frustration: 50f, roll: 0.1f), "roll under the 40% chance");
            Assert.AreEqual(DropResult.Binned, s.Drop(frustration: 50f, roll: 0.9f), "roll over the chance");
            Assert.AreEqual(1, s.Binned);
        }

        [Test]
        public void ARejection_ResetsTheStreak()
        {
            var s = New();
            s.Drop(10f, 0.9f); s.Drop(10f, 0.9f);
            Assert.AreEqual(2, s.Streak);
            s.Drop(50f, 0.0f);
            Assert.AreEqual(0, s.Streak);
        }

        [Test]
        public void FirstStreakOfThree_TheThirdItemFliesBackOnce()
        {
            var s = New();
            s.Drop(10f, 0.9f); s.Drop(10f, 0.9f);
            Assert.AreEqual(DropResult.ComedyFlyback, s.Drop(10f, 0.9f), "the joke happens even when calm");
            Assert.AreEqual(2, s.Binned, "the third item is back on the floor");
            s.Drop(10f, 0.9f); s.Drop(10f, 0.9f);
            Assert.AreEqual(DropResult.Binned, s.Drop(10f, 0.9f), "only once");
        }

        [Test]
        public void AStreakOfThree_GivesSixtySecondsOfPeace()
        {
            var s = New();
            ReachFirstRealStreak(s);
            Assert.IsTrue(s.GhostOnBreak);
            Assert.AreEqual(DropResult.Binned, s.Drop(90f, 0.0f), "no rejections during the break");
            s.Tick(61f);
            Assert.IsFalse(s.GhostOnBreak);
        }

        [Test]
        public void RoomStaysClean30Seconds_AfterAStreak_GivesTheBonusOnce()
        {
            var s = New(10);
            ReachFirstRealStreak(s);
            s.Tick(29f);
            Assert.IsFalse(s.CleanBonusThisTick);
            s.Tick(1.5f);
            Assert.IsTrue(s.CleanBonusThisTick, "-10 once");
            s.Tick(1f);
            Assert.IsFalse(s.CleanBonusThisTick);
            Assert.IsTrue(s.CleanBonusGiven);
        }

        [Test]
        public void Done_WhenEveryItemIsBinned()
        {
            var s = New(3);
            s.Drop(10f, 0.9f); s.Drop(10f, 0.9f); s.Drop(10f, 0.9f); // third flies back (joke)
            Assert.IsFalse(s.Done);
            s.Drop(10f, 0.9f);
            Assert.IsTrue(s.Done);
        }

        static void ReachFirstRealStreak(CleanupSession s)
        {
            s.Drop(10f, 0.9f); s.Drop(10f, 0.9f); s.Drop(10f, 0.9f); // joke flyback
            s.Drop(10f, 0.9f); s.Drop(10f, 0.9f); s.Drop(10f, 0.9f); // real streak of 3
        }
    }
}
