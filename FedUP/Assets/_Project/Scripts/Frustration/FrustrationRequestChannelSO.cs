using FedUp.Core;
using UnityEngine;

namespace FedUp.Frustration
{
    public readonly struct FrustrationRequest
    {
        public readonly FrustrationEventSO Event;
        public readonly float Scale;
        public FrustrationRequest(FrustrationEventSO ev, float scale) { Event = ev; Scale = scale; }
    }

    /// <summary>Anything that annoys or calms the player sends its event here. Only FrustrationSystem listens.</summary>
    [CreateAssetMenu(menuName = "FedUp/Channels/Frustration Requests", fileName = "FrustrationRequests")]
    public class FrustrationRequestChannelSO : EventChannelSO<FrustrationRequest>
    {
        /// <summary>For per-second events pass the frame time as <paramref name="scale"/>.</summary>
        public void Request(FrustrationEventSO ev, float scale = 1f) => Raise(new FrustrationRequest(ev, scale));
    }
}
