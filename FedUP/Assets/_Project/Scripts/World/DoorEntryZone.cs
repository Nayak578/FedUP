using FedUp.Player;
using UnityEngine;

namespace FedUp.World
{
    /// <summary>Invisible zone just inside the front door: stepping in closes and locks the door behind you.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public class DoorEntryZone : MonoBehaviour
    {
        [SerializeField] FrontDoor door;

        public bool PlayerInside { get; private set; }

        void Reset() => GetComponent<BoxCollider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<FirstPersonMotor>() == null) return;
            PlayerInside = true;
            if (door != null && door.IsOpen) door.CloseAndLock();
        }
    }
}
