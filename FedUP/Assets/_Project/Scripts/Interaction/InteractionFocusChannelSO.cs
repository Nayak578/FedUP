using FedUp.Core;
using UnityEngine;

namespace FedUp.Interaction
{
    /// <summary>What the player is currently looking at, for the HUD prompt.</summary>
    public readonly struct InteractionFocus
    {
        public readonly bool HasTarget;
        public readonly string Prompt;
        public readonly InteractionMode Mode;
        public readonly float Progress;

        public InteractionFocus(bool hasTarget, string prompt, InteractionMode mode, float progress)
        {
            HasTarget = hasTarget;
            Prompt = prompt;
            Mode = mode;
            Progress = progress;
        }

        public static readonly InteractionFocus None = new InteractionFocus(false, null, InteractionMode.Press, 0f);
    }

    [CreateAssetMenu(menuName = "FedUp/Channels/Interaction Focus", fileName = "InteractionFocus")]
    public class InteractionFocusChannelSO : EventChannelSO<InteractionFocus> { }
}
