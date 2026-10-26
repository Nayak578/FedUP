using FedUp.Player;
using FedUp.Story;
using FedUp.World;
using UnityEngine;

namespace FedUp.Tasks.Night
{
    /// <summary>
    /// Phase 2 (Gate 5a, DESIGN_LOGIC #61): the torch is his phone, and it's dying. The charger is by the bed upstairs,
    /// the bed he never sleeps in. The landing light is the one light on upstairs. Plugging in takes a short hold. On the
    /// way, the bedside drawer under the charger shows the gun for a heartbeat (<see cref="GunCameo"/>).
    /// </summary>
    public class ChargerTask : TaskBehaviour
    {
        [SerializeField] TaskInteractable charger;
        [SerializeField] PhoneTorch torch;
        [SerializeField] Light landingLight;
        [SerializeField] GunCameo gun;
        [SerializeField, Min(0f)] float plugHoldSeconds = 2f;
        [Tooltip("Let the line before this (back in the living room) land first. The torch starts dying at once.")]
        [SerializeField, Min(0f)] float lineDelay = 3f;
        [SerializeField] MonologueLibrarySO lines;

        public bool Charged { get; private set; }

        void Awake()
        {
            if (charger != null) { charger.available = false; charger.prompt = "Plug in your phone"; }
            if (landingLight != null) landingLight.enabled = false;
        }

        protected override void OnBegin()
        {
            if (torch != null) torch.Dying = true;
            if (landingLight != null) landingLight.enabled = true;
            gun?.Arm();
            charger.mode = Interaction.InteractionMode.Hold;
            charger.holdSeconds = plugHoldSeconds;
            charger.available = true;
            charger.Used += _ => Plug();
            StartCoroutine(SayLater());
        }

        System.Collections.IEnumerator SayLater()
        {
            yield return new WaitForSeconds(lineDelay);
            if (!Charged) lines?.Say("charger_low");
        }

        void Plug()
        {
            if (State != TaskState.Active || Charged) return;
            Charged = true;
            charger.available = false;
            if (torch != null) torch.Dying = false;
            lines?.Say("charger_done");
            Complete();
        }

        public override void DebugComplete()
        {
            if (charger != null) charger.available = false;
            if (torch != null) torch.Dying = false;
            base.DebugComplete();
        }
    }
}
