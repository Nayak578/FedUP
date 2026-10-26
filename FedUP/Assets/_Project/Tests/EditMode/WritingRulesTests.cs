using FedUp.Story;
using NUnit.Framework;
using UnityEditor;

namespace FedUp.Tests
{
    /// <summary>
    /// The designer's writing rules (CLAUDE.md, 26 Oct): every line names the rule it plays, and the Mr. Robot touch is
    /// there (at least two lines that talk to the player).
    /// </summary>
    public class WritingRulesTests
    {
        static MonologueLibrarySO Lib() => AssetDatabase.LoadAssetAtPath<MonologueLibrarySO>("Assets/_Project/Data/Story/Monologue.asset");

        [Test]
        public void EveryLine_NamesTheRuleItPlays()
        {
            var untagged = new System.Collections.Generic.List<string>();
            foreach (var l in Lib().lines) if (l.rule == MonologueLibrarySO.LineRule.Unset) untagged.Add(l.id);
            Assert.IsEmpty(untagged, "lines with no writing rule: " + string.Join(", ", untagged));
        }

        [Test]
        public void TheMrRobotTouch_AtLeastTwoLinesToThePlayer()
        {
            int n = 0;
            foreach (var l in Lib().lines) if (l.rule == MonologueLibrarySO.LineRule.ToThePlayer) n++;
            Assert.GreaterOrEqual(n, 2);
        }
    }
}
