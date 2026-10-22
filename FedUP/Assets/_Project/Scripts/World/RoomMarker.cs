using UnityEngine;

namespace FedUp.World
{
    /// <summary>
    /// Marks the middle of a room at floor level. Used by tests (every room must be reachable)
    /// and by the autopilot tour (visited in <see cref="tourOrder"/>; 0 = skip).
    /// </summary>
    public class RoomMarker : MonoBehaviour
    {
        public string roomName = "Room";
        [Tooltip("Order in the autopilot tour. 0 = not part of the tour.")]
        public int tourOrder;

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.8f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.05f, new Vector3(0.6f, 0.1f, 0.6f));
        }
    }
}
