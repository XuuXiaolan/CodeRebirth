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
                // Increase the NavMeshAgent's height offset over time to simulate flying
                agent.baseOffset = Mathf.Min(agent.baseOffset + Time.deltaTime * 0.2f, 4f);

                // After 20 seconds, switch to Perching state
                if (Time.time - lastIdleCycle > 20f) {
                    lastIdleCycle = Time.time;
                    currentBehaviourStateIndex = (int)State.Perching;
                    LogIfDebugBuild("Switching to Perching State.");
                }
                break;

            case (int)State.Perching:
                agent.speed = 1f;
                // Decrease the NavMeshAgent's height offset over time to simulate descending
                agent.baseOffset = Mathf.Max(agent.baseOffset - Time.deltaTime * 0.2f, 0f);

                // Once fully descended, switch to Idle state
                if (agent.baseOffset == 0f) {
                    currentBehaviourStateIndex = (int)State.Idle;
                    lastIdleCycle = Time.time;
                    LogIfDebugBuild("Switching to Idle State.");
                }
                break;

            case (int)State.Idle:
                agent.speed = 0f;
                // Set blend shape weight to 100 (assuming blend shape index 0 is for wings folded in idle position)
                SyncBlendShapeWeightClientRpc(100f);

                // After 5 seconds, switch back to Wandering state
                if (Time.time - lastIdleCycle > 5f) {
                    currentBehaviourStateIndex = (int)State.Wandering;
                    LogIfDebugBuild("Switching to Wandering State.");
                    // Set blend shape weight back to 0 (wings unfolded for wandering)
                    SyncBlendShapeWeightClientRpc(0f);
                }
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