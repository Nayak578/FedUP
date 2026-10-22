using System;
using UnityEngine;

namespace FedUp.Interaction
{
    /// <summary>A wall switch that turns one or more lights on and off.</summary>
    public class LightSwitch : MonoBehaviour, IInteractable
    {
        [SerializeField] Light[] targets = Array.Empty<Light>();
        [Tooltip("The small lever that flips when used.")]
        [SerializeField] Transform toggleVisual;
        [SerializeField] AudioSource clickSource;
        [SerializeField] bool startsOn;
        [SerializeField] string prompt = "Light switch";

        public bool IsOn { get; private set; }
        public event Action<bool> Toggled;

        public string Prompt => prompt;
        public InteractionMode Mode => InteractionMode.Press;
        public float HoldSeconds => 0f;
        public bool CanInteract(Interactor who) => isActiveAndEnabled;

        void Awake() => Set(startsOn, silent: true);

        public void Interact(Interactor who) => Set(!IsOn, silent: false);

        void Set(bool on, bool silent)
        {
            IsOn = on;
            foreach (var l in targets) if (l != null) l.enabled = on;
            if (toggleVisual != null) toggleVisual.localRotation = Quaternion.Euler(on ? -15f : 15f, 0f, 0f);
            if (!silent)
            {
                if (clickSource != null) clickSource.Play();
                Toggled?.Invoke(on);
            }
        }
    }
}
