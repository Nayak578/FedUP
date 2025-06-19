using UnityEngine;

public class DoorHanger : Interactable
{
    [SerializeField] GameObject player;
    private ItemHang item;
    private bool hangItem = false;
    [SerializeField] float speed = 5f;
    [SerializeField] Vector3 itemPos;
    [SerializeField] Quaternion itemRot;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (hangItem && item!=null) {
            item.transform.localPosition = Vector3.Lerp(item.transform.localPosition, itemPos, speed * Time.deltaTime);
            item.transform.localRotation = itemRot;
            if (Vector3.Distance(item.transform.localPosition, itemPos) < 0.01) {
                item.transform.localPosition = itemPos;
                hangItem = false;
            }
        }
    }
    public override void Interact() {
        Interacting.isInteracting = false;
        item = player.GetComponentInChildren<ItemHang>();
        if (item != null) {
            item.transform.SetParent(transform);
            hangItem = true;
        }
    }
}
