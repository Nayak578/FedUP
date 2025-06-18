using UnityEngine;
using UnityEngine.InputSystem;

public class Interactable : MonoBehaviour
{
    [SerializeField] protected PlayerInput playerInput;
    [SerializeField] protected PlayerInput interactInput;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T)) {
            UnlockInteract();
        }
    }
    public virtual void Interact() {
        Debug.Log("Interacting");
        LockInteract();
        
    }
    public virtual void LockInteract() {
        playerInput.enabled = false;
        interactInput.enabled = true;
    }
    public virtual void UnlockInteract() {
        interactInput.enabled = false;
        playerInput.enabled = true;
        
        Interacting.isInteracting = false;
    }

}
