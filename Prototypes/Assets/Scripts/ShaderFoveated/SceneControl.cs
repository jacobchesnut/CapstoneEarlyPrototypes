using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;  // for SceneView access, Selection
using System.Runtime.CompilerServices;
using Oculus.Voice.Windows;
using UnityEngine.Experimental.Rendering;
using System;

public class SceneControl : MonoBehaviour
{
    //const int kUseTexture = 1;
    //const int kCompAmbient = 2;
    //const int kCompDiffuse = 4;
    //const int kCompSpecular = 8;
    //const int kCompDistAtten = 16;
    //const int kCompAngularAtten = 32;
    
    //private static int kNumLights = 4; // must be identical to the M2_Shader


    //public bool UseTexture = false;
    //public bool ComputeAmbient = false;
    //public bool ComputeDiffuse = false;
    //public bool ComputeSpecular = false;
    //public bool ComputeDistanceAttenuation = false;
    //public bool ComputeAngularAttenuation = false;

    //info about frustum
    private Vector3 frustumInfo = Vector3.one;
    //info about gaze
    public Vector3 gazeInfo = Vector3.one;
    //size of non-foveated zone
    public float innerAngle = 15f;
    //size of regions
    public int regionSize = 2;
    //size of border region
    public float borderRegionSize = 2f;
    //debug to show regions with tint
    public bool showRegionsTint = true;
    private int kShowRegionsTint = 1;
    //debug to show overlay
    public bool showOverlay = true;
    private int kShowOverlay = 2;
    //toggle for the averaged border
    public bool showAverageBorder = true;
    private int kShowAverageBorder = 4;
    //toggle for the variable border
    public bool showVariableBorder = true;
    private int kShowVariableBorder = 8;

    //timer for frame capture (number of frames to capture)
    public int framesToCapture = 30; //30 frames across 2 cameras = 0.25 seconds of capture at 60fps
    public string cameraNameToCapture = "null"; //string to be set for file naming purposes
    private int framesLeft = 0;

    //variables for holding calibration info (public for testing purposes)
    public float angleOffsetX = 0f;
    public float angleOffsetY = 0f;
    public float upOutsideAngle = 90f;
    public float rightOutsideAngle = 90f;
    public float downOutsideAngle = 90f;
    public float leftOutsideAngle = 90f;

    //enum to select which light to use in specular calculations
    //public enum SelectCamera
    //{
    //    eMainCamera,
    //    eEditorCamera
    //};
    //public SelectCamera CameraToUse = SelectCamera.eMainCamera;


    //public float N = 1;
    //public float F = 10;
    //public float FogHeight = 1f;
    //public float FogDensity = 1.0f;
    //public Color FogColor = Color.white;
    // for debug support
    /*
    public enum DebugShowFlag
    {
        DebugOff = 0,

        DebugShowNear = 1,

        DebugShowBlend = 2
    };

    public enum FogAlgorithm
    {
        LinearFog = 4,
        ExponentialFog = 8
    }

    public enum RenderOrderType
    {
        OnlyFog = 1,
        OnlyLens = 2,
        FogThenLens = 3,
        LensThenFog = 4
    }
    public DebugShowFlag DebugFlag = DebugShowFlag.DebugOff;
    public FogAlgorithm FogType = FogAlgorithm.LinearFog;
    public RenderOrderType RenderOrder = RenderOrderType.OnlyFog;

    public bool EnableBackgoundFog = true;
    private const int BackgroundFog = 16;
    */
    public Material FoveatedMat = null;

    //material for blur
    public Material BlurMaterial = null;

    //render texture for blur
    private RenderTexture middle = null;

    //ground plane to calculate distances from:
    //public Transform groundPlaneTransform = null;

    //public float FogDenominator = 1f;


    void Start()
    {
        Debug.Assert(FoveatedMat != null);
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) //hit P to collect the next rendered frames and capture to disk
        {
            framesLeft = framesToCapture;
        }
        // Sets per-scene information
        //int BitMask = calculateBitmask();
        //send bitmask
        //Shader.SetGlobalInt("_ShaderMode", BitMask);

        //send camera position
        /*
        Vector4 CameraPos = Vector4.zero;
        switch (CameraToUse)
        {
            case SelectCamera.eMainCamera:
                CameraPos = Camera.main.transform.localPosition;
                break;
            case SelectCamera.eEditorCamera:
                CameraPos = SceneView.lastActiveSceneView.camera.transform.localPosition;
                break;
        }
        Shader.SetGlobalVector("_MyCameraPosition", CameraPos);
        */

