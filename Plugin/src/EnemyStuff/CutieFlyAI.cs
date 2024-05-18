using System;
using System.Collections;
using System.Diagnostics;
using GameNetcodeStuff;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.EnemyStuff;
public class CutieFlyAI : EnemyAI
{
    private SkinnedMeshRenderer skinnedMeshRenderer;
    private float lastIdleCycle = 0f;
    private float blendShapeWeight = 0f;
    private float blendShapeDirection = 1f;
    private const float blendShapeSpeed = 1000f;
    private bool climbing = true;

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
        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        lastIdleCycle = Time.time;
        StartSearch(transform.position);
        SwitchToBehaviourClientRpc((int)State.Wandering);
    }

    public override void Update() {
        base.Update();
        if (isEnemyDead) return;
        UpdateBlendShapeWeight();
    }

    private void UpdateBlendShapeWeight() {
        if (currentBehaviourStateIndex == (int)State.Idle) {
            blendShapeWeight += blendShapeDirection * blendShapeSpeed * Time.deltaTime;
            if (blendShapeWeight > 100f || blendShapeWeight < 90f) {
                blendShapeDirection *= -1f;
                blendShapeWeight = Mathf.Clamp(blendShapeWeight, 0f, 100f);
            }
            skinnedMeshRenderer.SetBlendShapeWeight(0, blendShapeWeight);
            return;
        };
        blendShapeWeight += blendShapeDirection * blendShapeSpeed * Time.deltaTime;
        if (blendShapeWeight > 100f || blendShapeWeight < 0f) {
            blendShapeDirection *= -1f;
            blendShapeWeight = Mathf.Clamp(blendShapeWeight, 0f, 100f);
        }
        skinnedMeshRenderer.SetBlendShapeWeight(0, blendShapeWeight);
    }

    public override void DoAIInterval() {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        float timeSinceLastStateChange = Time.time - lastIdleCycle;

        switch(currentBehaviourStateIndex) {
            case (int)State.Wandering:
                agent.speed = 3f;
                agent.baseOffset = Mathf.Lerp(agent.baseOffset, climbing ? 4f : 2f, Time.deltaTime * 5f);
                if (agent.baseOffset >= 3.5f) climbing = false;
                if (agent.baseOffset <= 2.5f) climbing = true;
                LogIfDebugBuild($"Wandering at Height: {agent.baseOffset}");

                if (timeSinceLastStateChange > 20f) {
                    SwitchToBehaviourClientRpc((int)State.Perching);
                    LogIfDebugBuild("Switching to Perching State.");
                    lastIdleCycle = Time.time;
                }
                break;

            case (int)State.Perching:
                agent.speed = 1f;
                agent.baseOffset = Mathf.Lerp(agent.baseOffset, 0f, Time.deltaTime * 6f);
                LogIfDebugBuild($"Descending to Height: {agent.baseOffset}");

                if (agent.baseOffset <= 0.1f) {
                    StopSearch(currentSearch);
                    SyncBlendShapeWeightClientRpc(100f);
                    SwitchToBehaviourClientRpc((int)State.Idle);
                    LogIfDebugBuild("Switching to Idle State.");
                    lastIdleCycle = Time.time;
                }
                break;

            case (int)State.Idle:
                agent.speed = 0f;
                LogIfDebugBuild("Idle State - No Movement");
                if (timeSinceLastStateChange > 5f) {
                    StartSearch(transform.position);
                    SwitchToBehaviourClientRpc((int)State.Wandering);
                    LogIfDebugBuild("Switching to Wandering State.");
                    lastIdleCycle = Time.time;
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
