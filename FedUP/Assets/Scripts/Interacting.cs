using TMPro;
using UnityEngine;

public class Interacting : MonoBehaviour
{
    [SerializeField] float range = 2.5f;
    [SerializeField]LayerMask layer;
    [SerializeField] GameObject E;
    [SerializeField] GameObject Esc;
    private Interactable interactable;
    private wastes wastes;
    private Destructable destructable;
    public static bool isInteracting=false;
    public static bool isHoldingTrash = false;
    void Update()
    {
         {
            RaycastHit hit;
            Vector3 fwd = transform.TransformDirection(Vector3.forward);
            if (Physics.Raycast(transform.position, fwd, out hit, range, layer)) {
                if (!isInteracting) {
                    hit.collider.TryGetComponent<Interactable>(out interactable);
                    hit.collider.TryGetComponent<Destructable>(out destructable);
                    
                    if ((interactable != null && interactable.isActiveAndEnabled) ||(destructable != null && destructable.isActiveAndEnabled)){
                        E.SetActive(true);
                        Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
                        if (Input.GetKeyDown(KeyCode.E)) {
                            E.SetActive(false);
                            Esc.SetActive(true);
                            isInteracting = true;

                            if (interactable.isActiveAndEnabled) interactable.Interact();
                            else if (destructable.isActiveAndEnabled) destructable.Destruct();
                        }
                    }
                }
                if (!isHoldingTrash) {
                    hit.collider.TryGetComponent<wastes>(out wastes);
                    if(wastes!=null && wastes.isActiveAndEnabled) {
                        E.SetActive(true);
                        Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
                        if (Input.GetKeyDown(KeyCode.E)) {
                            E.SetActive(false);
                            Esc.SetActive(true);
                            isHoldingTrash = true;
                            if (wastes.isActiveAndEnabled) wastes.Interact();
                           
                        }
                    }
                }
            } else {
                E.SetActive(false);
                Esc.SetActive(false);
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * range, Color.red);

            }
         }
    }

}
