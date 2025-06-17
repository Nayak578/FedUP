using UnityEngine;
using UnityEngine.InputSystem;

public class Interactable : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] PlayerInput playerInput;
    [SerializeField] PlayerInput interactInput;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T)) {
            UnlockInteract();
        }
    }
    public void Interact() {
        Debug.Log("Interacting");
        LockInteract();
        
    }
    public void LockInteract() {
        playerInput.enabled = false;
        interactInput.enabled = true;
    }
    public void UnlockInteract() {
        playerInput.enabled = true;
        interactInput.enabled = false;
        Interacting.isInteracting = false;
    }

}
