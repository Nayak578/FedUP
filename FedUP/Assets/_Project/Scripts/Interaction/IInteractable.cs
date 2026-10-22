namespace FedUp.Interaction
{
    public enum InteractionMode { Press, Hold }

    /// <summary>Anything the player can use by looking at it and pressing (or holding) Interact.</summary>
    public interface IInteractable
    {
        /// <summary>Short name shown on screen, e.g. "Light switch".</summary>
        string Prompt { get; }
        InteractionMode Mode { get; }
        /// <summary>Seconds to hold when <see cref="Mode"/> is Hold. Ignored for Press.</summary>
        float HoldSeconds { get; }
        bool CanInteract(Interactor who);
        void Interact(Interactor who);
    }
}
