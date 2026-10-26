using System.Collections;
using FedUp.Interaction;
using FedUp.Player;
using FedUp.Tasks;
using UnityEngine;

namespace FedUp.Story
{
    /// <summary>
    /// The secret ending (GDD "Secret Branch: Immediate Sleep"; DESIGN_LOGIC #75). Before your first chore is done, his
    /// bed upstairs works: lie down and the night is over. "Maybe ignoring it all was the real answer." Then, at random,
    /// the loop or the ghost wins. The cat rule doesn't apply (the cat hasn't come yet).
    /// </summary>
    public class SecretSleep : MonoBehaviour
    {
        [SerializeField] TaskInteractable bed;
        [SerializeField] TaskRunner runner;
        [SerializeField] Transform lieSpot;
        [SerializeField] float lyingEyeHeight = 0.6f;
        [SerializeField] FirstPersonMotor motor;
        [SerializeField] MonologueLibrarySO lines;
        [SerializeField] EndingPlayer endings;

        public bool Open { get; private set; }
        public bool Taken { get; private set; }
        /// <summary>Tests/autopilot: fix the coin.</summary>
        public float? ForcedRoll { get; set; }

        void Start()
        {
            bed.Used += _ => { if (Open && !Taken) StartCoroutine(Sleep()); };
        }

        void Update()
        {
            if (Taken) return;
            bool open = runner != null && runner.CurrentIndex == 0 && runner.Current != null && runner.Current.State == TaskState.Active;
            if (open == Open) return;
            Open = open;
            if (open)
            {
                bed.prompt = "Go to bed";
                bed.mode = InteractionMode.Hold;
                bed.holdSeconds = 1.5f;
            }
            bed.available = open;
            if (!open) enabled = false; // the first chore is done: the secret is gone for good
        }

        IEnumerator Sleep()
        {
            Taken = true;
            bed.available = false;
            motor.Locked = true;
            motor.Teleport(lieSpot.position, lieSpot.eulerAngles.y);
            motor.EyeHeightOverride = lyingEyeHeight;
            lines?.Say("secret_line");
            yield return new WaitForSeconds(3.5f);
            endings.Play(EndingResolver.ResolveSecret(ForcedRoll ?? Random.value));
        }
    }
}
