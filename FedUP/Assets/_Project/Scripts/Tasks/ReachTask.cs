using FedUp.Story;
using UnityEngine;

namespace FedUp.Tasks
{
    /// <summary>A one-step objective: get to a place (e.g. "Go back to the living room").</summary>
    public class ReachTask : TaskBehaviour
    {
        [SerializeField] Transform destination;
        [SerializeField, Min(0.3f)] float radius = 2.5f;
        [SerializeField] Transform player;
        [SerializeField] string lineId;
        [SerializeField] MonologueLibrarySO lines;

        protected override void OnBegin() { }

        void Update()
        {
            if (State != TaskState.Active) return;
            Vector3 d = player.position - destination.position; d.y = 0f;
            if (d.magnitude > radius) return;
            if (!string.IsNullOrEmpty(lineId)) lines?.Say(lineId);
            Complete();
        }
    }
}
