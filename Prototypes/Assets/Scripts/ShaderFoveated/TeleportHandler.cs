using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//this class is intended to move the user through the different test scenes for foveated rendering
public class TeleportHandler : MonoBehaviour
{
    public Transform UserObject; //the user object to move
    public Transform[] TeleportLocations; //the test scenes to go to
    public int startLocation = 0; //which place the user should start at
    
    // Start is called before the first frame update
    void Start()
    {
        startLocation--; //to send to the place in the list on start, need to subtract here.
        teleportUser();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            teleportUser();
        }
    }

    private void teleportUser()
    {
        startLocation++; //move to next place
        if (startLocation == TeleportLocations.Length) //at end of list
        {
            //Debug.Log("end of list, start location is " + startLocation);
            startLocation = 0;
        }
        Transform placeToGo = TeleportLocations[startLocation];
        UserObject.position = placeToGo.position;
    }
}
