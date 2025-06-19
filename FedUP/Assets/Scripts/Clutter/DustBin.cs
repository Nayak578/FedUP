using UnityEngine;

public class DustBin : Interactable
{
    [SerializeField] GameObject player;
    private wastes waste;
    [SerializeField] float speed = 5f;
    private bool moveTotrash;
    [SerializeField] Vector3 trashPos;
    Quaternion someRot;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (waste != null && moveTotrash) {
            waste.GetComponent<wastes>().enabled = false;
            
            waste.transform.localPosition = Vector3.Lerp(waste.transform.localPosition, trashPos, speed * Time.deltaTime);
            someRot = Quaternion.Euler(Random.Range(10, 60),0, Random.Range(10, 60));
            if (Vector3.Distance(waste.transform.localPosition, trashPos) < 0.01f) {
                waste.transform.localPosition = trashPos;
                waste.transform.localRotation = someRot;
                waste.GetComponent<Collider>().isTrigger = false;
                waste.GetComponent<Rigidbody>().useGravity = true;
               
                moveTotrash = false;
                
                Interacting.isHoldingTrash = false;
                CanInteractWithBin.wasteInHand--;
            }
        }
    }
    public override void Interact() {
        Interacting.isInteracting = false;
        waste = player.GetComponentInChildren<wastes>();
        if (waste != null) {
            waste.transform.SetParent(transform);
            moveTotrash = true;
        }
    }

}
