using UnityEngine;

namespace FedUp.Tasks
{
    /// <summary>
    /// Starts side tasks (outside the one-at-a-time order) once another task is done: the cat waiting on its perch
    /// while the night carries on, instead of the night stopping for the cat.
    /// </summary>
    public class BeginAfter : MonoBehaviour
    {
        [SerializeField] TaskBehaviour after;
        [SerializeField] TaskBehaviour[] begin = new TaskBehaviour[0];
        [SerializeField, Min(0f)] float delaySeconds = 1.5f;

        float doneAt = -1f;

        void Update()
        {
            if (after == null || after.State != TaskState.Done) return;
            if (doneAt < 0f) doneAt = Time.time;
            if (Time.time - doneAt < delaySeconds) return;
            foreach (var t in begin) if (t != null && t.State == TaskState.Waiting) t.Begin();
            enabled = false;
        }
    }
}
