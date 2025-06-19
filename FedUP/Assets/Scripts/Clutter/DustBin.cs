using UnityEngine;

public class DustBin : Interactable
{
    [SerializeField] GameObject player;
    private wastes waste;
    [SerializeField] float speed = 5f;
    private bool moveTotrash;
    [SerializeField] Vector3 trashPos;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (waste != null && moveTotrash) {
            waste.transform.localPosition = Vector3.Lerp(waste.transform.localPosition, trashPos, speed * Time.deltaTime);
           
            if (Vector3.Distance(waste.transform.localPosition, trashPos) < 0.01f) {
                waste.transform.localPosition = trashPos;
                moveTotrash = false;
                CanInteractWithBin.wasteInHand--;
                Interacting.isHoldingTrash = false;
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
