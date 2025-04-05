using System.Collections.Generic;
using CodeRebirth.src.MiscScripts;
using GameNetcodeStuff;
using Unity.Netcode;
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
        OnSurfaceHit.AddListener(OnSurfaceHitEvent);
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

    public void OnSurfaceHitEvent(int surfaceID)
    {
        if (!playerHeldBy.IsOwner) return;
        OnSurfaceHitServerRpc(surfaceID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnSurfaceHitServerRpc(int surfaceID)
    {
        OnSurfaceHitClientRpc(surfaceID);
    }

    [ClientRpc]
    public void OnSurfaceHitClientRpc(int surfaceID)
    {
        // switch this to a listener for the objects hit and rpc it
        stuckToWall = true;
        // Check if the weapon tip is angled downward.
        /*float dot = Vector3.Dot(weaponTip.forward, -Vector3.up);
        Plugin.ExtendedLogging($"Vector Dot: {dot}");
        if (dot > 0.92f)        {
            // The tip is aimed toward the ground, so skip the stuck logic.
            return;
        }*/ // doesn't work too well :/

        if (playerHeldBy != GameNetworkManager.Instance.localPlayerController)
        {
            grabbable = false;
            return;
        }
        stuckPlayer = playerHeldBy;
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

    public override void EquipItem()
    {
        base.EquipItem();
        grabbable = true;
        stuckToWall = false;
        stuckPlayer = null;
    }

    public void JumpActionTriggered(PlayerControllerB player)
    {
        if (!stuckToWall || stuckPlayer == null) return;
        weaponAudio.PlayOneShot(unlatchSounds[Random.Range(0, unlatchSounds.Length)]);
        stuckPlayer.activatingItem = false;
        stuckPlayer.disableMoveInput = false;
        stuckPlayer.externalForceAutoFade = Vector3.up * 30f;
        CRUtilities.MakePlayerGrabObject(stuckPlayer, this);
    }

    public void InteractActionTriggered(PlayerControllerB player)
    {
        if (!stuckToWall || stuckPlayer == null) return;
        weaponAudio.PlayOneShot(unlatchSounds[Random.Range(0, unlatchSounds.Length)]);
        stuckPlayer.activatingItem = false;
        stuckPlayer.disableMoveInput = false;
        CRUtilities.MakePlayerGrabObject(stuckPlayer, this);
    }

    public float GetFreezePercentage()
    {
        return iceMaterial.color.a * 100f;
    }
}