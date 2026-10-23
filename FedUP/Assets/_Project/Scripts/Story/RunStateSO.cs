using UnityEngine;

namespace FedUp.Story
{
    /// <summary>
    /// What this night has recorded so far, for the ending (CLAUDE.md "ending flags"). Reset at the start of
    /// every run by the TaskRunner. Records the boss calls and the cat visits; gate 5 adds the final choice.
    /// </summary>
    [CreateAssetMenu(menuName = "FedUp/Story/Run State", fileName = "RunState")]
    public class RunStateSO : ScriptableObject
    {
        public const int Calls = 4;
        public const int CatVisits = 3;

        [SerializeField] bool[] callHappened = new bool[Calls];
        [SerializeField] bool[] callAnswered = new bool[Calls];
        [SerializeField] bool[] catVisited = new bool[CatVisits];
        [SerializeField] bool[] catPetted = new bool[CatVisits];

        public void ResetRun()
        {
            callHappened = new bool[Calls];
            callAnswered = new bool[Calls];
            catVisited = new bool[CatVisits];
            catPetted = new bool[CatVisits];
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

        /// <param name="visit">1 to 3.</param>
        public void RecordCatVisit(int visit, bool petted)
        {
            if (visit < 1 || visit > CatVisits) return;
            catVisited[visit - 1] = true;
            catPetted[visit - 1] = petted;
        }

        public bool CatVisitHappened(int visit) => visit >= 1 && visit <= CatVisits && catVisited[visit - 1];
        public bool CatPettedOn(int visit) => visit >= 1 && visit <= CatVisits && catPetted[visit - 1];

        /// <summary>The mandatory-cat rule's input: petted on at least one visit.</summary>
        public bool CatPettedEver
        {
            get { foreach (var p in catPetted) if (p) return true; return false; }
        }
    }
}
