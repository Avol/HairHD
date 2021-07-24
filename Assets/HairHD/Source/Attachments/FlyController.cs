using UnityEngine;
using System.Collections;

public class FlyController : MonoBehaviour {
    public float Speed = 0.02f;
    public float RotateSpeed = 0.01f;

    private Vector2 lastMousePos = -Vector2.one;


	void Update () 
	{
        float shift = Input.GetKey(KeyCode.LeftShift) ? 5.0f : 1.0f;

        if (Input.GetKey("a"))
            transform.position -= transform.right * Speed * shift;
        if (Input.GetKey("d"))
            transform.position += transform.right * Speed * shift;
        if (Input.GetKey("w"))
            transform.position += transform.forward * Speed * shift;
        if (Input.GetKey("s"))
            transform.position -= transform.forward * Speed * shift;

        Vector2 currentMousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        if (Input.GetMouseButton(0))
        {
            if (lastMousePos != -Vector2.one)
            {
                Vector2 diff = currentMousePos - lastMousePos;
                Vector3 position = transform.position + transform.forward + transform.right * diff.x * RotateSpeed + transform.up * diff.y * RotateSpeed;
                transform.LookAt(position);
            }
            lastMousePos = currentMousePos;
        } else
        {
            lastMousePos = -Vector2.one;
        }
    }
}
