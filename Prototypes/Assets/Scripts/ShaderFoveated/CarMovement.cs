using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarMovement : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;
    public float timeForMovement = 3f;
    private float pointInMovement = 0f;
    // Start is called before the first frame update
    void Start()
    {
        transform.position = startPoint.position; //set to starting point
    }

    // Update is called once per frame
    void Update()
    {
        pointInMovement += Time.deltaTime;
        if(pointInMovement > timeForMovement || pointInMovement <= 0f)
        {
            pointInMovement = 0f;
            transform.position = startPoint.position;
            return;
        }
        Vector3 dist = endPoint.position - startPoint.position;
        dist *= pointInMovement / timeForMovement;
        transform.position = startPoint.position + dist;
    }
}
