using UnityEngine;

namespace FedUp.Player
{
    /// <summary>Every number that shapes how walking feels. Edit the asset in Data/Player, not the code.</summary>
    [CreateAssetMenu(menuName = "FedUp/Player Tuning", fileName = "PlayerTuning")]
    public class PlayerTuningSO : ScriptableObject
    {
        [Header("Movement (metres per second)")]
        [Min(0)] public float walkSpeed = 2.5f;
        [Min(0)] public float sprintSpeed = 4f;
        [Tooltip("How quickly speed changes when starting/stopping. Higher = snappier.")]
        [Min(0.1f)] public float acceleration = 12f;
        public float gravity = -15f;

        [Header("Body (metres)")]
        [Min(0.5f)] public float bodyHeight = 1.75f;
        [Min(0.1f)] public float bodyRadius = 0.3f;
        [Min(0.3f)] public float eyeHeight = 1.62f;
        [Min(0)] public float stepHeight = 0.3f;

        [Header("Looking")]
        [Tooltip("Degrees turned per pixel of mouse movement.")]
        [Min(0.001f)] public float lookSensitivity = 0.08f;
        [Range(10, 89)] public float pitchLimit = 85f;

        [Header("Head-bob")]
        [Min(0)] public float headBobAmplitude = 0.035f;
        [Tooltip("Bob cycles per second at walking speed.")]
        [Min(0)] public float headBobFrequency = 1.8f;
    }
}
