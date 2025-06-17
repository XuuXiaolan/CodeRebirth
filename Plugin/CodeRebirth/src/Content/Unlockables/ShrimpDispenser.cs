using System.Collections;
using CodeRebirth.src.Content.Weapons;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;

public class ShrimpDispenser : NetworkBehaviour
{
    public AudioSource audioSource = null!;
    public AudioClip dispenseSound = null!;
    public AudioClip buttonSound = null!;
    //public GameObject particleSystemGameObject = null!;
    public Transform dispenseTransform = null!;
    public InteractTrigger dispenserTrigger = null!;

    private ScaryShrimp? lastShrimpDispensed = null;
    private bool ItemPickedUp => lastShrimpDispensed != null && Vector3.Distance(dispenseTransform.position, lastShrimpDispensed.transform.position) >= 1.5f;
    private bool currentlyDispensing = false;
    private Item itemToSpawn = null!;
    private System.Random shrimpRandom = new();

    public void Start()
    {
        shrimpRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        itemToSpawn = UnlockableHandler.Instance.ShrimpDispenser!.ItemDefinitions.GetCRItemDefinitionWithItemName("Shrimp")!.item;
        dispenserTrigger.onInteract.AddListener(OnDispenserInteract);
    }

    private void OnDispenserInteract(PlayerControllerB playerInteracting)
    {
        PlayButtonSoundServerRpc();
        if (currentlyDispensing) return;
        currentlyDispensing = true;
        PlayDispenserAnimationServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayButtonSoundServerRpc()
    {
        PlayButtonSoundClientRpc();
    }

    [ClientRpc]
    private void PlayButtonSoundClientRpc()
    {
        audioSource.PlayOneShot(buttonSound, 1f);
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayDispenserAnimationServerRpc()
    {
        PlayDispenserAnimationClientRpc();
    }

    [ClientRpc]
    private void PlayDispenserAnimationClientRpc()
    {
        StartCoroutine(PlayDispenserAnimation());
    }

    private IEnumerator PlayDispenserAnimation()
    {
        currentlyDispensing = true;
        //var newParticles = GameObject.Instantiate(particleSystemGameObject, dispenseTransform.position, Quaternion.identity, this.transform);
        //newParticles.SetActive(true);
        //Destroy(newParticles, newParticles.GetComponent<ParticleSystem>().main.duration);

        if (shrimpRandom.Next(10) <= 0 || Plugin.ModConfig.ConfigDebugMode.Value) audioSource.PlayOneShot(dispenseSound);
        yield return new WaitForSeconds(0.6f);
        if (lastShrimpDispensed != null && !ItemPickedUp)
        {
            lastShrimpDispensed.grabbable = false;
        }
        yield return new WaitForSeconds(0.4f);
        Plugin.ExtendedLogging($"Current y rotation {this.transform.rotation.y} for this gameobject: {this.gameObject.name}");
        itemToSpawn.restingRotation.y = this.transform.rotation.eulerAngles.y + 180;
        Plugin.ExtendedLogging($"Spawning {itemToSpawn} at {dispenseTransform.position} with y rotation {itemToSpawn.restingRotation.y}");
        if (IsServer)
        {
            if (lastShrimpDispensed != null && !ItemPickedUp)
            {
                if (lastShrimpDispensed.isHeld && lastShrimpDispensed.playerHeldBy != null)
                {
                    if (!lastShrimpDispensed.grabbable) lastShrimpDispensed.grabbable = true;
                    // do nothing.
                }
                else
                {
                    if (lastShrimpDispensed.IsSpawned) lastShrimpDispensed.NetworkObject.Despawn();
                }
            }
            NetworkObjectReference shrimp = CodeRebirthUtils.Instance.SpawnScrap(itemToSpawn, dispenseTransform.position, false, true, 0);
            SetParentObjectServerRpc(shrimp);
        }
        currentlyDispensing = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetParentObjectServerRpc(NetworkObjectReference shrimp)
    {
        SetParentObjectClientRpc(shrimp);
    }

    [ClientRpc]
    private void SetParentObjectClientRpc(NetworkObjectReference shrimp)
    {
        lastShrimpDispensed = ((GameObject)shrimp).GetComponent<ScaryShrimp>();
        lastShrimpDispensed.parentObject = dispenseTransform;
    }
}