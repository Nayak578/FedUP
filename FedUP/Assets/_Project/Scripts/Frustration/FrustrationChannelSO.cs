using FedUp.Core;
using UnityEngine;

namespace FedUp.Frustration
{
    public readonly struct FrustrationState
    {
        public readonly float Hidden, Display, Delta;
        public readonly FrustrationBand Band;
        public readonly string Source;
        public FrustrationState(float hidden, float display, FrustrationBand band, float delta, string source)
        {
            Hidden = hidden; Display = display; Band = band; Delta = delta; Source = source;
        }
    }

    /// <summary>Broadcast after every change: the bar, atmosphere, audio and later the ghost director listen.</summary>
    [CreateAssetMenu(menuName = "FedUp/Channels/Frustration Changed", fileName = "FrustrationChanged")]
    public class FrustrationChannelSO : EventChannelSO<FrustrationState> { }
}
