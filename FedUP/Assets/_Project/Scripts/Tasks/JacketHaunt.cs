using System.Collections;
using UnityEngine;

namespace FedUp.Tasks
{
    /// <summary>
    /// GDD: "As the player moves away, their jacket subtly twitches on the hook or falls off randomly — no sound cue.
    /// It resets if picked up." Only happens when you're away and not looking.
    /// </summary>
    public class JacketHaunt : MonoBehaviour
    {
        [SerializeField] Transform viewer;
        [SerializeField, Min(0.5f)] float checkEverySeconds = 3f;
        [SerializeField, Min(0f)] float minDistance = 4f;
        [SerializeField, Range(0f, 1f)] float twitchChance = 0.6f;
        [SerializeField, Range(0f, 1f)] float fallChance = 0.25f;

        public bool Armed { get; private set; }
        public bool Fallen { get; private set; }
        public int Twitches { get; private set; }

        TaskInteractable hook;
        Vector3 hungPos;
        Quaternion hungRot;
        float timer;

        public void Arm(TaskInteractable coatHook)
        {
            hook = coatHook;
            hungPos = transform.position;
            hungRot = transform.rotation;
            Armed = true;
            hook.Used += OnHookUsed;
        }

        void Update()
        {
            if (!Armed || Fallen || viewer == null) return;
            timer += Time.deltaTime;
            if (timer < checkEverySeconds) return;
            timer = 0f;
            Vector3 toJacket = transform.position - viewer.position;
            bool looking = Vector3.Angle(viewer.forward, toJacket) < 60f;
            if (looking || toJacket.magnitude < minDistance) return;
            float roll = Random.value;
            if (roll < fallChance) Fall();
            else if (roll < fallChance + twitchChance) StartCoroutine(Twitch());
        }

        IEnumerator Twitch()
        {
            Twitches++;
            for (float t = 0; t < 0.45f; t += Time.deltaTime)
            {
                transform.rotation = hungRot * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 60f) * 7f * (1f - t / 0.45f));
                yield return null;
            }
            transform.rotation = hungRot;
        }

        /// <summary>Drops the jacket to the floor below the hook. Public so tests can force it.</summary>
        public void Fall()
        {
            Fallen = true;
            Vector3 floor = hungPos;
            if (Physics.Raycast(hungPos, Vector3.down, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore)) floor = hit.point;
            transform.SetPositionAndRotation(floor + Vector3.up * 0.06f + Vector3.ProjectOnPlane(-transform.forward, Vector3.up) * 0.25f,
                                             Quaternion.Euler(90f, hungRot.eulerAngles.y, 0f));
            hook.prompt = "Hang the jacket back up";
            hook.available = true;
        }

        void OnHookUsed(TaskInteractable t)
        {
            if (!Fallen) return;
            Fallen = false;
            transform.SetPositionAndRotation(hungPos, hungRot);
            hook.available = false;
        }
    }
}
