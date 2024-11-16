using System;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.MiscScripts;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Items;
[RequireComponent(typeof(SmartAgentNavigator))]
public class ChildEnemyAI : GrabbableObject
{
    public NavMeshAgent agent = null!;
    public Animator animator = null!;
    public NetworkAnimator networkAnimator = null!;
    public SmartAgentNavigator smartAgentNavigator = null!;

    [NonSerialized] public ParentEnemyAI parentEevee;
    [NonSerialized] public int health = 4;
    [NonSerialized] public bool mommyAlive = true;
    public bool CloseToSpawn => Vector3.Distance(transform.position, parentEevee.spawnTransform.position) < 1.5f;
    private static readonly int isChildDeadAnimation = Animator.StringToHash("isChildDead");
    private static readonly int childGrabbedAnimation = Animator.StringToHash("childGrabbed");
    private static readonly int isWalkingAnimation = Animator.StringToHash("isWalking");
    private static readonly int isGoofyAnimation = Animator.StringToHash("isGoofy");
    private static readonly int isRunningAnimation = Animator.StringToHash("isRunning");
    private static readonly int isScaredAnimation = Animator.StringToHash("isScared");
    private static readonly int isSittingAnimation = Animator.StringToHash("isSitting");
    private static readonly int isDancingAnimation = Animator.StringToHash("isDancing");
    private static readonly int doIdleGestureAnimation = Animator.StringToHash("doIdleGesture");
    private static readonly int doSitGesture1Animation = Animator.StringToHash("doSitGesture1");
    private static readonly int doSitGesture2Animation = Animator.StringToHash("doSitGesture2");
    private State eeveeState = State.Spawning;
    public enum State
    {
        Spawning,
        Wandering,
        FollowingPlayer,
        Scared
    }

    public override void Start()
    {
        base.Start();
        smartAgentNavigator.SetAllValues(parentEevee.isOutside);
    }

    public override void Update()
    {
        base.Update();
        if (!IsServer) return;

        DoHostSideUpdate();
    }

    private void DoHostSideUpdate()
    {
        switch (eeveeState)
        {
            case State.Spawning:
                break;
            case State.Wandering:
                DoWandering();
                break;
            case State.FollowingPlayer:
                break;
            case State.Scared:
                break;
        }
    }

    private void DoSpawning()
    {

    }

    private void DoWandering()
    {
        
    }

    private void DoFollowingPlayer()
    {
        
    }

    private void DoScared()
    {

    }

    private void DetectNearbyPlayer()
    {

    }

    [ServerRpc(RequireOwnership = false)]
    public void DoBoolAnimationServerRpc(bool isChildDead, bool childGrabbed, bool isWalking, bool isGoofy, bool isRunning, bool isScared, bool isSitting, bool isDancing)
    {
        animator.SetBool(isChildDeadAnimation, isChildDead);
        animator.SetBool(childGrabbedAnimation, childGrabbed);
        animator.SetBool(isWalkingAnimation, isWalking);
        animator.SetBool(isGoofyAnimation, isGoofy);
        animator.SetBool(isRunningAnimation, isRunning);
        animator.SetBool(isScaredAnimation, isScared);
        animator.SetBool(isSittingAnimation, isSitting);
        animator.SetBool(isDancingAnimation, isDancing);
    }
}