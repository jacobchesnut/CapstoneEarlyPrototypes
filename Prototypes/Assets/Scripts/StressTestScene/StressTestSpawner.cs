using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StressTestSpawner : MonoBehaviour
{
    public GameObject robotPrefab = null;
    private List<GameObject> spawnedRobots = new List<GameObject>();
    public int numberRobotsInScene = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        GameObject randomRobot = null;
        while(numberRobotsInScene > spawnedRobots.Count)
        {
            spawnedRobots.Add(Instantiate(robotPrefab,new Vector3(Random.value*100, Random.value * 100, Random.value * 100), Random.rotation));
        }
        while(numberRobotsInScene < spawnedRobots.Count)
        {
            randomRobot = spawnedRobots[0];
            Destroy(randomRobot);
            spawnedRobots.RemoveAt(0);
        }
    }
}
