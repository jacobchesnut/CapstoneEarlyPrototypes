using OpenRT;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TryCreateJoePipeline : MonoBehaviour
{
    //consts for global use
    //default width meta uses = 2160
    public static int RENDER_TEXTURE_WIDTH = 1080;
    //default height meta uses = 2224
    public static int RENDER_TEXTURE_HEIGHT = 1112;


    //for creating the pipeline
    public List<RenderPipelineConfigObject> m_config;
    public Color clearColor = Color.green;
    public ComputeShader mainShader;

    public RenderTexture[] textureToRenderTo; //will eventually need array
    public Camera[] camerasToRenderTo;

    private BasicPipeInstance joePipeInstance = null;
    public bool onlyOnce = true;

    //paramaters to set in editor
    public float innerAngleMax = 15f; //foveated region
    public bool showTint;
    public bool showOverlay;
    public float tintBorderSize = 1f;

    // Start is called before the first frame update
    void Start()
    {
        joePipeInstance = new BasicPipeInstance(clearColor, mainShader, m_config);
        textureToRenderTo = new RenderTexture[camerasToRenderTo.Length];
        for(int i = 0; i < camerasToRenderTo.Length; i++)
        {
            //textureToRenderTo[i] = new RenderTexture(camerasToRenderTo[i].scaledPixelWidth, camerasToRenderTo[i].scaledPixelHeight, 0);
            textureToRenderTo[i] = new RenderTexture(RENDER_TEXTURE_WIDTH, RENDER_TEXTURE_HEIGHT, 0); //hard coded to vr dimensions because of oddities with camera pixel width and heigh
        }
        //textureToRenderTo = new RenderTexture(Screen.width, Screen.height, 0); //currently screen width and height, will need pixel counts for VR camera
    }

    // Update is called once per frame
    void Update()
    {
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

    //only gets called if camera attached to object
    private void OnPreRender()
    {
        //Debug.Log("in prerender");
        //create the texture on pre render, this can then be sent out oncamerarender by other scripts with a reference here
        ScriptableRenderContext contextToUse = new ScriptableRenderContext();
        //Debug.Log("cam length: " + camerasToRenderTo.Length);

        //send foveated information to pipeline through struct
        ShaderFoveatedInfo infoToSend = new ShaderFoveatedInfo();
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

        joePipeInstance.Render(contextToUse, camerasToRenderTo, textureToRenderTo, onlyOnce, infoToSend);
        //if (Input.GetKeyDown(KeyCode.P)) << has to be done in update
        if(onlyOnce)
        {
            onlyOnce = false;
            //Debug.Log("in printout");
            Texture2D temp = new Texture2D(textureToRenderTo[0].width, textureToRenderTo[0].height);//format and other seem to be set correctly by default
            RenderTexture.active = textureToRenderTo[0];
            temp.ReadPixels(new Rect(0, 0, textureToRenderTo[0].width, textureToRenderTo[0].height), 0, 0);
            temp.Apply();
            File.WriteAllBytes(DateTime.Now.ToFileTime() + "_" + "JoeTestingLeftCam" + ".png", temp.EncodeToPNG());

            temp = new Texture2D(textureToRenderTo[1].width, textureToRenderTo[1].height);//format and other seem to be set correctly by default
            RenderTexture.active = textureToRenderTo[1];
            temp.ReadPixels(new Rect(0, 0, textureToRenderTo[1].width, textureToRenderTo[1].height), 0, 0);
            temp.Apply();
            File.WriteAllBytes(DateTime.Now.ToFileTime() + "_" + "JoeTestingRightCam" + ".png", temp.EncodeToPNG());
        }
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
}
