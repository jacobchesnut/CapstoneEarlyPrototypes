using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

//this class is used to send eye calibration info to the camera render call scripts
public class EyeCalibration : MonoBehaviour
{
    public CameraRenderCall leftCamera = null;
    public CameraRenderCall rightCamera = null;
    // Start is called before the first frame update
    void Start()
    {
        FileStream stream = File.Open("C:\\Users\\jakee\\CapstoneShared\\Calibration.dat", FileMode.Open);
        BinaryReader reader = new BinaryReader(stream);
        float LECalX = reader.ReadSingle();
        float LECalY = reader.ReadSingle();
        float RECalX = reader.ReadSingle();
        float RECalY = reader.ReadSingle();
        leftCamera.upOutsideAngle = reader.ReadSingle();
        leftCamera.rightOutsideAngle = reader.ReadSingle();
        leftCamera.downOutsideAngle = reader.ReadSingle();
        leftCamera.leftOutsideAngle = reader.ReadSingle();
        rightCamera.upOutsideAngle = reader.ReadSingle();
        rightCamera.rightOutsideAngle = reader.ReadSingle();
        rightCamera.downOutsideAngle = reader.ReadSingle();
        rightCamera.leftOutsideAngle = reader.ReadSingle();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
