using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class Mountaineer : CRWeapon
{
    public Renderer iceRenderer = null!;
    public AudioClip[] unlatchSounds = [];
    [HideInInspector] public float FreezePercentile => GetFreezePercentage();

    private bool stuckToWall = false;
    private PlayerControllerB? stuckPlayer = null;
    private Vector3 stuckPosition = Vector3.zero;
    private Material iceMaterial = null!;

    public static List<Mountaineer> Instances = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Instances.Add(this);
        iceMaterial = new Material(iceRenderer.sharedMaterial);
        iceRenderer.material = iceMaterial;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Instances.Remove(this);
    }

    public override void Update()
    {
        base.Update();
        if (heldOverHeadTimer > 0f && iceMaterial.color.a < 1)
        {
            iceMaterial.color = new Color(iceMaterial.color.r, iceMaterial.color.g, iceMaterial.color.b, iceMaterial.color.a + Time.deltaTime / 5f);
        }
        else if (heldOverHeadTimer <= 0f && iceMaterial.color.a > 0)
        {
            iceMaterial.color = new Color(iceMaterial.color.r, iceMaterial.color.g, iceMaterial.color.b, 0f);
        }
        iceMaterial.color = new Color(iceMaterial.color.r, iceMaterial.color.g, iceMaterial.color.b, heldOverHeadTimer);
        if (stuckPlayer != null && stuckToWall) // this wont be sync'd, which is okay!
        {
            stuckPlayer.transform.position = stuckPosition;
            stuckPlayer.ResetFallGravity();
            stuckPlayer.disableMoveInput = true;
            stuckPlayer.activatingItem = true;
        }
    }

    private void MakePlayerGrabObject(PlayerControllerB player)
    {
        player.currentlyGrabbingObject = this;
        player.currentlyGrabbingObject.InteractItem();
        if (player.currentlyGrabbingObject.grabbable && player.FirstEmptyItemSlot() != -1)
        {
            player.playerBodyAnimator.SetBool("GrabInvalidated", false);
            player.playerBodyAnimator.SetBool("GrabValidated", false);
            player.playerBodyAnimator.SetBool("cancelHolding", false);
            player.playerBodyAnimator.ResetTrigger("Throw");
            player.SetSpecialGrabAnimationBool(true, null);
            player.isGrabbingObjectAnimation = true;
            player.cursorIcon.enabled = false;
            player.cursorTip.text = "";
            player.twoHanded = player.currentlyGrabbingObject.itemProperties.twoHanded;
            player.carryWeight = Mathf.Clamp(player.carryWeight + (player.currentlyGrabbingObject.itemProperties.weight - 1f), 1f, 10f);
            if (player.currentlyGrabbingObject.itemProperties.grabAnimationTime > 0f)
            {
                player.grabObjectAnimationTime = player.currentlyGrabbingObject.itemProperties.grabAnimationTime;
            }
            else
            {
                player.grabObjectAnimationTime = 0.4f;
            }
            if (!player.isTestingPlayer)
            {
                player.GrabObjectServerRpc(this.NetworkObject);
            }
            if (player.grabObjectCoroutine != null)
            {
                base.StopCoroutine(player.grabObjectCoroutine);
            }
            player.grabObjectCoroutine = base.StartCoroutine(player.GrabObject());
        }
    }

    public override void HitSurface(int hitSurfaceID)
    {
        base.HitSurface(hitSurfaceID);
        stuckPlayer = playerHeldBy;
        stuckToWall = true;
        stuckPlayer.externalForces = Vector3.zero;
        stuckPlayer.externalForceAutoFade = Vector3.zero;
        stuckPosition = stuckPlayer.transform.position;
        StartCoroutine(playerHeldBy.waitToEndOfFrameToDiscard());
    }

    public override void FallWithCurve()
    {
        if (stuckToWall) return;
        base.FallWithCurve();
    }

    public override void EquipItem() // sync this
    {
        base.EquipItem();
        stuckToWall = false;
        stuckPlayer = null;
    }

    public void JumpActionTriggered(PlayerControllerB player)
    {
        if (!stuckToWall || stuckPlayer == null) return;
        weaponAudio.PlayOneShot(unlatchSounds[Random.Range(0, unlatchSounds.Length)]);
        if (stuckPlayer != player) return;
        stuckPlayer.activatingItem = false;
        stuckPlayer.disableMoveInput = false;
        stuckPlayer.externalForceAutoFade = Vector3.up * 30f;
        MakePlayerGrabObject(stuckPlayer);
    }

    public float GetFreezePercentage()
    {
        return iceMaterial.color.a * 100f;
    }
}