using System.Collections;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class Ceasefire : GrabbableObject
{
    [Header("Visuals")]
    [SerializeField]
    private GameObject _ceasefireBarrel = null!;
    [SerializeField]
    private float _rotationSpeed = 10f;
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

    public override void Update()
    {
        base.Update();
        if (_firingStartRoutine != null)
        {
            _ceasefireBarrel.transform.eulerAngles = new Vector3(_ceasefireBarrel.transform.eulerAngles.x + Time.deltaTime * _rotationSpeed * _startingTime, _ceasefireBarrel.transform.eulerAngles.y, _ceasefireBarrel.transform.eulerAngles.z);
        }
        else if (isBeingUsed)
        {
            _ceasefireBarrel.transform.eulerAngles = new Vector3(_ceasefireBarrel.transform.eulerAngles.x + Time.deltaTime * _rotationSpeed, _ceasefireBarrel.transform.eulerAngles.y, _ceasefireBarrel.transform.eulerAngles.z);
        }
        else if (_firingEndRoutine != null)
        {
            _ceasefireBarrel.transform.eulerAngles = new Vector3(_ceasefireBarrel.transform.eulerAngles.x + Time.deltaTime * _rotationSpeed * _endingTime, _ceasefireBarrel.transform.eulerAngles.y, _ceasefireBarrel.transform.eulerAngles.z);
        }
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        Plugin.ExtendedLogging($"Mole Digger used and button down: {used} {buttonDown}");
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
        _idleSource.clip = _fireLoopSound;
        _idleSource.Stop();
        _idleSource.Play();
        _firingStartRoutine = null;
    }

    private IEnumerator DoEndFiringSequence()
    {
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
}