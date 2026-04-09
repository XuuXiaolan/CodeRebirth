using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.MiscScripts;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;
using Dawn.Utils;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Items;
[RequireComponent(typeof(SmartAgentNavigator))]
public class OrbitalStrike : GrabbableObject
// public class OrbitalStrike : MonoBehaviour
{
    [field: SerializeField]
    public Animator Animator { get; private set; }
    [field: SerializeField]
    public VisualEffect VisualEffect { get; private set; }
    [field: SerializeField]
    public NavMeshAgent NavMeshAgent { get; private set; }
    [field: SerializeField]
    public SmartAgentNavigator SmartAgentNavigator { get; private set; }
    [field: SerializeField]
    public Rigidbody Rigidbody { get; private set; }

    private static readonly int RunSpeedAnimation = Animator.StringToHash("RunSpeed"); // Normalized Float 0.0-1.0
    private static readonly int DeactivateAnimation = Animator.StringToHash("Deactivate"); // Trigger
    private static readonly int ActivateAnimation = Animator.StringToHash("Activate"); // Trigger




    public override void Start()
    {
        base.Start();
    }

    public override void ItemActivate(bool used, bool buttonDown = true) {
        // public void ItemActivate(bool used, bool buttonDown = true){
        base.ItemActivate(used, buttonDown);
        if (base.IsOwner)
        {
            //Throw it
            Rigidbody.isKinematic = false;
            grabbable = false;
            Rigidbody.AddForce(playerHeldBy.gameplayCamera.transform.forward * 30.0f,ForceMode.VelocityChange);
            playerHeldBy.DiscardHeldObject(placeObject: true, null, playerHeldBy.transform.position);
        }
    }

    private void activateLaser(){
        Animator.SetTrigger(ActivateAnimation);
        NavMeshAgent.enabled = true;
        Rigidbody.isKinematic = true;
        SmartAgentNavigator.enabled = true;
        SmartAgentNavigator.StartSearchRoutine(20f);
        NavMeshAgent.Warp(playerHeldBy.transform.position);
    }

    public override void DiscardItem()
    {
        // public void DiscardItem(){
        base.DiscardItem();
    }

    public override void PocketItem(){
    // public void PocketItem(){
        base.PocketItem();
    }

    public override void LateUpdate(){
        // public void LateUpdate(){
        if (!NavMeshAgent.enabled && Rigidbody.isKinematic){
            base.LateUpdate();
        }else if (!Rigidbody.isKinematic){
            if(Rigidbody.velocity.magnitude<2.0){
                activateLaser();
            }
        }
    }
}
