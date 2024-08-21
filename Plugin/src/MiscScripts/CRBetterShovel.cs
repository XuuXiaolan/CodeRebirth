using System;
using System.Linq;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class CRBetterShovel : Shovel {
    public float ShovelSpeedMultiplier = 1f;

    public bool canBreakTrees = false;
    private bool _animatorSpeedCurrentlyModified;
    private float _originalPlayerAnimatorSpeed;
    private PlayerControllerB previouslyHeldBy = null!;
    [NonSerialized] public int defaultForce = 0;
    public bool critPossible = true;
    public float critChance = 25;
    public Transform weaponTip = null!;


    public override void Start()
    {
        base.Start();
    }
    public override void EquipItem() {
        base.EquipItem();
        // Get the reel up animation clip
        AnimationClip reelingUpClip = playerHeldBy.playerBodyAnimator.runtimeAnimatorController.animationClips
            .FirstOrDefault(clip => clip.name == "ShovelReelUp");
        AnimationClip swingDownClip = playerHeldBy.playerBodyAnimator.runtimeAnimatorController.animationClips
            .FirstOrDefault(clip => clip.name == "HitShovel");

        // Check if we found the clip successfully.
        if (reelingUpClip != null && swingDownClip != null && !_animatorSpeedCurrentlyModified)
        {
            // Get the current player body animator speed.
            _originalPlayerAnimatorSpeed = playerHeldBy.playerBodyAnimator.speed;
            
            // Get the length of the reel up animation.
            float animationOrigionalLength = reelingUpClip.length;
            
            // Calculate the new speed of the reel up.
            float newSpeed = animationOrigionalLength * ShovelSpeedMultiplier;
            
            // Set the player body animator to use the new speed.
            _animatorSpeedCurrentlyModified = true;
            playerHeldBy.playerBodyAnimator.speed = newSpeed;
        }
        previouslyHeldBy = playerHeldBy;
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
        // Get the reel up animation clip
        AnimationClip reelingUpClip = previouslyHeldBy.playerBodyAnimator.runtimeAnimatorController.animationClips
            .FirstOrDefault(clip => clip.name == "ShovelReelUp");
        AnimationClip swingDownClip = previouslyHeldBy.playerBodyAnimator.runtimeAnimatorController.animationClips
            .FirstOrDefault(clip => clip.name == "HitShovel");

        // Check if we found the clip successfully.
        if (reelingUpClip != null && swingDownClip != null &&  _animatorSpeedCurrentlyModified)
        {
            // Get the current player body animator speed.
            previouslyHeldBy.playerBodyAnimator.speed = _originalPlayerAnimatorSpeed;
            _animatorSpeedCurrentlyModified = false;
        }
    }

    public override void PocketItem()
    {
        base.PocketItem();
        // Get the reel up animation clip
        AnimationClip reelingUpClip = previouslyHeldBy.playerBodyAnimator.runtimeAnimatorController.animationClips
            .FirstOrDefault(clip => clip.name == "ShovelReelUp");
        AnimationClip swingDownClip = previouslyHeldBy.playerBodyAnimator.runtimeAnimatorController.animationClips
            .FirstOrDefault(clip => clip.name == "HitShovel");

        // Check if we found the clip successfully.
        if (reelingUpClip != null && swingDownClip != null && _animatorSpeedCurrentlyModified)
        {
            // Get the current player body animator speed.
            previouslyHeldBy.playerBodyAnimator.speed = _originalPlayerAnimatorSpeed;
            _animatorSpeedCurrentlyModified = false;
        }
    }
}