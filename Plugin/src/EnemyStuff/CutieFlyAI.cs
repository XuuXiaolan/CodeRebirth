using System.Collections;
using System.Diagnostics;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.EnemyStuff;
public class CutieFlyAI : EnemyAI
{
    private SkinnedMeshRenderer skinnedMeshRenderer;
    private float lastIdleCycle = 0f;
    private float blendShapeWeight = 0f;
    private float blendShapeDirection = 1f; // 1 for increasing, -1 for decreasing
    private const float blendShapeSpeed = 1000f; // Speed of blend shape change
    private const float initialHeight = 5f; // Initial height to start flying

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
        transform.position = new Vector3(transform.position.x, initialHeight, transform.position.z);
        SwitchToBehaviourClientRpc((int)State.Wandering);
    }

    public override void Update() {
        base.Update();
        if (isEnemyDead) return;

        UpdateBlendShapeWeight();
        UpdateVerticalPosition();
    }

    private void UpdateBlendShapeWeight() {
        blendShapeWeight += blendShapeDirection * blendShapeSpeed * Time.deltaTime;
        if (blendShapeWeight > 100f || blendShapeWeight < 0f) {
            blendShapeDirection *= -1f;
            blendShapeWeight = Mathf.Clamp(blendShapeWeight, 0f, 100f);
        }
        skinnedMeshRenderer.SetBlendShapeWeight(0, blendShapeWeight);
    }

    private void UpdateVerticalPosition() {
        // Update vertical position to match NavMesh baseOffset
        transform.position = new Vector3(transform.position.x, initialHeight + agent.baseOffset, transform.position.z);
    }

    public override void DoAIInterval() {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) {
            return;
        };

        switch(currentBehaviourStateIndex) {
            case (int)State.Wandering:
                agent.speed = 3f;
                agent.baseOffset = Mathf.Min(agent.baseOffset + Time.deltaTime * 0.2f, 4f);
                LogIfDebugBuild($"Wandering at Height: {agent.baseOffset}");
                StartSearchIfNotActive();

                if (Time.time - lastIdleCycle > 20f) {
                    lastIdleCycle = Time.time;
                    currentBehaviourStateIndex = (int)State.Perching;
                    LogIfDebugBuild("Switching to Perching State.");
                }
                break;

            case (int)State.Perching:
                agent.speed = 1f;
                agent.baseOffset = Mathf.Max(agent.baseOffset - Time.deltaTime * 0.2f, 0f);
                LogIfDebugBuild($"Descending to Height: {agent.baseOffset}");
                StopSearchIfActive();

                if (agent.baseOffset == 0f) {
                    currentBehaviourStateIndex = (int)State.Idle;
                    lastIdleCycle = Time.time;
                    LogIfDebugBuild("Switching to Idle State.");
                }
                break;

            case (int)State.Idle:
                agent.speed = 0f;
                LogIfDebugBuild("Idle State - No Movement");
                StopSearchIfActive();
                
                if (Time.time - lastIdleCycle > 5f) {
                    currentBehaviourStateIndex = (int)State.Wandering;
                    LogIfDebugBuild("Switching to Wandering State.");
                }
                break;

            default:
                LogIfDebugBuild("This Behavior State doesn't exist!");
                break;
        }
    }

    private void StartSearchIfNotActive() {
            StartSearch(transform.position);
            LogIfDebugBuild("Search Started");
    }

    private void StopSearchIfActive() {
            StopSearch(currentSearch);
            LogIfDebugBuild("Search Stopped");
    }

    [ClientRpc]
    public void SyncBlendShapeWeightClientRpc(float currentBlendShapeWeight) {
        skinnedMeshRenderer.SetBlendShapeWeight(0, currentBlendShapeWeight);
    }
}
