using System;
using FedUp.Interaction;
using UnityEngine;

namespace FedUp.Tasks
{
    /// <summary>
    /// A generic "look at it and press/hold E" object used by tasks (coat hook, toaster, smoke alarm…).
    /// The task listens to <see cref="Used"/> and decides what happens; the prompt can change as the task moves on.
    /// </summary>
    public class TaskInteractable : MonoBehaviour, IInteractable
    {
        [Tooltip("Stable name the task, autopilot and tests use to find this object.")]
        public string id;
        public string prompt = "Use";
        public InteractionMode mode = InteractionMode.Press;
        [Min(0f)] public float holdSeconds = 1f;
        public bool available = true;

        public event Action<TaskInteractable> Used;

        public string Prompt => prompt;
        public InteractionMode Mode => mode;
        public float HoldSeconds => holdSeconds;
        public bool CanInteract(Interactor who) => available && isActiveAndEnabled;
        public void Interact(Interactor who) => Used?.Invoke(this);

        public static TaskInteractable Find(string id)
        {
            foreach (var t in FindObjectsByType<TaskInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.id == id) return t;
            return null;
        }
    }
}