        //Foveated specific
        FoveatedMat.SetVector("_frustumVector", new Vector4(frustumInfo.x, frustumInfo.y, frustumInfo.z, 0));
        FoveatedMat.SetVector("_viewVector", new Vector4(gazeInfo.x, gazeInfo.y, gazeInfo.z ,0));
        FoveatedMat.SetFloat("_widthPix", Screen.width);
        FoveatedMat.SetFloat("_heightPix", Screen.height);
        //invwidth and invheight possibly not needed
        float invW = 1.0f / (float)Camera.main.pixelWidth;
        float invH = 1.0f / (float)Camera.main.pixelHeight;
        FoveatedMat.SetFloat("_invWidth", invW);
        FoveatedMat.SetFloat("_invHeight", invH);
        FoveatedMat.SetFloat("_innerAngleMax", Mathf.Deg2Rad * innerAngle);
        FoveatedMat.SetInt("_regionSize", regionSize);
        FoveatedMat.SetFloat("_debugRegionBorderSize", Mathf.Deg2Rad * borderRegionSize);
        //set flag
        int theFlag = calculateBitmask();
        FoveatedMat.SetInteger("_flag", theFlag);


        //Blur
        BlurMaterial.SetVector("_frustumVector", new Vector4(frustumInfo.x, frustumInfo.y, frustumInfo.z, 0));
        BlurMaterial.SetVector("_viewVector", new Vector4(gazeInfo.x, gazeInfo.y, gazeInfo.z, 0));
        BlurMaterial.SetFloat("_widthPix", Screen.width);
        BlurMaterial.SetFloat("_heightPix", Screen.height);
        //invwidth and invheight possibly not needed
        BlurMaterial.SetFloat("_invWidth", invW);
        BlurMaterial.SetFloat("_invHeight", invH);
        BlurMaterial.SetFloat("_innerAngleMax", Mathf.Deg2Rad * innerAngle);
        BlurMaterial.SetInt("_regionSize", regionSize);
        BlurMaterial.SetFloat("_debugRegionBorderSize", Mathf.Deg2Rad * borderRegionSize);
        //set flag
        BlurMaterial.SetInteger("_flag", theFlag);

