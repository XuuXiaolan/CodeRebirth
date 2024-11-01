using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Items;
public class ChildEnemyAI : GrabbableObject
{
    public NavMeshAgent agent = null!;
    public Animator animator = null!;
    public NetworkAnimator networkAnimator = null!;

    [NonSerialized] public int health = 4;
    [NonSerialized] public bool mommyAlive = true;
    private static readonly int isChildDeadAnimation = Animator.StringToHash("isChildDead");
    private static readonly int childGrabbedAnimation = Animator.StringToHash("childGrabbed");
    private static readonly int isWalkingAnimation = Animator.StringToHash("isWalking");
    private static readonly int isGoofyAnimation = Animator.StringToHash("isGoofy");
    private static readonly int isRunningAnimation = Animator.StringToHash("isRunning");
    private static readonly int isScaredAnimation = Animator.StringToHash("isScared");
    private static readonly int isSittingAnimation = Animator.StringToHash("isSitting");
    private static readonly int doIdleGestureAnimation = Animator.StringToHash("doIdleGesture");
    private static readonly int doSitGesture1Animation = Animator.StringToHash("doSitGesture1");
    private static readonly int doSitGesture2Animation = Animator.StringToHash("doSitGesture2");

    public override void Start()
    {
        base.Start();
    }

    public override void Update()
    {
        base.Update();
        if (!IsServer) return;
    }

    [ServerRpc(RequireOwnership = false)]
    public void DoBoolAnimationServerRpc(bool isChildDead, bool childGrabbed, bool isWalking, bool isGoofy, bool isRunning, bool isScared, bool isSitting)
    {
        animator.SetBool(isChildDeadAnimation, isChildDead);
        animator.SetBool(childGrabbedAnimation, childGrabbed);
        animator.SetBool(isWalkingAnimation, isWalking);
        animator.SetBool(isGoofyAnimation, isGoofy);
        animator.SetBool(isRunningAnimation, isRunning);
        animator.SetBool(isScaredAnimation, isScared);
        animator.SetBool(isSittingAnimation, isSitting);
    }
}