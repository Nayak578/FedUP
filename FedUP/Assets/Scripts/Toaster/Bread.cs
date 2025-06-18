using UnityEngine;

public class Bread : Interactable
{

    private bool firstMovement=false;
    private bool secondMovement = false;
    [SerializeField] float speed = 5f;
    [SerializeField] Vector3 firstPos;
    [SerializeField] Quaternion firstRot;
    [SerializeField] Vector3 secondPos;
    [SerializeField] Toaster toaster;
    void Start()
    {
        
    }

    void Update() {
        if (firstMovement) {
            if (!secondMovement) {
                transform.localPosition = Vector3.Lerp(transform.localPosition, firstPos, speed * Time.deltaTime);
                transform.localRotation = Quaternion.Lerp(transform.localRotation, firstRot, speed * Time.deltaTime);
                if (Vector3.Distance(transform.localPosition, firstPos) < 0.01f) {
                    transform.localPosition = firstPos;
                    secondMovement = true;
                }
                
            } else {
                transform.localPosition = Vector3.Lerp(transform.localPosition, secondPos, speed * Time.deltaTime);
                if (Vector3.Distance(transform.localPosition, secondPos) < 0.01f) {
                    transform.localPosition = secondPos;
                    firstMovement = false;
                    secondMovement = false;
                    toaster.GetComponent<Collider>().enabled = true;
                    gameObject.GetComponent<Collider>().enabled = false;
                    Interacting.isInteracting = false;
                   
                }
            }
        }
    }
    public override void Interact() {
        firstMovement = true;
    }
    public override void UnlockInteract() {
    }

}
