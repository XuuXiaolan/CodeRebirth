using System;
using UnityEngine;

namespace CodeRebirth.src.Content.Weapons;
public class ScaryShrimp : Shovel
{
    public AudioClip killClip = null!;
    public GameObject particleEffectGameObject = null!;

    [NonSerialized] public bool hitEnemy = false;
    public override void Start()
    {
        base.Start();
        shovelHitForce = 0;
    }

    public override void OnHitGround()
    {
        base.OnHitGround();
        // var particleEffect = Instantiate(particleEffectGameObject, transform.position, transform.rotation);
        // Destroy(particleEffect, 2f);
        if (IsServer) this.NetworkObject.Despawn();
    }
}