using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StressTestSpawner : MonoBehaviour
{
    public GameObject robotPrefab = null;
    public GameObject lightPrefab = null;
    private List<GameObject> spawnedRobots = new List<GameObject>();
    private List<GameObject> spawnedLights = new List<GameObject>();
    public int numberRobotsInScene = 0;
    private int numberFramesSpawned = 0;
    public float spawnBoxSize = 5f;
    public TryCreateJoePipeline pipeline = null;
    public bool onePerFrame = false; //makes one robot each frame, until 100 robots
    public int numberRobotsToSpawn = 20;

    public int framesForPeriodicIncrease = 400;
    public bool increaseRobotsPeriodic = false;
    public int targetNumRobots = 20; //already two robots in scene
    public bool increaseLightsPeriodic = false;
    public int targetNumLights = 20; //already one light in scene
    public bool increaseSamplesPeriodic = false;
    public int targetNumSamples = 8; //TAA frames should be set to 1 if using this. does not work for Joe's original ray tracer.
    private int numFramesPassed = 0;
    private int timesAdding = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        numFramesPassed++;
        GameObject randomRobot = null;
        if (!increaseRobotsPeriodic)
        {
            if (!onePerFrame)
            {
                while (numberRobotsInScene > spawnedRobots.Count)
                {
                    spawnedRobots.Add(Instantiate(robotPrefab, new Vector3(-1, 2, 4), Random.rotation));
                    //pipeline.ReloadGeometry(); << thought this might fix material bug, does not, robots do not get texture
                }
                while (numberRobotsInScene < spawnedRobots.Count)
                {
                    randomRobot = spawnedRobots[0];
                    Destroy(randomRobot);
                    spawnedRobots.RemoveAt(0);
                    //pipeline.ReloadGeometry();
                }
            }
            else
            {
                if (numberFramesSpawned < numberRobotsToSpawn)
                {
                    numberFramesSpawned++;
                    spawnedRobots.Add(Instantiate(robotPrefab, new Vector3(Random.value * spawnBoxSize / 2, Random.value * spawnBoxSize, Random.value * spawnBoxSize * 2), Random.rotation));
                    //pipeline.ReloadGeometry();
                }
            }
        }
        else
        {
            if(numFramesPassed % framesForPeriodicIncrease == 0 && timesAdding < targetNumRobots)
            {
                spawnedRobots.Add(Instantiate(robotPrefab, new Vector3(-1, 1, 4), Random.rotation));
            }
        }

        if(increaseLightsPeriodic && numFramesPassed % framesForPeriodicIncrease == 0 && timesAdding < targetNumLights)
        {
            spawnedLights.Add(Instantiate(lightPrefab, new Vector3(0, 1, 0), Quaternion.identity));
        }

        if (increaseSamplesPeriodic && numFramesPassed % framesForPeriodicIncrease == 0 && timesAdding < targetNumSamples)
        {
            pipeline.MaxTAAFrame++;
        }

        if (numFramesPassed % framesForPeriodicIncrease == 0 && (increaseRobotsPeriodic || increaseLightsPeriodic || increaseSamplesPeriodic))
        {
            timesAdding++;
        }
    }
}
