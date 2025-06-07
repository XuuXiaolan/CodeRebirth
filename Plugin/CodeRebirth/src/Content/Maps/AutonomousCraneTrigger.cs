using CodeRebirth.src.Util;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class AutonomousCraneTrigger : MonoBehaviour
{
    [SerializeField]
    private AutonomousCrane _mainScript = null!;
    [SerializeField]
    private MeshCollider _mainCollider = null!;
    [SerializeField]
    private MeshCollider _exemptionCollider = null!;

    private Vector3 _mainLocalCenter, _exemptLocalCenter;
    private float _mainLocalRadius, _mainLocalHalfH;
    private float _exemptLocalRadius, _exemptLocalHalfH;

    private void Awake()
    {
        var mBounds = _mainCollider.sharedMesh.bounds;
        _mainLocalCenter = mBounds.center;
        _mainLocalRadius = mBounds.extents.x;
        _mainLocalHalfH  = mBounds.extents.y;

        var eBounds = _exemptionCollider.sharedMesh.bounds;
        _exemptLocalCenter = eBounds.center;
        _exemptLocalRadius = eBounds.extents.x;
        _exemptLocalHalfH  = eBounds.extents.y;
    }

    private void Update()
    {
        // Convert local→world for main cylinder
        Vector3 mainCenterWS = _mainCollider.transform.TransformPoint(_mainLocalCenter);
        Vector3 mainScale = _mainCollider.transform.lossyScale;
        float mainRadiusWS = _mainLocalRadius * Mathf.Max(mainScale.x, mainScale.z);
        float mainHalfHWS = _mainLocalHalfH * mainScale.y;

        // Convert local→world for exemption cylinder
        Vector3 exCenterWS = _exemptionCollider.transform.TransformPoint(_exemptLocalCenter);
        Vector3 exScale = _exemptionCollider.transform.lossyScale;
        float exRadiusWS = _exemptLocalRadius * Mathf.Max(exScale.x, exScale.z);
        float exHalfHWS = _exemptLocalHalfH * exScale.y;

        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player == null) 
                continue;

            bool playerInTargetablePlayers = _mainScript._targetablePlayers.Contains(player);
            if (player.isPlayerDead || !player.isPlayerControlled || player.IsPseudoDead())
            {
                if (playerInTargetablePlayers)
                {
                    _mainScript._targetablePlayers.Remove(player);
                }
                continue;
            }

            bool insideExemption = IsInsideCylinder(player.transform.position, exCenterWS, exRadiusWS, exHalfHWS);
            if (insideExemption)
            {
                if (playerInTargetablePlayers)
                {
                    _mainScript._targetablePlayers.Remove(player);
                }
                continue;
            }

            bool insideMain = IsInsideCylinder(player.transform.position, mainCenterWS, mainRadiusWS, mainHalfHWS);
            if (playerInTargetablePlayers)
            {
                if (!insideMain)
                {
                    _mainScript._targetablePlayers.Remove(player);
                }
                continue;
            }

            if (insideMain)
            {
                _mainScript._targetablePlayers.Add(player);
            }
        }
    }

    private static bool IsInsideCylinder(Vector3 pt, Vector3 center, float radius, float halfH)
    {
        // Horizontal (XZ) test
        Vector2 distance = new(pt.x - center.x, pt.z - center.z);
        if (distance.sqrMagnitude > radius * radius)
            return false;

        // Vertical (Y) test
        float dy = pt.y - center.y;
        return dy >= -halfH && dy <= halfH;
    }
}