using UnityEngine;

public class Interacting : MonoBehaviour
{
    [SerializeField] int interactionLayer=7;
    [SerializeField] float range = 2.5f;
    [SerializeField]LayerMask layer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit;
        Vector3 fwd = transform.TransformDirection(Vector3.forward);
        if (Physics.Raycast(transform.position, fwd, out hit, range, layer)) {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);

        } else {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * range, Color.red);

        }
    }
}
