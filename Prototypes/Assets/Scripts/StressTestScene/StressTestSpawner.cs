using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StressTestSpawner : MonoBehaviour
{
    public GameObject robotPrefab = null;
    private List<GameObject> spawnedRobots = new List<GameObject>();
    public int numberRobotsInScene = 0;
    private int numberFramesSpawned = 0;
    public float spawnBoxSize = 5f;
    public TryCreateJoePipeline pipeline = null;
    public bool onePerFrame = false; //makes one robot each frame, until 100 robots

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        GameObject randomRobot = null;
        if (!onePerFrame)
        {
            while (numberRobotsInScene > spawnedRobots.Count)
            {
                spawnedRobots.Add(Instantiate(robotPrefab, new Vector3(Random.value * spawnBoxSize / 2, Random.value * spawnBoxSize, Random.value * spawnBoxSize * 2), Random.rotation));
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
            if(numberFramesSpawned < 100)
            {
                numberFramesSpawned++;
                spawnedRobots.Add(Instantiate(robotPrefab, new Vector3(Random.value * spawnBoxSize / 2, Random.value * spawnBoxSize, Random.value * spawnBoxSize * 2), Random.rotation));
                //pipeline.ReloadGeometry();
            }
        }
    }
}
