using FedUp.Audio;
using FedUp.Story;
using FedUp.Tasks;
using UnityEngine;

namespace FedUp.World
{
    /// <summary>
    /// The west-porch front door. You open it to come home; once you're inside it swings shut and locks
    /// (the night happens inside). The hinge is the parent transform.
    /// </summary>
    public class FrontDoor : MonoBehaviour
    {
        [SerializeField] TaskInteractable handle;
        [SerializeField] Transform hinge;
        [SerializeField] float openAngle = -100f;
        [SerializeField] float swingSpeed = 140f;
        [SerializeField] MonologueLibrarySO lines;

        public bool IsOpen { get; private set; }
        public bool IsLocked { get; private set; }

        float angle, target;
        AudioSource sfx;

        void Awake()
        {
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 1f;
            if (handle != null)
            {
                handle.prompt = "Open the door";
                handle.Used += _ => Open();
            }
        }

        public void Open()
        {
            if (IsLocked || IsOpen) return;
            IsOpen = true;
            target = openAngle;
            handle.available = false;
            sfx.PlayOneShot(PlaceholderTones.Get("click"));
            lines?.Say("door_home");
        }

        /// <summary>Called by the entry zone once the player is inside.</summary>
        public void CloseAndLock()
        {
            if (IsLocked) return;
            IsOpen = false;
            IsLocked = true;
            target = 0f;
            handle.available = false;
        }

        void Update()
        {
            if (Mathf.Approximately(angle, target)) return;
            angle = Mathf.MoveTowards(angle, target, swingSpeed * Time.deltaTime);
            hinge.localRotation = Quaternion.Euler(0f, angle, 0f);
            if (Mathf.Approximately(angle, 0f) && IsLocked) sfx.PlayOneShot(PlaceholderTones.Get("thunk"));
        }
    }
}
