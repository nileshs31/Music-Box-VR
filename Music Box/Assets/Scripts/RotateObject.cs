using UnityEngine;

public class RotateObject : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float speed = 10f;
    public bool onZ = false;
    void Update()
    {
        if (onZ)
            transform.Rotate(Vector3.forward * speed * Time.deltaTime, Space.Self);
        else 
            transform.Rotate(Vector3.right * speed * Time.deltaTime, Space.Self);
    }
}
