using UnityEngine;

public class CanInteractWithBin : MonoBehaviour
{
    public static int wasteInHand = 0;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (wasteInHand > 0) {
            gameObject.GetComponent<DustBin>().enabled = true;
        } else {
            gameObject.GetComponent<DustBin>().enabled = false;
        }
    }
}
