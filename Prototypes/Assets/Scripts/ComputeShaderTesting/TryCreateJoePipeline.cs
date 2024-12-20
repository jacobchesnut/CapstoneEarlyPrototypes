using OpenRT;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Diagnostics;

public class TryCreateJoePipeline : MonoBehaviour
{
    //consts for global use
    //default width meta uses = 2160
    public static int RENDER_TEXTURE_WIDTH = 2160;
    //default height meta uses = 2224
    public static int RENDER_TEXTURE_HEIGHT = 2224;
    //average region info
    public static float DEFAULT_FIRST_REGION = 0.222f;
    public static float DEFAULT_SECOND_REGION = 0.485f;
    public static float DEFAULT_THIRD_REGION = 1.010f;

    private static bool DEBUG_PRINT_CALIBRATION_INFO = true;


    //for creating the pipeline
    public List<RenderPipelineConfigObject> m_config;
    public Color clearColor = Color.green;
    public ComputeShader mainShader; //set in editor

    public RenderTexture[] textureToRenderTo; //will eventually need array
    public Camera[] camerasToRenderTo;

    private BasicPipeInstance joePipeInstance = null;
    public bool onlyOnce = true;
    public bool reduceObjectGhosting = false;
    public bool cullNoRenderZone = true;
    public bool disableFoveatedRendering = false;
    public bool disableRendering = false;
    public bool disableBlur = false;

    //vars for creating diffs
    public RenderTexture[] pastTextureToRenderTo;
    public RenderTexture differenceTexture;
    public ComputeShader differenceShader; //set in editor

    //paramaters to set in editor
    public float innerAngleMax = 15f; //foveated region
    public bool showTint;
    public bool showOverlay;
    public float tintBorderSize = 1f;
    public int MaxTAAFrame = 3;
    public float TAAWeightFactor = 0.9f;
    public float TriangularWeightFactor = 0f;
    public bool UseGaussianTAA = false;
    public Material BlurMaterial = null;

    //config parameters from user tests
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

    //additional variables
    private Vector3 pastCamPosition = new Vector3();
    private Vector3 pastCamRotation = new Vector3();

    //for coroutine
    private IEnumerator endOfFrameCoroutine;

    //for timing
    private Stopwatch fullFrameStopwatch = new Stopwatch();

    // Start is called before the first frame update
    void Start()
    {
        joePipeInstance = new BasicPipeInstance(clearColor, mainShader, m_config, BlurMaterial);
        textureToRenderTo = new RenderTexture[camerasToRenderTo.Length];
        pastTextureToRenderTo = new RenderTexture[camerasToRenderTo.Length];
        for (int i = 0; i < camerasToRenderTo.Length; i++)
        {
            //textureToRenderTo[i] = new RenderTexture(camerasToRenderTo[i].scaledPixelWidth, camerasToRenderTo[i].scaledPixelHeight, 0);
            textureToRenderTo[i] = new RenderTexture(RENDER_TEXTURE_WIDTH, RENDER_TEXTURE_HEIGHT, 0); //hard coded to vr dimensions because of oddities with camera pixel width and height
            textureToRenderTo[i].enableRandomWrite = true;
            pastTextureToRenderTo[i] = new RenderTexture(RENDER_TEXTURE_WIDTH, RENDER_TEXTURE_HEIGHT, 0);
            pastTextureToRenderTo[i].enableRandomWrite = true;
        }

        differenceTexture = new RenderTexture(RENDER_TEXTURE_WIDTH, RENDER_TEXTURE_HEIGHT, 0);
        differenceTexture.enableRandomWrite = true;
        differenceTexture.Create();

        differenceShader.SetTexture(0, "Result", differenceTexture);
        //textureToRenderTo = new RenderTexture(Screen.width, Screen.height, 0); //currently screen width and height, will need pixel counts for VR camera

        readCalibrationInfo();

        endOfFrameCoroutine = TimeEndOfFrame();
        StartCoroutine(endOfFrameCoroutine);


        //use to test accuracy of stopwatch
        //long frequency = Stopwatch.Frequency;
        //UnityEngine.Debug.LogWarning("Timer frequency in ticks per second = " + frequency);
        //long nanosecPerTick = (1000L * 1000L * 1000L) / frequency;
        //UnityEngine.Debug.LogWarning("Timer is accurate within " + nanosecPerTick + "nanoseconds");
    }

