using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class AutoRotate : MonoBehaviour
{
    [SerializeField]
    private float _rotationSpeedMax = 5f;

    [SerializeField]
    private float _rotationSpeedMin = 0f;

    private Vector3 _rotation = Vector3.zero;

    private void Start()
    {
        float _rotationSpeedX = CodeRebirthUtils.Instance.CRRandom.NextFloat(_rotationSpeedMin, _rotationSpeedMax);
        float _rotationSpeedY = CodeRebirthUtils.Instance.CRRandom.NextFloat(_rotationSpeedMin, _rotationSpeedMax);
        float _rotationSpeedZ = CodeRebirthUtils.Instance.CRRandom.NextFloat(_rotationSpeedMin, _rotationSpeedMax);
        _rotation = new Vector3(_rotationSpeedX, _rotationSpeedY, _rotationSpeedZ);
    }

    private void Update()
    {
        transform.Rotate(_rotation * Time.deltaTime);
    }
}