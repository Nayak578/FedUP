using System;
using UnityEngine;

namespace FedUp.Tasks
{
    public enum TaskState { Waiting, Active, Done }

    /// <summary>Base for every chore in the night. The TaskRunner starts them in order.</summary>
    public abstract class TaskBehaviour : MonoBehaviour
    {
        [Tooltip("Shown on screen while this task is active.")]
        public string objective = "Do the thing";

        /// <summary>The on-screen objective; tasks can add live detail (e.g. "2/4").</summary>
        public virtual string Objective => objective;

        public TaskState State { get; private set; } = TaskState.Waiting;
        public event Action<TaskBehaviour> Completed;

        public void Begin()
        {
            if (State != TaskState.Waiting) return;
            State = TaskState.Active;
            OnBegin();
        }

        protected abstract void OnBegin();

        /// <summary>Autopilot/tests: skip straight past this task.</summary>
        public virtual void DebugComplete() => Complete();

        protected void Complete()
        {
            if (State == TaskState.Done) return;
            State = TaskState.Done;
            Completed?.Invoke(this);
        }
    }
}
