using FedUp.Story;
using NUnit.Framework;
using UnityEditor;

namespace FedUp.Tests
{
    /// <summary>Gate 5b card pass (DESIGN_LOGIC #86): every ending has a title, a line about him, and an outcome line.</summary>
    public class EndingCardsTests
    {
        static MonologueLibrarySO Lib() => AssetDatabase.LoadAssetAtPath<MonologueLibrarySO>("Assets/_Project/Data/Story/Monologue.asset");

        [Test]
        public void EveryEnding_HasATitle_ALine_AndAnOutcome()
        {
            var lib = Lib();
            foreach (Ending e in System.Enum.GetValues(typeof(Ending)))
                foreach (var part in new[] { "_title", "_line", "_outcome" })
                {
                    string id = EndingPlayer.IdFor(e) + part;
                    Assert.IsTrue(lib.TryGet(id, out var entry) && !string.IsNullOrWhiteSpace(entry.text), $"{e}: {id}");
                }
        }

        [Test]
        public void TheBurialCards_CarryTheBossesVoicemail_FromCanon()
        {
            var lib = Lib();
            lib.TryGet("ending_burial_answered_line", out var a);
            lib.TryGet("ending_burial_ignored_line", out var i);
            StringAssert.Contains("take a few days off", a.text);
            StringAssert.Contains("don't bother coming in", i.text);
        }

        [Test]
        public void MomsVoicemail_NeverSaysTheMantraWhole()
        {
            // writing rule 2: you only get pieces of it until the Burial
            var lib = Lib();
            lib.TryGet("burial_mantra", out var whole);
            foreach (var id in new[] { "mom_vm_1", "mom_vm_2", "mom_vm_3" })
            {
                Assert.IsTrue(lib.TryGet(id, out var e), id);
                StringAssert.DoesNotContain(whole.text, e.text);
            }
        }
    }
}
