using UnityEngine;

public class wastes : MonoBehaviour
{
    [SerializeField] GameObject player;
    void Start()
    {
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T)) {
            transform.SetParent(null);
        }

    }

    public virtual void Interact() {
        Interacting.isHoldingTrash = true;
        transform.SetParent(player.transform);
        CanInteractWithBin.wasteInHand++;
        transform.localRotation = Quaternion.identity;
        transform.localPosition = new Vector3(0.5f,1,0);
        //Interacting.isInteracting = false;
        
    }
}
