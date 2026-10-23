using FedUp.Story;
using FedUp.Tasks;
using UnityEngine;

namespace FedUp.World
{
    /// <summary>
    /// Environmental storytelling (STORY_BIBLE.md approved props): look at it, press E, hear what it means to him.
    /// Not a task, never required.
    /// </summary>
    [RequireComponent(typeof(TaskInteractable))]
    public class StoryProp : MonoBehaviour
    {
        [SerializeField] string lineId;
        [SerializeField] MonologueLibrarySO lines;

        public int TimesRead { get; private set; }

        void Awake() => GetComponent<TaskInteractable>().Used += _ => { TimesRead++; lines?.Say(lineId); };
    }
}
