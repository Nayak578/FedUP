using UnityEngine;

public class Eaten : Destructable
{
    [SerializeField] Bread bread;
    [SerializeField] Collider collider;
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
}
