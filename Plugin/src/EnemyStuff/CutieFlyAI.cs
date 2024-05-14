using System.Collections;
using System.Diagnostics;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.EnemyStuff;
public class CutieFlyAI : EnemyAI
{
    public bool flying = false;
    private SkinnedMeshRenderer skinnedMeshRenderer;
    public float lastIdleCycle = 0f;
    enum State {
        Wandering,
        Perching,
        Idle,
    }

    [Conditional("DEBUG")]
    void LogIfDebugBuild(string text) {
        Plugin.Logger.LogInfo(text);
    }

    public override void Start() {
        base.Start();
        LogIfDebugBuild("CutieFly Spawned.");
        skinnedMeshRenderer = transform.Find("Wings").GetComponent<SkinnedMeshRenderer>();
        flying = true;
        lastIdleCycle = Time.time;
        currentBehaviourStateIndex = (int)State.Wandering;
        StartSearch(transform.position);
    }

    public override void Update() {
        base.Update();
        if (isEnemyDead) return;
    }

    public override void DoAIInterval() {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) {
            return;
        };

        switch(currentBehaviourStateIndex) {
            case (int)State.Wandering:
                agent.speed = 3f;
                // start wandering by slowly increasing navmesh offset to 4 then randomly decide to start perching after like 20 seconds or smthn
                lastIdleCycle += Time.deltaTime;
                break;

            case (int)State.Perching:
                agent.speed = 1f;
                // start perching by slowly decreasing navmesh offset to 0, once 0, start idle
                break;

            case (int)State.Idle:
                agent.speed = 0f;
                // set blend shape weight to 100 and after 5 seconds go back to wandering
                break;
                
            default:
                LogIfDebugBuild("This Behavior State doesn't exist!");
                break;
        }
    }
    [ClientRpc]
    public void SyncBlendShapeWeightClientRpc(float currentBlendShapeWeight) {
        skinnedMeshRenderer.SetBlendShapeWeight(0, currentBlendShapeWeight);
    }
}