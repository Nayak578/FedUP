using System.Collections;
using UnityEngine;

public class Eaten : Destructable
{
    [SerializeField] Bread bread;
    [SerializeField] Collider collider;
    [SerializeField] Rigidbody rb;
    void Start()
    {
        bread.enabled = false;
        collider.enabled = true;   
    }

    void Update()
    {
        
    }
    public override void Destruct() {
        Debug.Log("Eating Bread");
        Destroy(gameObject);
    }
    private void OnCollisionEnter(Collision collision) {
        
        StartCoroutine(Stop(1.5f));
    }
    private IEnumerator Stop(float time) {
        yield return new WaitForSeconds(time);
        rb.constraints = RigidbodyConstraints.FreezeAll;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}
