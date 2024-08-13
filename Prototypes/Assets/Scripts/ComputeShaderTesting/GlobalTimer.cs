using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class GlobalTimer : MonoBehaviour
{
    private const int STOPWATCH_ARRAY_LENGTH = 10;
    public static Stopwatch endOnFrameStart = new Stopwatch();
    private static Stopwatch stopwatch = new Stopwatch();
    private static Stopwatch[] stopwatches = new Stopwatch[10];


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

    private static void CreateStopwatchArray()
    {
        for(int i = 0; i < STOPWATCH_ARRAY_LENGTH; i++)
        {
            stopwatches[i] = new Stopwatch();
        }
    }

    public static void StartStopwatch(int position)
    {
        if (stopwatches[position] == null)
        {
            CreateStopwatchArray();
        }

        if(position < 0 || position >= STOPWATCH_ARRAY_LENGTH)
        {
            return;
        }
        stopwatches[position].Start();
    }

    public static string EndStopwatch(int position)
    {
        if (position < 0 || position >= STOPWATCH_ARRAY_LENGTH)
        {
            return(null);
        }
        stopwatches[position].Stop();
        string toReturn = stopwatches[position].Elapsed.ToString();
        stopwatches[position].Reset();
        return toReturn;

    }
}
