using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class MonarchVFXScript : MonoBehaviour
{
    public VisualEffect monarchParticle;
    // public Transform beamStart;
    public SkinnedMeshRenderer wingMesh;
    // Start is called before the first frame update
    void OnValidate()
    {
        if(wingMesh!=null)
            monarchParticle.SetSkinnedMeshRenderer("wingMesh",wingMesh);
        // if(beamStart!=null)
            // monarchParticle.SetVector3("BeamStart",beamStart.position);
    }
    void Start()
    {
        if(wingMesh!=null)
            monarchParticle.SetSkinnedMeshRenderer("wingMesh",wingMesh);
        // if(beamStart!=null)
            // monarchParticle.SetVector3("BeamStart",beamStart.position);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
