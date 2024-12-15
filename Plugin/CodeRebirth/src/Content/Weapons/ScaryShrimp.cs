using UnityEngine;

namespace CodeRebirth.src.Content.Weapons;
public class ScaryShrimp : Shovel
{
    public AudioClip killClip = null!;

    public override void Start()
    {
        base.Start();
        shovelHitForce = 0;
    }
}