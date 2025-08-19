using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using CodeRebirthLib;
using CodeRebirthLib.Utils;


using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class CompactorToby : NetworkBehaviour, IHittable
{
    [Header("Sounds")]
    [SerializeField]
    private AudioSource _tobySource = null!;
    [SerializeField]
    private AudioClip _buttonPressSound = null!;
    [SerializeField]
    private AudioClip _buttonErrorSound = null!;
    [SerializeField]
    private AudioClip _compactorWhackedSound = null!;
    [SerializeField]
    private AudioSource _tobyMalfunctionSource = null!;
    [SerializeField]
    private AudioClip _compactorCloseGateSound = null!;
    [SerializeField]
    private AudioClip _compactorEndSound = null!;

    [Header("Animations")]
    [SerializeField]
    private Animator _tobyAnimator = null!;
    [SerializeField]
    private NetworkAnimator _tobyNetworkAnimator = null!;

    private List<CompactorEnemyLevelSpawner> _compactorEnemyLevelSpawners = new();

    [HideInInspector] public bool compacting = false;
    private bool _usedOnce = false;
    private static readonly int HitAnimation = Animator.StringToHash("hit"); // Trigger
    private static readonly int StartCompactAnimation = Animator.StringToHash("startCompact"); // Trigger
    private static readonly int FastEndCompactAnimation = Animator.StringToHash("fastEnd"); // Trigger
    private static readonly int SlowEndCompactAnimation = Animator.StringToHash("slowEnd"); // Trigger

    public void Start()
    {
        foreach (var enemyLevelSpawner in EnemyLevelSpawner.enemyLevelSpawners)
        {
            if (enemyLevelSpawner is CompactorEnemyLevelSpawner compactorSpawner)
            {
                _compactorEnemyLevelSpawners.Add(compactorSpawner);
            }
        }
    }

    public void CompactorInteract(PlayerControllerB player)
    {
        if (compacting)
            return;

        if (!player.IsLocalPlayer())
            return;

        int valueOfItems = 0;
        bool isFast = true;
        List<GrabbableObject> grabbableObjects = new();
        List<Vector3> vectorPositions = new();
        foreach (var grabbableObject in transform.GetComponentsInChildren<GrabbableObject>())
        {
            if (grabbableObject.isHeld)
                continue;

            valueOfItems += grabbableObject.scrapValue;
            grabbableObjects.Add(grabbableObject);
            vectorPositions.Add(grabbableObject.transform.position);
            if (grabbableObject.itemProperties.itemName.Contains("Shredded Scraps"))
            {
                continue;
            }
            isFast = false;
        }

        if (valueOfItems == 0)
        {
            ErrorSoundServerRpc();
            return;
        }

        foreach (GrabbableObject grabbableObject in grabbableObjects)
        {
            DespawnItemServerRpc(new NetworkBehaviourReference(grabbableObject));
        }
        Vector3 randomPosition = vectorPositions[UnityEngine.Random.Range(0, vectorPositions.Count)];
        TryCompactItemServerRpc(randomPosition, valueOfItems, null, isFast);
    }

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (compacting)
            return false;

        TriggerAnimationServerRpc(HitAnimation);
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void ErrorSoundServerRpc()
    {
        ErrorSoundClientRpc();
    }

    [ClientRpc]
    private void ErrorSoundClientRpc()
    {
        _tobySource.PlayOneShot(_buttonErrorSound);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TryCompactItemServerRpc(Vector3 randomPosition, int value, PlayerControllerReference? deadPlayer, bool fast)
    {
        StartOrStopCompactingClientRpc(true, !fast);
        StartCoroutine(CompactProcess(randomPosition, value, deadPlayer, fast));
    }

    private IEnumerator CompactProcess(Vector3 randomPosition, int value, PlayerControllerReference? deadPlayer, bool fast)
    {
        Plugin.ExtendedLogging($"Value: {value} | Dead Player: {deadPlayer} | Fast: {fast}");
        _tobyNetworkAnimator.SetTrigger(StartCompactAnimation);
        yield return new WaitForSeconds(21);
        float timeElapsed = 0f;
        float timeToWait = fast ? 5f : 30f;
        if (fast)
        {
            _tobyNetworkAnimator.SetTrigger(FastEndCompactAnimation);
        }
        else
        {
            _tobyNetworkAnimator.SetTrigger(SlowEndCompactAnimation);
        }

        float Timethreshold = 2.4f;
        while (timeElapsed < timeToWait)
        {
            yield return null;
            timeElapsed += Time.deltaTime;
            Timethreshold -= Time.deltaTime;
            if (Timethreshold <= 0f)
            {
                Timethreshold = 2.4f;
                Plugin.ExtendedLogging("Toby Spawning Enemy");
                int randomIndex = UnityEngine.Random.Range(0, _compactorEnemyLevelSpawners.Count);
                CompactorEnemyLevelSpawner enemyLevelSpawner = _compactorEnemyLevelSpawners[randomIndex];
                EnemyAI? enemyAI = null;
                for (int i = 0; i < 5; i++)
                {
                    enemyAI = enemyLevelSpawner.SpawnRandomEnemy();
                    if (enemyAI != null)
                    {
                        break;
                    }
                }

                if (enemyAI != null && enemyAI.agent != null && enemyAI.agent.enabled)
                {
                    var component = enemyAI.gameObject.AddComponent<ForceEnemyToDestination>();
                    component.enemy = enemyAI;
                    component.Destination = this.transform.position;
                }
            }

            if (timeElapsed >= 11.1f && _tobyMalfunctionSource.isPlaying)
            {
                StopTobySourceClientRpc();
            }
        }
        StartOrStopCompactingClientRpc(false, false);
        if (deadPlayer != null)
        {
            NetworkObjectReference flattenedBodyNetObjRef = CodeRebirthUtils.Instance.SpawnScrap(LethalContent.Items[CodeRebirthItemKeys.FlattenedBody].Item, randomPosition, false, true, value);
            if (flattenedBodyNetObjRef.TryGet(out NetworkObject flattenedBodyNetObj))
            {
                PlayerControllerB player = deadPlayer;
                flattenedBodyNetObj.GetComponent<FlattenedBody>()._flattenedBodyName = player;
            }
            yield break;
        }

        CodeRebirthUtils.Instance.SpawnScrap(LethalContent.Items[CodeRebirthItemKeys.SallyCube].Item, randomPosition, false, true, value);
    }

    private IEnumerator PlaySourceWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        _tobyMalfunctionSource.Play();
    }

    [ClientRpc]
    private void StopTobySourceClientRpc()
    {
        _tobyMalfunctionSource.Stop();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerAnimationServerRpc(int hash)
    {
        _tobyNetworkAnimator.SetTrigger(hash);
    }

    [ClientRpc]
    public void StartOrStopCompactingClientRpc(bool starting, bool malfunctioning)
    {
        compacting = starting;

        if (starting)
        {
            _tobySource.PlayOneShot(_buttonPressSound);
            if (malfunctioning)
                StartCoroutine(PlaySourceWithDelay(21));
        }
        else
        {
            if (!_usedOnce)
            {
                foreach (var enemyLevelSpawner in EnemyLevelSpawner.enemyLevelSpawners)
                {
                    enemyLevelSpawner.spawnTimerMin /= 2f;
                    enemyLevelSpawner.spawnTimerMax /= 2f;
                }
                OxydeLightsManager.oxydeLightsManager.IncrementLights();
                _usedOnce = true;
                HUDManager.Instance.DisplayTip("Warning!", "Site machinery activated, anomaly levels rising.", true);
            }
            _tobyMalfunctionSource.Stop();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DespawnItemServerRpc(NetworkBehaviourReference networkBehaviourReference)
    {
        if (networkBehaviourReference.TryGet(out GrabbableObject grabbableObject))
        {
            grabbableObject.NetworkObject.Despawn();
        }
    }

    public void PlayMiscSoundsAnimEvent(int SoundID)
    {
        switch (SoundID)
        {
            case 0:
                _tobySource.PlayOneShot(_compactorWhackedSound);
                break;
            case 1:
                _tobySource.PlayOneShot(_compactorCloseGateSound);
                break;
            case 2:
                _tobySource.PlayOneShot(_compactorEndSound);
                break;
        }
    }
}