        /*
        int f = (int)DebugFlag | (int)FogType;
        if (EnableBackgoundFog)
        {
            f |= BackgroundFog;
        }
        // Debug.Log("Flag = " + f);
        FoveatedMat.SetInt("_flag", f);*/
    }

    
    private int calculateBitmask()
    {
        int BM = 0;
        if (showRegionsTint)
        {
            BM |= kShowRegionsTint;
        }
        if (showOverlay)
        {
            BM |= kShowOverlay;
        }
        if (showAverageBorder)
        {
            BM |= kShowAverageBorder;
        }
        if (showVariableBorder)
        {
            BM |= kShowVariableBorder;
        }
        return BM;
    }

    public void RenderingImage(RenderTexture src, RenderTexture dst)
    {
        //simplifying this to one pass, just need to group far pixels based on foveation
        Graphics.Blit(src, dst, FoveatedMat);
        //function messes with active render texture, so make sure dst is called last
        captureRenderTexture(src, "src");
        captureRenderTexture(dst, "dst");
    }

    public void RenderingImage(RenderTexture src, RenderTexture dst, Vector3 eyeLookVector)
    {
        //set the eye look vector as the used direction for the shader
        FoveatedMat.SetVector("_viewVector", new Vector4(eyeLookVector.x, eyeLookVector.y, eyeLookVector.z, 0));
        //simplifying this to one pass, just need to group far pixels based on foveation
        Graphics.Blit(src, dst, FoveatedMat);
        //function messes with active render texture, so make sure dst is called last
        captureRenderTexture(src, "src");
        captureRenderTexture(dst, "dst");
    }

    public void RenderingImage(RenderTexture src, RenderTexture dst, Vector3 eyeLookVector, Vector3 frustumInformation)
    {
        //set camera frustum info
        FoveatedMat.SetVector("_frustumVector", new Vector4(frustumInformation.x, frustumInformation.y, frustumInformation.z, 0));
        //set the eye look vector as the used direction for the shader
        FoveatedMat.SetVector("_viewVector", new Vector4(eyeLookVector.x, eyeLookVector.y, eyeLookVector.z, 0));
        //simplifying this to one pass, just need to group far pixels based on foveation
        Graphics.Blit(src, dst, FoveatedMat);
        //function messes with active render texture, so make sure dst is called last
        captureRenderTexture(src, "src");
        captureRenderTexture(dst, "dst");
    }

    public void RenderingImage(RenderTexture src, RenderTexture dst, Vector3 eyeLookVector, Vector3 trueLookVector, Vector3 frustumInformation, RenderTexture oldTex)
    {
        if(middle == null)
        {
            middle = new RenderTexture(src);
        }

        //give previous frame info
        FoveatedMat.SetTexture("_PreviousTex", oldTex);
        //set camera frustum info
        FoveatedMat.SetVector("_frustumVector", new Vector4(frustumInformation.x, frustumInformation.y, frustumInformation.z, 0));
        //set the eye look vector as the used direction for the shader
        FoveatedMat.SetVector("_viewVector", new Vector4(eyeLookVector.x, eyeLookVector.y, eyeLookVector.z, 0));
        //set the actual look vector
        FoveatedMat.SetVector("_trueViewVector", new Vector4(trueLookVector.x, trueLookVector.y, trueLookVector.z, 0));
        //set calibration info
        FoveatedMat.SetFloat("_offsetAngleX", angleOffsetX);//degrees
        FoveatedMat.SetFloat("_offsetAngleY", angleOffsetY);//degrees
        FoveatedMat.SetFloat("_offscreenAngleUp", Mathf.Deg2Rad * upOutsideAngle);
        FoveatedMat.SetFloat("_offscreenAngleRight", Mathf.Deg2Rad * rightOutsideAngle);
        FoveatedMat.SetFloat("_offscreenAngleDown", Mathf.Deg2Rad * downOutsideAngle);
        FoveatedMat.SetFloat("_offscreenAngleLeft", Mathf.Deg2Rad * leftOutsideAngle);

        //give previous frame info
        BlurMaterial.SetTexture("_PreviousTex", oldTex);
        //set camera frustum info
        BlurMaterial.SetVector("_frustumVector", new Vector4(frustumInformation.x, frustumInformation.y, frustumInformation.z, 0));
        //set the eye look vector as the used direction for the shader
        BlurMaterial.SetVector("_viewVector", new Vector4(eyeLookVector.x, eyeLookVector.y, eyeLookVector.z, 0));
        //set the actual look vector
        BlurMaterial.SetVector("_trueViewVector", new Vector4(trueLookVector.x, trueLookVector.y, trueLookVector.z, 0));
        //set calibration info
        BlurMaterial.SetFloat("_offsetAngleX", angleOffsetX);//degrees
        BlurMaterial.SetFloat("_offsetAngleY", angleOffsetY);//degrees
        BlurMaterial.SetFloat("_offscreenAngleUp", Mathf.Deg2Rad * upOutsideAngle);
        BlurMaterial.SetFloat("_offscreenAngleRight", Mathf.Deg2Rad * rightOutsideAngle);
        BlurMaterial.SetFloat("_offscreenAngleDown", Mathf.Deg2Rad * downOutsideAngle);
        BlurMaterial.SetFloat("_offscreenAngleLeft", Mathf.Deg2Rad * leftOutsideAngle);

        //simplifying this to one pass, just need to group far pixels based on foveation
        Graphics.Blit(src, middle, FoveatedMat);
        Graphics.Blit(middle, dst, BlurMaterial);
        //function messes with active render texture, so make sure dst is called last
        captureRenderTexture(src, "src");
        captureRenderTexture(dst, "dst");
    }

    public void captureRenderTexture(RenderTexture renderToCapture, string additionalName = "")
    {
        if(framesLeft > 0)
        {
            framesLeft--;
            //save to the name of the camera, and current date
            //methods to call and process sourced from:
            //https://stackoverflow.com/questions/44264468/convert-rendertexture-to-texture2d
            Texture2D temp = new Texture2D(renderToCapture.width, renderToCapture.height);//format and other seem to be set correctly by default
            RenderTexture.active = renderToCapture;
            temp.ReadPixels(new Rect(0, 0, renderToCapture.width, renderToCapture.height), 0, 0);
            temp.Apply();
            File.WriteAllBytes(DateTime.Now.ToFileTime() + "_" + additionalName + cameraNameToCapture + ".png", temp.EncodeToPNG());
        }
    }

    public void setFrustumSettings(Vector3 frustumInformation)
    {
        frustumInfo = frustumInformation;
    }
}
