using UnityEngine;

public class RotateItem : MonoBehaviour
{
    public bool isRotate;
    public float speed = 100;
    private float t;

    // Update is called once per frame
    void Update()
    {
        if (isRotate)
        {
            t = Time.deltaTime;
            transform.Rotate(new Vector3(0, speed * t, 0)); // X; Y; Z
        }
    }
}
