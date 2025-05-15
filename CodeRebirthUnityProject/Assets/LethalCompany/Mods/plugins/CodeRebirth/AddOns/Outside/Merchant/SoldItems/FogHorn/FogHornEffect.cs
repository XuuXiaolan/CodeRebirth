using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[ExecuteAlways]
public class FogHornEffect : MonoBehaviour
{
    // Start is called before the first frame update
    public VisualEffect FogEffect;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        FogEffect.SetVector3("ObjectForward",transform.forward);
        FogEffect.SetVector3("ObjectUp",transform.up);
    }
}
