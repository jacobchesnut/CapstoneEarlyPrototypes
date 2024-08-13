using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Oscillate : MonoBehaviour
{
    public float speed = 1f;
    private float maxX, minX;
    private bool forward = true;
    public bool stopMoving = false;

    // Start is called before the first frame update
    void Start()
    {
        maxX = transform.position.x + 1;
        minX = transform.position.x - 1;
    }

    // Update is called once per frame
    void Update()
    {
        if (stopMoving)
        {
            return;
        }
        float tempPos = transform.position.x;
        if(forward)
        {
            tempPos += speed * Time.deltaTime;
            if(tempPos >= maxX)
            {
                transform.position = new Vector3(maxX, transform.position.y, transform.position.z);
                forward = false;
            }
            else
            {
                transform.position = new Vector3(tempPos, transform.position.y, transform.position.z);
            }
        }
        else
        {
            tempPos -= speed * Time.deltaTime;
            if (tempPos <= minX)
            {
                transform.position = new Vector3(minX, transform.position.y, transform.position.z);
                forward = true;
            }
            else
            {
                transform.position = new Vector3(tempPos, transform.position.y, transform.position.z);
            }
        }
    }
}
