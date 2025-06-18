using TMPro;
using UnityEngine;

public class Interacting : MonoBehaviour
{
    [SerializeField] float range = 2.5f;
    [SerializeField]LayerMask layer;
    [SerializeField] GameObject E;
    [SerializeField] GameObject Esc;
    private Interactable interactable;
    private Destructable destructable;
    public static bool isInteracting=false;
    void Update()
    {
        if (!isInteracting) {
            RaycastHit hit;
            Vector3 fwd = transform.TransformDirection(Vector3.forward);
            if (Physics.Raycast(transform.position, fwd, out hit, range, layer)) {
                E.SetActive(true);
                Esc.SetActive(true);
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
                if (Input.GetKeyDown(KeyCode.E)) {
                    E.SetActive(false);
                    isInteracting = true;
                    hit.collider.TryGetComponent<Interactable>(out interactable);
                    hit.collider.TryGetComponent<Destructable>(out destructable);
                    if (interactable.isActiveAndEnabled) interactable.Interact();
                    else if (destructable.isActiveAndEnabled) destructable.Destruct();
                }

            } else {
                E.SetActive(false);
                Esc.SetActive(false);
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * range, Color.red);

            }
        }
    }

}
