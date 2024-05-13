using Oculus.Voice.Windows;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//this class is intended to handle swapping light modes for testing foveated rendering
public class LightsController : MonoBehaviour
{
    public GameObject[] lights;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            ToggleLights();
        }
    }

    private void ToggleLights()
    {
        for(int i = 0; i < lights.Length; i++)
        {
            lights[i].SetActive(!lights[i].activeSelf); //toggle active status of lights
        }
    }
}
