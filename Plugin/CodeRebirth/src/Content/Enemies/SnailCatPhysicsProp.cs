using Dawn.Utils;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class SnailCatPhysicsProp : GrabbableObject
{
    public Animator animator = null!;
    public OwnerNetworkAnimator ownerNetworkAnimator = null!;
    public SnailCatAI snailCatScript = null!;
    public PlayerControllerB? previousPlayerHeldBy = null;

    internal static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeed"); // Float
    internal static readonly int HitAnimation = Animator.StringToHash("doHit"); // Trigger
    internal static readonly int GrabbedAnimation = Animator.StringToHash("grabbed"); // Bool
    internal static readonly int SittingAnimation = Animator.StringToHash("sitting"); // Bool
    internal static readonly int SleepingAnimation = Animator.StringToHash("sleeping"); // Bool

    public override void EquipItem()
    {
        base.EquipItem();
        animator.SetBool(GrabbedAnimation, true);
        animator.SetBool(SittingAnimation, false);
        animator.SetBool(SleepingAnimation, false);
        snailCatScript.PickUpBabyLocalClient();
        previousPlayerHeldBy = playerHeldBy;
    }

    public override void FallWithCurve()
    {
        if (snailCatScript.inSpecialAnimation)
        {
            base.FallWithCurve();
        }
    }

    public override void Start()
    {
        propColliders = gameObject.GetComponentsInChildren<Collider>();
        for (int i = 0; i < propColliders.Length; i++)
        {
            if (!propColliders[i].CompareTag("DoNotSet") && !propColliders[i].CompareTag("Enemy"))
            {
                propColliders[i].excludeLayers = -2621449;
            }
        }
        originalScale = transform.localScale;
        if (itemProperties.itemSpawnsOnGround)
        {
            startFallingPosition = transform.position;
            if (transform.parent != null)
            {
                startFallingPosition = transform.parent.InverseTransformPoint(startFallingPosition);
            }
            FallToGround(false);
        }
        else
        {
            fallTime = 1f;
            hasHitGround = true;
            reachedFloorTarget = true;
            targetFloorPosition = transform.localPosition;
        }
        if (itemProperties.isScrap)
        {
            fallTime = 1f;
            hasHitGround = true;
        }
        if (itemProperties.isScrap && RoundManager.Instance.mapPropsContainer != null)
        {
            radarIcon = GameObject.Instantiate(StartOfRound.Instance.itemRadarIconPrefab, RoundManager.Instance.mapPropsContainer.transform).transform;
        }
        if (!itemProperties.isScrap)
        {
            HoarderBugAI.grabbableObjectsInMap.Add(gameObject);
        }
        MeshRenderer[] componentsInChildren = gameObject.GetComponentsInChildren<MeshRenderer>();
        for (int j = 0; j < componentsInChildren.Length; j++)
        {
            componentsInChildren[j].renderingLayerMask = 1U;
        }
        SkinnedMeshRenderer[] componentsInChildren2 = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
        for (int k = 0; k < componentsInChildren2.Length; k++)
        {
            componentsInChildren2[k].renderingLayerMask = 1U;
        }
    }

    public override void Update()
    {
        if (IsOwner)
        {
            animator.SetFloat(RunSpeedFloat, snailCatScript.agent.velocity.magnitude / 3);
            if (!wasOwnerLastFrame)
            {
                wasOwnerLastFrame = true;
            }
        }
        else if (wasOwnerLastFrame)
        {
            wasOwnerLastFrame = false;
        }
        if (!isHeld && parentObject == null)
        {
            if (fallTime < 1f)
            {
                reachedFloorTarget = false;
                FallWithCurve();
                if (transform.localPosition.y - targetFloorPosition.y < 0.05f && !hasHitGround)
                {
                    PlayDropSFX();
                    OnHitGround();
                    return;
                }
            }
            else
            {
                if (!reachedFloorTarget)
                {
                    if (!hasHitGround)
                    {
                        PlayDropSFX();
                        OnHitGround();
                    }
                    reachedFloorTarget = true;
                    if (floorYRot == -1)
                    {
                        transform.rotation = Quaternion.Euler(itemProperties.restingRotation.x, transform.eulerAngles.y, itemProperties.restingRotation.z);
                    }
                    else
                    {
                        transform.rotation = Quaternion.Euler(itemProperties.restingRotation.x, floorYRot + itemProperties.floorYOffset + 90f, itemProperties.restingRotation.z);
                    }
                }
                if (snailCatScript.inSpecialAnimation)
                {
                    transform.localPosition = targetFloorPosition;
                    return;
                }
            }
        }
        else if (isHeld || isHeldByEnemy)
        {
            reachedFloorTarget = false;
        }
    }

    public override void LateUpdate()
    {
        if (snailCatScript.inSpecialAnimation && parentObject != null)
        {
            transform.rotation = parentObject.rotation;
            transform.Rotate(itemProperties.rotationOffset);
            transform.position = parentObject.position;
            Vector3 vector = itemProperties.positionOffset;
            vector = parentObject.rotation * vector;
            transform.position += vector;
        }
    }

    public override void EnableItemMeshes(bool enable)
    {
        MeshRenderer[] componentsInChildren = gameObject.GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < componentsInChildren.Length; i++)
        {
            if (!componentsInChildren[i].gameObject.CompareTag("DoNotSet") && !componentsInChildren[i].gameObject.CompareTag("InteractTrigger") && !componentsInChildren[i].gameObject.CompareTag("Enemy"))
            {
                componentsInChildren[i].enabled = enable;
            }
        }
        SkinnedMeshRenderer[] componentsInChildren2 = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
        for (int j = 0; j < componentsInChildren2.Length; j++)
        {
            componentsInChildren2[j].enabled = enable;
        }
    }

    public override void DiscardItem()
    {
        snailCatScript.DropBabyLocalClient();
        DropBabyServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
        previousPlayerHeldBy = playerHeldBy;
        base.DiscardItem();
        Plugin.ExtendedLogging($"Snail Cat Discarded and isBeingUsed: {isBeingUsed}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void DropBabyServerRpc(int playerClientId)
    {
        animator.SetBool(GrabbedAnimation, false);
        DropBabyClientRpc(playerClientId);
    }

    [ClientRpc]
    public void DropBabyClientRpc(int playerClientId)
    {
        if (playerClientId == (int)GameNetworkManager.Instance.localPlayerController.playerClientId)
        {
            return;
        }
        snailCatScript.DropBabyLocalClient();
    }
}
