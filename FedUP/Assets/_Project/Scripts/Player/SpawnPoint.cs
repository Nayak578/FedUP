using UnityEngine;

namespace FedUp.Player
{
    /// <summary>Where the player stands when the night begins. Blue arrow = facing direction.</summary>
    public class SpawnPoint : MonoBehaviour
    {
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, 0.35f);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.9f, transform.position + Vector3.up * 0.9f + transform.forward);
        }
    }
}
