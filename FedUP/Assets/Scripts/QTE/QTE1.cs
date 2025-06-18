using System;
using UnityEngine;

public class QTE1 : MonoBehaviour
{
    [SerializeField] GameObject canvas;
    [SerializeField] float greenX;
    [SerializeField] float YellowX;
    [SerializeField] float speed=10f;
    [SerializeField] float startX;
    [SerializeField] float endX;
    [SerializeField] Rigidbody2D rb;
    public enum QTEResult { 
    red,green,yellow}
    QTEResult result;
    private void Start() {
        rb.linearVelocityX = speed;
    }
    public event Action<QTEResult> QteComplete;
    private void Update() {
        if (transform.localPosition.x < startX) {
            rb.linearVelocityX = speed;
        } else if (transform.localPosition.x > endX) {
            rb.linearVelocityX = -speed;
        }
        if (Input.GetKeyDown(KeyCode.R)) {
            if (transform.localPosition.x < greenX) {
                result = QTEResult.red;
            } else if (transform.localPosition.x < YellowX) {
                result = QTEResult.green;
            } else {
                result = QTEResult.yellow;
            }
            if(QteComplete!=null)
            QteComplete(result);
            canvas.SetActive(false);
            //gameObject.SetActive(false);
        }
    }
}
