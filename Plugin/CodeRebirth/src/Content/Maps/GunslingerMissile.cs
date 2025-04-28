using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class GunslingerMissile : MonoBehaviour
{
    public float speed = 20f;
    public float curveStrength = 2f; // Strength of curve adjustment

    private Transform _oldParent = null!;
    [HideInInspector] public GunslingerGreg gregScript = null!;
    [HideInInspector] public bool ready = false;
    [HideInInspector] public Transform mainTransform = null!;
    private Collider[] _cachedColliders = new Collider[8];
    private Transform? _targetTransform = null;

    public void Initialize(Transform targetTransform, GunslingerGreg greg)
    {
        ready = false;
        Plugin.ExtendedLogging($"Initializing rocket for target: {targetTransform.name} at position: ({targetTransform.position.x} {targetTransform.position.y} {targetTransform.position.z})");
        _targetTransform = targetTransform;
        gregScript = greg;
        _oldParent = transform.parent;
        transform.SetParent(null);
        greg.rockets.Enqueue(this);
    }

    public void FixedUpdate()
    {
        if (_targetTransform == null)
        {
            this.transform.SetParent(_oldParent, true);
            this.transform.SetPositionAndRotation(mainTransform.position, mainTransform.rotation);
            this.gameObject.SetActive(false);
            return;
        }

        int collidersFound = Physics.OverlapSphereNonAlloc(this.transform.position, 2f, _cachedColliders, CodeRebirthUtils.Instance.collidersAndRoomAndPlayersAndEnemiesAndTerrainAndVehicleMask, QueryTriggerInteraction.Ignore);
        if (collidersFound > 0)
        {
            if (Vector3.Distance(this.transform.position, _targetTransform.position) < 7.5f && _targetTransform.gameObject.layer == 19 && _targetTransform.name == "metarig")
            {
                RadMechAI radMechAI = _targetTransform.GetComponentInParent<RadMechAI>();
                if (radMechAI != null)
                {
                    if (radMechAI.IsOwner)
                        radMechAI.KillEnemyOnOwnerClient(overrideDestroy: true);                    

                    var gameobject = GameObject.Instantiate(MapObjectHandler.Instance.GunslingerGreg!.OldBirdExplosionPrefab, this.transform.position, Quaternion.identity);
                    Destroy(gameobject, 15f);
                }
            }
            CRUtilities.CreateExplosion(this.transform.position, true, 15, 0, 4, 6, null, null, 20f);
            // playerHitSoundSource.Play();
            if (Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, this.transform.position) < 10)
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
            }
            // windSource.volume = 0f;
            this.transform.SetParent(_oldParent, true);
            this.transform.SetPositionAndRotation(mainTransform.position, mainTransform.rotation);
            _targetTransform = null;
            this.gameObject.SetActive(false);
            return;
        }

        transform.position += transform.forward * speed * Time.fixedDeltaTime;

        Vector3 directionToTarget = (_targetTransform.position - transform.position).normalized;
        Vector3 newDirection = Vector3.Lerp(transform.forward, directionToTarget, curveStrength * Time.fixedDeltaTime).normalized;
        transform.forward = newDirection;
    }
}