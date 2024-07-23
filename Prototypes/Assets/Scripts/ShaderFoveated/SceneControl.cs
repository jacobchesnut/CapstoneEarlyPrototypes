using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;  // for SceneView access, Selection
using System.Runtime.CompilerServices;
using Oculus.Voice.Windows;
using UnityEngine.Experimental.Rendering;
using System;
using Unity.VisualScripting;
using System.Drawing;

public class SceneControl : MonoBehaviour
{
    private const float FIRST_CPD_MODIFIER = 3393;
    private const float SECOND_CPD_MODIFIER = 1696;
    private const float THIRD_CPD_MODIFIER = 848;


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

    public enum testMode
    {
        NoTest = 0,
        OffsetTest = 1,
        BorderTest = 2,
        ImprecisionTest = 3,
        QualityTestFirstLeft = 4,
        QualityTestSecondLeft = 5,
        QualityTestThirdLeft = 6,
        QualityTestFirstRight = 7,
        QualityTestSecondRight = 8,
        QualityTestThirdRight = 9
    }

    private testMode testState = 0;

    //testing materials
    public Material OffsetTestMaterial = null;
    public Material BorderTestMaterial = null;
    public Material QualityTestMaterial = null;
    public Material BlackoutMaterial = null;

    //debug for testing materials
    public float cpdOverride = 0f;

    //testing variables
    private float testXOffset = 0f;
    private float testYOffset = 0f;
    private float testBorderAngle = 0f;
    private float testImprecision = 0f;
    private float testFirstQualityOffsetLeft = 90f;
    private float testSecondQualityOffsetLeft = 90f;
    private float testThirdQualityOffsetLeft = 90f;
    private float testFirstQualityOffsetRight = 90f;
    private float testSecondQualityOffsetRight = 90f;
    private float testThirdQualityOffsetRight = 90f;

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
        if (Input.GetKeyDown(KeyCode.Space))
        {
            switch (testState)
            {
                case testMode.NoTest:
                    testState = testMode.OffsetTest;
                    break;
                case testMode.OffsetTest:
                    testState = testMode.BorderTest;
                    break;
                case testMode.BorderTest:
                    testState = testMode.ImprecisionTest;
                    break;
                case testMode.ImprecisionTest:
                    testState = testMode.QualityTestFirstLeft;
                    break;
                case testMode.QualityTestFirstLeft:
                    testState = testMode.QualityTestSecondLeft;
                    break;
                case testMode.QualityTestSecondLeft:
                    testState = testMode.QualityTestThirdLeft;
                    break;
                case testMode.QualityTestThirdLeft:
                    testState = testMode.QualityTestFirstRight;
                    break;
                case testMode.QualityTestFirstRight:
                    testState = testMode.QualityTestSecondRight;
                    break;
                case testMode.QualityTestSecondRight:
                    testState = testMode.QualityTestThirdRight;
                    break;
                case testMode.QualityTestThirdRight:
                    writeCalibrationToFile(); //write out the info
                    testState = testMode.NoTest;
                    break;
                default:
                    Debug.LogError("teststate in unknown state!");
                    break;
            }
        }
        testBehavior();

        
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

