using TMPro;
using UnityEngine;

public class Interacting : MonoBehaviour
{
    [SerializeField] float range = 2.5f;
    [SerializeField]LayerMask layer;
    [SerializeField] GameObject E;
    [SerializeField] GameObject Esc;
    private Interactable interactable;
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
            E.SetActive(true);
            Esc.SetActive(true);
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
            if (Input.GetKeyDown(KeyCode.E)) {
                E.SetActive(false);
                hit.collider.TryGetComponent<Interactable>(out interactable);
                if(interactable.isActiveAndEnabled)interactable.Interact();
            }

        } else {
            E.SetActive(false);
            Esc.SetActive(false);
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * range, Color.red);

        }
    }
}
