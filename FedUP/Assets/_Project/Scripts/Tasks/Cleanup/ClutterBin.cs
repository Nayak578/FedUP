using UnityEngine;

namespace FedUp.Tasks.Cleanup
{
    /// <summary>The trash bin or the laundry basket.</summary>
    [RequireComponent(typeof(TaskInteractable))]
    public class ClutterBin : MonoBehaviour
    {
        public ClutterKind accepts = ClutterKind.Trash;
        public TaskInteractable Target => GetComponent<TaskInteractable>();
    }
}
