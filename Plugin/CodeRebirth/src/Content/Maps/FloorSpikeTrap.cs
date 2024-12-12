using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class FloorSpikeTrap : MonoBehaviour
{
    public enum DeathAnimation
    {
        Default,
        HeadBurst,
        Spring,
        Electrocuted,
        ComedyMask,
        TragedyMask,
        Burnt,
        Snipped,
        SliceHead,
        PlaceHolder1,
        PlaceHolder2,
        PlaceHolder3,
        PlaceHolder4,
        PlaceHolder5,
        PlaceHolder6
    }
    public Animator spikeAnimator = null!;
    public NetworkAnimator networkAnimator = null!;
    public int damageToPlayer = 25;
    public CauseOfDeath causeOfDeath = CauseOfDeath.Bludgeoning;
    public DeathAnimation deathAnimation = DeathAnimation.Default;
    public float upwardForce = 5f;
    public float animationTimer = 0f;
    public float damageTimer = 2f;

    private bool damagedLocalPlayerRecently = false;
    private float internalDamageTimer = 0f;
    private float internalAnimationTimer = 0f;
    private static readonly int pushAnimation = Animator.StringToHash("doSpikePush");

    public void OnTriggerEnter(Collider other)
    {
        if (!NetworkManager.Singleton.IsServer || animationTimer > 0f) return;

        PlayerControllerB player = other.GetComponent<PlayerControllerB>();
        if (player != null)
        {
            networkAnimator.SetTrigger(pushAnimation);
        }        
    }

    public void Update()
    {
        if (damagedLocalPlayerRecently)
        {
            internalDamageTimer += Time.deltaTime;
            if (internalDamageTimer >= damageTimer)
            {
                damagedLocalPlayerRecently = false;
                internalDamageTimer = 0f;
            }
        }
        if (animationTimer <= 0 || !NetworkManager.Singleton.IsServer) return;

        internalAnimationTimer += Time.deltaTime;
        if (internalAnimationTimer >= animationTimer)
        {
            internalAnimationTimer = 0f;
            networkAnimator.SetTrigger(pushAnimation);
        }
    }

    public void DamagePlayer(PlayerControllerB player, Transform spikeTransform)
    {
        if (damagedLocalPlayerRecently) return;
        damagedLocalPlayerRecently = true;
        player.DamagePlayer(damageToPlayer, true, true, causeOfDeath, (int)deathAnimation, false, spikeTransform.up * upwardForce);
    }
}