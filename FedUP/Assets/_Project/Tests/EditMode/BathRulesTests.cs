using FedUp.Tasks.Bath;
using NUnit.Framework;

namespace FedUp.Tests
{
    /// <summary>
    /// Take a Bath (GDD Phase 1 + designer notes): hold to relax and fill the bar while clothes slide off the hooks;
    /// a direction prompt appears for each one and you have a moment to snatch it back. Each lost item +6,
    /// a perfect bath -10.
    /// </summary>
    public class BathRulesTests
    {
        static BathSession NewBath() => new BathSession(relaxSeconds: 10f, slideTimes: new[] { 2f, 5f, 8f },
            directions: new[] { BathDirection.Left, BathDirection.Up, BathDirection.Right }, windowSeconds: 1.5f);

        [Test]
        public void RelaxBar_OnlyFillsWhileHolding()
        {
            var b = NewBath();
            b.Tick(1f, relaxHeld: true);
            b.Tick(1f, relaxHeld: false);
            Assert.AreEqual(0.1f, b.Relax, 1e-4f, "1 s held out of 10");
        }

        [Test]
        public void Clothes_StartSliding_AtTheirTimes_WithADirectionPrompt()
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
        public void RightDirectionInTime_SavesTheItem()
        {
            var b = NewBath();
            b.Tick(2.1f, true);
            Assert.IsTrue(b.Press(BathDirection.Left));
            Assert.IsNull(b.Prompt);
            Assert.AreEqual(1, b.Saved);
            Assert.AreEqual(0, b.Lost);
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
        public void TooSlow_TheItemIsLost()
        {
            var b = NewBath();
            b.Tick(2.1f, true);
            b.Tick(1.6f, true); // window is 1.5 s
            Assert.IsNull(b.Prompt);
            Assert.AreEqual(1, b.Lost);
            Assert.AreEqual(1, b.LostThisTick, "reported once, so the game can add +6 once");
            b.Tick(0.1f, true);
            Assert.AreEqual(0, b.LostThisTick);
        }

        [Test]
        public void Bath_EndsWhenRelaxed_AndNothingIsSliding()
        {
            var b = NewBath();
            float t = 0f;
            while (!b.Finished && t < 30f)
            {
                b.Tick(0.1f, true);
                if (b.Prompt.HasValue) b.Press(b.Prompt.Value.Direction);
                t += 0.1f;
            }
            Assert.IsTrue(b.Finished);
            Assert.AreEqual(3, b.Saved);
            Assert.IsTrue(b.Perfect, "nothing lost");
        }

        [Test]
        public void LosingAnything_MeansNotPerfect()
        {
            var b = NewBath();
            float t = 0f;
            while (!b.Finished && t < 30f) { b.Tick(0.1f, true); t += 0.1f; } // never snatch
            Assert.IsTrue(b.Finished);
            Assert.AreEqual(3, b.Lost);
            Assert.IsFalse(b.Perfect);
        }

        [Test]
        public void ReleasingTheButton_MakesTheBathLonger_ButClothesStillSlideOnTime()
        {
            var b = NewBath();
            b.Tick(2.1f, relaxHeld: false);
            Assert.IsNotNull(b.Prompt, "clothes slide on the clock, not on the relax bar");
            Assert.AreEqual(0f, b.Relax, 1e-4f);
        }
    }
}
