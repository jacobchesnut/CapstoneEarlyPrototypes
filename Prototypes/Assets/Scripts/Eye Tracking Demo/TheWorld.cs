using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheWorld : MonoBehaviour
{
    //gameobjects which reference the player's eyes
    public GameObject leftEye = null;
    public GameObject rightEye = null;
    public GameObject hitCube = null;
    public GameObject target = null;

    //gameobjects which make up the saved scene
    // 0 = left eye cube
    // 1 = right eye cube
    // 2 = left eye gaze cylinder
    // 3 = right eye gaze cylinder
    // 4 = left eye hit sphere
    // 5 = right eye hit sphere
    // 6 = hit cube
    // 7 = target
    public GameObject[] savedObjects = null;

    // Start is called before the first frame update
    void Start()
    {
        //instantiate saved objects
        savedObjects = new GameObject[8];
        //gameobject reference for eye gazes
        Transform gazeReference = null;
        //create left eye cube
        savedObjects[0] = GameObject.CreatePrimitive(PrimitiveType.Cube);
        savedObjects[0].transform.localScale = leftEye.transform.localScale;
        //create right eye cube
        savedObjects[1] = GameObject.CreatePrimitive(PrimitiveType.Cube);
        savedObjects[1].transform.localScale = rightEye.transform.localScale;
        //create left eye gaze
        gazeReference = leftEye.transform.GetChild(0); //assumes gaze cylinder is the first child object
        savedObjects[2] = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        savedObjects[2].transform.localScale = gazeReference.localScale;
        savedObjects[2].GetComponent<CapsuleCollider>().enabled = false; //turn off raycast collision
        //create right eye gaze
        gazeReference = rightEye.transform.GetChild(0); //assumes gaze cylinder is the first child object
        savedObjects[3] = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        savedObjects[3].transform.localScale = gazeReference.localScale;
        savedObjects[3].GetComponent<CapsuleCollider>().enabled = false; //turn off raycast collision
        //create left eye sphere
        savedObjects[4] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        savedObjects[4].transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        //create right eye sphere
        savedObjects[5] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        savedObjects[5].transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        //create hit cube
        savedObjects[6] = GameObject.CreatePrimitive(PrimitiveType.Cube);
        savedObjects[6].transform.localScale = hitCube.transform.localScale;
        //create target
        savedObjects[7] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        savedObjects[7].transform.localScale = target.transform.localScale;

        //throw everything up 100 units to be out of the way
        Vector3 tempPos;
        for(int i = 0; i < 8; i++)
        {
            tempPos = savedObjects[i].transform.position;
            tempPos.y += 100;
            savedObjects[i].transform.position = tempPos;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //if hit space, save the current position of the eyes and cube
            Transform gazeReference = null;
            //left eye cube
            savedObjects[0].transform.SetPositionAndRotation(leftEye.transform.position, leftEye.transform.rotation);
            //right eye cube
            savedObjects[1].transform.SetPositionAndRotation(rightEye.transform.position, rightEye.transform.rotation);
            //left eye gaze
            gazeReference = leftEye.transform.GetChild(0); //assumes gaze cylinder is the first child object
            savedObjects[2].transform.SetPositionAndRotation(gazeReference.position, gazeReference.rotation);
            //right eye gaze
            gazeReference = rightEye.transform.GetChild(0); //assumes gaze cylinder is the first child object
            savedObjects[3].transform.SetPositionAndRotation(gazeReference.position, gazeReference.rotation);
            //hit cube
            savedObjects[6].transform.SetPositionAndRotation(hitCube.transform.position, hitCube.transform.rotation);
            //target
            savedObjects[7].transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
            //then cast some rays through the gaze direction to see the exact hit points on the cube, and calculate the distance between them
            RaycastHit gazePoint;
            bool hit;
            Vector3 rayStartPos;
            //calculate left
            rayStartPos = savedObjects[0].transform.position + (savedObjects[0].transform.TransformDirection(Vector3.forward) * 0.2f); //small adjustment in the forward direction to avoid collision with the cylinder
            Debug.Log("left eye pos and dir: " + rayStartPos + " " + savedObjects[0].transform.rotation.eulerAngles);
            hit = Physics.Raycast(rayStartPos, savedObjects[0].transform.TransformDirection(Vector3.forward), out gazePoint, Mathf.Infinity);
            if (hit)
            {
                savedObjects[4].transform.position = gazePoint.point;
                savedObjects[4].GetComponent<Renderer>().material.color = Color.green; //green = hit
            }
            else
            {
                savedObjects[4].GetComponent<Renderer>().material.color = Color.red; //red = miss
            }
            //calculate right
            rayStartPos = savedObjects[1].transform.position + (savedObjects[1].transform.TransformDirection(Vector3.forward) * 0.2f); //small adjustment in the forward direction to avoid collision with the cylinder
            Debug.Log("right eye pos and dir: " + rayStartPos + " " + savedObjects[1].transform.rotation.eulerAngles);
            hit = Physics.Raycast(rayStartPos, savedObjects[1].transform.TransformDirection(Vector3.forward), out gazePoint, Mathf.Infinity, ~128);
            if (hit)
            {
                savedObjects[5].transform.position = gazePoint.point;
                savedObjects[5].GetComponent<Renderer>().material.color = Color.green; //green = hit
            }
            else
            {
                savedObjects[5].GetComponent<Renderer>().material.color = Color.red; //red = miss
            }
            //calculate distance
            Vector3 distanceVector = savedObjects[4].transform.position - savedObjects[5].transform.position;
            Debug.Log("distance between eye points = " + distanceVector.magnitude);

            //calculate degree offset
            //left eye
            //a
            Vector3 C = savedObjects[7].transform.position - savedObjects[0].transform.position; //left eye to target
            Vector3 B = savedObjects[4].transform.position - savedObjects[0].transform.position; //left eye to end point
            Vector3 A = B - C; //target to end point
            //calculate angle of offset
            //Law of cosines : https://en.wikipedia.org/wiki/Law_of_cosines
            float offset = Mathf.Acos(
                ((B.magnitude * B.magnitude) + (C.magnitude * C.magnitude) - (A.magnitude * A.magnitude))
                / (2 * B.magnitude * C.magnitude)); //offset =  arccos((b^2 + c^2 - a^2)/2bc).
            Debug.Log("Left Eye offset: " + offset * Mathf.Rad2Deg);
            //Debug.Log("Left Eye Vals: " + A + " " + B + " " + C);
            //Debug.Log("Left Eye Mags: " + A.magnitude + " " + B.magnitude + " " + C.magnitude);

            //right eye
            //a
            C = savedObjects[7].transform.position - savedObjects[1].transform.position; //left eye to target
            B = savedObjects[5].transform.position - savedObjects[1].transform.position; //left eye to end point
            A = B - C; //target to end point
            //calculate angle of offset
            offset = Mathf.Acos(
                ((B.magnitude * B.magnitude) + (C.magnitude * C.magnitude) - (A.magnitude * A.magnitude))
                / (2 * B.magnitude * C.magnitude)); //offset =  arccos((b^2 + c^2 - a^2)/2bc).
            Debug.Log("Right Eye offset: " + offset * Mathf.Rad2Deg);
            //Debug.Log("Right Eye Vals: " + A + " " + B + " " + C);
            //Debug.Log("Right Eye Mags: " + A.magnitude + " " + B.magnitude + " " + C.magnitude);

            //move everything away from the center a little
            Vector3 tempPos;
            for (int i = 0; i < 7; i++)
            {
                tempPos = savedObjects[i].transform.position;
                tempPos.x += 5;
                savedObjects[i].transform.position = tempPos;
            }
        }
    }
}
