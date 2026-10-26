using FedUp.Tasks.Bath;
using NUnit.Framework;

namespace FedUp.Tests
{
    /// <summary>
    /// Take a Bath (GDD Phase 1 + designer notes: "keep an eye on all clothes"): hold to relax and fill the bar
    /// while the ghost keeps snatching at the clothes on the hooks. Each snatch shows a direction prompt; press it
    /// in time and the piece goes back on its hook (and can be snatched again). Each lost item +6, a perfect bath
    /// -10. Designer change 24 Oct 2026: the clothes keep being snatched, not just three times.
    /// </summary>
    public class BathRulesTests
    {
        static readonly BathDirection[] Dirs = { BathDirection.Left, BathDirection.Up, BathDirection.Right, BathDirection.Down };

        static BathSession NewBath() => new BathSession(relaxSeconds: 10f, items: 3, firstSnatchAt: 2f, snatchEvery: 2.5f,
            directions: Dirs, windowSeconds: 1.5f);

        static void SaveEverything(BathSession b, float seconds)
        {
            for (float t = 0f; t < seconds && !b.Finished; t += 0.1f)
            {
                b.Tick(0.1f, true);
                if (b.Prompt.HasValue) b.Press(b.Prompt.Value.Direction);
            }
        }

        [Test]
        public void RelaxBar_OnlyFillsWhileHolding()
        {
            var b = NewBath();
            b.Tick(1f, relaxHeld: true);
            b.Tick(1f, relaxHeld: false);
            Assert.AreEqual(0.1f, b.Relax, 1e-4f, "1 s held out of 10");
        }

        [Test]
        public void TheFirstSnatch_ComesOnTheClock_WithADirectionPrompt()
        {
            var b = NewBath();
            b.Tick(1.9f, true);
            Assert.IsNull(b.Prompt, "nothing sliding yet");
            b.Tick(0.2f, true);
            Assert.IsNotNull(b.Prompt);
            Assert.AreEqual(BathDirection.Left, b.Prompt.Value.Direction);
            Assert.AreEqual(0, b.Prompt.Value.Item);
        }

        [Test]
        public void RightDirectionInTime_SavesIt_AndItGoesBackOnItsHook()
        {
            var b = NewBath();
            b.Tick(2.1f, true);
            Assert.IsTrue(b.Press(BathDirection.Left));
            Assert.IsNull(b.Prompt);
            Assert.AreEqual(1, b.Saved);
            Assert.AreEqual(3, b.OnHooks, "back on its hook");
        }

        [Test]
        public void WrongDirection_DoesNothing_AndTheClockKeepsRunning()
        {
            var b = NewBath();
            b.Tick(2.1f, true);
            Assert.IsFalse(b.Press(BathDirection.Down));
            Assert.IsNotNull(b.Prompt, "still sliding");
            Assert.AreEqual(0, b.Saved);
        }

        [Test]
        public void TooSlow_ThatPieceIsGone()
        {
            var b = NewBath();
            b.Tick(2.1f, true);
            b.Tick(1.6f, true); // window is 1.5 s
            Assert.IsNull(b.Prompt);
            Assert.AreEqual(1, b.Lost);
            Assert.AreEqual(2, b.OnHooks);
            Assert.AreEqual(1, b.LostThisTick, "reported once, so the game can add +6 once");
            b.Tick(0.1f, true);
            Assert.AreEqual(0, b.LostThisTick);
        }

        [Test]
        public void TheSnatchingKeepsComing_ForAsLongAsYouRelax()
        {
            var b = NewBath();
            SaveEverything(b, 30f);
            Assert.IsTrue(b.Finished);
            Assert.AreEqual(4, b.Snatches, "at 2, 4.5, 7 and 9.5 s: more snatches than clothes");
            Assert.AreEqual(4, b.Saved);
            Assert.IsTrue(b.Perfect);
        }

        [Test]
        public void SavedClothes_GetSnatchedAgain()
        {
            var b = NewBath();
            int[] items = new int[4];
            int n = 0;
            for (float t = 0f; t < 30f && !b.Finished; t += 0.1f)
            {
                b.Tick(0.1f, true);
                if (b.Prompt.HasValue) { items[n++] = b.Prompt.Value.Item; b.Press(b.Prompt.Value.Direction); }
            }
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 0 }, items, "it goes round the hooks, then starts again");
        }

        [Test]
        public void EachSnatch_AsksForTheNextDirection()
        {
            var b = NewBath();
            var seen = new BathDirection[4];
            int n = 0;
            for (float t = 0f; t < 30f && !b.Finished; t += 0.1f)
            {
                b.Tick(0.1f, true);
                if (b.Prompt.HasValue) { seen[n++] = b.Prompt.Value.Direction; b.Press(b.Prompt.Value.Direction); }
            }
            CollectionAssert.AreEqual(Dirs, seen);
        }

        [Test]
        public void NeverSnatching_LosesEverything_ThenThereIsNothingLeftToTake()
        {
            var b = NewBath();
            for (float t = 0f; t < 30f && !b.Finished; t += 0.1f) b.Tick(0.1f, true);
            Assert.IsTrue(b.Finished);
            Assert.AreEqual(3, b.Lost);
            Assert.AreEqual(0, b.OnHooks);
            Assert.AreEqual(3, b.Snatches, "nothing left to snatch after the third");
            Assert.IsFalse(b.Perfect);
        }

        [Test]
        public void ReleasingTheButton_MakesTheBathLonger_AndTheSnatchingGoesOn()
        {
            var b = NewBath();
            b.Tick(2.1f, relaxHeld: false);
            Assert.IsNotNull(b.Prompt, "snatches come on the clock, not on the relax bar");
            Assert.AreEqual(0f, b.Relax, 1e-4f);
            b.Press(b.Prompt.Value.Direction);
            for (int i = 0; i < 100; i++) { b.Tick(0.1f, false); if (b.Prompt.HasValue) b.Press(b.Prompt.Value.Direction); }
            Assert.IsFalse(b.Finished, "not relaxed yet");
            Assert.Greater(b.Snatches, 4, "the longer you take, the more it snatches");
        }

        [Test]
        public void OnceYouAreRelaxed_ItStopsSnatching()
        {
            var b = NewBath();
            SaveEverything(b, 30f);
            int snatches = b.Snatches;
            b.Tick(20f, true);
            Assert.AreEqual(snatches, b.Snatches);
        }
    }
}
