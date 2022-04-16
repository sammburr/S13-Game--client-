using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class billboardUI : MonoBehaviour
{

    Camera renderCamera;

    void Update()
    {
        if (renderCamera != null)
        {

            var lookPos = renderCamera.transform.position - transform.position;
            lookPos.y = 0;
            var rotation = Quaternion.LookRotation(lookPos);
            transform.rotation = rotation;

        }
        else
        {
            if(GameObject.FindGameObjectWithTag("playerCamera") != null)
                renderCamera = GameObject.FindGameObjectWithTag("playerCamera").GetComponent<Camera>() as Camera; // this is dumb and retarded
    
        }
    }
}
