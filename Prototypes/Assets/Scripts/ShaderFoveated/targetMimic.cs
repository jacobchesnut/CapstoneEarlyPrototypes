using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class targetMimic : MonoBehaviour
{
    public Transform targetToMimic = null;
    // Start is called before the first frame update
    void Start()
    {
        transform.position = targetToMimic.position;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
