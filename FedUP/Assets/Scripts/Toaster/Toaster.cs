using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Toaster : Interactable
{
    private State state = State.nothing;
    private Vector2 input;
    [SerializeField] float slidingLength = 10f;
    [SerializeField] GameObject Qte;
    [SerializeField] Bread bread;
    private Vector2 initial;
    private Vector2 final;
    [SerializeField] Color GoldenBrown;
    [SerializeField] Color SlightlyBurned;
    [SerializeField] Color Burned;
    [SerializeField] Vector3 force;
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
            RedResult();
        }else if (obj == QTE1.QTEResult.green) {
            GreenResult();

        } else if (obj == QTE1.QTEResult.yellow) {
            YellowResult();
        }
        

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
        interactInput.enabled = false;
        playerInput.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        
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
    private void RedResult() {
        bread.GetComponent<Renderer>().material.color = Burned;
        if (EndToastingAnimation != null) EndToastingAnimation();
        bread.GetComponent<Rigidbody>().useGravity = true;
        bread.GetComponent<Rigidbody>().AddForce(force, ForceMode.Force);
        bread.GetComponent<Collider>().enabled = true;
        StartCoroutine(StopBreadAfter(1.5f));

    }
    public void GreenResult() {
        bread.GetComponent<Renderer>().material.color = GoldenBrown;
        if (EndToastingAnimation != null) EndToastingAnimation();
    }
    public void YellowResult() {
        bread.GetComponent<Renderer>().material.color = SlightlyBurned;
        if (EndToastingAnimation != null) EndToastingAnimation();
 
    }
    private IEnumerator StopBreadAfter(float time) {
        yield return new WaitForSeconds(time);
        Rigidbody rb = bread.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true; // Optional, makes it stop reacting to physics
    }

}
