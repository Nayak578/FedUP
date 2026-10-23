using UnityEngine;

namespace FedUp.Ghost
{
    /// <summary>Ghost director timing. Edit Data/Ghost/GhostDirectorTuning.asset to tune how often the house scares.</summary>
    [CreateAssetMenu(menuName = "FedUp/Ghost/Director Tuning", fileName = "GhostDirectorTuning")]
    public class GhostDirectorTuningSO : ScriptableObject
    {
        [Tooltip("No scares for this long after a phase starts, so the change itself can land.")]
        [Min(0f)] public float graceSeconds = 6f;
        [Tooltip("Quiet after a scare when you're calm (0%).")]
        [Min(0f)] public float quietCalmSeconds = 10f;
        [Tooltip("Quiet after a scare when you're fed up (100%+). The angrier you are, the sooner the next one.")]
        [Min(0f)] public float quietFedUpSeconds = 5f;
        [Tooltip("A big scare buys this much longer quiet: the house backs off after a big moment.")]
        [Min(1f)] public float bigMultiplier = 1.3f;
    }
}
