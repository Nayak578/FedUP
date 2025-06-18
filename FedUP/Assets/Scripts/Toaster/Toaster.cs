using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Toaster : Interactable
{
    private State state = State.nothing;
    private Vector2 input;
    [SerializeField] float slidingLength = 10f;
    [SerializeField] GameObject Qte;
    private Vector2 initial;
    private Vector2 final;
    public enum State {
        start, end, pressing, nothing
    }
    public event Action StartToastingAnimation;
    public event Action EndToastingAnimation;
    private void Start() {
        Qte.GetComponentInChildren<QTE1>().QteComplete += Toaster_QteComplete;
    }

    private void Toaster_QteComplete(QTE1.QTEResult obj) {
        if (obj == QTE1.QTEResult.red) {
            Debug.Log("Throw Bread");
        }else if (obj == QTE1.QTEResult.green) {
            Debug.Log("good Bread");

        } else if (obj == QTE1.QTEResult.yellow) {
            Debug.Log("little burnt Bread");
        }
        if (EndToastingAnimation != null) EndToastingAnimation();

    }

    void Update()
    {
        if (state==State.start) {
            initial = input;
            state = State.nothing;
        }else if(state == State.end){
            final = input;
            state = State.nothing;
            if (initial.y - final.y > slidingLength) {
                Debug.Log("Sliding Successfull");
                if(StartToastingAnimation!=null)
                StartToastingAnimation();
                Qte.SetActive(true);
            }
        }
        if (Input.GetKeyDown(KeyCode.K)) {
        }
       
    }
    public override void Interact() {
        LockInteract();
    }
    public override void LockInteract() {
        base.LockInteract();
        Cursor.lockState = CursorLockMode.None;

    }
    public override void UnlockInteract() {
        Cursor.lockState = CursorLockMode.Locked;
        interactInput.enabled = false;
        playerInput.enabled = true;
        
        Interacting.isInteracting = false;
        
        
    }
    public void OnClick(InputAction.CallbackContext context) {
        if (context.performed) {
            state = State.start;
        }
        if (context.canceled) {
            state = State.end;
        }
    }
    public void MousesePos(InputAction.CallbackContext context) {
        input = context.ReadValue<Vector2>();
    }
}
