using FedUp.Frustration;
using NUnit.Framework;

namespace FedUp.Tests
{
    /// <summary>The frustration rules from MACRO_PLAN.md D2/D3 and CLAUDE.md.</summary>
    public class FrustrationModelTests
    {
        [Test]
        public void StartsAtZero_Calm()
        {
            var m = new FrustrationModel();
            Assert.AreEqual(0f, m.Hidden);
            Assert.AreEqual(FrustrationBand.Calm, m.Band);
        }

        [Test]
        public void Apply_RaisesAndLowers_NeverBelowZero()
        {
            var m = new FrustrationModel();
            m.Apply(10f);
            Assert.AreEqual(10f, m.Hidden, 1e-4f);
            m.Apply(-5f);
            Assert.AreEqual(5f, m.Hidden, 1e-4f);
            float actual = m.Apply(-20f);
            Assert.AreEqual(0f, m.Hidden, 1e-4f, "never below zero");
            Assert.AreEqual(-5f, actual, 1e-4f, "reports the change that actually happened");
        }

        [TestCase(0f, FrustrationBand.Calm)]
        [TestCase(29.9f, FrustrationBand.Calm)]
        [TestCase(30f, FrustrationBand.Uneasy)]
        [TestCase(59.9f, FrustrationBand.Uneasy)]
        [TestCase(60f, FrustrationBand.Agitated)]
        [TestCase(89.9f, FrustrationBand.Agitated)]
        [TestCase(90f, FrustrationBand.Breaking)]
        [TestCase(99.9f, FrustrationBand.Breaking)]
        [TestCase(100f, FrustrationBand.FedUp)]
        [TestCase(180f, FrustrationBand.FedUp)]
        public void Bands_FollowTheDesignerThresholds(float value, FrustrationBand expected)
        {
            Assert.AreEqual(expected, FrustrationModel.BandFor(value));
        }

        [Test]
        public void Bar_StopsAt100_HiddenValueKeepsCountingTo200()
        {
            var m = new FrustrationModel();
            m.Apply(150f);
            Assert.AreEqual(100f, m.Display, 1e-4f, "bar is full");
            Assert.AreEqual(150f, m.Hidden, 1e-4f, "hidden value keeps counting");
            m.Apply(100f);
            Assert.AreEqual(200f, m.Hidden, 1e-4f, "hidden value caps at 200");
        }

        [Test]
        public void Overshooting_MeansCalmingDownTakesLonger()
        {
            var m = new FrustrationModel(140f);
            m.Apply(-41f);
            Assert.AreEqual(99f, m.Hidden, 1e-4f);
            Assert.AreEqual(99f, m.Display, 1e-4f);
            Assert.AreEqual(FrustrationBand.Breaking, m.Band);
        }

        [Test]
        public void PerSecondPenalty_AddsRateTimesTime()
        {
            var m = new FrustrationModel();
            m.ApplyRate(3f, 2f); // smoke alarm: +3% per second for 2 s
            Assert.AreEqual(6f, m.Hidden, 1e-4f);
        }
    }
}
