using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class RiseFromGroundOnSpawn : MonoBehaviour
{
    public float depthToRaise = 1f;
    public float raiseSpeed = 0.25f;

    private float _timeToTake = 4f;
    private Vector3 _originalPosition = Vector3.zero;
    public void Start()
    {
        _originalPosition = this.transform.position;
        this.transform.position = this.transform.position + transform.up * -depthToRaise;
        _timeToTake = depthToRaise / raiseSpeed;
    }

    public void Update()
    {
        _timeToTake -= Time.deltaTime;

        this.transform.position = this.transform.position + transform.up * raiseSpeed * Time.deltaTime;
        if (_timeToTake <= 0)
        {
            this.transform.position = _originalPosition;
            Destroy(this);
        }
    }
}