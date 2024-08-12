using System.Collections;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class OffsetStunBirdAnimation : MonoBehaviour
{
    private Animator animator = null!;
    public float Offset;
    // Start is called before the first frame update
    public void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void PlayAnimationWithOffset()
    {
        animator!.Play("StunnedBirds", 0, Offset);
    }

    public void OnEnable()
    {
        StartCoroutine(ApplyOffsetNextFrame());
    }

    public IEnumerator ApplyOffsetNextFrame()
    {
        yield return new WaitForEndOfFrame();
        PlayAnimationWithOffset();
    }
}
