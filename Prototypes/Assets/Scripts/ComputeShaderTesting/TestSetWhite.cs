using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSetWhite : MonoBehaviour
{
    public ComputeShader theShader = null;
    private RenderTexture theTexture = null;
    private bool onlyOnce = true;
    
    // Start is called before the first frame update
    void Start()
    {
        theTexture = new RenderTexture(255, 255, 0);
        theTexture.enableRandomWrite = true;
        theShader.SetTexture(0, "Result", theTexture);
        //only renders to the number of pixels respective to the multiple of how many threads are given here times the number of threads given in compute shader
        theShader.Dispatch(0, 64, 64, 1);
    }

    // Update is called once per frame
    void Update()
    {
        
        if (onlyOnce)
        {
            onlyOnce = false;
            Texture2D temp = new Texture2D(theTexture.width, theTexture.height);//format and other seem to be set correctly by default
            RenderTexture.active = theTexture;
            temp.ReadPixels(new Rect(0, 0, theTexture.width, theTexture.height), 0, 0);
            temp.Apply();
            File.WriteAllBytes(DateTime.Now.ToFileTime() + "_" + "WhiteTesting" + ".png", temp.EncodeToPNG());
        }
    }
}
