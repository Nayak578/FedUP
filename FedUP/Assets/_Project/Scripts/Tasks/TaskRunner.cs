using System.Collections.Generic;
using FedUp.Story;
using UnityEngine;
using UnityEngine.UI;

namespace FedUp.Tasks
{
    /// <summary>Phase 1 is guided: one chore at a time, in order, with the current objective on screen.</summary>
    public class TaskRunner : MonoBehaviour
    {
        [SerializeField] List<TaskBehaviour> tasks = new List<TaskBehaviour>();
        [SerializeField] Text objectiveText;
        [SerializeField] MonologueLibrarySO lines;
        [SerializeField] RunStateSO runState;
        [Tooltip("Line said when the last task of this build is done.")]
        [SerializeField] string allDoneLineId = "gate1_done";
        [SerializeField] string allDoneObjective = "";

        public int CurrentIndex { get; private set; } = -1;
        public bool AllDone { get; private set; }
        public string ObjectiveText => objectiveText != null ? objectiveText.text : null;

        void Awake()
        {
            if (runState != null) runState.ResetRun();
        }

        void Update()
        {
            if (!AllDone && CurrentIndex >= 0 && CurrentIndex < tasks.Count) SetObjective(tasks[CurrentIndex].Objective);
        }

        /// <summary>Autopilot/tests: finish the current task immediately.</summary>
        public void DebugSkipCurrent()
        {
            if (CurrentIndex >= 0 && CurrentIndex < tasks.Count) tasks[CurrentIndex].DebugComplete();
        }

        public TaskBehaviour Current => CurrentIndex >= 0 && CurrentIndex < tasks.Count ? tasks[CurrentIndex] : null;

        void Start()
        {
            foreach (var t in tasks) if (t != null) t.Completed += OnCompleted;
            Next();
        }

        void Next()
        {
            CurrentIndex++;
            if (CurrentIndex >= tasks.Count)
            {
                AllDone = true;
                SetObjective(allDoneObjective);
                if (lines != null) lines.Say(allDoneLineId);
                return;
            }
            var task = tasks[CurrentIndex];
            SetObjective(task.Objective);
            task.Begin();
        }

        void OnCompleted(TaskBehaviour t)
        {
            if (CurrentIndex < tasks.Count && tasks[CurrentIndex] == t) Next();
        }

        void SetObjective(string s)
        {
            if (objectiveText != null) objectiveText.text = string.IsNullOrEmpty(s) ? "" : "Objective:  " + s;
        }
    }
}
