using FedUp.Qte;
using FedUp.Tasks.Toast;
using NUnit.Framework;

namespace FedUp.Tests
{
    /// <summary>Make Toast rules (GDD Task 1 + MACRO_PLAN Appendix 1) and the timing bar they use.</summary>
    public class ToastRulesTests
    {
        // ---- timing bar ----

        [Test]
        public void Marker_SweepsFromRawToBurnt_AndStopsAtTheEnd()
        {
            Assert.AreEqual(0f, TimingQte.MarkerPosition(0f, 4f), 1e-4f);
            Assert.AreEqual(0.5f, TimingQte.MarkerPosition(2f, 4f), 1e-4f);
            Assert.AreEqual(1f, TimingQte.MarkerPosition(9f, 4f), 1e-4f, "never passes the end");
        }

        [Test]
        public void CrookedMarker_StartsSlow_ThenLurches()
        {
            Assert.AreEqual(0f, TimingQte.MarkerPosition(0f, 4f, 1.3f), 1e-4f);
            Assert.AreEqual(0.40613f, TimingQte.MarkerPosition(2f, 4f, 1.3f), 1e-4f, "half the time, well under half way");
            Assert.AreEqual(1f, TimingQte.MarkerPosition(9f, 4f, 1.3f), 1e-4f, "never passes the end");
        }

        [Test]
        public void CrookedMarker_RushesThroughTheGoldenZone()
        {
            // seconds the marker spends inside 0.62..0.73 on a 2.8 s sweep
            float Window(float crook)
            {
                float inside = 0f;
                for (float t = 0f; t < 2.8f; t += 0.0005f)
                {
                    float p = TimingQte.MarkerPosition(t, 2.8f, crook);
                    if (p >= 0.62f && p <= 0.73f) inside += 0.0005f;
                }
                return inside;
            }
            Assert.Less(Window(1.3f), Window(1f), "the crooked toaster gives you less time where it matters");
            Assert.Greater(Window(1.3f), 0.2f, "but still a fair window");
        }

        [TestCase(0.40f, TimingJudgement.Early)]
        [TestCase(0.60f, TimingJudgement.Perfect)]
        [TestCase(0.70f, TimingJudgement.Perfect)]
        [TestCase(0.75f, TimingJudgement.Perfect)]
        [TestCase(0.80f, TimingJudgement.Late)]
        public void Judge_UsesTheGoldenZone(float position, TimingJudgement expected)
        {
            Assert.AreEqual(expected, TimingQte.Judge(position, 0.60f, 0.75f));
        }

        // ---- toast outcomes ----

        [Test]
        public void Perfect_IsGoldenToast_Early_Launches_Late_Burns()
        {
            Assert.AreEqual(ToastOutcome.Golden, new ToastSession().Resolve(TimingJudgement.Perfect));
            Assert.AreEqual(ToastOutcome.Launched, new ToastSession().Resolve(TimingJudgement.Early));
            Assert.AreEqual(ToastOutcome.Burnt, new ToastSession().Resolve(TimingJudgement.Late));
        }

        [Test]
        public void TwoFailuresInARow_SetOffTheSmokeAlarm()
        {
            var s = new ToastSession();
            s.Resolve(TimingJudgement.Late);
            Assert.IsFalse(s.AlarmShouldRing, "one failure is not enough");
            s.Resolve(TimingJudgement.Early);
            Assert.IsTrue(s.AlarmShouldRing, "two failures in a row");
        }

        [Test]
        public void GoldenToast_ResetsTheFailureStreak()
        {
            var s = new ToastSession();
            s.Resolve(TimingJudgement.Late);
            s.Resolve(TimingJudgement.Perfect);
            s.Resolve(TimingJudgement.Late);
            Assert.IsFalse(s.AlarmShouldRing);
        }

        [Test]
        public void SilencingTheAlarm_StartsTheCountAgain()
        {
            var s = new ToastSession();
            s.Resolve(TimingJudgement.Late);
            s.Resolve(TimingJudgement.Late);
            s.AlarmSilenced();
            Assert.IsFalse(s.AlarmShouldRing);
            s.Resolve(TimingJudgement.Late);
            Assert.IsFalse(s.AlarmShouldRing, "needs two new failures");
        }

        [TestCase(30f, false)]
        [TestCase(30.1f, true)]
        [TestCase(10f, false)]
        public void Toaster_JamsOnlyAbove30PercentFrustration(float frustration, bool jams)
        {
            Assert.AreEqual(jams, ToastSession.ShouldJam(frustration, 30f));
        }

        // ---- mash to unjam ----

        [Test]
        public void Mash_EnoughPressesInTime_Succeeds()
        {
            var m = new MashChallenge(8, 3f);
            for (int i = 0; i < 8; i++) { m.Tick(0.2f); m.Press(); }
            Assert.IsTrue(m.Succeeded);
            Assert.IsFalse(m.Failed);
        }

        [Test]
        public void Mash_TooSlow_Fails()
        {
            var m = new MashChallenge(8, 3f);
            for (int i = 0; i < 5; i++) { m.Press(); m.Tick(0.7f); }
            Assert.IsTrue(m.Failed);
            Assert.IsFalse(m.Succeeded);
        }

        [Test]
        public void Mash_ReportsProgress()
        {
            var m = new MashChallenge(8, 3f);
            m.Press(); m.Press();
            Assert.AreEqual(0.25f, m.Progress, 1e-4f);
        }
    }
}
