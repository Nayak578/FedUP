using UnityEngine;

public class CanInteractWithHanger : MonoBehaviour
{
    public static int itemsToBehanged=2;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (itemsToBehanged > 0) {
            gameObject.GetComponent<DoorHanger>().enabled = true;
        } else {
            gameObject.GetComponent<DoorHanger>().enabled = false;

        }

    }
}
