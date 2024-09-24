using Oculus.Voice.Windows;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ExcelLogHandler : MonoBehaviour
{
    private const bool DEBUG_PRINT_ON_WRITEOUT = true;
    //this list is added to by whatever is handling timing the end of the frame. this time is the amount of time spent between dispatch and update on the next frame
    public static List<double> endFrameTimes = new List<double>();
    //this list captures the average of every <framestocapture, currently 100> frames
    private static List<double> averageEndFrameTimes = new List<double>();
    //this list is added to by whatever is handling timing the end of the frame. this time is the amount of time spent between dispatch and update on the next frame
    public static List<double> totalFrameTimes = new List<double>();
    //this list captures the average of every <framestocapture, currently 100> frames
    private static List<double> averageTotalFrameTimes = new List<double>();
    private bool onlyPrintEndFrameTimesOnce = true;
    private int numEndFrameTimesPrinted = 0;
    private int numTotalFrameTimesPrinted = 0;
    private int framesPassed = 0;
    public int framesToCapture = 100;
    public int avgFramesToCapture = 4;
    public int testsUntilFullPrintout = 20;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //capture frame time
        //totalFrameTimes.Add(Time.deltaTime * 1000); //time in ms

        framesPassed++;
        
        //print out times
        if(endFrameTimes.Count >= framesToCapture && onlyPrintEndFrameTimesOnce) //trying to account for update synchronicity timing, there's a chance adding to this array happens late then early, skipping 100 here
        {
            if (DEBUG_PRINT_ON_WRITEOUT)
            {
                Debug.LogWarning("printing end frame times");
            }
            //numEndFrameTimesPrinted++;
            printOutFrameTimes(endFrameTimes, "EndFrameTimes" + numEndFrameTimesPrinted);
            averageEndFrameTimes.Add(endFrameTimes.Average());
            endFrameTimes.Clear();
        }
        if (totalFrameTimes.Count >= framesToCapture) //trying to account for update synchronicity timing, there's a chance adding to this array happens late then early, skipping 100 here
        {
            if (DEBUG_PRINT_ON_WRITEOUT)
            {
                Debug.LogWarning("printing total frame times");
            }
            //numTotalFrameTimesPrinted++;
            printOutFrameTimes(totalFrameTimes, "TotalFrameTimes" + numTotalFrameTimesPrinted);
            averageTotalFrameTimes.Add(totalFrameTimes.Average());
            totalFrameTimes.Clear();
        }
        if (averageEndFrameTimes.Count >= avgFramesToCapture)
        {
            if (DEBUG_PRINT_ON_WRITEOUT)
            {
                Debug.LogWarning("printing average end frame times");
            }
            printOutFrameTimes(averageEndFrameTimes, "AverageEndFrameTimes");
            averageEndFrameTimes.Clear();
        }
        if (averageTotalFrameTimes.Count >= avgFramesToCapture)
        {
            if (DEBUG_PRINT_ON_WRITEOUT)
            {
                Debug.LogWarning("printing average total frame times");
            }
            printOutFrameTimes(averageTotalFrameTimes, "AverageTotalFrameTimes");
            averageTotalFrameTimes.Clear();
        }

        if(averageTotalFrameTimes.Count >= testsUntilFullPrintout)
        {
            printOutFull("FullPrint");
        }
    }

    /*
    private void printOutEndFrameTimes()
    {
        string toPrint = endFrameTimes[0].ToString();
        for(int i = 1; i < endFrameTimes.Count; i++)
        {
            toPrint += "\t" + endFrameTimes[i].ToString(); //txt uses tab deliniation when converting to excel
        }
        StreamWriter outputFile = new StreamWriter("C:\\Users\\jakee\\CapstoneShared\\EndFrameTimes.txt", true);
        outputFile.WriteLine(toPrint);
        outputFile.Close();
    }
    */

    private void printOutFrameTimes(List<double> frameTimesToUse, string fileName)
    {
        string toPrint = frameTimesToUse[0].ToString();
        for (int i = 1; i < frameTimesToUse.Count; i++)
        {
            toPrint += "\t" + frameTimesToUse[i].ToString(); //txt uses tab deliniation when converting to excel
        }
        toPrint += "\n";
        StreamWriter outputFile = new StreamWriter("C:\\Users\\jakee\\CapstoneShared\\" + fileName + ".txt", true);
        outputFile.WriteLine(toPrint);
        outputFile.Close();
    }

    private void printOutFull(string fileName)
    {
        printOutFrameTimes(averageEndFrameTimes, fileName);
        printOutFrameTimes(averageTotalFrameTimes, fileName);
        printOutFrameTimes(endFrameTimes, fileName);
        printOutFrameTimes(totalFrameTimes, fileName);
    }
}
