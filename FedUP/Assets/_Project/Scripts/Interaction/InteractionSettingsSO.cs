using UnityEngine;

namespace FedUp.Interaction
{
    [CreateAssetMenu(menuName = "FedUp/Interaction Settings", fileName = "InteractionSettings")]
    public class InteractionSettingsSO : ScriptableObject
    {
        [Tooltip("How far the player can reach, in metres.")]
        [Min(0.5f)] public float reach = 2.2f;
        [Tooltip("Layers the look-ray can hit. Walls must be included so you can't use things through them.")]
        public LayerMask rayMask = ~0;
        [Tooltip("Key name shown in the prompt.")]
        public string keyLabel = "E";
    }
}
