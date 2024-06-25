using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodeRebirth.Misc;
public class OffsetStunBirdAnimation : MonoBehaviour
{
    Animator animator;
    public float Offset;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void PlayAnimationWithOffset()
    {
        animator.Play("StunnedBirds", 0, Offset);
    }

    void OnEnable()
    {
        StartCoroutine(ApplyOffsetNextFrame());
    }

    System.Collections.IEnumerator ApplyOffsetNextFrame()
    {
        yield return new WaitForEndOfFrame();
        PlayAnimationWithOffset();
    }
    // // Update is called once per frame
    // void Update()
    // {
        
    // }
}
