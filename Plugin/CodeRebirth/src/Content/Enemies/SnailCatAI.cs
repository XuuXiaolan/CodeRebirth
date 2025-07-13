
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
using CodeRebirthLib.ContentManagement.Enemies;
using CodeRebirthLib.ContentManagement.Items;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Enemies;

public class SnailCatAI : CodeRebirthEnemyAI
{
    public SnailCatPhysicsProp propScript = null!;
    public ScanNodeProperties scanNodeProperties = null!;
    public AudioClip[] hitSounds = [];
    public AudioClip enemyDetectSound = null!;
    public AudioClip[] wiwiwiiiSound = [];

    private string currentName = "";
    private bool holdingBaby = false;
    private Coroutine? dropBabyCoroutine = null;
    private PlayerControllerB? playerHolding = null;
    private float specialActionTimer = 1f;
    private float detectEnemyInterval = 0f;
    private bool isWiWiWiii = false;
    internal Vector3 localScale = Vector3.one;
    internal string snailCatName = "Mu";
    internal float shiftHash = 0;
    internal bool wasFake = false;

    public enum State
    {
        Wandering,
        Following,
        Sleeping,
        Sitting,
        Grabbed
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer || !wasFake)
            return;

        StartCoroutine(DelayForBit());
    }

    private IEnumerator DelayForBit()
    {
        yield return new WaitForSeconds(0.1f);
        SyncNewSnailCatServerRpc(localScale, snailCatName, shiftHash);
    }

    public override void Start()
    {
        base.Start();
        QualitySettings.skinWeights = SkinWeights.FourBones;
        List<string> randomizedNames = new();
        if (Plugin.Mod.EnemyRegistry().TryGetFromEnemyName("Real Enemy SnailCat", out CREnemyDefinition? CREnemyDefinition))
        {
            randomizedNames = CREnemyDefinition.GetGeneralConfig<string>("SnailCat | Possible SnailCat Names").Value.Split(';').Select(s => s.Trim()).ToList();
        }
        if (randomizedNames.Count == 0) randomizedNames.Add("Mu");
        string randomName = randomizedNames[enemyRandom.Next(randomizedNames.Count)];
        float randomScale = enemyRandom.NextFloat(0.75f, 1.25f);
        this.transform.localScale *= randomScale;
        scanNodeProperties.headerText = randomName;
        currentName = randomName;
        if (currentName == "Mu")
        {
            this.transform.localScale *= 0.1f;
        }
        propScript.originalScale = this.transform.localScale;
        isWiWiWiii = currentName == "Wiwiwii";
        if (IsServer) smartAgentNavigator.StartSearchRoutine(50);
    }

    public override void Update()
    {
        base.Update();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;
        _idleTimer -= Time.deltaTime;
        if (_idleTimer <= 0)
        {
            _idleTimer = enemyRandom.NextFloat(_idleAudioClips.minTime, _idleAudioClips.maxTime);
            if (isWiWiWiii)
            {
                creatureVoice.PlayOneShot(wiwiwiiiSound[enemyRandom.Next(wiwiwiiiSound.Length)]);
            }
            else
            {
                creatureVoice.PlayOneShot(_idleAudioClips.audioClips[enemyRandom.Next(_idleAudioClips.audioClips.Length)]);
            }
        }

        if (currentBehaviourStateIndex != (int)State.Grabbed && currentBehaviourStateIndex != (int)State.Following) return;
        detectEnemyInterval -= Time.deltaTime;
        if (detectEnemyInterval <= 0)
        {
            detectEnemyInterval = enemyRandom.NextFloat(7.5f, 15.5f);
            bool enemyNearby = false;
            foreach (var enemy in RoundManager.Instance.SpawnedEnemies)
            {
                if (enemy is SnailCatAI) continue;
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < 15)
                {
                    enemyNearby = true;
                    break;
                }
            }
            if (!enemyNearby) return;
            creatureVoice.PlayOneShot(enemyDetectSound);
        }
    }

    #region State Machines
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        switch (currentBehaviourStateIndex)
        {
            case (int)State.Wandering:
                DoWandering();
                break;
            case (int)State.Following:
                DoFollowing();
                break;
            case (int)State.Sleeping:
                break;
            case (int)State.Sitting:
                break;
            case (int)State.Grabbed:
                break;
        }
    }

    private void DoWandering()
    {
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player.isPlayerDead || !player.isPlayerControlled) continue;
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance > 5) continue;
            SetPlayerTargetServerRpc(player);
            SwitchToBehaviourServerRpc((int)State.Following);
            return;
        }

        specialActionTimer -= AIIntervalTime;
        if (specialActionTimer <= 0)
        {
            smartAgentNavigator.StopSearchRoutine();
            agent.SetDestination(transform.position);
            specialActionTimer = UnityEngine.Random.Range(7.5f, 15f);
            if (UnityEngine.Random.Range(0, 100) < 50)
            {
                creatureAnimator.SetBool(SnailCatPhysicsProp.SleepingAnimation, true);
                SwitchToBehaviourServerRpc((int)State.Sleeping);
                StartCoroutine(ChangeStateWithDelayRoutine(20, State.Sleeping, SnailCatPhysicsProp.SleepingAnimation));
            }
            else
            {
                creatureAnimator.SetBool(SnailCatPhysicsProp.SittingAnimation, true);
                SwitchToBehaviourServerRpc((int)State.Sitting);
                StartCoroutine(ChangeStateWithDelayRoutine(10, State.Sitting, SnailCatPhysicsProp.SittingAnimation));
            }
        }
    }

    private void DoFollowing()
    {
        smartAgentNavigator.DoPathingToDestination(targetPlayer.transform.position);
        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);
        if (distanceToPlayer > 15)
        {
            // agent.speed = 4f;
            ClearPlayerTargetServerRpc();
            SwitchToBehaviourServerRpc((int)State.Wandering);
            smartAgentNavigator.StartSearchRoutine(50);
            return;
        }
    }

    #endregion
    public IEnumerator ChangeStateWithDelayRoutine(float delay, State stateToSwitchOutOf, int animationToSwitchOutOf)
    {
        yield return new WaitForSeconds(delay);
        if (currentBehaviourStateIndex != (int)stateToSwitchOutOf)
            yield break;

        smartAgentNavigator.StartSearchRoutine(50);
        creatureAnimator.SetBool(animationToSwitchOutOf, false);
        SwitchToBehaviourServerRpc((int)State.Wandering);
    }

    public void PickUpBabyLocalClient()
    {
        Plugin.ExtendedLogging($"picked up by {propScript.playerHeldBy}");
        SwitchToBehaviourServerRpc((int)State.Grabbed);
        EnableOrDisableAgentWithRoutineServerRpc(false, false);
        currentOwnershipOnThisClient = (int)propScript.playerHeldBy.playerClientId;
        inSpecialAnimation = true;
        holdingBaby = true;
        if (dropBabyCoroutine != null)
        {
            StopCoroutine(dropBabyCoroutine);
        }

        playerHolding = propScript.playerHeldBy;
    }

    public void DropBabyLocalClient()
    {
        Plugin.ExtendedLogging($"dropped by {propScript.previousPlayerHeldBy}");
        SwitchToBehaviourStateOnLocalClient((int)State.Wandering);
        holdingBaby = false;
        playerHolding = null;
        if (IsOwner && !IsOwnedByServer)
        {
            ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
        }

        bool gotValidDropPosition = true;
        Vector3 startPosition = this.transform.position;
        if (propScript.previousPlayerHeldBy == null)
        {
            gotValidDropPosition = false;
        }
        else
        {
            startPosition = propScript.previousPlayerHeldBy.transform.position;
        }
        Vector3 itemFloorPosition = propScript.GetItemFloorPosition(startPosition + Vector3.up * 0.5f);
        Vector3 groundNavmeshPosition = RoundManager.Instance.GetNavMeshPosition(itemFloorPosition, default(NavMeshHit), 10f, -1);

        if (!RoundManager.Instance.GotNavMeshPositionResult)
        {
            gotValidDropPosition = false;
            itemFloorPosition = propScript.startFallingPosition;
            if (propScript.transform.parent != null)
            {
                itemFloorPosition = propScript.transform.parent.TransformPoint(propScript.startFallingPosition);
            }
            Transform transform = ChooseClosestNodeToPositionNoPathCheck(itemFloorPosition);
            groundNavmeshPosition = RoundManager.Instance.GetNavMeshPosition(transform.transform.position, default(NavMeshHit), 5f, -1);
        }

        if (propScript.transform.parent == null)
        {
            propScript.targetFloorPosition = groundNavmeshPosition;
        }
        else
        {
            propScript.targetFloorPosition = propScript.transform.parent.InverseTransformPoint(groundNavmeshPosition);
        }

        if (gotValidDropPosition)
        {
            if (dropBabyCoroutine != null)
            {
                StopCoroutine(dropBabyCoroutine);
            }
            dropBabyCoroutine = StartCoroutine(DropBabyAnimation(groundNavmeshPosition));
            return;
        }

        transform.position = groundNavmeshPosition;
        inSpecialAnimation = false;
        if (!IsServer) return;
        agent.enabled = true;
        smartAgentNavigator.enabled = true;
        smartAgentNavigator.StartSearchRoutine(50);
    }

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (propScript.IsOwner) propScript.ownerNetworkAnimator.SetTrigger(SnailCatPhysicsProp.HitAnimation);
        // trigger hit animation
        creatureVoice.PlayOneShot(hitSounds[enemyRandom.Next(hitSounds.Length)]);
    }

    private IEnumerator DropBabyAnimation(Vector3 dropOnPosition)
    {
        float time = Time.realtimeSinceStartup;
        yield return new WaitUntil(() => propScript.reachedFloorTarget || Time.realtimeSinceStartup - time > 2f);
        transform.position = dropOnPosition;
        inSpecialAnimation = false;
        dropBabyCoroutine = null;
        if (!IsServer) yield break;
        agent.enabled = true;
        smartAgentNavigator.enabled = true;
        smartAgentNavigator.StartSearchRoutine(50);
    }

    public Transform ChooseClosestNodeToPositionNoPathCheck(Vector3 pos)
    {
        List<GameObject> allAINodes = RoundManager.Instance.insideAINodes.Concat(RoundManager.Instance.outsideAINodes).ToList();
        var nodesTempArray = allAINodes.Where(x => x != null).OrderBy(x => Vector3.Distance(pos, x.transform.position));
        return nodesTempArray.First().transform;
    }

    [ServerRpc(RequireOwnership = false)]
    private void EnableOrDisableAgentWithRoutineServerRpc(bool enable, bool startSearchRoutine)
    {
        agent.enabled = enable;
        smartAgentNavigator.enabled = enable;
        if (startSearchRoutine)
        {
            smartAgentNavigator.StartSearchRoutine(50);
        }
        else
        {
            smartAgentNavigator.StopSearchRoutine();
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (!IsServer)
            return;

        if (!StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(this.transform.position))
            return;

        if (Plugin.Mod.ItemRegistry().TryGetFromItemName("Fake Item SnailCat", out CRItemDefinition? fakeSnailCatItemDefinition))
        {
            NetworkObjectReference netObjRef = CodeRebirthUtils.Instance.SpawnScrap(fakeSnailCatItemDefinition.Item, this.transform.position, false, true, 0);
            FakeSnailCat fakeSnailCat = ((NetworkObject)netObjRef).GetComponent<FakeSnailCat>();
            fakeSnailCat.localScale = propScript.originalScale;
            fakeSnailCat.snailCatName = currentName;
            fakeSnailCat.shiftHash = _specialRenderer!.materials[0].GetFloat(ShiftHash);
        }
        // CRUtilities.CreateExplosion(this.transform.position, true, 99999, 0, 15, 999, null, null, 1000f);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SyncNewSnailCatServerRpc(Vector3 scale, string name, float magicalHashNumber)
    {
        SyncNewSnailCatClientRpc(scale, name, magicalHashNumber);
    }

    [ClientRpc]
    private void SyncNewSnailCatClientRpc(Vector3 scale, string name, float magicalHashNumber)
    {
        StartCoroutine(InitaliseRealSnailCat(scale, name, magicalHashNumber));
    }

    private IEnumerator InitaliseRealSnailCat(Vector3 scale, string name, float magicalHashNumber)
    {
        yield return new WaitUntil(() => detectLightInSurroundings != null);
        this.transform.localScale = scale;
        propScript.originalScale = scale;
        currentName = name;
        scanNodeProperties.headerText = currentName;
        isWiWiWiii = currentName == "Wiwiwii";
        _specialRenderer!.materials[0].SetFloat(ShiftHash, magicalHashNumber);
    }
}