using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class moving : MonoBehaviour
{
    private float timer = 0f;
    private float endTime = 2f;
    private float speed = 1f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if(timer > endTime)
        {
            timer = 0;
            speed *= -1;
        }
        Vector3 newPos = transform.position;
        newPos += (transform.forward * speed * Time.deltaTime);
    }
}
