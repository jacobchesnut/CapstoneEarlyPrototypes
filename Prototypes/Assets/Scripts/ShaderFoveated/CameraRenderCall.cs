using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

//this class is attached to the main camera to alert other scripts when
//on render image has been called
public class CameraRenderCall : MonoBehaviour
{
    //enable or disable printing out of unity's frames at interim points during rendering events
    //these are mostly disinteresting, as all of unity's rendering code is handled between onprerender and onpostrender
    //but it does show the order of cameras processed, and the amount of used rendertextures.
    private const bool SHOW_EXTENDED_DEBUG_FRAMES = false;

    public SceneControl ScriptToMessage;
    public Transform eyeObject = null;
    public Transform targetObject = null; //object for determining actual looking direction of eyes
    public Transform simTarget = null; //used for getting target's position in camera space
    public string cameraName = "null";
    //calibration info
    public float angleOffsetX = 0f;
    public float angleOffsetY = 0f;
    public float upOutsideAngle = 90f;
    public float rightOutsideAngle = 90f;
    public float downOutsideAngle = 90f;
    public float leftOutsideAngle = 90f;

    //attempting to access intermediate texture stages by saving render texture references
    private RenderTexture savedSRCTexture = null;
    private RenderTexture savedDSTTexture = null;
    private RenderTexture savedTexture = null;

    private void Start()
    {
        //frustum calculation math by Kelvin Sung from CSS451 draw camera frustum
        //link: https://github.com/myuwbclasses/CSS451/blob/master/ClassExamples/Topic6-3DViewing/4.DrawCameraFrustum/Assets/Source/CameraManipluation/CameraManipulation_DrawFrustum.cs
        Vector3 eye = transform.localPosition;
        Camera c = GetComponent<Camera>();
        float tanFOV = Mathf.Tan(Mathf.Deg2Rad * 0.5f * c.fieldOfView);
        // near plane dimension
        float n = c.nearClipPlane;
        float nearPlaneHeight = 2f * n * tanFOV;
        float nearPlaneWidth = c.aspect * nearPlaneHeight;
        Vector3 frustumInformation = new Vector3(nearPlaneWidth, nearPlaneHeight, n);
        //send the distance, height, and width to the scene control
        ScriptToMessage.setFrustumSettings(frustumInformation);
    }

    private void OnPreCull()
    {
        if (savedSRCTexture != null)
        {
            if (SHOW_EXTENDED_DEBUG_FRAMES)
            {
                ScriptToMessage.cameraNameToCapture = cameraName;
                ScriptToMessage.captureRenderTexture(savedSRCTexture, "SRCOnPreCull");
            }
        }
        if (savedDSTTexture != null)
        {
            if (SHOW_EXTENDED_DEBUG_FRAMES)
            {
                ScriptToMessage.cameraNameToCapture = cameraName;
                ScriptToMessage.captureRenderTexture(savedDSTTexture, "DSTOnPreCull");
            }
        }
    }

    private void OnPreRender()
    {
        if (savedSRCTexture != null)
        {
            if (SHOW_EXTENDED_DEBUG_FRAMES)
            {
                ScriptToMessage.cameraNameToCapture = cameraName;
                ScriptToMessage.captureRenderTexture(savedSRCTexture, "SRCOnPreRender");
            }
        }
        if (savedDSTTexture != null)
        {
            if (SHOW_EXTENDED_DEBUG_FRAMES)
            {
                ScriptToMessage.cameraNameToCapture = cameraName;
                ScriptToMessage.captureRenderTexture(savedDSTTexture, "DSTOnPreRender");
            }
        }
    }

    private void OnPostRender()
    {
        if (savedSRCTexture != null)
        {
            if (SHOW_EXTENDED_DEBUG_FRAMES)
            {
                ScriptToMessage.cameraNameToCapture = cameraName;
                ScriptToMessage.captureRenderTexture(savedSRCTexture, "SRCOnPostRender");
            }
        }
        if (savedDSTTexture != null)
        {
            if (SHOW_EXTENDED_DEBUG_FRAMES)
            {
                ScriptToMessage.cameraNameToCapture = cameraName;
                ScriptToMessage.captureRenderTexture(savedDSTTexture, "DSTOnPostRender");
            }
        }
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (savedSRCTexture == null)
        {
            savedSRCTexture = src;
        }
        if(savedDSTTexture == null)
        {
            savedDSTTexture = dst;
        }
        if(savedTexture == null)
        {
            savedTexture = new RenderTexture(src.width, src.height, src.depth);
            Graphics.Blit(src, savedTexture); //set to src for the first pass
        }
        //set the camera's name for the potential printout
        ScriptToMessage.cameraNameToCapture = cameraName;
        //set the calibration info
        ScriptToMessage.angleOffsetX = angleOffsetX;
        ScriptToMessage.angleOffsetY = angleOffsetY;
        ScriptToMessage.upOutsideAngle = upOutsideAngle;
        ScriptToMessage.rightOutsideAngle = rightOutsideAngle;
        ScriptToMessage.downOutsideAngle = downOutsideAngle;
        ScriptToMessage.leftOutsideAngle = leftOutsideAngle;
        if (eyeObject == null)
        {
            //if no eye object send to call with no vector
            ScriptToMessage.RenderingImage(src, dst);
            return;
        }
        Camera c = GetComponent<Camera>();
        float tanFOV = Mathf.Tan(Mathf.Deg2Rad * 0.5f * c.fieldOfView);
        float n = c.nearClipPlane;
        float nearPlaneHeight = 2f * n * tanFOV;
        float nearPlaneWidth = c.aspect * nearPlaneHeight;
        Vector3 frustumInformation = new Vector3(nearPlaneWidth, nearPlaneHeight, n);
        //make a transform rotate opposite the camera and get it's position to get the vector to the target in camera space
        simTarget.position = targetObject.position; //place it in world space
        simTarget.RotateAround(transform.position, new Vector3(-1, 0, 0), transform.rotation.eulerAngles.x); //backwards x
        simTarget.RotateAround(transform.position, new Vector3(0, -1, 0), transform.rotation.eulerAngles.y); //backwards y
        Vector3 trueLookInformation = simTarget.position - transform.position; //the direction of this vector should now be correct relative to camera space
        //otherwise send eye vector and frustum
        ScriptToMessage.RenderingImage(src, dst, eyeObject.forward, trueLookInformation, frustumInformation, savedTexture);
        //now blit the current frame over
        Graphics.Blit(dst, savedTexture);
    }

    
}
