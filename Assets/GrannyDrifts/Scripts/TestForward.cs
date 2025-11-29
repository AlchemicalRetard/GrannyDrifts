using UnityEngine;

public class TestForward : MonoBehaviour
{
    public Rigidbody sphere;

    void Start()
    {
     sphere.transform.parent = null;   
    }
    private void Update()
    {
        // Follow sphere position
        transform.position = sphere.transform.position ;
        
      
    }

   

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 3f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 1f);
    }
}