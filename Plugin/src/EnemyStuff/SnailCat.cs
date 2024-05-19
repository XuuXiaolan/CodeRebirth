using System;
using System.Collections;
using System.Diagnostics;
using GameNetcodeStuff;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.EnemyStuff;
public class SnailCatAI : EnemyAI
{

    enum State {
        Wandering,
    }

    [Conditional("DEBUG")]
    void LogIfDebugBuild(string text) {
        Plugin.Logger.LogInfo(text);
    }

    public override void Start() {
        base.Start();
        LogIfDebugBuild("SnailCat Spawned.");
        StartSearch(transform.position);
        SwitchToBehaviourClientRpc((int)State.Wandering);
    }

    public override void Update() {
        base.Update();
        if (isEnemyDead) return;
    }

    public override void DoAIInterval() {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        switch(currentBehaviourStateIndex) {
            case (int)State.Wandering:
                agent.speed = 4f;
                break;
            default:
                LogIfDebugBuild("This Behavior State doesn't exist!");
                break;
        }
    }
    [ClientRpc]
    private void DoAnimationClientRpc(string animationName) {
        LogIfDebugBuild(animationName);
        creatureAnimator.SetTrigger(animationName);
    }
}
