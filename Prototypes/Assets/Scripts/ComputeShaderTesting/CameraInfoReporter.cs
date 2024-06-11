using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraInfoReporter : MonoBehaviour
{
    //variables to set in scene
    public Transform eyeObject = null;
    public bool printInfo = true;
    public string infoName = "camera";
    //variables to be pulled from
    public Vector4 _frustumVector;
    public Vector4 _viewVector;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("camera info for " + infoName + " Angle: " + transform.rotation.eulerAngles + " position: " + transform.position);
        //important this all gets set before OnPreRender() in the trycreatejoepipeline script.
        Camera c = GetComponent<Camera>();
        float tanFOV = Mathf.Tan(Mathf.Deg2Rad * 0.5f * c.fieldOfView);
        float n = c.nearClipPlane;
        float nearPlaneHeight = 2f * n * tanFOV;
        float nearPlaneWidth = c.aspect * nearPlaneHeight;
        _frustumVector = new Vector4(nearPlaneWidth, nearPlaneHeight, n, 0);

        _viewVector = new Vector4(eyeObject.forward.x, eyeObject.forward.y, eyeObject.forward.z, 0);
    }
}
