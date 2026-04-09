using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.MiscScripts;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;
using Dawn.Utils;
using UnityEngine.AI;
using CodeRebirth.src.Util.Timer;
using System;

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
    [field: SerializeField]
    public float MinimumActivationTime { get; private set; }
    [field: SerializeField]
    public float LaserDuration { get; private set; }


    private Timer MinimumActivationTimer;
    private Timer LaserDurationTimer;

    private static readonly int RunSpeedAnimation = Animator.StringToHash("RunSpeed"); // Normalized Float 0.0-1.0
    private static readonly int DeactivateAnimation = Animator.StringToHash("Deactivate"); // Trigger
    private static readonly int ActivateAnimation = Animator.StringToHash("Activate"); // Trigger


    public override void Start()
    {
        base.Start();
        MinimumActivationTimer = new Timer(MinimumActivationTime, TimerExecutionTime.Update);
        LaserDurationTimer = new Timer(LaserDuration, TimerExecutionTime.Update);
        // MinimumActivationTimer.OnFinish += ActivateLaserFromTimer;
        LaserDurationTimer.OnFinish += DeactivateLaser;
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        // public void ItemActivate(bool used, bool buttonDown = true){
        base.ItemActivate(used, buttonDown);
        if (base.IsOwner)
        {
            //Throw it
            Rigidbody.isKinematic = false;
            grabbable = false;
            MinimumActivationTimer.Start();
            Rigidbody.AddForce(playerHeldBy.gameplayCamera.transform.forward * 30.0f, ForceMode.VelocityChange);
            playerHeldBy.DiscardHeldObject(placeObject: true, null, playerHeldBy.transform.position);
        }
    }


    private void DeactivateLaser(Timer t){
        grabbable = true;
        Animator.SetTrigger(DeactivateAnimation);
        NavMeshAgent.enabled = false;
        Rigidbody.isKinematic = false;
        SmartAgentNavigator.enabled = false;

    }

    private void ActivateLaser() {
        MinimumActivationTimer.Stop();
        LaserDurationTimer.Start();
        Animator.SetTrigger(ActivateAnimation);
        NavMeshAgent.enabled = true;
        Rigidbody.isKinematic = true;
        SmartAgentNavigator.enabled = true;
        SmartAgentNavigator.StartSearchRoutine(20f);
        NavMeshAgent.Warp(playerHeldBy.transform.position);
    }

    public override void DiscardItem(){
        // public void DiscardItem(){
        base.DiscardItem();
    }

    public override void PocketItem()
    {
        // public void PocketItem(){
        base.PocketItem();
    }
    public override void Update(){
        base.Update();
        Animator.SetFloat(RunSpeedAnimation, NavMeshAgent.velocity.magnitude / NavMeshAgent.speed);
    }

    public override void LateUpdate(){
        // public void LateUpdate(){
        if (!NavMeshAgent.enabled && Rigidbody.isKinematic){
            base.LateUpdate();
        }else if (!Rigidbody.isKinematic){
            if(Rigidbody.velocity.magnitude<2.0 && MinimumActivationTimer.IsFinished){
                ActivateLaser();
            }
        }
    }
}
