using UnityEngine;

namespace FedUp.Story
{
    /// <summary>
    /// What this night has recorded so far, for the ending (CLAUDE.md "ending flags"). Reset at the start of
    /// every run by the TaskRunner. Gate 2a records phone call #1; later gates add the cat and the final choice.
    /// </summary>
    [CreateAssetMenu(menuName = "FedUp/Story/Run State", fileName = "RunState")]
    public class RunStateSO : ScriptableObject
    {
        public const int Calls = 4;

        [SerializeField] bool[] callHappened = new bool[Calls];
        [SerializeField] bool[] callAnswered = new bool[Calls];

        public void ResetRun()
        {
            callHappened = new bool[Calls];
            callAnswered = new bool[Calls];
        }

        /// <param name="call">1 to 4.</param>
        public void RecordCall(int call, bool answered)
        {
            if (call < 1 || call > Calls) return;
            callHappened[call - 1] = true;
            callAnswered[call - 1] = answered;
        }

        public bool CallHappened(int call) => call >= 1 && call <= Calls && callHappened[call - 1];
        public bool CallAnswered(int call) => call >= 1 && call <= Calls && callAnswered[call - 1];

        public int CallsAnswered
        {
            get { int n = 0; foreach (var a in callAnswered) if (a) n++; return n; }
        }
    }
}
