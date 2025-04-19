using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class Ceasefire : GrabbableObject
{
    // todo: slow down particle system with _chargedTime
    // todo: make material redder on the emissive with _chargedTime
    // todo: slow down damage dealing with _chargedTime
    [Header("Visuals")]
    [SerializeField]
    private GameObject _ceasefireBarrel = null!;
    [SerializeField]
    private GameObject _particleSystemsGO = null!;
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

    private float _startingTime = 0f;
    private float _endingTime = 0f;
    private Coroutine? _firingStartRoutine = null;
    private Coroutine? _firingEndRoutine = null;
    private float _currentBarrelRotationX = 0f;
    private Material _ceasefireMaterial = null!;
    private float _chargedTime = 0f;

    public override void Start()
    {
        base.Start();
        _ceasefireMaterial = _ceasefireRenderer.material;
    }

    public override void Update()
    {
        base.Update();
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

        if (rotationDelta != 0f)
        {
            _currentBarrelRotationX += rotationDelta;
            _ceasefireBarrel.transform.localEulerAngles = new Vector3(-280 + _currentBarrelRotationX, 270f, 90f);
        }
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (!buttonDown)
        {
            if (_firingStartRoutine != null)
            {
                StopCoroutine(_firingStartRoutine);
            }
            _firingStartRoutine = null;
            _firingEndRoutine = StartCoroutine(DoEndFiringSequence());
            isBeingUsed = false;
        }
        else
        {
            if (_firingEndRoutine != null)
            {
                StopCoroutine(_firingEndRoutine);
            }
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
    private float _damageInterval = 0f;

    private Collider[] _cachedColliders = new Collider[20];
    private List<EnemyAI> enemyAIs = new();

    public void DoGatlingGunDamage()
    {
        if (_damageInterval < _minigunDamageInterval)
        {
            _damageInterval += Time.deltaTime;
            return;
        }
        _damageInterval = 0f;

        if (!IsServer) return;

        enemyAIs.Clear();

        Vector3 capsuleStart = _ceasefireBarrel.transform.position;
        Vector3 capsuleEnd = _ceasefireBarrel.transform.position + playerHeldBy.transform.forward * _minigunRange;
        int numHits = Physics.OverlapCapsuleNonAlloc(capsuleStart, capsuleEnd, _minigunWidth, _cachedColliders, CodeRebirthUtils.Instance.playersAndInteractableAndEnemiesAndPropsHazardMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < numHits; i++)
        {
            Collider collider = _cachedColliders[i];
            if (!collider.TryGetComponent(out IHittable hittable))
                continue;

            if (hittable is PlayerControllerB player)
            {
                if (player == playerHeldBy)
                    continue;

                Vector3 damageDirection = (player.transform.position - _ceasefireBarrel.transform.position).normalized;
                player.DamagePlayer(_minigunDamage, true, true, CauseOfDeath.Gunshots, 0, false, damageDirection * 10f);
                player.externalForceAutoFade += damageDirection * 2f;
            }
            else if (hittable is EnemyAICollisionDetect enemy)
            {
                if (enemyAIs.Contains(enemy.mainScript))
                    continue;

                enemyAIs.Add(enemy.mainScript);
                enemy.mainScript.HitEnemyOnLocalClient(1, _ceasefireBarrel.transform.position, null, true, -1);
            }
            else
            {
                hittable.Hit(1, _ceasefireBarrel.transform.position, null, true, -1);
            }
        }
    }
}