using System;
using System.Collections;
using System.Diagnostics;
using GameNetcodeStuff;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using CodeRebirth.Misc;

namespace CodeRebirth.EnemyStuff;
public class CutieFlyAI : EnemyAI
{
    SkinnedMeshRenderer skinnedMeshRenderer;
    float lastIdleCycle = 0f;
    float blendShapeWeight = 0f;
    float blendShapeDirection = 1f;
    const float blendShapeSpeed = 1000f;
    bool climbing = true;

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
        if (currentBehaviourStateIndex == (int)State.Idle) return;
        blendShapeWeight += blendShapeDirection * blendShapeSpeed * Time.deltaTime;
        if (blendShapeWeight > 100f || blendShapeWeight < 0f) {
            blendShapeDirection *= -1f;
            blendShapeWeight = Mathf.Clamp(blendShapeWeight, 0f, 100f);
        }
        skinnedMeshRenderer.SetBlendShapeWeight(0, blendShapeWeight);
    }
    void WanderAround(float timeSinceLastStateChange)
    {
        agent.speed = 3f;
        agent.baseOffset = Mathf.Lerp(agent.baseOffset, climbing ? 4f : 2f, Time.deltaTime * 5f);
        if (agent.baseOffset >= 3.5f) climbing = false;
        if (agent.baseOffset <= 2.5f) climbing = true;
        if (timeSinceLastStateChange > 20f)
        {
            SwitchToBehaviourClientRpc((int)State.Perching);
            LogIfDebugBuild("Switching to Perching State.");
            lastIdleCycle = Time.time;
        }
    }

    void Perch()
    {
        agent.speed = 1f;
        agent.baseOffset = Mathf.Lerp(agent.baseOffset, 0f, Time.deltaTime * 6f);

        if (agent.baseOffset <= 0.1f)
        {
            StopSearch(currentSearch);
            creatureSFX.enabled = false;
            creatureVoice.enabled = false;
            SwitchToBehaviourClientRpc((int)State.Idle);
            SyncBlendShapeWeightClientRpc(100f);
            LogIfDebugBuild("Switching to Idle State.");
            lastIdleCycle = Time.time;
        }
    }

    void Idling(float timeSinceLastStateChange)
    {
        agent.speed = 0f;
        if (timeSinceLastStateChange > 5f)
        {
            StartSearch(transform.position);
            creatureSFX.enabled = true;
            creatureVoice.enabled = true;
            SwitchToBehaviourClientRpc((int)State.Wandering);
            LogIfDebugBuild("Switching to Wandering State.");
            lastIdleCycle = Time.time;
        }
    }
    public override void DoAIInterval() {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        float timeSinceLastStateChange = Time.time - lastIdleCycle;

        switch(currentBehaviourStateIndex) {
            case (int)State.Wandering:
                WanderAround(timeSinceLastStateChange);
                break;

            case (int)State.Perching:
                Perch();
                break;

            case (int)State.Idle:
                Idling(timeSinceLastStateChange);
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
