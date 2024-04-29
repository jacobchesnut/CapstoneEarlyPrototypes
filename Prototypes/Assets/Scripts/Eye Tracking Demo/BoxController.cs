using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxController : MonoBehaviour
{
    //which eye to follow, 0 = disabled, 1 = left eye, 2 = right eye
    public int eyeFollowMode = 1;
    public bool testRotations = false;
    //calibration test vars
    public bool runUserCalibrationTest = true;
    public enum userEyeCalibrationTestState
    {
        disabled = 0,
        centerLeftEye = 1,
        centerRightEye = 2,
        LeftEyeUp = 3,
        LeftEyeRight = 4,
        LeftEyeDown = 5,
        LeftEyeLeft = 6,
        RightEyeUp = 7,
        RightEyeRight = 8,
        RightEyeDown = 9,
        RightEyeLeft = 10,
        done = 11,
    }
    private Vector2 leftRot = Vector2.zero;
    private Vector2 rightRot = Vector2.zero;
    private userEyeCalibrationTestState calState = userEyeCalibrationTestState.disabled;
    private bool testChange = false;

    public GameObject leftEye = null;
    public GameObject rightEye = null;

    //test rotations (x and y) to use when testing eye tracking from different angles
    public Vector2 testRot = Vector2.zero;
    public float rotationSpeed = 2f;

    private GameObject rotationsObject; //object for handling position and rotation of the box when testing from different angles
    public GameObject centerRotationsObject;
    public GameObject leftRotationsObject;
    public GameObject rightRotationsObject;
    public int eyeTestMode = 0; //0 = center, 1 = left, 2 = right

    /*variables for saving info to write to the calibration file
     *order is
     *float - left eye (LE) center angle X
     *float - LE center angle Y
     *float - right eye (RE) center angle X
     *float - RE center angle Y
     *float - LE up angle
     *float - LE right angle
     *float - LE down angle
     *float - LE left angle
     *float - RE up angle
     *float - RE right angle
     *float - RE down angle
     *float - RE left angle
     */
    private float LEUpAngle, LERightAngle, LEDownAngle, LELeftAngle, REUpAngle, RERightAngle, REDownAngle, RELeftAngle;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //appearantly breaks OVRInput.getdown()?
        //even though documentation says to include it.
        //OVRInput.Update();

        testLogic();

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

    private void testLogic()
    {
        if (runUserCalibrationTest)
        {
            calState = userEyeCalibrationTestState.centerLeftEye;
            runUserCalibrationTest = false;
            testChange = true;
        }
        switch (calState)
        {
            case userEyeCalibrationTestState.disabled:
                //do nothing
                break;
            case userEyeCalibrationTestState.centerLeftEye:
                //test rotations
                if (testChange)
                {
                    eyeFollowMode = 0;
                    testRotations = true;
                    eyeTestMode = 1;
                    //put back to center
                    testChange = false;
                    testRot = Vector2.zero;
                }
                if (OVRInput.GetDown(OVRInput.Button.One))
                {
                    testChange = true;
                    leftRot = testRot; //save rotation as center
                    Debug.Log("Calibration left eye center angle: " + leftRot); //print to log
                    calState++; //move to next test
                }
                break;
            case userEyeCalibrationTestState.centerRightEye:
                //test rotations
                if (testChange)
                {
                    eyeFollowMode = 0;
                    testRotations = true;
                    eyeTestMode = 2;
                    //put back to center
                    testChange = false;
                    testRot = Vector2.zero;
                }
                if (OVRInput.GetDown(OVRInput.Button.One))
                {
                    testChange = true;
                    rightRot = testRot; //save rotation as center
                    Debug.Log("Calibration right eye center angle: " + rightRot); //print to log
                    calState++; //move to next test
                }
                break;
            case userEyeCalibrationTestState.LeftEyeUp:
                if (testChange)
                {
                    eyeFollowMode = 0;
                    testRotations = true;
                    eyeTestMode = 1;
                    //put back to center
                    testChange = false;
                    testRot = leftRot;
                }
                if (OVRInput.GetDown(OVRInput.Button.One))
                {
                    testChange = true;
                    float angleDifference = Mathf.Abs(leftRot.x - testRot.x);
                    Debug.Log("Calibration left eye angle up: " + angleDifference); //print to log
                    LEUpAngle = angleDifference;
                    calState++; //move to next test
                }
                break;
            case userEyeCalibrationTestState.LeftEyeRight:
                if (testChange)
                {
                    eyeFollowMode = 0;
                    testRotations = true;
                    eyeTestMode = 1;
                    //put back to center
                    testChange = false;
                    testRot = leftRot;
                }
                if (OVRInput.GetDown(OVRInput.Button.One))
                {
                    testChange = true;
                    float angleDifference = Mathf.Abs(leftRot.y - testRot.y);
                    Debug.Log("Calibration left eye angle right: " + angleDifference); //print to log
                    LERightAngle = angleDifference;
                    calState++; //move to next test
                }
                break;
            case userEyeCalibrationTestState.LeftEyeDown:
                if (testChange)
                {
                    eyeFollowMode = 0;
                    testRotations = true;
                    eyeTestMode = 1;
                    //put back to center
                    testChange = false;
                    testRot = leftRot;
                }
                if (OVRInput.GetDown(OVRInput.Button.One))
                {
                    testChange = true;
                    float angleDifference = Mathf.Abs(leftRot.x - testRot.x);
                    Debug.Log("Calibration left eye angle down: " + angleDifference); //print to log
                    LEDownAngle = angleDifference;
                    calState++; //move to next test
                }
                break;
            case userEyeCalibrationTestState.LeftEyeLeft:
                if (testChange)
                {
                    eyeFollowMode = 0;
                    testRotations = true;
                    eyeTestMode = 1;
                    //put back to center
                    testChange = false;
                    testRot = leftRot;
                }
                if (OVRInput.GetDown(OVRInput.Button.One))
                {
                    testChange = true;
                    float angleDifference = Mathf.Abs(leftRot.y - testRot.y);
                    Debug.Log("Calibration left eye angle left: " + angleDifference); //print to log
                    LELeftAngle = angleDifference;
                    calState++; //move to next test
                }
                break;
            case userEyeCalibrationTestState.RightEyeUp:
                if (testChange)
                {
                    eyeFollowMode = 0;
                    testRotations = true;
                    eyeTestMode = 2;
                    //put back to center
                    testChange = false;
                    testRot = rightRot;
                }
                if (OVRInput.GetDown(OVRInput.Button.One))
                {
                    testChange = true;
                    float angleDifference = Mathf.Abs(rightRot.x - testRot.x);
                    Debug.Log("Calibration right eye angle up: " + angleDifference); //print to log
                    REUpAngle = angleDifference;
                    calState++; //move to next test
                }
                break;
            case userEyeCalibrationTestState.RightEyeRight:
                if (testChange)
                {
                    eyeFollowMode = 0;
                    testRotations = true;
                    eyeTestMode = 2;
                    //put back to center
                    testChange = false;
                    testRot = rightRot;
                }
                if (OVRInput.GetDown(OVRInput.Button.One))
                {
                    testChange = true;
                    float angleDifference = Mathf.Abs(rightRot.y - testRot.y);
                    Debug.Log("Calibration right eye angle right: " + angleDifference); //print to log
                    RERightAngle = angleDifference;
                    calState++; //move to next test
                }
                break;
            case userEyeCalibrationTestState.RightEyeDown:
                if (testChange)
                {
                    eyeFollowMode = 0;
                    testRotations = true;
                    eyeTestMode = 2;
                    //put back to center
                    testChange = false;
                    testRot = rightRot;
                }
                if (OVRInput.GetDown(OVRInput.Button.One))
                {
                    testChange = true;
                    float angleDifference = Mathf.Abs(rightRot.x - testRot.x);
                    Debug.Log("Calibration right eye angle down: " + angleDifference); //print to log
                    REDownAngle = angleDifference;
                    calState++; //move to next test
                }
                break;
            case userEyeCalibrationTestState.RightEyeLeft:
                if (testChange)
                {
                    eyeFollowMode = 0;
                    testRotations = true;
                    eyeTestMode = 2;
                    //put back to center
                    testChange = false;
                    testRot = rightRot;
                }
                if (OVRInput.GetDown(OVRInput.Button.One))
                {
                    testChange = true;
                    float angleDifference = Mathf.Abs(rightRot.y - testRot.y);
                    Debug.Log("Calibration right eye angle left: " + angleDifference); //print to log
                    RELeftAngle = angleDifference;
                    calState++; //move to next test
                }
                break;
            case userEyeCalibrationTestState.done:
                Debug.Log("calibration is done, printing to file");
                FileStream stream = File.Open("C:\\Users\\jakee\\CapstoneShared\\Calibration.dat", FileMode.OpenOrCreate);
                BinaryWriter writer = new BinaryWriter(stream);
                //y then x because the orders are reversed in test
                writer.Write(leftRot.y);
                writer.Write(leftRot.x);
                writer.Write(rightRot.y);
                writer.Write(rightRot.x);
                writer.Write(LEUpAngle);
                writer.Write(LERightAngle);
                writer.Write(LEDownAngle);
                writer.Write(LELeftAngle);
                writer.Write(REUpAngle);
                writer.Write(RERightAngle);
                writer.Write(REDownAngle);
                writer.Write(RELeftAngle);
                stream.Close();
                testChange = false;
                calState = userEyeCalibrationTestState.disabled;
                break;
            default:
                Debug.Log("calState is in an unknown state!");
                break;
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
        switch (eyeTestMode)
        {
            case 0:
                rotationsObject = centerRotationsObject;
                break;
            case 1:
                rotationsObject = leftRotationsObject;
                break;
            case 2:
                rotationsObject = rightRotationsObject;
                break;
            default:
                Debug.LogError("unknown state for eyeTestMode");
                rotationsObject = null;
                return;
        }

        Vector2 controllerRaw = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        Vector2 controllerMovement = new Vector2(controllerRaw.y * rotationSpeed, controllerRaw.x * rotationSpeed);
        testRot += (controllerMovement * Time.deltaTime);
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
