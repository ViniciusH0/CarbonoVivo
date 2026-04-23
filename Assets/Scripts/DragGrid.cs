using UnityEngine;

public class ScrollCam : MonoBehaviour
{
    public float speed = 0.01f;
    public float minY, maxY;

    public void MoveCamera(float deltaY)
    {
        float newY = transform.position.y - (deltaY * speed);
        newY = Mathf.Clamp(newY, minY, maxY);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}