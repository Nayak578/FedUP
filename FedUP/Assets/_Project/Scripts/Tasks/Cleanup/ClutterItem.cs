using UnityEngine;

namespace FedUp.Tasks.Cleanup
{
    public enum ClutterKind { Trash, Laundry }

    /// <summary>One piece of mess on the floor. What he says when he picks it up is his life, one object at a time.</summary>
    [RequireComponent(typeof(TaskInteractable))]
    public class ClutterItem : MonoBehaviour
    {
        public ClutterKind kind = ClutterKind.Trash;
        [Tooltip("Monologue line id said when picked up.")]
        public string pickupLineId;

        public TaskInteractable Pickup => GetComponent<TaskInteractable>();
        public Vector3 Home { get; private set; }
        public bool InBin { get; set; }

        void Awake() => Home = transform.position;
    }
}
