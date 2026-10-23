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

        [TestCase("FE_Bath_ItemLost", 6f)]
        [TestCase("FE_Bath_Success", -10f)]
        [TestCase("FE_Cleanup_Flyback", 6f)]
        [TestCase("FE_Cleanup_Giggle", 2f)]
        [TestCase("FE_Cleanup_RoomClean", -10f)]
        [TestCase("FE_Phone1_Ignored", 2f)]
        [TestCase("FE_Phone1_Answered", -5f)]
        [TestCase("FE_TV_StaticOrHorror", 7f)]
        [TestCase("FE_TV_Infomercial", 5f)]
        [TestCase("FE_TV_LostRemote", 10f)]
        [TestCase("FE_TV_RemoteStolen", 5f)]
        [TestCase("FE_TV_Calm", -10f)]
        [TestCase("FE_Cat_Petted", -20f)]
        [TestCase("FE_Cat_PettedAfterHiss", -10f)]
        public void Gate2Values_MatchTheDesignerTable(string asset, float amount)
        {
            var e = Load(asset);
            Assert.IsNotNull(e, asset);
            Assert.AreEqual(amount, e.amount, 1e-4f, asset);
            Assert.IsFalse(e.perSecond, asset);
        }
    }
}
