using UnityEngine;

public class Toasting : MonoBehaviour
{
    [SerializeField] Toaster toaster;
    [SerializeField] GameObject lever;
    [SerializeField] Vector3 firstPosBread;
    [SerializeField] Vector3 firstPosLever;
    [SerializeField] Vector3 endPosBread;
    [SerializeField] Vector3 endPosLever;
    [SerializeField] float speed;
    [SerializeField] Eaten eaten;

    private bool startToasting = false;
    private bool endToasting = false;
    private void Start() {
        toaster.StartToastingAnimation += Toaster_StartToastingAnimation;
        toaster.EndToastingAnimation += Toaster_EndToastingAnimation;
    }
    private void Toaster_EndToastingAnimation() {
        endToasting = true;
    }

    private void Toaster_StartToastingAnimation() {
        startToasting = true;
    }

    void Update()
    {
        if (startToasting) {
            transform.localPosition = Vector3.Lerp(transform.localPosition, firstPosBread, speed * Time.deltaTime);
            lever.transform.localPosition = Vector3.Lerp(lever.transform.localPosition, firstPosLever, speed * Time.deltaTime);
            if (Vector3.Distance(transform.localPosition, firstPosBread) < 0.01f) {
                transform.localPosition = firstPosBread;
                startToasting = false;
                
            }
        }
        if (endToasting) {
            transform.localPosition = Vector3.Lerp(transform.localPosition, endPosBread, speed * Time.deltaTime);
            lever.transform.localPosition = Vector3.Lerp(lever.transform.localPosition, endPosLever, speed * Time.deltaTime);
            if (Vector3.Distance(transform.localPosition, endPosBread) < 0.01f) {
                transform.localPosition = endPosBread;
                endToasting = false;
                toaster.UnlockInteract();
                eaten.enabled = true;
            }
        }
    }
}
