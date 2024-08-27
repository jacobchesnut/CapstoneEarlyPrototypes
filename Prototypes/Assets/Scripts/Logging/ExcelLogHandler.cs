using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ExcelLogHandler : MonoBehaviour
{
    public static List<double> endFrameTimes = new List<double>();
    private bool onlyPrintEndFrameTimesOnce = true;
    private int framesPassed = 0;
    public int framesToCapture = 100;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        framesPassed++;
        if(endFrameTimes.Count >= framesToCapture && onlyPrintEndFrameTimesOnce) //trying to account for update synchronicity timing, there's a chance adding to this array happens late then early, skipping 100 here
        {
            onlyPrintEndFrameTimesOnce = false;
            printOutEndFrameTimes();
        }
    }

    private void printOutEndFrameTimes()
    {
        string toPrint = endFrameTimes[0].ToString();
        for(int i = 1; i < endFrameTimes.Count; i++)
        {
            toPrint += "\t" + endFrameTimes[i].ToString(); //txt uses tab deliniation when converting to excel
        }
        StreamWriter outputFile = new StreamWriter("C:\\Users\\jakee\\CapstoneShared\\EndFrameTimes.txt");
        outputFile.WriteLine(toPrint);
        outputFile.Close();
    }
}
