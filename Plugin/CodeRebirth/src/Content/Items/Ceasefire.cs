using System;
using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Util;
using CodeRebirthLib.Util;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class Ceasefire : GrabbableObject
{
    [Header("Visuals")]
    [SerializeField]
    private GameObject _ceasefireBarrel = null!;
    [SerializeField]
    private GameObject _particleSystemsGO = null!;
    [SerializeField]
    private ParticleSystem[] _particleSystems = [];
    [SerializeField]
    private float _rotationSpeed = 10f;
    [SerializeField]
    private Renderer _ceasefireRenderer = null!;

    [Header("Audio")]
    [SerializeField]
    private AudioSource _idleSource = null!;
    [SerializeField]
    private AudioClip _fireStartSound = null!;
    [SerializeField]
    private AudioClip _fireLoopSound = null!;
    [SerializeField]
    private AudioClip _fireEndSound = null!;

    [Header("Charge Effects")]
    [SerializeField]
    private float _maxChargedTime = 10f;
    [SerializeField]
    private float _minParticleRateOverTime = 10f;
    [SerializeField]
    private float _maxParticleRateOverTime = 45f;
    [SerializeField]
    private float _minEmissionIntensity = 1f;
    [SerializeField]
    private float _maxEmissionIntensity = 10f;
    [SerializeField]
    private float _maxDamageIntervalAtMaxCharge = 2f;

    private float _startingTime = 0f;
    private float _endingTime = 0f;
    private Coroutine? _firingStartRoutine = null;
    private Coroutine? _firingEndRoutine = null;
    private float _currentBarrelRotationX = 0f;
    private Material _ceasefireMaterial = null!;
    private Color _baseEmissionColor;
    private float _chargedTime = 0f;

    [Header("Gatling Gun")]
    [SerializeField]
    private float _minigunDamageInterval = 0.21f;
    [SerializeField]
    private float _minigunRange = 30f;
    [SerializeField]
    private float _minigunWidth = 1f;
    [SerializeField]
    private int _minigunDamage = 5;
    [SerializeField]
    private AnimationCurve _minigunDamageCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
    private float _damageInterval = 0f;

    private Collider[] _cachedColliders = new Collider[20];
    private HashSet<EnemyAI> enemyAIs = new();

    private float CurrentDamageInterval => Mathf.Lerp(_minigunDamageInterval, _maxDamageIntervalAtMaxCharge, _minigunDamageCurve.Evaluate(Mathf.Clamp01(_chargedTime / _maxChargedTime)));

    private static readonly int _EmissiveColorHash = Shader.PropertyToID("_EmissiveColor");
    public override void Start()
    {
        base.Start();
        _ceasefireMaterial = _ceasefireRenderer.material;
        _baseEmissionColor = _ceasefireMaterial.GetColor(_EmissiveColorHash);

        float intensity = Mathf.Lerp(_minEmissionIntensity, _maxEmissionIntensity, 0);
        Color emitColor = _baseEmissionColor * intensity;
        _ceasefireMaterial.SetColor(_EmissiveColorHash, emitColor);
    }

    public override void Update()
    {
        base.Update();
        if (!isHeld || isPocketed)
            return;

        float rotationDelta = 0f;

        if (_firingStartRoutine != null)
        {
            rotationDelta = Time.deltaTime * _rotationSpeed * _startingTime;
        }
        else if (isBeingUsed)
        {
            DoGatlingGunDamage();
            rotationDelta = Time.deltaTime * _rotationSpeed;
        }
        else if (_firingEndRoutine != null)
        {
            rotationDelta = Time.deltaTime * _rotationSpeed * _endingTime;
        }

        if (isBeingUsed && _chargedTime < _maxChargedTime)
        {
            _chargedTime += Time.deltaTime;
        }
        else if (!isBeingUsed && _chargedTime > 0f)
        {
            _chargedTime -= Time.deltaTime;
        }

        float tNorm = Mathf.Clamp01(_chargedTime / _maxChargedTime);
        float evaluatedValue = _minigunDamageCurve.Evaluate(tNorm);
        if (rotationDelta != 0f)
        {
            rotationDelta *= 1 - evaluatedValue;
            _currentBarrelRotationX += rotationDelta;
            _ceasefireBarrel.transform.localEulerAngles = new Vector3(-280 + _currentBarrelRotationX, 270f, 90f);
            if (playerHeldBy != null && playerHeldBy.IsOwner && _particleSystemsGO.activeSelf)
            {
                playerHeldBy.externalForceAutoFade += (-playerHeldBy.gameplayCamera.transform.forward) * 20f * (playerHeldBy.isCrouching ? 0.25f : 1f) * Time.deltaTime * (rotationDelta / 35f);
            }
        }

        // Slow down particle systems as charge increases
        foreach (ParticleSystem ps in _particleSystems)
        {
            var emission = ps.emission;
            emission.rateOverTime = Mathf.Lerp(_maxParticleRateOverTime, _minParticleRateOverTime, evaluatedValue);
        }

        // Increase emissive intensity with charge
        float intensity = Mathf.Lerp(_minEmissionIntensity, _maxEmissionIntensity, evaluatedValue);
        Color emitColor = _baseEmissionColor * intensity;
        _ceasefireMaterial.SetColor(_EmissiveColorHash, emitColor);
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (!buttonDown)
        {
            if (_firingStartRoutine != null)
                StopCoroutine(_firingStartRoutine);
            _firingStartRoutine = null;
            _firingEndRoutine = StartCoroutine(DoEndFiringSequence());
            isBeingUsed = false;
        }
        else
        {
            if (_firingEndRoutine != null)
                StopCoroutine(_firingEndRoutine);
            _firingEndRoutine = null;
            _firingStartRoutine = StartCoroutine(DoStartFiringSequence());
            isBeingUsed = true;
        }
    }

    private IEnumerator DoStartFiringSequence()
    {
        _idleSource.clip = _fireStartSound;
        _idleSource.Stop();
        _idleSource.Play();
        _startingTime = 0f;
        float timeElapsed = 0f;
        while (timeElapsed <= _fireStartSound.length)
        {
            yield return null;
            _startingTime = timeElapsed / _fireStartSound.length;
            timeElapsed += Time.deltaTime;
        }
        _particleSystemsGO.SetActive(true);
        _idleSource.clip = _fireLoopSound;
        _idleSource.Stop();
        _idleSource.Play();
        _firingStartRoutine = null;
    }

    private IEnumerator DoEndFiringSequence()
    {
        _particleSystemsGO.SetActive(false);
        _idleSource.clip = _fireEndSound;
        _idleSource.Stop();
        _idleSource.Play();
        _endingTime = 2f;
        float timeElapsed = _fireEndSound.length;
        while (timeElapsed > 0)
        {
            yield return null;
            _endingTime = timeElapsed / _fireEndSound.length;
            timeElapsed -= Time.deltaTime;
        }
        _idleSource.Stop();
        _firingEndRoutine = null;
    }

    public void DoGatlingGunDamage()
    {
        float damageThreshold = CurrentDamageInterval;
        if (_damageInterval < damageThreshold)
        {
            _damageInterval += Time.deltaTime;
            return;
        }
        if (playerHeldBy.IsOwner) HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
        if (damageThreshold >= _maxDamageIntervalAtMaxCharge - 0.25f)
        {
            playerHeldBy.DamagePlayer(5, true, true, CauseOfDeath.Gunshots, 0, false, -playerHeldBy.gameplayCamera.transform.forward * 20f);
        }
        _damageInterval = 0f;

        if (!IsServer) return;

        enemyAIs.Clear();

        Vector3 capsuleStart = _ceasefireBarrel.transform.position;
        Vector3 capsuleEnd = capsuleStart + (-_ceasefireBarrel.transform.up) * _minigunRange;
        int numHits = Physics.OverlapCapsuleNonAlloc(capsuleStart, capsuleEnd, _minigunWidth, _cachedColliders, MoreLayerMasks.PlayersAndInteractableAndEnemiesAndPropsHazardMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < numHits; i++)
        {
            Collider collider = _cachedColliders[i];
            if (!collider.TryGetComponent(out IHittable hittable))
                continue;

            if (Physics.Linecast(capsuleStart, collider.gameObject.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                continue;

            if (hittable is PlayerControllerB player)
            {
                if (player == playerHeldBy)
                    continue;

                Vector3 damageDirection = (player.transform.position - capsuleStart).normalized;
                player.DamagePlayerFromOtherClientServerRpc(_minigunDamage, damageDirection, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerHeldBy));
                player.externalForceAutoFade += damageDirection * 2f;
            }
            else if (hittable is EnemyAICollisionDetect enemy)
            {
                if (enemyAIs.Contains(enemy.mainScript))
                    continue;

                enemyAIs.Add(enemy.mainScript);
                enemy.mainScript.HitEnemyOnLocalClient(1, capsuleStart, playerHeldBy, true, -1);
            }
            else
            {
                hittable.Hit(1, capsuleStart, playerHeldBy, true, -1);
            }
        }
    }
}