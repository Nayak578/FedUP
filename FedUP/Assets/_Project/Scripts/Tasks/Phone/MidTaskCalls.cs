using FedUp.Tasks.Appliances;
using UnityEngine;

namespace FedUp.Tasks.Phone
{
    /// <summary>
    /// Calls #3 and #4 come mid-task (GDD "Answer the Phone": "calls may now come mid-task"; STORY_FLOW: "mid-chaos").
    /// #3 rings while the first appliance is running, #4 while the last one is. They aren't in the task order: they
    /// ring over whatever you're doing, so answering costs you the appliance and saving the appliance costs the call.
    /// One phone, so #4 waits until #3 is over. If the chaos ends first, any call still to come rings anyway: every
    /// night has four calls.
    /// </summary>
    public class MidTaskCalls : MonoBehaviour
    {
        [SerializeField] ApplianceChaosTask chaos;
        [SerializeField] PhoneCallTask call3;
        [SerializeField, Tooltip("Which appliance (0 = first) call #3 rings over.")] int call3During = 0;
        [SerializeField] PhoneCallTask call4;
        [SerializeField, Tooltip("Which appliance call #4 rings over.")] int call4During = 2;

        void Update()
        {
            if (chaos == null || chaos.State == TaskState.Waiting) return;
            bool chaosOver = chaos.State == TaskState.Done;
            if (call3 != null && call3.State == TaskState.Waiting && (chaosOver || (chaos.Session != null && chaos.Index >= call3During)))
                call3.Begin();
            bool phoneFree = call3 == null || call3.State == TaskState.Done;
            if (call4 != null && call4.State == TaskState.Waiting && phoneFree && (chaosOver || (chaos.Session != null && chaos.Index >= call4During)))
                call4.Begin();
        }
    }
}
