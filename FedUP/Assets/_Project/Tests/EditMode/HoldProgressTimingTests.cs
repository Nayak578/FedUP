using FedUp.Interaction;
using NUnit.Framework;

namespace FedUp.Tests
{
    /// <summary>Press/hold timing rules for every interaction in the game.</summary>
    public class HoldProgressTimingTests
    {
        [Test]
        public void NoTimeHeld_IsNotComplete()
        {
            var p = new HoldProgress(1.5f);
            Assert.IsFalse(p.Completed);
            Assert.AreEqual(0f, p.Normalized, 1e-5f);
        }

        [Test]
        public void HoldingPartWay_ReportsProgress()
        {
            var p = new HoldProgress(2f).Tick(true, 0.5f);
            Assert.IsFalse(p.Completed);
            Assert.AreEqual(0.25f, p.Normalized, 1e-5f);
        }

        [Test]
        public void HoldingFullTime_CompletesExactlyOnce()
        {
            var p = new HoldProgress(1f).Tick(true, 0.6f).Tick(true, 0.6f);
            Assert.IsTrue(p.Completed);
            Assert.IsTrue(p.JustCompleted, "the tick that finishes the hold must report JustCompleted");

            p = p.Tick(true, 0.1f);
            Assert.IsTrue(p.Completed);
            Assert.IsFalse(p.JustCompleted, "keeping the key down must not fire the interaction again");
        }

        [Test]
        public void LettingGo_ResetsToZero()
        {
            var p = new HoldProgress(1f).Tick(true, 0.7f).Tick(false, 0.016f);
            Assert.IsFalse(p.Completed);
            Assert.AreEqual(0f, p.Normalized, 1e-5f);
        }

        [Test]
        public void HugeTimeStep_DoesNotOvershoot()
        {
            var p = new HoldProgress(1f).Tick(true, 10f);
            Assert.IsTrue(p.Completed);
            Assert.AreEqual(1f, p.Normalized, 1e-5f);
        }

        [Test]
        public void PressMode_ZeroSecondHold_CompletesImmediately()
        {
            var p = new HoldProgress(0f).Tick(true, 0f);
            Assert.IsTrue(p.Completed);
            Assert.IsTrue(p.JustCompleted);
            Assert.AreEqual(1f, p.Normalized, 1e-5f);
        }

        [Test]
        public void AfterRelease_CanCompleteAgain()
        {
            var p = new HoldProgress(0.5f).Tick(true, 0.6f).Tick(false, 0.016f).Tick(true, 0.6f);
            Assert.IsTrue(p.JustCompleted);
        }
    }
}