    private IEnumerator TimeEndOfFrame()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();
            /*if (GlobalTimer.endOnFrameStart.IsRunning)
            {
                GlobalTimer.endOnFrameStart.Stop();
                UnityEngine.Debug.Log("time to frame end: " + GlobalTimer.endOnFrameStart.Elapsed);
                ExcelLogHandler.endFrameTimes.Add(GlobalTimer.endOnFrameStart.Elapsed.TotalMilliseconds);
                GlobalTimer.endOnFrameStart.Reset();
            }*/ //not doing it here anymore because it doesn't seem like unity actually blocks for GPU yet
        }
    }

    // Update is called once per frame
    void Update()
    {
        //this is roughly the start of a frame
        //here instead of global timer in order to avoid having a single instance of global timer in scene
        if (GlobalTimer.endOnFrameStart.IsRunning)
        {
            GlobalTimer.endOnFrameStart.Stop();
            UnityEngine.Debug.Log("time to frame end: " + GlobalTimer.endOnFrameStart.Elapsed);
            ExcelLogHandler.endFrameTimes.Add(GlobalTimer.endOnFrameStart.Elapsed.TotalMilliseconds);
            GlobalTimer.endOnFrameStart.Reset();
        }

        //also start timing for end of frame
        if (fullFrameStopwatch.IsRunning)
        {
            fullFrameStopwatch.Stop();
            UnityEngine.Debug.Log("time for total frame: " + fullFrameStopwatch.Elapsed);
            UnityEngine.Debug.Log("time difference between elapsed and time delta time: " + (fullFrameStopwatch.Elapsed.TotalMilliseconds - (Time.deltaTime * 1000)));
            ExcelLogHandler.totalFrameTimes.Add(fullFrameStopwatch.Elapsed.TotalMilliseconds);
            fullFrameStopwatch.Reset();
        }
        fullFrameStopwatch.Start();//start here to measure the full frame


        if (Input.GetKeyDown(KeyCode.P))
        {
            onlyOnce = true;
        }
        //change the texture resolution if - or = is hit, halve (if possible) or double, respectively
        if (Input.GetKeyDown(KeyCode.Minus))
        {
            if(RENDER_TEXTURE_WIDTH % 2 == 0 && RENDER_TEXTURE_HEIGHT % 2 == 0) //we can halve without rounding, preserving height to width ratio
            {
                RENDER_TEXTURE_WIDTH /= 2;
                RENDER_TEXTURE_HEIGHT /= 2;
            }
        }
        if (Input.GetKeyDown(KeyCode.Equals))
        {
            RENDER_TEXTURE_WIDTH *= 2;
            RENDER_TEXTURE_HEIGHT *= 2;
        }
    }

    private void readCalibrationInfo()
    {
        FileStream stream = File.Open("C:\\Users\\jakee\\CapstoneShared\\FoveatedCalibration.dat", FileMode.Open);
        BinaryReader reader = new BinaryReader(stream);
        testXOffset = reader.ReadSingle();
        testYOffset = reader.ReadSingle();
        testYOffset += 12;
        testBorderAngle = reader.ReadSingle();
        testImprecision = reader.ReadSingle();
        testFirstQualityOffsetLeft = reader.ReadSingle();
        testFirstQualityOffsetLeft = ((testFirstQualityOffsetLeft * Mathf.Deg2Rad) + DEFAULT_FIRST_REGION) / 2;
        testSecondQualityOffsetLeft = reader.ReadSingle();
        testSecondQualityOffsetLeft = ((testSecondQualityOffsetLeft * Mathf.Deg2Rad) + DEFAULT_SECOND_REGION) / 2;
        testThirdQualityOffsetLeft = reader.ReadSingle();
        testThirdQualityOffsetLeft = ((testThirdQualityOffsetLeft * Mathf.Deg2Rad) + DEFAULT_THIRD_REGION) / 2;
        testFirstQualityOffsetRight = reader.ReadSingle();
        testFirstQualityOffsetRight = ((testFirstQualityOffsetRight * Mathf.Deg2Rad) + DEFAULT_FIRST_REGION) / 2;
        testSecondQualityOffsetRight = reader.ReadSingle();
        testSecondQualityOffsetRight = ((testSecondQualityOffsetRight * Mathf.Deg2Rad) + DEFAULT_SECOND_REGION) / 2;
        testThirdQualityOffsetRight = reader.ReadSingle();
        testThirdQualityOffsetRight = ((testThirdQualityOffsetRight * Mathf.Deg2Rad) + DEFAULT_THIRD_REGION) / 2;
        reader.Close();
        stream.Close();

        if (DEBUG_PRINT_CALIBRATION_INFO)
        {
            UnityEngine.Debug.Log("XOffset is " + testXOffset);
            UnityEngine.Debug.Log("YOffset is " + testYOffset);
            UnityEngine.Debug.Log("border angle is: " + testBorderAngle);
            UnityEngine.Debug.Log("Left regions are: " + Mathf.Rad2Deg * testFirstQualityOffsetLeft + " " + 
                                  Mathf.Rad2Deg * testSecondQualityOffsetLeft + " " + 
                                  Mathf.Rad2Deg * testThirdQualityOffsetLeft + "\n" +
                                  "Right regions are: " + Mathf.Rad2Deg * testFirstQualityOffsetRight + 
                                  " " + Mathf.Rad2Deg * testSecondQualityOffsetRight + " " + 
                                  Mathf.Rad2Deg * testThirdQualityOffsetRight);
        }
    }

    //only gets called if camera attached to object
    private void OnPreRender()
    {
        GlobalTimer.StartStopwatch(3);
        //Debug.Log("in prerender");
        //create the texture on pre render, this can then be sent out oncamerarender by other scripts with a reference here
        ScriptableRenderContext contextToUse = new ScriptableRenderContext();
        //Debug.Log("cam length: " + camerasToRenderTo.Length);

        //send foveated information to pipeline through struct
        ShaderFoveatedInfo infoToSend = new ShaderFoveatedInfo();
        infoToSend._UseTAAObjectID = reduceObjectGhosting;
        infoToSend._DisableRendering = disableRendering;
        infoToSend._TriangularDecreaseFactor = TriangularWeightFactor;
        infoToSend._UseGaussianTAA = UseGaussianTAA;
        infoToSend._cullNoRender = cullNoRenderZone;
        //set camera frustum info
        CameraInfoReporter camInfo;
        infoToSend._frustumVector = new Vector4[camerasToRenderTo.Length];   //(frustumInformation.x, frustumInformation.y, frustumInformation.z, 0);
        for(int i = 0; i < infoToSend._frustumVector.Length; i++)
        {
            camInfo = camerasToRenderTo[i].GetComponent<CameraInfoReporter>();
            if(camInfo == null)
            {
                //set default info
                infoToSend._frustumVector[i] = Vector4.one;
            }
            else
            {
                infoToSend._frustumVector[i] = camInfo._frustumVector;
            }
        }
        //set the eye look vector as the used direction for the shader
        infoToSend._viewVector = new Vector4[camerasToRenderTo.Length];   //(eyeLookVector.x, eyeLookVector.y, eyeLookVector.z, 0);
        for (int i = 0; i < infoToSend._viewVector.Length; i++)
        {
            camInfo = camerasToRenderTo[i].GetComponent<CameraInfoReporter>();
            if (camInfo == null)
            {
                //set default info
                infoToSend._viewVector[i] = Vector4.one;
            }
            else
            {
                infoToSend._viewVector[i] = camInfo._viewVector;
            }
        }
        infoToSend._innerAngleMax = innerAngleMax;
        infoToSend._showTint = showTint;
        infoToSend._showOverlay = showOverlay;
        infoToSend._debugRegionBorderSize = Mathf.Deg2Rad * tintBorderSize;

        //calibration info
        infoToSend._XOffset = testXOffset;
        infoToSend._YOffset = testYOffset;
        infoToSend._BorderAngle = testBorderAngle;
        infoToSend._FirstQualityOffsetLeft = testFirstQualityOffsetLeft;
        infoToSend._SecondQualityOffsetLeft = testSecondQualityOffsetLeft;
        infoToSend._ThirdQualityOffsetLeft = testThirdQualityOffsetLeft;
        infoToSend._FirstQualityOffsetRight = testFirstQualityOffsetRight;
        infoToSend._SecondQualityOffsetRight = testSecondQualityOffsetRight;
        infoToSend._ThirdQualityOffsetRight = testThirdQualityOffsetRight;

        //blit out old render textures for differences
        for (int i = 0; i < textureToRenderTo.Length; i++)
        {
            Graphics.Blit(textureToRenderTo[i], pastTextureToRenderTo[i]);
        }

        //determine TAA confidence
        Transform cCamera = camerasToRenderTo[0].transform;
        float TAAConfidence = 1;
        TAAConfidence = Mathf.Min(TAAConfidence, 1 / Mathf.Abs(cCamera.position.x - pastCamPosition.x));
        TAAConfidence = Mathf.Min(TAAConfidence, 1 / Mathf.Abs(cCamera.position.y - pastCamPosition.y));
        TAAConfidence = Mathf.Min(TAAConfidence, 1 / Mathf.Abs(cCamera.position.z - pastCamPosition.z));
        TAAConfidence = Mathf.Min(TAAConfidence, 1 / Mathf.Abs(cCamera.rotation.eulerAngles.x - pastCamRotation.x));
        TAAConfidence = Mathf.Min(TAAConfidence, 1 / Mathf.Abs(cCamera.rotation.eulerAngles.y - pastCamRotation.y));
        TAAConfidence = Mathf.Min(TAAConfidence, 1 / Mathf.Abs(cCamera.rotation.eulerAngles.z - pastCamRotation.z));
        //now set previous position
        pastCamPosition = cCamera.position;
        pastCamRotation = cCamera.rotation.eulerAngles;


        joePipeInstance.setTAAWeight(TAAWeightFactor * TAAConfidence);
        joePipeInstance.setMaxTAAFrames(MaxTAAFrame);

        //long startTime = DateTime.Now.ToFileTime();
        joePipeInstance.Render(contextToUse, camerasToRenderTo, textureToRenderTo, onlyOnce, infoToSend, disableFoveatedRendering, false, disableBlur);
        //long endTime = DateTime.Now.ToFileTime();
        string timeReporter = GlobalTimer.EndStopwatch(3);
        UnityEngine.Debug.Log("Time spent through frame generation and setting outside vars: " + timeReporter);
        float timeSpent = Time.deltaTime;
        //Debug.Log("Time for frame generation: " + (endTime - startTime));
        UnityEngine.Debug.Log("Time spent: " + timeSpent);

        //if (Input.GetKeyDown(KeyCode.P)) << has to be done in update
        if (onlyOnce)
        {
            onlyOnce = false;
            //Debug.Log("in printout");
            Texture2D temp = new Texture2D(textureToRenderTo[0].width, textureToRenderTo[0].height);//format and other seem to be set correctly by default
            RenderTexture.active = textureToRenderTo[0];
            temp.ReadPixels(new Rect(0, 0, textureToRenderTo[0].width, textureToRenderTo[0].height), 0, 0);
            temp.Apply();
            File.WriteAllBytes(DateTime.Now.ToFileTime() + "_" + "JoeTestingLeftCam" + ".png", temp.EncodeToPNG());

            temp = new Texture2D(pastTextureToRenderTo[0].width, pastTextureToRenderTo[0].height);//format and other seem to be set correctly by default
            RenderTexture.active = pastTextureToRenderTo[0];
            temp.ReadPixels(new Rect(0, 0, pastTextureToRenderTo[0].width, pastTextureToRenderTo[0].height), 0, 0);
            temp.Apply();
            File.WriteAllBytes(DateTime.Now.ToFileTime() + "_" + "JoeTestingLeftCamOld" + ".png", temp.EncodeToPNG());

            createDifference(textureToRenderTo[0], pastTextureToRenderTo[0]);

            temp = new Texture2D(differenceTexture.width, differenceTexture.height);//format and other seem to be set correctly by default
            RenderTexture.active = differenceTexture;
            temp.ReadPixels(new Rect(0, 0, differenceTexture.width, differenceTexture.height), 0, 0);
            temp.Apply();
            File.WriteAllBytes(DateTime.Now.ToFileTime() + "_" + "JoeTestingLeftCamDiff" + ".png", temp.EncodeToPNG());

            temp = new Texture2D(textureToRenderTo[1].width, textureToRenderTo[1].height);//format and other seem to be set correctly by default
            RenderTexture.active = textureToRenderTo[1];
            temp.ReadPixels(new Rect(0, 0, textureToRenderTo[1].width, textureToRenderTo[1].height), 0, 0);
            temp.Apply();
            File.WriteAllBytes(DateTime.Now.ToFileTime() + "_" + "JoeTestingRightCam" + ".png", temp.EncodeToPNG());

            temp = new Texture2D(pastTextureToRenderTo[1].width, pastTextureToRenderTo[1].height);//format and other seem to be set correctly by default
            RenderTexture.active = pastTextureToRenderTo[1];
            temp.ReadPixels(new Rect(0, 0, pastTextureToRenderTo[1].width, pastTextureToRenderTo[1].height), 0, 0);
            temp.Apply();
            File.WriteAllBytes(DateTime.Now.ToFileTime() + "_" + "JoeTestingLeftCamOld" + ".png", temp.EncodeToPNG());

            createDifference(textureToRenderTo[1], pastTextureToRenderTo[1]);

            temp = new Texture2D(differenceTexture.width, differenceTexture.height);//format and other seem to be set correctly by default
            RenderTexture.active = differenceTexture;
            temp.ReadPixels(new Rect(0, 0, differenceTexture.width, differenceTexture.height), 0, 0);
            temp.Apply();
            File.WriteAllBytes(DateTime.Now.ToFileTime() + "_" + "JoeTestingLeftCamDiff" + ".png", temp.EncodeToPNG());

            //now create ideal and "ok" diffs to print out
            joePipeInstance.Render(contextToUse, camerasToRenderTo, pastTextureToRenderTo, onlyOnce, infoToSend, true, false, disableBlur);

            createDifference(textureToRenderTo[0], pastTextureToRenderTo[0]);

            temp = new Texture2D(differenceTexture.width, differenceTexture.height);//format and other seem to be set correctly by default
            RenderTexture.active = differenceTexture;
            temp.ReadPixels(new Rect(0, 0, differenceTexture.width, differenceTexture.height), 0, 0);
            temp.Apply();
            File.WriteAllBytes(DateTime.Now.ToFileTime() + "_" + "JoeTestingLeftCamIdealDiff" + ".png", temp.EncodeToPNG());

            createDifference(textureToRenderTo[1], pastTextureToRenderTo[1]);

            temp = new Texture2D(differenceTexture.width, differenceTexture.height);//format and other seem to be set correctly by default
            RenderTexture.active = differenceTexture;
            temp.ReadPixels(new Rect(0, 0, differenceTexture.width, differenceTexture.height), 0, 0);
            temp.Apply();
            File.WriteAllBytes(DateTime.Now.ToFileTime() + "_" + "JoeTestingRightCamIdealDiff" + ".png", temp.EncodeToPNG());

            joePipeInstance.Render(contextToUse, camerasToRenderTo, pastTextureToRenderTo, onlyOnce, infoToSend, true, true, disableBlur);

            createDifference(textureToRenderTo[0], pastTextureToRenderTo[0]);

            temp = new Texture2D(differenceTexture.width, differenceTexture.height);//format and other seem to be set correctly by default
            RenderTexture.active = differenceTexture;
            temp.ReadPixels(new Rect(0, 0, differenceTexture.width, differenceTexture.height), 0, 0);
            temp.Apply();
            File.WriteAllBytes(DateTime.Now.ToFileTime() + "_" + "JoeTestingLeftCamOKDiff" + ".png", temp.EncodeToPNG());

            createDifference(textureToRenderTo[1], pastTextureToRenderTo[1]);

            temp = new Texture2D(differenceTexture.width, differenceTexture.height);//format and other seem to be set correctly by default
            RenderTexture.active = differenceTexture;
            temp.ReadPixels(new Rect(0, 0, differenceTexture.width, differenceTexture.height), 0, 0);
            temp.Apply();
            File.WriteAllBytes(DateTime.Now.ToFileTime() + "_" + "JoeTestingRightCamOKDiff" + ".png", temp.EncodeToPNG());
        }
    }

    private void createDifference(RenderTexture baseTexture, RenderTexture minusTexture)
    {
        differenceShader.SetTexture(0, "Base", baseTexture);
        differenceShader.SetTexture(0, "Minus", minusTexture);

        int threadGroupsX = Mathf.CeilToInt(differenceTexture.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(differenceTexture.height / 8.0f);

        differenceShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
    }

    public void ReloadGeometry()
    {
        joePipeInstance.ReloadGeometry();
    }

    public void ReloadMaterials()
    {
        joePipeInstance.reloadMaterialsBuffers();
    }
}

public struct ShaderFoveatedInfo
{
    public Vector4[] _frustumVector;
    public Vector4[] _viewVector;
    public float _innerAngleMax;
    public float _debugRegionBorderSize;
    public bool _showTint;
    public bool _showOverlay;
    public float _XOffset;
    public float _YOffset;
    public float _BorderAngle;
    public float _FirstQualityOffsetLeft;
    public float _SecondQualityOffsetLeft;
    public float _ThirdQualityOffsetLeft;
    public float _FirstQualityOffsetRight;
    public float _SecondQualityOffsetRight;
    public float _ThirdQualityOffsetRight;
    public float _TriangularDecreaseFactor;
    public bool _DisableRendering;
    public bool _UseTAAObjectID;
    public bool _UseGaussianTAA;
    public bool _cullNoRender;
}
