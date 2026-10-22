using UnityEngine;
using UnityEngine.InputSystem;

namespace FedUp.Interaction
{
    /// <summary>
    /// Sits on the player's camera. Finds the interactable under the crosshair within reach,
    /// tells the HUD about it, and fires it on press (or after a full hold).
    /// </summary>
    public class Interactor : MonoBehaviour
    {
        [SerializeField] InteractionSettingsSO settings;
        [SerializeField] InteractionFocusChannelSO focusChannel;
        [SerializeField] InputActionAsset controls;

        public IInteractable Current { get; private set; }
        /// <summary>Autopilot/tests: hold the interact button.</summary>
        public bool VirtualHeld { get; set; }

        InputAction interact;
        HoldProgress hold;
        bool virtualPressQueued;
        InteractionFocus lastPublished = InteractionFocus.None;

        void Awake()
        {
            if (controls != null) interact = controls.FindAction("Player/Interact", true);
        }

        void OnEnable() => interact?.Enable();

        // The focus channel is an asset and keeps its last value across scene loads; start from a clean "nothing focused".
        void Start() => Publish(force: true);

        /// <summary>Autopilot/tests: press the interact button once.</summary>
        public void VirtualPress() => virtualPressQueued = true;

        void Update()
        {
            var found = Probe();
            if (!ReferenceEquals(found, Current))
            {
                Current = found;
                hold = new HoldProgress(Current?.HoldSeconds ?? 0f);
            }

            bool pressed = virtualPressQueued || (interact != null && interact.WasPressedThisFrame());
            bool held = VirtualHeld || virtualPressQueued || (interact != null && interact.IsPressed());
            virtualPressQueued = false;

            if (Current != null && Current.CanInteract(this))
            {
                if (Current.Mode == InteractionMode.Press)
                {
                    if (pressed) Current.Interact(this);
                }
                else
                {
                    hold = hold.Tick(held, Time.deltaTime);
                    if (hold.JustCompleted) Current.Interact(this);
                }
            }
            Publish();
        }

        IInteractable Probe()
        {
            if (settings == null) return null;
            if (!Physics.Raycast(transform.position, transform.forward, out var hit, settings.reach, settings.rayMask, QueryTriggerInteraction.Collide))
                return null;
            var target = hit.collider.GetComponentInParent<IInteractable>();
            return target != null && target.CanInteract(this) ? target : null;
        }

        void Publish(bool force = false)
        {
            var f = Current == null
                ? InteractionFocus.None
                : new InteractionFocus(true, Current.Prompt, Current.Mode, Current.Mode == InteractionMode.Hold ? hold.Normalized : 0f);
            if (!force && f.HasTarget == lastPublished.HasTarget && f.Prompt == lastPublished.Prompt && Mathf.Approximately(f.Progress, lastPublished.Progress))
                return;
            lastPublished = f;
            if (focusChannel != null) focusChannel.Raise(f);
        }
    }
}
