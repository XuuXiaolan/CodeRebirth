using Unity.Netcode;
using UnityEngine;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Util.Extensions;

namespace CodeRebirth.src.Content.Enemies;
public class CutieFlyAI : CodeRebirthEnemyAI
{
    private SkinnedMeshRenderer skinnedMeshRenderer = null!;
    float lastIdleCycle = 0f;
    float blendShapeWeight = 0f;
    
    float blendShapeDirection = 1f;
    const float blendShapeSpeed = 1000f;
    bool climbing = true;

    const float WANDER_SPEED = 3f;
    const float PERCH_SPEED = 1f;
    const float IDLE_SPEED = 0f;

    const float MAXIMUM_CLIMBING_OFFSET = 3.5f;
    const float MINIMUM_CLIMBING_OFFSET = 2.5f;

    const float LAND_OFFSET = 0.1f;

    const float IDLE_MAXIMUM_TIME = 5f;
    const float WANDERING_MAXIMUM_TIME = 20f;

    public enum State {
        Wandering,
        Perching,
        Idle,
    }

    public override void Start() {
        base.Start();
        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        lastIdleCycle = Time.time;
        StartSearch(transform.position);
        this.SwitchToBehaviourStateOnLocalClient(State.Wandering);
    }

    public override void Update() {
        base.Update();
        if (isEnemyDead) return;
        creatureSFX.volume = Plugin.ModConfig.ConfigCutieFlyFlapWingVolume.Value;
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
        agent.speed = WANDER_SPEED;
        agent.baseOffset = Mathf.Lerp(agent.baseOffset, climbing ? 4f : 2f, Time.deltaTime * 5f);
        climbing = agent.baseOffset <= MINIMUM_CLIMBING_OFFSET && agent.baseOffset < MAXIMUM_CLIMBING_OFFSET;
        if (timeSinceLastStateChange > WANDERING_MAXIMUM_TIME)
        {
            this.SwitchToBehaviourStateOnLocalClient(State.Perching);
            lastIdleCycle = Time.time;
        }
    }

    void Perch()
    {
        agent.speed = PERCH_SPEED;
        agent.baseOffset = Mathf.Lerp(agent.baseOffset, 0f, Time.deltaTime * 6f);

        if (agent.baseOffset <= LAND_OFFSET)
        {
            StopSearch(currentSearch);
            ToggleEnemySounds(false);
            this.SwitchToBehaviourStateOnLocalClient(State.Idle);
            SyncBlendShapeWeightOnLocalClient(100f);
            lastIdleCycle = Time.time;
        }
    }

    void Idling(float timeSinceLastStateChange)
    {
        agent.speed = IDLE_SPEED;
        if (timeSinceLastStateChange > IDLE_MAXIMUM_TIME)
        {
            StartSearch(transform.position);
            ToggleEnemySounds(true);
            this.SwitchToBehaviourStateOnLocalClient(State.Wandering);
            lastIdleCycle = Time.time;
        }
    }
    public override void DoAIInterval() {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        float timeSinceLastStateChange = Time.time - lastIdleCycle;

        switch(currentBehaviourStateIndex.ToCutieState()) {
            case State.Wandering:
                WanderAround(timeSinceLastStateChange);
                break;

            case State.Perching:
                Perch();
                break;

            case State.Idle:
                Idling(timeSinceLastStateChange);
                break;

            default:
                Plugin.Logger.LogWarning("This Behavior State doesn't exist!");
                break;
        }
    }

    [ClientRpc]
    public void SyncBlendShapeWeightClientRpc(float currentBlendShapeWeight) {
        SyncBlendShapeWeightOnLocalClient(currentBlendShapeWeight);
    }

    public void SyncBlendShapeWeightOnLocalClient(float currentBlendShapeWeight)
    {
        skinnedMeshRenderer.SetBlendShapeWeight(0, currentBlendShapeWeight);
    }
}