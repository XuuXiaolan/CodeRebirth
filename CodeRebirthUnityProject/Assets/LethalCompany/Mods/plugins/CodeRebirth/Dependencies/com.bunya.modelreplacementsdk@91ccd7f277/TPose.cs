using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class TPose : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        base.GetComponentInChildren<Animator>().avatar.humanDescription.skeleton.ToList().ForEach(sk =>
        {
            var a = base.GetComponentsInChildren<Transform>().Where(x => x.name == sk.name);
            if (a.Any())
            {
                a.First().localRotation = sk.rotation;
            }
            else
            {
                Debug.Log($"Missing bone {sk.name}");
            }

        });
    }
}
