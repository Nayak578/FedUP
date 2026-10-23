using FedUp.Story;
using UnityEngine;

namespace FedUp.Tasks
{
    /// <summary>A one-step objective: use one thing (e.g. "Splash water on your face"), hear his line, done.</summary>
    public class UseTask : TaskBehaviour
    {
        [SerializeField] TaskInteractable target;
        [SerializeField] string lineId;
        [SerializeField] MonologueLibrarySO lines;

        public TaskInteractable Target => target;

        void Awake()
        {
            if (target != null) target.available = false;
        }

        protected override void OnBegin()
        {
            target.available = true;
            target.Used += _ => Use();
        }

        void Use()
        {
            if (State != TaskState.Active) return;
            target.available = false;
            if (!string.IsNullOrEmpty(lineId)) lines?.Say(lineId);
            Complete();
        }

        public override void DebugComplete()
        {
            if (target != null) target.available = false;
            base.DebugComplete();
        }
    }
}
