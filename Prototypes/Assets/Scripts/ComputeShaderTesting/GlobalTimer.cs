using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class GlobalTimer : MonoBehaviour
{
    public static Stopwatch endOnFrameStart = new Stopwatch();
    private static Stopwatch stopwatch = new Stopwatch();


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //this is roughly the start of a frame
        if (endOnFrameStart.IsRunning)
        {
            endOnFrameStart.Stop();
            UnityEngine.Debug.Log("time to frame end: " + endOnFrameStart.Elapsed);
            endOnFrameStart.Reset();
        }
    }

    public static void StartStopwatch()
    {
        stopwatch.Start();
    }

    public static string EndStopwatch()
    {
        stopwatch.Stop();
        string toReturn = stopwatch.Elapsed.ToString();
        stopwatch.Reset();
        return toReturn;
        
    }
}