    private void writeCalibrationToFile()
    {
        FileStream stream = File.Open("C:\\Users\\jakee\\CapstoneShared\\FoveatedCalibration.dat", FileMode.OpenOrCreate);
        BinaryWriter writer = new BinaryWriter(stream);
        //current order of writing:
        /*
         * testXOffset
         * testYOffset
         * testBorderAngle
         * testImprecision
         * testFirstQualityOffsetLeft
         * testSecondQualityOffsetLeft
         * testThirdQualityOffsetLeft
         * testFirstQualityOffsetRight
         * testSecondQualityOffsetRight
         * testThirdQualityOffsetRight
         */
        Debug.Log("writing user test data");
        writer.Write(testXOffset);
        Debug.Log("testXOffset = " + testXOffset);
        writer.Write(testYOffset);
        Debug.Log("testYOffset = " + testYOffset);
        writer.Write(testBorderAngle);
        Debug.Log("testBorderAngle = " + testBorderAngle);
        writer.Write(testImprecision);
        Debug.Log("testImprecision = " + testImprecision);
        writer.Write(testFirstQualityOffsetLeft);
        Debug.Log("testFirstQualityOffsetLeft = " + testFirstQualityOffsetLeft);
        writer.Write(testSecondQualityOffsetLeft);
        Debug.Log("testSecondQualityOffsetLeft = " + testSecondQualityOffsetLeft);
        writer.Write(testThirdQualityOffsetLeft);
        Debug.Log("testThirdQualityOffsetLeft = " + testThirdQualityOffsetLeft);
        writer.Write(testFirstQualityOffsetRight);
        Debug.Log("testFirstQualityOffsetRight = " + testFirstQualityOffsetRight);
        writer.Write(testSecondQualityOffsetRight);
        Debug.Log("testSecondQualityOffsetRight = " + testSecondQualityOffsetRight);
        writer.Write(testThirdQualityOffsetRight);
        Debug.Log("testThirdQualityOffsetRight = " + testThirdQualityOffsetRight);
        stream.Close();
    }

