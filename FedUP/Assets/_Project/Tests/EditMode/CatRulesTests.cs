using FedUp.Tasks.Cat;
using NUnit.Framework;

namespace FedUp.Tests
{
    /// <summary>Cat visit rules from the GDD (Phase 1 Cat Visit #1 + Task 8 "Pet the Cat").</summary>
    public class CatRulesTests
    {
        static CatVisitSession New() => new CatVisitSession(hissAbove: 60f, hissChance: 0.5f, retryCooldownSeconds: 30f,
            patienceSeconds: 20f, petAmount: -20f, retryPetAmount: -10f);

        [Test]
        public void Petting_ACalmCat_Purrs_Minus20()
        {
            var cat = New();
            Assert.AreEqual(PetResult.Purred, cat.Pet(30f, 0f));
            Assert.IsTrue(cat.Petted);
            Assert.IsTrue(cat.Done);
            Assert.AreEqual(-20f, cat.PetAmount, 1e-4f);
        }

        [Test]
        public void Above60Percent_HalfTheTime_ItHisses()
        {
            var a = New();
            Assert.AreEqual(PetResult.Hissed, a.Pet(65f, 0.4f));
            Assert.IsFalse(a.Petted);
            Assert.IsFalse(a.Done);
            var b = New();
            Assert.AreEqual(PetResult.Purred, b.Pet(65f, 0.6f));
        }

        [Test]
        public void At60Percent_ItNeverHisses()
        {
            Assert.AreEqual(PetResult.Purred, New().Pet(60f, 0f));
        }

        [Test]
        public void AfterAHiss_YouWait30Seconds_AndTheRetryCalmsLess()
        {
            var cat = New();
            cat.Pet(70f, 0f);
            Assert.AreEqual(PetResult.TooSoon, cat.Pet(70f, 0.9f));
            cat.Tick(29.9f);
            Assert.AreEqual(PetResult.TooSoon, cat.Pet(70f, 0.9f));
            cat.Tick(0.2f);
            Assert.AreEqual(PetResult.Purred, cat.Pet(70f, 0.9f));
            Assert.AreEqual(-10f, cat.PetAmount, 1e-4f, "petted on retry after a hiss");
        }

        [Test]
        public void Ignored_ItLeaves_AfterItsPatience()
        {
            var cat = New();
            cat.Tick(19.9f);
            Assert.IsFalse(cat.Left);
            cat.Tick(0.2f);
            Assert.IsTrue(cat.Left);
            Assert.IsTrue(cat.LeftThisTick);
            Assert.IsTrue(cat.Done);
            Assert.IsFalse(cat.Petted, "ignored is recorded as not petted");
            cat.Tick(0.1f);
            Assert.IsFalse(cat.LeftThisTick, "leaves once");
            Assert.AreEqual(PetResult.Gone, cat.Pet(10f, 0.9f));
        }

        [Test]
        public void AHiss_DoesNotEatIntoItsPatience()
        {
            var cat = New();
            cat.Tick(15f);
            cat.Pet(70f, 0f);
            cat.Tick(30f + 19f);
            Assert.IsFalse(cat.Left, "it waits out the cooldown, then gives you the full 20 s again");
            cat.Tick(2f);
            Assert.IsTrue(cat.Left);
        }

        [Test]
        public void APettedCat_NeverLeavesAsIgnored()
        {
            var cat = New();
            cat.Pet(10f, 0.9f);
            cat.Tick(100f);
            Assert.IsFalse(cat.Left);
            Assert.AreEqual(PetResult.Gone, cat.Pet(10f, 0.9f), "one pet per visit");
        }
    }
}
