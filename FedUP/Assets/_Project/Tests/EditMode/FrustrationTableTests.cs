using FedUp.Frustration;
using NUnit.Framework;
using UnityEditor;

namespace FedUp.Tests
{
    /// <summary>
    /// Guards the designer's frustration table (MACRO_PLAN.md Appendix 1): if someone edits a data asset by
    /// accident, this fails. Changing a value on purpose needs the designer's sign-off and an update here.
    /// </summary>
    public class FrustrationTableTests
    {
        static FrustrationEventSO Load(string name) =>
            AssetDatabase.LoadAssetAtPath<FrustrationEventSO>($"Assets/_Project/Data/Frustration/{name}.asset");

        [TestCase("FE_Toast_FailedOrBurnt", 10f, false)]
        [TestCase("FE_Toast_Success", -5f, false)]
        [TestCase("FE_Toast_EatenByGhost", 10f, false)]
        [TestCase("FE_SmokeAlarm_PerSecond", 3f, true)]
        public void Gate1Values_MatchTheDesignerTable(string asset, float amount, bool perSecond)
        {
            var e = Load(asset);
            Assert.IsNotNull(e, asset);
            Assert.AreEqual(amount, e.amount, 1e-4f, asset);
            Assert.AreEqual(perSecond, e.perSecond, asset);
        }
    }
}
