using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.Util;
using Dawn;
using Dawn.Utils;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;

namespace CodeRebirth.src.Content.Maps;

public class AutonomousCrane : NetworkBehaviour
{
    [SerializeField]
    private Collider[] _colliders = [];

    [Header("Audio")]
    [SerializeField]
    private AudioSource _audioSource = null!;
    [SerializeField]
    private AudioClip _craneStopMovingSound = null!;
    [SerializeField]
    private AudioClip _craneTurningSound = null!;
    [SerializeField]
    private AudioClip _magnetDroppingSound = null!;
    [SerializeField]
    private AudioClip _magnetImpactSound = null!;
    [SerializeField]
    private AudioClip _magnetReelingUpSound = null!;

    [Header("Events")]
    [SerializeField]
    private UnityEvent _onMagnetHitGround = new();
    [SerializeField]
    private UnityEvent _onActivateCrane = new();
    [SerializeField]
    private UnityEvent _onDeactivateCrane = new();
    [Header("Crane Parts")]
    [SerializeField]
    private GameObject _properCraneArmEndGO = null!;
    [SerializeField]
    private GameObject _cabinHead = null!; // this can turn in a 360 degree circle
    [SerializeField]
    private GameObject _craneArmStart = null!;
    [SerializeField]
    private GameObject _craneArmEnd = null!;
    [SerializeField]
    private GameObject _magnet = null!; // this can move up and down, but not rotate, it should always be facing downwards, and be straight under the arm, it can't get closer than 5 units of the arm's downward raycast

    [Header("Disabling Interacts")]
    [SerializeField]
    private Animator _leverAnimator = null!;
    [SerializeField]
    private InteractTrigger _disableInteract = null!;
    [SerializeField]
    private float _rotationSpeed = 12.5f;

    internal List<PlayerControllerB> _targetablePlayers = new();
    internal PlayerControllerB? _targetPlayer = null;

    private bool _craneIsActive = true;
    private Vector3 _targetPosition = Vector3.zero;
    private CraneState _currentState = CraneState.Idle;
    private BoundedRange _deactivationLengthRange = new(15f, 25f);
    private System.Random _craneRandomiser;
    private Coroutine? _reenableCraneRoutine = null;
    private Collider[] _cachedColliders = new Collider[32];

    public enum CraneState
    {
        Idle,
        GetTargetPosition,
        MoveCraneHead,
        MoveCraneArm,
        DropMagnet
    }

