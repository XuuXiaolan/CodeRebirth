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
    public AudioClip AttackSound = null!;
    public AudioClip BigWalkingSound = null!;
    public AudioClip IdleDanceSound = null!;
    public AudioClip PushUpSound = null!;
    public AudioClip AdmireSelfSound = null!;
    public AudioClip JumpingJacksSound = null!;
    public AudioClip ArmCurlSound = null!;
    public AudioClip Death1Sound = null!;
    public AudioClip Death2Sound = null!;
    public AudioClip BegForApparatusSound = null!;
    public List<AudioClip> RandomDialogueSounds = [];
    public List<AudioClip> DamageSounds = [];
    public AnimationClip SmashDoorAnimation = null!;
    public AnimationClip MeleeAnimation = null!;
    public GameObject LungPropPrefab = null!;
    public List<Collider> RegularColliders = null!;
    public List<Collider> DeathColliders = null!;

    [NonSerialized] public bool meleeAttack = false;
    private LungProp? targetLungProp;
    private bool holdingLungProp = false;
    private List<DoorLock> doorLocks = [];
    private readonly float idleCooldownTimer = 15f;
    private float randomSoundsTimer = 10f;
    private float idleTimer = 5f;
    private float timeSinceSpinAttack = 3f;
    private float begTimer = 10f;
    private const float WALKING_SPEED = 1.5f;
    private const float SPRINTING_SPEED = 2f;
    private Coroutine? delayRoutine = null;
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
        Death1 = 6,
        Death2 = 7
    }

    public override void Start() 
    {
        base.Start();
        foreach (var collider in DeathColliders)
        {
            collider.enabled = false;
        }
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
        randomSoundsTimer -= Time.deltaTime;
        timeSinceSpinAttack += Time.deltaTime;
        begTimer -= Time.deltaTime;

        if (randomSoundsTimer <= 0 && doorLockRoutine == null && grabLungPropRoutine == null && currentBehaviourStateIndex == (int)State.Walking)
        {
            PlayRandomSoundsServerRpc();
            randomSoundsTimer = 10f;
        }

        if (doorLockRoutine == null && !holdingLungProp && grabLungPropRoutine == null)
        {
            foreach (DoorLock doorLock in doorLocks)
            {
                if (doorLock == null || doorLock.isDoorOpened) continue;
                if (Vector3.Distance(doorLock.transform.position, transform.position) <= 3f)
                {
                    doorLockRoutine = StartCoroutine(DoOpenDoor(doorLock));
                }
            }
        }

        if (idleTimer > 0 && doorLockRoutine == null && (currentBehaviourStateIndex == (int)State.Walking || currentBehaviourStateIndex == (int)State.SearchingForApparatus) && !creatureAnimator.GetBool("GrabbingAppy"))
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
            SetTargetApparatusStatusServerRpc(false);
            grabLungPropRoutine = StartCoroutine(GrabLungAnimation());
        }
        if (Vector3.Distance(targetLungProp.transform.position, transform.position) > 3f && grabLungPropRoutine != null)
        {
            StopCoroutine(grabLungPropRoutine);
            grabLungPropRoutine = null;
            SetTargetApparatusStatusServerRpc(true);
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
        LungPropPrefab.SetActive(true);
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
        if (doorLock.isLocked) doorLock.UnlockDoorServerRpc();
        doorLock.OpenDoorAsEnemyServerRpc();
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
                delayRoutine = StartCoroutine(DelayGoingBackToPreviousState(IdleDanceSound.length, previousSpeed));
                PlaySpecificSoundServerRpc((int)RandomActions.IdleDance);
                break;
            case RandomActions.AdmireSelf:
                creatureAnimator.SetBool("AdmiringSelf", true);
                delayRoutine = StartCoroutine(DelayGoingBackToPreviousState(AdmireSelfSound.length, previousSpeed));
                PlaySpecificSoundServerRpc((int)RandomActions.AdmireSelf);
                break;
            case RandomActions.JumpingJacks:
                networkAnimator.SetTrigger("DoJumpJacks");
                delayRoutine = StartCoroutine(DelayGoingBackToPreviousState(JumpingJacksSound.length, previousSpeed));
                PlaySpecificSoundServerRpc((int)RandomActions.JumpingJacks);
                break;
            case RandomActions.PushUps:
                networkAnimator.SetTrigger("DoPushUp");
                delayRoutine = StartCoroutine(DelayGoingBackToPreviousState(PushUpSound.length, previousSpeed));
                PlaySpecificSoundServerRpc((int)RandomActions.PushUps);
                break;
            case RandomActions.ArmCurl:
                networkAnimator.SetTrigger("DoArmCurl");
                delayRoutine = StartCoroutine(DelayGoingBackToPreviousState(ArmCurlSound.length, previousSpeed));
                PlaySpecificSoundServerRpc((int)RandomActions.ArmCurl);
                break;
            case RandomActions.WalkDialogue:
                PlaySpecificSoundServerRpc((int)RandomActions.WalkDialogue);
                SwitchToBehaviourServerRpc(previousBehaviourStateIndex);
                break;
        }
    }

    [ServerRpc]
    private void SetTargetApparatusStatusServerRpc(bool value)
    {
        SetTargetApparatusStatusClientRpc(value);
    }

    [ClientRpc]
    private void SetTargetApparatusStatusClientRpc(bool value)
    {
        if (targetLungProp == null) return;
        targetLungProp.grabbable = value;
        targetLungProp.grabbableToEnemies = value;
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
            case 6:
                creatureVoice.PlayOneShot(Death1Sound);
                break;
            case 7:
                creatureVoice.PlayOneShot(Death2Sound);
                break;
            case 8:
                creatureVoice.PlayOneShot(AttackSound);
                meleeAttack = true;
                StartCoroutine(ResetMeleeAttack());
                break;
            case 9:
                creatureVoice.PlayOneShot(BegForApparatusSound);
                break;
        }
    }

    private IEnumerator ResetMeleeAttack()
    {
        yield return new WaitForSeconds(MeleeAnimation.length);
        meleeAttack = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayRandomSoundsServerRpc()
    {
        PlayRandomSoundsClientRpc();
    }

    [ClientRpc]
    private void PlayRandomSoundsClientRpc()
    {
        creatureVoice.PlayOneShot(RandomDialogueSounds[JPRandom.NextInt(0, RandomDialogueSounds.Count - 1)]);
    }

    private IEnumerator DelayGoingBackToPreviousState(float delay, float speed) // todo: if a player is close and they're holding the apparatus, say "please" soundclip
    {
        yield return new WaitForSeconds(delay);
        agent.speed = speed;
        creatureAnimator.SetBool("AdmiringSelf", false);
        if (holdingLungProp) HideOrUnhideHeldLungPropServerRpc(false);
        SwitchToBehaviourServerRpc(previousBehaviourStateIndex);
        delayRoutine = null;
    }

    [ServerRpc(RequireOwnership = false)]
    private void HideOrUnhideHeldLungPropServerRpc(bool hide)
    {
        HideOrUnhideHeldLungPropClientRpc(hide);
    }

    [ClientRpc]
    private void HideOrUnhideHeldLungPropClientRpc(bool hide)
    {
        LungPropPrefab.SetActive(!hide);
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead) return;
        if (currentBehaviourStateIndex != (int)State.IdleAction) creatureAnimator.SetFloat("RunSpeed", agent.velocity.magnitude/2);
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
                if (player == null || player.isInHangarShipRoom || player.isPlayerDead || !player.isPlayerControlled) continue;
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
            if (targetLungProp.isHeld)
            {
                foreach (var player in StartOfRound.Instance.allPlayerScripts)
                {
                    if (player == null || player.isInHangarShipRoom || player.isPlayerDead || !player.isPlayerControlled) continue;
                    if (Vector3.Distance(transform.position, player.transform.position) <= 10)
                    {
                        if (player.currentlyHeldObjectServer != null && player.currentlyHeldObjectServer == targetLungProp)
                        {
                            if (begTimer <= 0)
                            {
                                begTimer = 3f;
                                PlaySpecificSoundServerRpc(9);
                            }
                        }                        
                    }
                } 
            }
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
                PlaySpecificSoundServerRpc(8);
            }
            if (Vector3.Distance(transform.position, targetPlayer.transform.position) > 30f)
            {
                StartSearch(this.transform.position);
                SwitchToBehaviourServerRpc((int)State.Walking);
            }
        }
    }

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead) return;
        creatureVoice.PlayOneShot(DamageSounds[JPRandom.NextInt(0, DamageSounds.Count - 1)]);
        if (grabLungPropRoutine == null)
        {
            enemyHP -= force;
            if (delayRoutine == null) agent.speed += 0.5f;
            // todo: add the number to a list which adds after the delayroutine ends.
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
        foreach (var collider in RegularColliders)
        {
            collider.enabled = false;
        }
        foreach (var collider in DeathColliders)
        {
            collider.enabled = true;
        }
        SwitchToBehaviourStateOnLocalClient((int)State.Death);
        agent.speed = 0f;
        if (IsServer)
        {
            creatureAnimator.SetBool("isDead", true);
            creatureAnimator.SetBool("GrabbingAppy", false);
            creatureAnimator.SetBool("AdmiringSelf", false);
            creatureAnimator.SetFloat("HasAppy", 0);
            int randomDeathNumber = JPRandom.NextInt(1,2);
            string deathNumber = randomDeathNumber.ToString();
            Plugin.Logger.LogInfo("DoDeath" + deathNumber);
            networkAnimator.SetTrigger("DoDeath" + deathNumber);

            if (randomDeathNumber == 1)
            {
                PlaySpecificSoundServerRpc((int)RandomActions.Death1);
            }
            else
            {
                PlaySpecificSoundServerRpc((int)RandomActions.Death2);
            }

            if (targetLungProp == null) 
            {
                NetworkObjectReference lungPropRef = CodeRebirthUtils.Instance.SpawnScrap(StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == "Apparatus").First(), RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(this.transform.position, 4, default, JPRandom), false, true, 200);
            }
        }
        if (holdingLungProp)
        {
            holdingLungProp = false;
            LungPropPrefab.SetActive(false);
            // spawn a lung prop and set its value to 200.
        }
        if (targetLungProp != null)
        {
            SetTargetApparatusStatusServerRpc(true);
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