using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodeRebirth.ScrapStuff;
public class Hoverboard : GrabbableObject
{
    Rigidbody hb;
    public float mult;
    public float moveForce;
    public float turnTorque;
    
    public override void Start()
    {
        base.Start();
        hb = GetComponent<Rigidbody>();
    }

    public Transform[] anchors = new Transform[4];
    public RaycastHit[] hits = new RaycastHit[4];

    public void FixedUpdate()
    {
        for (int i = 0; i < 4; i++)
            ApplyForce(anchors[i], hits[i]);

        //hb.AddForce(Input.GetAxis("Vertical") * moveForce * transform.forward);
        //hb.AddTorque(Input.GetAxis("Horizontal") * turnTorque * transform.up);

    }

    public void ApplyForce(Transform anchor, RaycastHit hit)
    {
        if (Physics.Raycast(anchor.position, -anchor.up, out hit))
        {
            float force = 0;
            force = Mathf.Abs(1 / (hit.point.y - anchor.position.y));
            hb.AddForceAtPosition(transform.up * force * mult, anchor.position, ForceMode.Acceleration);
        }
    }
    
}