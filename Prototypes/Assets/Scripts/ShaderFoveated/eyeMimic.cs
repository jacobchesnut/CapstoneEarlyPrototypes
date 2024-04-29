using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class eyeMimic : MonoBehaviour
{
    public Transform eye = null;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.localRotation = eye.localRotation;
    }
}
