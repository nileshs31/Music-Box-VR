using UnityEngine;

public class RotateObject : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float speed = 10f; // degrees per second
    public bool onZ = false;
    void Update()
    {
        // Rotate around local X axis
        if (onZ)
            transform.Rotate(Vector3.forward * speed * Time.deltaTime, Space.Self);
        else 
            transform.Rotate(Vector3.right * speed * Time.deltaTime, Space.Self);
    }
}
