using CodeRebirth.src.MiscScripts;
using CodeRebirthLib.Util;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;

public class GuardsmanBullet : NetworkBehaviour
{
    internal GuardsmanTurret GuardsmanTurret = null!;

    private float _endTimer = 10f;
    private Vector3 _movingDirection = Vector3.zero;

    public void SetMovingDirection(Vector3 startingPosition, Vector3 direction, float endTimer)
    {
        this.gameObject.SetActive(true);
        GuardsmanTurret.bulletsPool.Remove(this);
        _endTimer = endTimer;
        _movingDirection = direction;
        this.transform.position = startingPosition;
        this.transform.rotation = Quaternion.Euler(direction);
    }

    private void Start()
    {
        ResetBullet();
    }

    public void FixedUpdate()
    {
        if (_movingDirection == Vector3.zero || GuardsmanTurret == null)
            return;

        _endTimer -= Time.fixedDeltaTime;
        if (IsServer)
            this.transform.position = this.transform.position + _movingDirection * 2.5f * Time.fixedDeltaTime;

        if (!Physics.CheckSphere(this.transform.position, 2.5f, MoreLayerMasks.CollidersAndRoomAndPlayersAndEnemiesAndTerrainAndVehicleMask, QueryTriggerInteraction.Ignore) && _endTimer > 0f)
            return;

        CRUtilities.CreateExplosion(this.transform.position, true, 25, 0, 6, 1, null, null, 25f);
        ResetBullet();
    }

    private void ResetBullet()
    {
        this.transform.localPosition = Vector3.zero;
        _movingDirection = Vector3.zero;
        GuardsmanTurret.bulletsPool.Add(this);
        this.gameObject.SetActive(false);
    }
}