    private void testBehavior()
    {
        switch (testState)
        {
            case testMode.NoTest:
                //do nothing
                break;
            case testMode.OffsetTest:
                if (Input.GetKey(KeyCode.UpArrow))
                {
                    testYOffset -= 5 * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.DownArrow))
                {
                    testYOffset += 5 * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.RightArrow))
                {
                    testXOffset += 5 * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    testXOffset -= 5 * Time.deltaTime;
                }
                break;
            case testMode.BorderTest:
                if (Input.GetKey(KeyCode.UpArrow))
                {
                    testBorderAngle += 5 * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.DownArrow))
                {
                    testBorderAngle -= 5 * Time.deltaTime;
                }
                break;
            case testMode.ImprecisionTest:
                break;
            case testMode.QualityTestFirstLeft:
                if (Input.GetKey(KeyCode.RightArrow))
                {
                    testFirstQualityOffsetLeft += 5 * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    testFirstQualityOffsetLeft -= 5 * Time.deltaTime;
                }
                break;
            case testMode.QualityTestSecondLeft:
                if (Input.GetKey(KeyCode.RightArrow))
                {
                    testSecondQualityOffsetLeft += 5 * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    testSecondQualityOffsetLeft -= 5 * Time.deltaTime;
                }
                break;
            case testMode.QualityTestThirdLeft:
                if (Input.GetKey(KeyCode.RightArrow))
                {
                    testThirdQualityOffsetLeft += 5 * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    testThirdQualityOffsetLeft -= 5 * Time.deltaTime;
                }
                break;
            case testMode.QualityTestFirstRight:
                if (Input.GetKey(KeyCode.RightArrow))
                {
                    testFirstQualityOffsetRight += 5 * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    testFirstQualityOffsetRight -= 5 * Time.deltaTime;
                }
                break;
            case testMode.QualityTestSecondRight:
                if (Input.GetKey(KeyCode.RightArrow))
                {
                    testSecondQualityOffsetRight += 5 * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    testSecondQualityOffsetRight -= 5 * Time.deltaTime;
                }
                break;
            case testMode.QualityTestThirdRight:
                if (Input.GetKey(KeyCode.RightArrow))
                {
                    testThirdQualityOffsetRight += 5 * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    testThirdQualityOffsetRight -= 5 * Time.deltaTime;
                }
                break;
            default:
                Debug.LogError("teststate in unknown state!");
                break;
        }
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
        if(testState != testMode.NoTest)
        {
            testRender(src, dst, eyeLookVector, trueLookVector, frustumInformation);
            return;
        }

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

    private void testRender(RenderTexture src, RenderTexture dst, Vector3 eyeLookVector, Vector3 trueLookVector, Vector3 frustumInformation)
    {
        switch (testState)
        {
            case testMode.OffsetTest:
                OffsetTestMaterial.SetVector("_frustumVector", new Vector4(frustumInformation.x, frustumInformation.y, frustumInformation.z, 0));
                if (angleOffsetX < 0)
                {
                    OffsetTestMaterial.SetFloat("_offsetAngleX", -testXOffset);//degrees, reverse for left eye
                }
                else
                {
                    OffsetTestMaterial.SetFloat("_offsetAngleX", testXOffset);//degrees
                }
                OffsetTestMaterial.SetFloat("_offsetAngleY", testYOffset);//degrees
                Graphics.Blit(src, dst, OffsetTestMaterial);
                break;
            case testMode.BorderTest:
                BorderTestMaterial.SetVector("_frustumVector", new Vector4(frustumInformation.x, frustumInformation.y, frustumInformation.z, 0));
                if (angleOffsetX < 0)
                {
                    BorderTestMaterial.SetFloat("_offsetAngleX", -testXOffset);//degrees, reverse for left eye
                }
                else
                {
                    BorderTestMaterial.SetFloat("_offsetAngleX", testXOffset);//degrees
                }
                BorderTestMaterial.SetFloat("_offsetAngleY", testYOffset);//degrees
                BorderTestMaterial.SetFloat("_outsideAngle", testBorderAngle * Mathf.Deg2Rad); //radians
                Graphics.Blit(src, dst, BorderTestMaterial);
                break;
            case testMode.ImprecisionTest:
                testImprecision = Vector3.Angle(eyeLookVector, trueLookVector);
                Graphics.Blit(src, dst);
                break;
            case testMode.QualityTestFirstLeft:
                QualityTestMaterial.SetVector("_frustumVector", new Vector4(frustumInformation.x, frustumInformation.y, frustumInformation.z, 0));
                if (angleOffsetX < 0)
                {
                    QualityTestMaterial.SetFloat("_offsetAngleX", -testXOffset);//degrees, reverse for left eye
                }
                else
                {
                    //right eye, do not render
                    Graphics.Blit(src, dst, BlackoutMaterial);
                    break;
                }
                QualityTestMaterial.SetFloat("_offsetAngleY", testYOffset);//degrees
                QualityTestMaterial.SetFloat("_addedXOffset", -testFirstQualityOffsetLeft);//degrees
                QualityTestMaterial.SetFloat("_cpdOverride", cpdOverride); //override for debug
                QualityTestMaterial.SetFloat("_cpdMultiplier", FIRST_CPD_MODIFIER); //6cpd
                Graphics.Blit(src, dst, QualityTestMaterial);
                break;
            case testMode.QualityTestSecondLeft:
                QualityTestMaterial.SetVector("_frustumVector", new Vector4(frustumInformation.x, frustumInformation.y, frustumInformation.z, 0));
                if (angleOffsetX < 0)
                {
                    QualityTestMaterial.SetFloat("_offsetAngleX", -testXOffset);//degrees, reverse for left eye
                }
                else
                {
                    //right eye, do not render
                    Graphics.Blit(src, dst, BlackoutMaterial);
                    break;
                }
                QualityTestMaterial.SetFloat("_offsetAngleY", testYOffset);//degrees
                QualityTestMaterial.SetFloat("_addedXOffset", -testSecondQualityOffsetLeft);//degrees
                QualityTestMaterial.SetFloat("_cpdOverride", cpdOverride); //override for debug
                QualityTestMaterial.SetFloat("_cpdMultiplier", SECOND_CPD_MODIFIER); //3cpd
                Graphics.Blit(src, dst, QualityTestMaterial);
                break;
            case testMode.QualityTestThirdLeft:
                QualityTestMaterial.SetVector("_frustumVector", new Vector4(frustumInformation.x, frustumInformation.y, frustumInformation.z, 0));
                if (angleOffsetX < 0)
                {
                    QualityTestMaterial.SetFloat("_offsetAngleX", -testXOffset);//degrees, reverse for left eye
                }
                else
                {
                    //right eye, do not render
                    Graphics.Blit(src, dst, BlackoutMaterial);
                    break;
                }
                QualityTestMaterial.SetFloat("_offsetAngleY", testYOffset);//degrees
                QualityTestMaterial.SetFloat("_addedXOffset", -testThirdQualityOffsetLeft);//degrees
                QualityTestMaterial.SetFloat("_cpdOverride", cpdOverride); //override for debug
                QualityTestMaterial.SetFloat("_cpdMultiplier", THIRD_CPD_MODIFIER); //1.5cpd
                Graphics.Blit(src, dst, QualityTestMaterial);
                break;
            case testMode.QualityTestFirstRight:
                QualityTestMaterial.SetVector("_frustumVector", new Vector4(frustumInformation.x, frustumInformation.y, frustumInformation.z, 0));
                if (angleOffsetX < 0)
                {
                    //left eye, do not render
                    Graphics.Blit(src, dst, BlackoutMaterial);
                    break;
                }
                else
                {
                    QualityTestMaterial.SetFloat("_offsetAngleX", testXOffset);//degrees
                }
                QualityTestMaterial.SetFloat("_offsetAngleY", testYOffset);//degrees
                QualityTestMaterial.SetFloat("_addedXOffset", -testFirstQualityOffsetRight);//degrees
                QualityTestMaterial.SetFloat("_cpdOverride", cpdOverride); //override for debug
                QualityTestMaterial.SetFloat("_cpdMultiplier", FIRST_CPD_MODIFIER); //6cpd
                Graphics.Blit(src, dst, QualityTestMaterial);
                break;
            case testMode.QualityTestSecondRight:
                QualityTestMaterial.SetVector("_frustumVector", new Vector4(frustumInformation.x, frustumInformation.y, frustumInformation.z, 0));
                if (angleOffsetX < 0)
                {
                    //left eye, do not render
                    Graphics.Blit(src, dst, BlackoutMaterial);
                    break;
                }
                else
                {
                    QualityTestMaterial.SetFloat("_offsetAngleX", testXOffset);//degrees
                }
                QualityTestMaterial.SetFloat("_offsetAngleY", testYOffset);//degrees
                QualityTestMaterial.SetFloat("_addedXOffset", -testSecondQualityOffsetRight);//degrees
                QualityTestMaterial.SetFloat("_cpdOverride", cpdOverride); //override for debug
                QualityTestMaterial.SetFloat("_cpdMultiplier", SECOND_CPD_MODIFIER); //3cpd
                Graphics.Blit(src, dst, QualityTestMaterial);
                break;
            case testMode.QualityTestThirdRight:
                QualityTestMaterial.SetVector("_frustumVector", new Vector4(frustumInformation.x, frustumInformation.y, frustumInformation.z, 0));
                if (angleOffsetX < 0)
                {
                    //left eye, do not render
                    Graphics.Blit(src, dst, BlackoutMaterial);
                    break;
                }
                else
                {
                    QualityTestMaterial.SetFloat("_offsetAngleX", testXOffset);//degrees
                }
                QualityTestMaterial.SetFloat("_offsetAngleY", testYOffset);//degrees
                QualityTestMaterial.SetFloat("_addedXOffset", -testThirdQualityOffsetRight);//degrees
                QualityTestMaterial.SetFloat("_cpdOverride", cpdOverride); //override for debug
                QualityTestMaterial.SetFloat("_cpdMultiplier", THIRD_CPD_MODIFIER); //1.5cpd
                Graphics.Blit(src, dst, QualityTestMaterial);
                break;
            default:
                Debug.LogError("teststate in unknown state!");
                break;
        }
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