    private static readonly int PullLeverAnimation = Animator.StringToHash("pullLever"); // Trigger
    private static readonly int UnpullLeverAnimation = Animator.StringToHash("unpullLever"); // Trigger
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _craneRandomiser = new System.Random(StartOfRound.Instance.randomMapSeed + 33333);
        _deactivationLengthRange = MapObjectHandler.Instance.AutonomousCrane!.GetConfig<BoundedRange>("Autonomous Crane | Deactivation Length").Value;
        float distanceToShip = Vector3.Distance(this.transform.position, StartOfRound.Instance.shipLandingPosition.position);
        Plugin.ExtendedLogging($"Distance to ship: {distanceToShip}");
        if (distanceToShip <= 30f)
        {
            if (IsServer)
            {
                _craneIsActive = false;
                StartCoroutine(DiscardAfterABit());
            }
            return;
        }
        else
        {
            StartCoroutine(LandingCondition());
        }
        _disableInteract.onInteract.AddListener(EditCraneStateTrigger);
    }

    private IEnumerator LandingCondition()
    {
        foreach (Collider collider in _colliders)
        {
            collider.enabled = false;
        }

        yield return new WaitUntil(() => StartOfRound.Instance.shipHasLanded || !GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom);

        foreach (Collider collider in _colliders)
        {
            collider.enabled = true;
        }
    }

    private IEnumerator DiscardAfterABit()
    {
        yield return new WaitForSeconds(0.1f);
        NetworkObject.Despawn(true);
    }

    private void EditCraneStateTrigger(PlayerControllerB player)
    {
        if (!player.IsLocalPlayer())
            return;

        if (!_craneIsActive)
        {
            Plugin.ExtendedLogging($"Activating crane");
            EnableCraneServerRpc();
        }
        else
        {
            Plugin.ExtendedLogging($"Deactivating crane");
            DisableCraneServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DisableCraneServerRpc()
    {
        DisableCraneClientRpc();
    }

    [ClientRpc]
    private void DisableCraneClientRpc()
    {
        _reenableCraneRoutine = StartCoroutine(ReEnableCraneAfterDelay());
    }

    public void Update()
    {
        if (StartOfRound.Instance.shipIsLeaving && GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom && _colliders[0].enabled)
        {
            foreach (Collider collider in _colliders)
            {
                collider.enabled = false;
            }
        }

        if (!IsServer)
            return;

        if (!_craneIsActive)
        {
            _currentState = CraneState.DropMagnet;
            if (_magnetState == MagnetState.IdleBottom)
            {
                return;
            }

            DoDroppingMagnetBehaviour();
            return;
        }

        switch (_currentState)
        {
            case CraneState.Idle:
                DoIdleBehaviour();
                break;
            case CraneState.GetTargetPosition:
                DoGetTargetPositionBehaviour();
                break;
            case CraneState.MoveCraneHead:
                DoMovingCraneHeadBehaviour();
                break;
            case CraneState.MoveCraneArm:
                DoMovingCraneArmBehaviour();
                break;
            case CraneState.DropMagnet:
                DoDroppingMagnetBehaviour();
                break;
        }
    }

    private void DoIdleBehaviour()
    {
        if (_targetablePlayers.Count <= 0)
            return;

        foreach (var player in _targetablePlayers)
        {
            Plugin.ExtendedLogging($"AutonomousCrane: Player {player} is in range, switching to GetTargetPosition state.");
            _targetPosition = Vector3.zero;
            _targetablePlayers.Remove(player);
            _targetPlayer = player;
            _currentState = CraneState.GetTargetPosition;
            break;
        }
    }

    private void DoGetTargetPositionBehaviour()
    {
        if (_targetPlayer == null)
        {
            DisableAudioSourceServerRpc(true);
            _currentState = CraneState.Idle;
            return;
        }

        GetTargetPosition(_targetPlayer);
        ChangeAudioSourceClipServerRpc(0);
        _currentState = CraneState.MoveCraneHead;
    }

    private void DoMovingCraneHeadBehaviour()
    {
        if (_targetPlayer == null)
        {
            DisableAudioSourceServerRpc(true);
            _currentState = CraneState.Idle;
            return;
        }

        GetTargetPosition(_targetPlayer);

        if (!RotateCraneHead())
        {
            return;
        }

        _currentState = CraneState.MoveCraneArm;
    }

    private void DoMovingCraneArmBehaviour()
    {
        if (_targetPlayer == null)
        {
            DisableAudioSourceServerRpc(true);
            _currentState = CraneState.Idle;
            return;
        }

        Vector3 newTargetPosition = _targetPosition;
        newTargetPosition.y = _cabinHead.transform.position.y;
        float dot = Vector3.Dot(_cabinHead.transform.forward, (newTargetPosition - _cabinHead.transform.position).normalized);
        if (dot < 0.98f)
        {
            _currentState = CraneState.MoveCraneHead;
            return;
        }

        GetTargetPosition(_targetPlayer);
        if (!MoveCraneArm())
        {
            return;
        }

        DisableAudioSourceServerRpc(false);
        ChangeAudioSourceClipServerRpc(1);
        _targetPlayer = null;
        _currentState = CraneState.DropMagnet;
    }

    private void DoDroppingMagnetBehaviour() // just drop pretty fast and align self with terrain
    {
        MoveMagnet();
    }

    private bool RotateCraneHead()
    {
        Vector3 directionToTarget = _targetPosition - _cabinHead.transform.position;
        directionToTarget.y = 0f;

        Quaternion targetRot = Quaternion.LookRotation(directionToTarget, Vector3.up);
        _cabinHead.transform.rotation = Quaternion.RotateTowards(_cabinHead.transform.rotation, targetRot, (_rotationSpeed * Time.deltaTime) / this.transform.localScale.x);
        float angleDelta = Quaternion.Angle(_cabinHead.transform.rotation, targetRot);

        return angleDelta < 0.1f;
    }

    private bool MoveCraneArm()
    {
        Vector3 localTarget = _craneArmStart.transform.parent.InverseTransformPoint(_targetPosition);

        float horiz = new Vector2(localTarget.x, localTarget.z).magnitude;
        float armLen = Vector3.Distance(_craneArmStart.transform.position, _craneArmEnd.transform.position);

        float rawAngle = Mathf.Asin(Mathf.Clamp01(horiz / armLen)) * Mathf.Rad2Deg;
        if (rawAngle < 10f || rawAngle > 90f)
            return false;

        float pitch = Mathf.Clamp(rawAngle, 20f, 80f);

        Quaternion want = Quaternion.Euler(pitch, _craneArmStart.transform.localRotation.eulerAngles.y, _craneArmStart.transform.localRotation.eulerAngles.z);
        _craneArmStart.transform.localRotation = Quaternion.RotateTowards(_craneArmStart.transform.localRotation, want, (_rotationSpeed * Time.deltaTime) / this.transform.localScale.x);
        float remaining = Quaternion.Angle(_craneArmStart.transform.localRotation, want);

        _craneArmEnd.transform.position = _properCraneArmEndGO.transform.position;
        return remaining < 0.1f;
    }

    private Vector3 _magnetTargetPosition = Vector3.zero;
    private Vector3 _originalMagnetPosition = Vector3.zero;
    private float _magnetMovingProgress = 0f;
    private enum MagnetState
    {
        IdleTop,
        MovingDown,
        IdleBottom,
        MovingUp,
    }
    private MagnetState _magnetState = MagnetState.IdleTop;

    private void MoveMagnet()
    {
        if (_magnetState == MagnetState.IdleTop && Physics.Raycast(_magnet.transform.position, Vector3.down, out RaycastHit hit, 100f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
        {
            _originalMagnetPosition = _magnet.transform.position;
            _magnetTargetPosition = hit.point;
            _magnetMovingProgress = 0f;
            _magnetState = MagnetState.MovingDown;
            Plugin.ExtendedLogging($"Moving magnet to {hit.point}");
        }

        if (_magnetState == MagnetState.MovingDown)
        {
            _magnetMovingProgress += Time.deltaTime * 2.5f;
            _magnet.transform.position = Vector3.Lerp(_originalMagnetPosition, _magnetTargetPosition, _magnetMovingProgress);
            if (_magnetMovingProgress >= 1f)
            {
                _magnetMovingProgress = 1f;
                _magnetState = MagnetState.IdleBottom;
                CraneHitBottomServerRpc(_magnetTargetPosition);
            }
        }
        else if (_magnetState == MagnetState.IdleBottom)
        {
            _magnetMovingProgress -= Time.deltaTime * 0.25f;
            if (_magnetMovingProgress <= 0)
            {
                _magnetMovingProgress = 0f;
                ChangeAudioSourceClipServerRpc(2);
                _magnetState = MagnetState.MovingUp;
            }
        }
        else if (_magnetState == MagnetState.MovingUp)
        {
            _magnetMovingProgress += Time.deltaTime * 0.5f;
            if (_magnetMovingProgress >= 1f)
            {
                _magnetMovingProgress = 1f;
                DisableAudioSourceServerRpc(false);
                _magnetState = MagnetState.IdleTop;
                _currentState = CraneState.Idle;
            }
            _magnet.transform.position = Vector3.Lerp(_magnetTargetPosition, _originalMagnetPosition, _magnetMovingProgress);
        }
    }

    private void GetTargetPosition(PlayerControllerB targetPlayer)
    {
        _targetPosition = targetPlayer.transform.position;
    }

    [ServerRpc(RequireOwnership = false)]
    private void CraneHitBottomServerRpc(Vector3 magnetTargetPosition)
    {
        _playerKillList.Clear();
        _enemyKillList.Clear();
        int numHits = Physics.OverlapSphereNonAlloc(magnetTargetPosition, 5f * this.transform.localScale.x, _cachedColliders, MoreLayerMasks.PlayersAndInteractableAndEnemiesAndPropsHazardMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < numHits; i++)
        {
            Collider collider = _cachedColliders[i];
            Plugin.ExtendedLogging($"Collider: {collider.name}");
            if (!collider.TryGetComponent(out IHittable hittable))
                continue;

            if (hittable is PlayerControllerB player)
            {
                if (player.isPlayerDead || !player.isPlayerControlled || player.IsPseudoDead() || _playerKillList.Contains(player))
                    continue;

                _playerKillList.Add(player);
            }
            else if (hittable is EnemyAICollisionDetect enemy)
            {
                if (_enemyKillList.Contains(enemy.mainScript))
                    continue;

                if (Physics.Linecast(magnetTargetPosition, enemy.mainScript.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                    continue;

                _enemyKillList.Add(enemy.mainScript);
            }
            else
            {
                hittable.Hit(99, magnetTargetPosition, null, true, -1);
            }
        }


        foreach (PlayerControllerB player in _playerKillList)
        {
            HandlePlayerDeathClientRpc(magnetTargetPosition, player);
        }
        foreach (EnemyAI enemy in _enemyKillList)
        {
            CodeRebirthUtils.Instance.KillEnemyOnOwnerClientRpc(new NetworkBehaviourReference(enemy), false);
        }
        CraneHitBottomResultsClientRpc(magnetTargetPosition);
    }

    [ClientRpc]
    private void HandlePlayerDeathClientRpc(Vector3 magnetTargetPosition, PlayerControllerReference playerToDie)
    {
        PlayerControllerB player = playerToDie;
        if (Physics.Linecast(magnetTargetPosition, player.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            return;

        player.KillPlayer(player.velocityLastFrame, true, CauseOfDeath.Crushing, 0, default);

        if (!player.IsLocalPlayer())
        {
            return;
        }

        SpawnScrapOnServerRpc(playerToDie);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnScrapOnServerRpc(PlayerControllerReference playerToDie)
    {
        PlayerControllerB player = playerToDie;
        NetworkObjectReference flattenedBodyNetObjRef = CodeRebirthUtils.Instance.SpawnScrap(LethalContent.Items[CodeRebirthItemKeys.FlattenedBody].Item, player.transform.position, false, true, 0);
        if (flattenedBodyNetObjRef.TryGet(out NetworkObject flattenedBodyNetObj))
        {
            flattenedBodyNetObj.GetComponent<FlattenedBody>()._flattenedBodyName = player;
        }
    }

    private List<PlayerControllerB> _playerKillList = new();
    private List<EnemyAI> _enemyKillList = new();

    [ClientRpc]
    private void CraneHitBottomResultsClientRpc(Vector3 magnetTargetPosition)
    {
        float distanceToLocalPlayer = Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, magnetTargetPosition);
        if (distanceToLocalPlayer >= 40 && distanceToLocalPlayer < 60)
        {
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
        }
        else if (distanceToLocalPlayer >= 20 && distanceToLocalPlayer < 40)
        {
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
        }
        else if (distanceToLocalPlayer > 10 && distanceToLocalPlayer < 20)
        {
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
        }
        else if (distanceToLocalPlayer <= 10)
        {
            HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
        }

        _audioSource.clip = null;
        _audioSource.Stop();
        _audioSource.PlayOneShot(_magnetImpactSound);
        _onMagnetHitGround.Invoke();
    }

    private IEnumerator ReEnableCraneAfterDelay()
    {
        DisableCrane();
        yield return new WaitForSeconds(_deactivationLengthRange.GetRandomInRange(_craneRandomiser));
        EnableCrane();
        _reenableCraneRoutine = null;
    }

    private void DisableCrane()
    {
        _leverAnimator.SetTrigger(PullLeverAnimation);
        _audioSource.Stop();
        _craneIsActive = false;
        _onDeactivateCrane.Invoke();
    }

    [ServerRpc(RequireOwnership = false)]
    private void EnableCraneServerRpc()
    {
        EnableCraneClientRpc();
    }

    [ClientRpc]
    private void EnableCraneClientRpc()
    {
        if (_reenableCraneRoutine != null)
        {
            StopCoroutine(_reenableCraneRoutine);
            _reenableCraneRoutine = null;
        }

        EnableCrane();
    }

    private void EnableCrane()
    {
        _craneIsActive = true;
        _onActivateCrane.Invoke();
        _leverAnimator.SetTrigger(UnpullLeverAnimation);
    }

    [ServerRpc]
    private void DisableAudioSourceServerRpc(bool stopping)
    {
        DisableAudioSourceClientRpc(stopping);
    }

    [ClientRpc]
    private void DisableAudioSourceClientRpc(bool stopping)
    {
        DisableAudioSource(stopping);
    }

    private void DisableAudioSource(bool stopping)
    {
        _audioSource.clip = null;
        _audioSource.Stop();
        if (stopping)
        {
            _audioSource.PlayOneShot(_craneStopMovingSound);
        }
    }

    [ServerRpc]
    private void ChangeAudioSourceClipServerRpc(int soundID)
    {
        ChangeAudioSourceClipClientRpc(soundID);
    }

    [ClientRpc]
    public void ChangeAudioSourceClipClientRpc(int soundID)
    {
        ChangeAudioSourceClip(soundID);
    }

    private void ChangeAudioSourceClip(int soundID)
    {
        DisableAudioSource(false);

        switch (soundID)
        {
            case 0:
                _audioSource.clip = _craneTurningSound;
                break;
            case 1:
                _audioSource.clip = _magnetDroppingSound;
                break;
            case 2:
                _audioSource.clip = _magnetReelingUpSound;
                break;
        }
        _audioSource.Play();
    }
}