using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxController : MonoBehaviour
{
    //which eye to follow, 0 = disabled, 1 = left eye, 2 = right eye
    public int eyeFollowMode = 1;
    public bool testRotations = false;

    public GameObject leftEye = null;
    public GameObject rightEye = null;

    //test rotations (x and y) to use when testing eye tracking from different angles
    public Vector2 testRot = Vector2.zero;

    public GameObject rotationsObject; //object for handling position and rotation of the box when testing from different angles
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(eyeFollowMode > 0)
        {
            followEye();
            return;
        }
        if (testRotations)
        {
            moveToTestPos();
        }
    }

    private void followEye()
    {
        GameObject eyeToUse;
        if(eyeFollowMode == 1)
        {
            eyeToUse = leftEye;
        }
        else
        {
            eyeToUse = rightEye;
        }

        transform.position = eyeToUse.transform.GetChild(0).transform.position;
        transform.rotation = eyeToUse.transform.rotation;
    }

    private void moveToTestPos()
    {
        Vector3 newRot = new Vector3(-testRot.x, testRot.y, 0);
        //rotationsObject.transform.rotation = Quaternion.Euler(Vector3.zero);
        rotationsObject.transform.localRotation = Quaternion.Euler(newRot);

        transform.position = rotationsObject.transform.GetChild(0).position;
        transform.rotation = rotationsObject.transform.GetChild(0).rotation;
    }

    private void LateUpdate()
    {
        if (testRotations)
        {
            //used to force eye positions to a static place near 0,0 for testing purposes
            //eyes are roughly seven cm apart (unity units roughly translate to cm, this is the distance meta quest defaults to
            leftEye.transform.parent.position = new Vector3(-0.035f, 0f, 0f);
            rightEye.transform.parent.position = new Vector3(0.035f, 0f, 0f);
            //now force eyes forward
            leftEye.transform.parent.rotation = Quaternion.Euler(Vector3.zero);
            rightEye.transform.parent.rotation = Quaternion.Euler(Vector3.zero);
        }
    }
}
