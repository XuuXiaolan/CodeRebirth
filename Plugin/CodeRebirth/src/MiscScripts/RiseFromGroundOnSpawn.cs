using System;
using UnityEngine;
using UnityEngine.Events;

namespace CodeRebirth.src.MiscScripts;

[Serializable]
public class RiseFromDifferentGroundTypes
{
    public string groundTag;

    [Header("Events")]
    public UnityEvent onRiseStart = new();
    public UnityEvent onRiseComplete = new();
    public UnityEvent onGroundStart = new();
    public UnityEvent onGroundComplete = new();

    [Header("Rising Or Descending Settings")]
    public float depthToRaise = 1f;
    public float raiseSpeed = 0.25f;
}

public class RiseFromGroundOnSpawn : MonoBehaviour
{
    [SerializeField]
    private RiseFromDifferentGroundTypes[] _riseFromDifferentGroundTypes = [];

    private RiseFromDifferentGroundTypes _riseFromDifferentGroundType;
    internal float _timeToTake = 0f;
    private Vector3 _originalPosition = Vector3.zero;
    public void Start()
    {
        string tagName = string.Empty;
        if (Physics.Raycast(this.transform.position + this.transform.up * 0.1f, -this.transform.up, out RaycastHit hit, 2f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
        {
            tagName = hit.collider.gameObject.tag;
        }
        else
        {
            return;
        }

        _riseFromDifferentGroundType = _riseFromDifferentGroundTypes[0];
        foreach (RiseFromDifferentGroundTypes riseFromDifferentGroundType in _riseFromDifferentGroundTypes)
        {
            if (!riseFromDifferentGroundType.groundTag.Equals(tagName, StringComparison.OrdinalIgnoreCase))
                continue;

            _riseFromDifferentGroundType = riseFromDifferentGroundType;
            break;
        }

        _originalPosition = this.transform.position;
        this.transform.position = this.transform.position + transform.up * -_riseFromDifferentGroundType.depthToRaise;
        if (_riseFromDifferentGroundType.raiseSpeed > 0)
            _timeToTake = _riseFromDifferentGroundType.depthToRaise / _riseFromDifferentGroundType.raiseSpeed;
    }

    public void Update()
    {
        _timeToTake -= Time.deltaTime;

        this.transform.position = this.transform.position + transform.up * _riseFromDifferentGroundType.raiseSpeed * Time.deltaTime;
        if (_timeToTake <= 0)
        {
            this.transform.position = _originalPosition;
            FinishRisingOrGrounding();
        }
    }

    private void FinishRisingOrGrounding()
    {
        if (_riseFromDifferentGroundType.raiseSpeed > 0)
        {
            _riseFromDifferentGroundType.onRiseComplete.Invoke();
            _riseFromDifferentGroundType.raiseSpeed *= -0.5f;
        }
        else
        {
            _riseFromDifferentGroundType.onGroundComplete.Invoke();
            _riseFromDifferentGroundType.raiseSpeed *= -2f;
        }
        _timeToTake = _riseFromDifferentGroundType.depthToRaise / (Mathf.Abs(_riseFromDifferentGroundType.raiseSpeed));
        this.enabled = false;
    }
}