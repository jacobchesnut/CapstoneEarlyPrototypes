using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

//this class is attached to the main camera to copy the rendered raytracing image over to 
public class CameraRenderCopy : MonoBehaviour
{


    public TryCreateJoePipeline pipelineReference = null;
    public int texToReadFrom = 0; //this needs to be set to the index of the camera that this script corresponds to
    

    private void Start()
    {
        if(pipelineReference == null)
        {
            Debug.LogError("pipeline reference not set");
        }
    }


    private void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        Graphics.Blit(pipelineReference.textureToRenderTo[texToReadFrom], dst);
    }

    
}
