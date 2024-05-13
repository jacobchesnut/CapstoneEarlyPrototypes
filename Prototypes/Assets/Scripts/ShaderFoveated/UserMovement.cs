using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 controllerRaw = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        Vector2 controllerMovement = new Vector2(controllerRaw.x * moveSpeed, controllerRaw.y * moveSpeed);
        controllerMovement *= Time.deltaTime;
        Vector3 movement = new Vector3(controllerMovement.x, 0, controllerMovement.y);
        transform.position += movement;
    }
}
