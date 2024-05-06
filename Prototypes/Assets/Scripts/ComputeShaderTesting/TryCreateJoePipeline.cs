using OpenRT;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TryCreateJoePipeline : MonoBehaviour
{
    //for creating the pipeline
    public List<RenderPipelineConfigObject> m_config;
    public Color clearColor = Color.green;
    public ComputeShader mainShader;

    public RenderTexture[] textureToRenderTo; //will eventually need array
    public Camera[] camerasToRenderTo;

    private BasicPipeInstance joePipeInstance = null;
    public bool onlyOnce = true;
    // Start is called before the first frame update
    void Start()
    {
        joePipeInstance = new BasicPipeInstance(clearColor, mainShader, m_config);
        textureToRenderTo = new RenderTexture[camerasToRenderTo.Length];
        for(int i = 0; i < camerasToRenderTo.Length; i++)
        {
            //textureToRenderTo[i] = new RenderTexture(camerasToRenderTo[i].scaledPixelWidth, camerasToRenderTo[i].scaledPixelHeight, 0);
            textureToRenderTo[i] = new RenderTexture(2160, 2224, 0); //hard coded to vr dimensions because of oddities with camera pixel width and heigh
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
    }

    //only gets called if camera attached to object
    private void OnPreRender()
    {
        //Debug.Log("in prerender");
        //create the texture on pre render, this can then be sent out oncamerarender by other scripts with a reference here
        ScriptableRenderContext contextToUse = new ScriptableRenderContext();
        //Debug.Log("cam length: " + camerasToRenderTo.Length);
        joePipeInstance.Render(contextToUse, camerasToRenderTo, textureToRenderTo, onlyOnce);
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
