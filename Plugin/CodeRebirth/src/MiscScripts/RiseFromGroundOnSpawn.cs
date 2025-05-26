using UnityEngine;
using UnityEngine.Events;

namespace CodeRebirth.src.MiscScripts;
public class RiseFromGroundOnSpawn : MonoBehaviour
{
    [Header("Events")]
    [SerializeField]
    private UnityEvent onRiseComplete = new();

    [Header("Rising Settings")]
    [SerializeField]
    private float depthToRaise = 1f;
    [SerializeField]
    private float raiseSpeed = 0.25f;

    [Header("Camera Shake")]
    [SerializeField]
    private ScreenShakeType screenShakeOnRise = ScreenShakeType.Small;
    [SerializeField]
    private float distanceToShake = 5f;


    private float _timeToTake = 4f;
    private Vector3 _originalPosition = Vector3.zero;
    public void Start()
    {
        if (Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, this.transform.position) <= distanceToShake)
            HUDManager.Instance.ShakeCamera(screenShakeOnRise);

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
            onRiseComplete.Invoke();
            Destroy(this);
        }
    }
}