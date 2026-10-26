using FedUp.Ghost;
using NUnit.Framework;

namespace FedUp.Tests
{
    /// <summary>Ghost director v1: scares fire once, in their phase, after the grace, and never during the quiet.</summary>
    public class DirectorRulesTests
    {
        static DirectorRules New() => new DirectorRules(graceSeconds: 6f, quietCalmSeconds: 14f, quietFedUpSeconds: 7f, bigMultiplier: 1.5f);

        static DirectorRules AwakeAt(float now)
        {
            var d = New();
            d.EnterPhase(2, now);
            return d;
        }

        [Test]
        public void InTheEvening_Phase2ScaresAreBlocked()
        {
            var d = New();
            d.EnterPhase(1, 0f);
            Assert.AreEqual(ScareDecision.WrongPhase, d.Request("mirror", 2, ScareSize.Big, 100f, 0f));
        }

        [Test]
        public void AfterWaking_ThereIsAGraceBeforeTheFirstScare()
        {
            var d = AwakeAt(100f);
            Assert.AreEqual(ScareDecision.Grace, d.Request("steps", 2, ScareSize.Small, 105.9f, 0f));
            Assert.AreEqual(ScareDecision.Allowed, d.Request("steps", 2, ScareSize.Small, 106.1f, 0f));
        }

        [Test]
        public void AScare_FiresOnlyOnce()
        {
            var d = AwakeAt(0f);
            Assert.AreEqual(ScareDecision.Allowed, d.Request("steps", 2, ScareSize.Small, 10f, 0f));
            Assert.IsTrue(d.HasFired("steps"));
            Assert.AreEqual(ScareDecision.AlreadyFired, d.Request("steps", 2, ScareSize.Small, 100f, 0f));
            Assert.AreEqual(1, d.Fired);
        }

        [Test]
        public void AfterAScare_TheHouseGoesQuiet()
        {
            var d = AwakeAt(0f);
            d.Request("steps", 2, ScareSize.Small, 10f, 0f);
            Assert.AreEqual(24f, d.QuietUntil, 1e-4f, "14 s of quiet when calm");
            Assert.AreEqual(ScareDecision.Quiet, d.Request("mirror", 2, ScareSize.Big, 23.9f, 0f));
            Assert.AreEqual(ScareDecision.Allowed, d.Request("mirror", 2, ScareSize.Big, 24.1f, 0f));
        }

        [TestCase(0f, 14f)]
        [TestCase(50f, 10.5f)]
        [TestCase(100f, 7f)]
        [TestCase(180f, 7f)]
        public void TheAngrierYouAre_TheShorterTheQuiet(float frustration, float expected)
        {
            Assert.AreEqual(expected, New().QuietAfter(ScareSize.Small, frustration), 1e-4f);
        }

        [Test]
        public void ABigScare_BuysALongerQuiet()
        {
            var d = New();
            Assert.AreEqual(21f, d.QuietAfter(ScareSize.Big, 0f), 1e-4f);
            Assert.AreEqual(10.5f, d.QuietAfter(ScareSize.Big, 100f), 1e-4f);
        }

        [Test]
        public void ADeniedScare_IsNotUsedUp()
        {
            var d = AwakeAt(0f);
            d.Request("steps", 2, ScareSize.Small, 10f, 0f);
            Assert.AreEqual(ScareDecision.Quiet, d.Request("door", 2, ScareSize.Big, 12f, 0f));
            Assert.IsFalse(d.HasFired("door"));
            Assert.AreEqual(ScareDecision.Allowed, d.Request("door", 2, ScareSize.Big, 30f, 0f));
        }

        [Test]
        public void AngrierWhenItFired_MeansTheNextScareComesSooner()
        {
            var d = AwakeAt(0f);
            d.Request("steps", 2, ScareSize.Small, 10f, 100f);
            Assert.AreEqual(ScareDecision.Allowed, d.Request("mirror", 2, ScareSize.Big, 17.1f, 0f));
        }

        [Test]
        public void ChangingPhase_StartsANewGrace()
        {
            var d = AwakeAt(0f);
            d.EnterPhase(3, 50f);
            Assert.AreEqual(3, d.Phase);
            Assert.AreEqual(ScareDecision.Grace, d.Request("door", 2, ScareSize.Big, 52f, 0f));
            Assert.AreEqual(ScareDecision.Allowed, d.Request("door", 2, ScareSize.Big, 56.5f, 0f), "phase 2 scares still allowed in phase 3");
        }

        [Test]
        public void PettingTheCat_HushesTheHouse_ForAWhile()
        {
            // GDD Cat Visit #2-3: "temporarily pauses ghost activity"
            var d = AwakeAt(0f);
            d.Hush(40f, 20f);
            Assert.AreEqual(ScareDecision.Quiet, d.Request("door", 2, ScareSize.Big, 55f, 0f));
            Assert.AreEqual(ScareDecision.Allowed, d.Request("door", 2, ScareSize.Big, 60.5f, 0f));
        }

        [Test]
        public void AHush_NeverShortensAQuietAlreadyRunning()
        {
            var d = AwakeAt(0f);
            d.Request("steps", 2, ScareSize.Big, 10f, 0f); // quiet until 23
            d.Hush(11f, 2f);
            Assert.AreEqual(ScareDecision.Quiet, d.Request("door", 2, ScareSize.Big, 20f, 0f));
        }
    }
}
