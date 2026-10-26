using FedUp.Tasks.Phone;
using NUnit.Framework;

namespace FedUp.Tests
{
    /// <summary>Boss calls #2-4 (GDD "Phone Rings #2-4", Appendix 1): rings cost, quick answers calm, the distorted call stings.</summary>
    public class CallRulesTests
    {
        static CallSession Call(bool distorted = false)
        {
            var c = new CallSession(ringEverySeconds: 2f, ringOutSeconds: 20f, quickWithinSeconds: 10f, perRingCost: 2f,
                quickRelief: -5f, distortedCost: 5f, distorted: distorted);
            c.Start();
            return c;
        }

        static float Run(CallSession c, float seconds, float step = 0.1f)
        {
            float total = 0f;
            for (float t = 0f; t < seconds - 1e-4f; t += step) total += c.Tick(step);
            return total;
        }

        [Test]
        public void TheFirstRing_IsFree()
        {
            var c = Call();
            Assert.IsTrue(c.Ringing);
            Assert.AreEqual(1, c.Rings);
            Assert.AreEqual(0f, c.Tick(1f), 1e-4f);
        }

        [Test]
        public void EachRingAfterTheFirst_Plus2()
        {
            var c = Call();
            float cost = Run(c, 4.05f); // rings at 2 s and 4 s
            Assert.AreEqual(3, c.Rings);
            Assert.AreEqual(4f, cost, 1e-4f);
        }

        [Test]
        public void IgnoredToTheEnd_RingsOutAfter20s_TenRings()
        {
            var c = Call();
            float cost = Run(c, 21f);
            Assert.IsTrue(c.RangOut);
            Assert.IsFalse(c.Ringing);
            Assert.AreEqual(10, c.Rings);
            Assert.AreEqual(18f, cost, 1e-4f);
        }

        [Test]
        public void OneBigTick_CountsEveryRingInIt()
        {
            var c = Call();
            Assert.AreEqual(6f, c.Tick(6.5f), 1e-4f); // rings at 2, 4 and 6 s
        }

        [Test]
        public void AnsweredQuickly_Minus5_AndTheRingingStops()
        {
            var c = Call();
            Run(c, 5f);
            Assert.AreEqual(-5f, c.Answer(), 1e-4f);
            Assert.IsTrue(c.Answered);
            Assert.IsFalse(c.Ringing);
            Assert.AreEqual(0f, Run(c, 10f), 1e-4f);
        }

        [Test]
        public void AnsweredLate_NoRelief_ButItStillCounts()
        {
            var c = Call();
            Run(c, 12f);
            Assert.AreEqual(0f, c.Answer(), 1e-4f);
            Assert.IsTrue(c.Answered);
        }

        [Test]
        public void TheDistortedCall_Plus5_WhenYouHearIt()
        {
            var c = Call(distorted: true);
            Run(c, 1f);
            Assert.AreEqual(0f, c.Answer(), 1e-4f); // -5 quick, +5 distorted
            var late = Call(distorted: true);
            Run(late, 15f);
            Assert.AreEqual(5f, late.Answer(), 1e-4f);
        }

        [Test]
        public void AfterItRangOut_YouCantAnswer()
        {
            var c = Call();
            Run(c, 21f);
            Assert.AreEqual(0f, c.Answer(), 1e-4f);
            Assert.IsFalse(c.Answered);
        }

        [Test]
        public void AnsweringTwice_OnlyCountsOnce()
        {
            var c = Call();
            Assert.AreEqual(-5f, c.Answer(), 1e-4f);
            Assert.AreEqual(0f, c.Answer(), 1e-4f);
        }
    }
}
