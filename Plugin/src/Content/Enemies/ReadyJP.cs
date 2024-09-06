using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class ReadyJP : CodeRebirthEnemyAI
{

    public NetworkAnimator networkAnimator = null!;
    public AudioClip BigWalkingSound = null!;
    public AudioClip IdleDanceSound = null!;
    public AudioClip PushUpSound = null!;
    public AudioClip AdmireSelfSound = null!;
    public AudioClip JumpingJacksSound = null!;
    public AudioClip ArmCurlSound = null!;
    public List<AudioClip> DamageSounds = [];
    public AnimationClip SmashDoorAnimation = null!;
    public AnimationClip MeleeAnimation = null!;
    public GameObject lungPropPrefab = null!;

    private LungProp? targetLungProp;
    private bool holdingLungProp = false;
    private List<DoorLock> doorLocks = [];
    private readonly float idleCooldownTimer = 15f;
    private float idleTimer = 5f;
    private float timeSinceSpinAttack = 3f;
    private const float WALKING_SPEED = 2f;
    private const float SPRINTING_SPEED = 3f;
    private Coroutine? grabLungPropRoutine = null;
    private Coroutine? doorLockRoutine = null;

    private readonly List<RandomActions> randomActions = [
                                                            RandomActions.IdleDance,
                                                            RandomActions.AdmireSelf,
                                                            RandomActions.JumpingJacks,
                                                            RandomActions.PushUps,
                                                            RandomActions.ArmCurl,
                                                            RandomActions.WalkDialogue,
                                                         ];
    private System.Random JPRandom = new();
    public enum State {
        Spawn,
        IdleAction,
        Walking,
        SearchingForApparatus,
        AttackMode,
        Death,
    }

    public enum RandomActions
    {
        IdleDance = 0,
        AdmireSelf = 1,
        JumpingJacks = 2,
        PushUps = 3,
        ArmCurl = 4,
        WalkDialogue = 5,
    }

    public override void Start() 
    {
        base.Start();
        doorLocks = FindObjectsOfType<DoorLock>().ToList();
        JPRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 223);
        StartCoroutine(CheckState());
    }

    private IEnumerator CheckState()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);
            Plugin.Logger.LogInfo(currentBehaviourStateIndex.ToString());
        }
    }

    public override void Update()
    {
        base.Update();
        if (isEnemyDead) return;
        if (!IsServer) return;
        timeSinceSpinAttack += Time.deltaTime;
        if (doorLockRoutine == null)
        {
            foreach (DoorLock doorLock in doorLocks)
            {
                if (doorLock == null) continue;
                if (Vector3.Distance(doorLock.transform.position, transform.position) <= 3f)
                {
                    doorLockRoutine = StartCoroutine(DoOpenDoor(doorLock));
                }
            }
        }

        if (idleTimer > 0 && (currentBehaviourStateIndex == (int)State.Walking || currentBehaviourStateIndex == (int)State.SearchingForApparatus) && !creatureAnimator.GetBool("GrabbingAppy"))
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0)
            {
                idleTimer = idleCooldownTimer;
                previousBehaviourStateIndex = currentBehaviourStateIndex;
                SwitchToBehaviourServerRpc((int)State.IdleAction);
                PerformRandomIdleAction(randomActions[JPRandom.Next(0, randomActions.Count)]);
            }
        }

        if (targetLungProp == null || holdingLungProp) return;

        if (Vector3.Distance(targetLungProp.transform.position, transform.position) <= 1f && grabLungPropRoutine == null)
        {
            targetLungProp.grabbable = false;
            targetLungProp.grabbableToEnemies = false;
            grabLungPropRoutine = StartCoroutine(GrabLungAnimation());
        }
        if (Vector3.Distance(targetLungProp.transform.position, transform.position) > 3f && grabLungPropRoutine != null)
        {
            StopCoroutine(grabLungPropRoutine);
            grabLungPropRoutine = null;
            targetLungProp.grabbable = true;
            targetLungProp.grabbableToEnemies = true;
            holdingLungProp = false;
        }
    }

    private IEnumerator GrabLungAnimation()
    {
        creatureAnimator.SetBool("GrabbingAppy", true);
        agent.speed = 0f;
        yield return new WaitForSeconds(10f);
        holdingLungProp = true;
        if (IsHost && targetLungProp != null)
        {
            targetLungProp.GetComponent<NetworkObject>().Despawn();
        }
        // trigger apparatus stuff.
        StartSearch(transform.position);
        SwitchToBehaviourServerRpc((int)State.Walking);
        lungPropPrefab.SetActive(true);
        creatureAnimator.SetFloat("HasAppy", 1);
        targetLungProp = null;
        agent.speed = SPRINTING_SPEED;
        creatureAnimator.SetBool("GrabbingAppy", false);
        grabLungPropRoutine = null;
    }

    private IEnumerator DoOpenDoor(DoorLock doorLock)
    {
        networkAnimator.SetTrigger("DoDoorKick");
        yield return new WaitForSeconds(SmashDoorAnimation.length);
        doorLock.UnlockDoorServerRpc();
    }

    private void PerformRandomIdleAction(RandomActions randomAction)
    {
        Plugin.ExtendedLogging($"Performing random idle action: {randomAction}");
        float previousSpeed = agent.speed;
        if (randomAction != RandomActions.WalkDialogue) agent.speed = 0f;
        if (holdingLungProp && randomAction != RandomActions.WalkDialogue) HideOrUnhideHeldLungPropServerRpc(true);
        switch (randomAction)
        {
            case RandomActions.IdleDance:
                StartCoroutine(DelayGoingBackToPreviousState(IdleDanceSound.length, previousSpeed));
                PlaySpecificSoundServerRpc((int)RandomActions.IdleDance);
                break;
            case RandomActions.AdmireSelf:
                creatureAnimator.SetBool("AdmiringSelf", true);
                StartCoroutine(DelayGoingBackToPreviousState(AdmireSelfSound.length, previousSpeed));
                PlaySpecificSoundServerRpc((int)RandomActions.AdmireSelf);
                break;
            case RandomActions.JumpingJacks:
                networkAnimator.SetTrigger("DoJumpJacks");
                StartCoroutine(DelayGoingBackToPreviousState(JumpingJacksSound.length, previousSpeed));
                PlaySpecificSoundServerRpc((int)RandomActions.JumpingJacks);
                break;
            case RandomActions.PushUps:
                networkAnimator.SetTrigger("DoPushUp");
                StartCoroutine(DelayGoingBackToPreviousState(PushUpSound.length, previousSpeed));
                PlaySpecificSoundServerRpc((int)RandomActions.PushUps);
                break;
            case RandomActions.ArmCurl:
                networkAnimator.SetTrigger("DoArmCurl");
                StartCoroutine(DelayGoingBackToPreviousState(ArmCurlSound.length, previousSpeed));
                PlaySpecificSoundServerRpc((int)RandomActions.ArmCurl);
                break;
            case RandomActions.WalkDialogue:
                PlaySpecificSoundServerRpc((int)RandomActions.WalkDialogue);
                SwitchToBehaviourServerRpc(previousBehaviourStateIndex);
                break;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlaySpecificSoundServerRpc(int index)
    {
        PlaySpecificSoundClientRpc(index);
    }

    [ClientRpc]
    private void PlaySpecificSoundClientRpc(int index)
    {
        switch (index)
        {
            case 0:
                creatureVoice.PlayOneShot(IdleDanceSound);
                break;
            case 1:
                creatureVoice.PlayOneShot(AdmireSelfSound);
                break;
            case 2:
                creatureVoice.PlayOneShot(JumpingJacksSound);
                break;
            case 3:
                creatureVoice.PlayOneShot(PushUpSound);
                break;
            case 4:
                creatureVoice.PlayOneShot(ArmCurlSound);
                break;
            case 5:
                creatureVoice.PlayOneShot(BigWalkingSound);
                break;
        }
    }
    private IEnumerator DelayGoingBackToPreviousState(float delay, float speed)
    {
        yield return new WaitForSeconds(delay);
        agent.speed = speed;
        creatureAnimator.SetBool("AdmiringSelf", false);
        if (holdingLungProp) HideOrUnhideHeldLungPropServerRpc(false);
        SwitchToBehaviourServerRpc(previousBehaviourStateIndex);
    }

    [ServerRpc(RequireOwnership = false)]
    private void HideOrUnhideHeldLungPropServerRpc(bool hide)
    {
        HideOrUnhideHeldLungPropClientRpc(hide);
    }

    [ClientRpc]
    private void HideOrUnhideHeldLungPropClientRpc(bool hide)
    {
        lungPropPrefab.SetActive(!hide);
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead) return;
        if (currentBehaviourStateIndex != (int)State.IdleAction) creatureAnimator.SetFloat("RunSpeed", agent.velocity.sqrMagnitude);
        else creatureAnimator.SetFloat("RunSpeed", 0);
        
        switch (currentBehaviourStateIndex)
        {
            case (int)State.Spawn:
                break;
            case (int)State.IdleAction:
                break;
            case (int)State.Walking:
                DoWalking();
                break;
            case (int)State.SearchingForApparatus:
                DoSearchingForApparatus();
                break;
            case (int)State.AttackMode:
                DoAttackMode();
                break;
            case (int)State.Death:
                break;
        }
    }

    private void DoWalking()
    {
        if (holdingLungProp)
        {
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player.isInHangarShipRoom || player.isPlayerDead || !player.isPlayerControlled) continue;
                if (Vector3.Distance(transform.position, player.transform.position) <= 20)
                {
                    SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
                    StopSearch(currentSearch);
                    SwitchToBehaviourServerRpc((int)State.AttackMode);
                }
            }
        }
    }

    private void DoSearchingForApparatus()
    {
        if (targetLungProp != null)
        {
            SetDestinationToPosition(targetLungProp.transform.position);
        }
    }

    private void DoAttackMode()
    {
        if (targetPlayer != null)
        {
            SetDestinationToPosition(targetPlayer.transform.position);
        }

        if ((timeSinceSpinAttack >= (MeleeAnimation.length+3f)) && holdingLungProp && targetPlayer != null)
        {
            if (Vector3.Distance(transform.position, targetPlayer.transform.position) <= 3f)
            {
                timeSinceSpinAttack = 0;
                networkAnimator.SetTrigger("DoMelee");
            }
            if (Vector3.Distance(transform.position, targetPlayer.transform.position) > 30f)
            {
                StartSearch(this.transform.position);
                SwitchToBehaviourServerRpc((int)State.Walking);
            }
        }
    }


    public override void OnCollideWithPlayer(Collider other)
    {
        base.OnCollideWithPlayer(other);
    }

    public override void OnCollideWithEnemy(Collider other, EnemyAI? collidedEnemy = null)
    {
        base.OnCollideWithEnemy(other, collidedEnemy); // implement oncollidewithenemy and oncollidewithplayer in a separate script.
    }

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead) return;
        creatureVoice.PlayOneShot(DamageSounds[JPRandom.NextInt(0, DamageSounds.Count - 1)]);
        if (grabLungPropRoutine == null)
        {
            enemyHP -= force;
            agent.speed += 0.5f;
            Plugin.Logger.LogInfo($"EnemyHP: {enemyHP}");
        }
        if (enemyHP <= 0 && !isEnemyDead && IsOwner) {
            KillEnemyOnOwnerClient();
            Plugin.Logger.LogInfo("KillEnemyOnOwnerClient");
            // todo, trigger stuff that would start death animation.
        }
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);
        Plugin.Logger.LogInfo("KillEnemy");
        StopAllCoroutines();
        SwitchToBehaviourStateOnLocalClient((int)State.Death);
        agent.speed = 0f;
        if (IsServer)
        {
            creatureAnimator.SetBool("GrabbingAppy", false);
            creatureAnimator.SetBool("AdmiringSelf", false);
            creatureAnimator.SetFloat("HasAppy", 0);
            int randomDeathNumber = JPRandom.NextInt(1,2);
            string deathNumber = randomDeathNumber.ToString();
            Plugin.Logger.LogInfo("DoDeath" + deathNumber);
            networkAnimator.SetTrigger("DoDeath" + deathNumber);
            NetworkObjectReference lungPropRef = CodeRebirthUtils.Instance.SpawnScrap(StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == "Apparatus").First(), RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(this.transform.position, 4, default, JPRandom), false, true, 200);
        }
        if (holdingLungProp)
        {
            holdingLungProp = false;
            lungPropPrefab.SetActive(false);
            // spawn a lung prop and set its value to 200.
        }
        if (targetLungProp != null)
        {
            targetLungProp.grabbable = true;
            targetLungProp.grabbableToEnemies = true;
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void SetTargetLungPropServerRpc()
    {
        GameObject[] insideAINodes = GameObject.FindGameObjectsWithTag("AINode");
        NetworkObjectReference lungPropRef = CodeRebirthUtils.Instance.SpawnScrap(StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == "Apparatus").First(), RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(insideAINodes[JPRandom.NextInt(0, insideAINodes.Length - 1)].transform.position, 4, default, JPRandom), false, true, 0);
        GameObject lungPropGameObject = (GameObject)lungPropRef;
        SetTargetLungPropClientRpc(new NetworkObjectReference(lungPropGameObject));
    }

    [ClientRpc]
    private void SetTargetLungPropClientRpc(NetworkObjectReference targetLungPropRef)
    {
        GameObject lungPropGameObject = (GameObject)targetLungPropRef;
        targetLungProp = lungPropGameObject.GetComponent<LungProp>();
        targetLungProp.SetScrapValue(40);
        SetDestinationToPosition(targetLungProp.transform.position);
        if (targetLungProp != null) 
        {
            SwitchToBehaviourStateOnLocalClient((int)State.SearchingForApparatus);
        }
        else
        {
            Plugin.Logger.LogError("targetLungProp is null");
        }
    }

    public void WanderAroundForApparatusAnimEvent()
    {
        List<LungProp> lungProp = new();
        lungProp.AddRange(FindObjectsByType<LungProp>(FindObjectsSortMode.InstanceID));
        lungProp = lungProp.Where(x => x.isLungDocked && x.isInFactory).ToList();

        if (lungProp.Count > 0)
        {
            targetLungProp = lungProp[JPRandom.Next(0, lungProp.Count)];
        }
        if (targetLungProp == null && IsServer)
        {
            SetTargetLungPropServerRpc();
        }
        agent.speed = WALKING_SPEED;
    }
}