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
    public GameObject particleSystemGameObject = null!;
    public Transform dispenseTransform = null!;
    public InteractTrigger dispenserTrigger = null!;

    private ScaryShrimp? lastShrimpDispensed = null;
    private bool ItemPickedUp => lastShrimpDispensed != null && Vector3.Distance(dispenseTransform.position, lastShrimpDispensed.transform.position) >= 1.5f;
    private bool currentlyDispensing = false;
    private readonly Item itemToSpawn = UnlockableHandler.Instance.ShrimpDispenser.ShrimpWeapon;

    public void Start()
    {
        dispenserTrigger.onInteract.AddListener(OnDispenserInteract);
    }

    private void OnDispenserInteract(PlayerControllerB playerInteracting)
    {
        if (currentlyDispensing) return;
        currentlyDispensing = true;
        PlayDispenserAnimationServerRpc();
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
        var newParticles = GameObject.Instantiate(particleSystemGameObject, dispenseTransform.position, Quaternion.identity, this.transform);
        newParticles.SetActive(true);
        Destroy(newParticles, newParticles.GetComponent<ParticleSystem>().main.duration);
        audioSource.PlayOneShot(dispenseSound);
        yield return new WaitForSeconds(1f);
        if (IsServer)
        {
            if (lastShrimpDispensed != null && !ItemPickedUp)
            {
                lastShrimpDispensed.NetworkObject.Despawn();
            }
            NetworkObjectReference shrimp = CodeRebirthUtils.Instance.SpawnScrap(itemToSpawn, dispenseTransform.position, false, true, 0);
            lastShrimpDispensed = ((GameObject)shrimp).GetComponent<ScaryShrimp>();
        }
        currentlyDispensing = false;
    }
}