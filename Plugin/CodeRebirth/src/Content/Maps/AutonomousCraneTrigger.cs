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

    // Main cylinder local data
    private Vector3 _mainLocalCenter;
    private Vector2 _mainLocalRadii;  // x = radiusX, y = radiusZ
    private float   _mainLocalHalfH;

    // Exemption cylinder local data
    private Vector3 _exemptLocalCenter;
    private Vector2 _exemptLocalRadii;
    private float _exemptLocalHalfH;

    private void Awake()
    {
        // Cache main collider bounds
        var mb = _mainCollider.sharedMesh.bounds;
        _mainLocalCenter = mb.center;
        _mainLocalRadii = new Vector2(mb.extents.x, mb.extents.z);
        _mainLocalHalfH = mb.extents.y;

        // Cache exemption collider bounds
        var eb = _exemptionCollider.sharedMesh.bounds;
        _exemptLocalCenter = eb.center;
        _exemptLocalRadii = new Vector2(eb.extents.x, eb.extents.z);
        _exemptLocalHalfH = eb.extents.y;
    }

    private void Update()
    {
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player == null) 
                continue;

            bool inTargets = _mainScript._targetablePlayers.Contains(player);

            if (player.isPlayerDead || !player.isPlayerControlled || player.IsPseudoDead())
            {
                if (inTargets)
                {
                    Plugin.ExtendedLogging($"AutonomousCraneTrigger: Player {player} is dead or not controlled, removing from targetable players.");
                    _mainScript._targetablePlayers.Remove(player);
                }
                continue;
            }

            if (IsInsideLocalCylinder(player.transform.position, _exemptionCollider.transform, _exemptLocalCenter, _exemptLocalRadii, _exemptLocalHalfH))
            {
                if (inTargets)
                {
                    Plugin.ExtendedLogging($"AutonomousCraneTrigger: Player {player} is inside the exemption area at position {player.transform.position}, removing from targetable players.");
                    _mainScript._targetablePlayers.Remove(player);
                }
                continue;
            }

            bool insideMain = IsInsideLocalCylinder(player.transform.position, _mainCollider.transform, _mainLocalCenter, _mainLocalRadii, _mainLocalHalfH);
            if (inTargets)
            {
                if (!insideMain)
                {
                    Plugin.ExtendedLogging($"AutonomousCraneTrigger: Player {player} is outside the crane's targetable area at position {player.transform.position}, removing from targetable players.");
                    _mainScript._targetablePlayers.Remove(player);
                }
            }
            else if (insideMain)
            {
                Plugin.ExtendedLogging($"AutonomousCraneTrigger: Player {player} is inside the crane's targetable area at position {player.transform.position}.");
                _mainScript._targetablePlayers.Add(player);
            }
        }
    }

    private static bool IsInsideLocalCylinder(Vector3 worldPoint, Transform colliderTransform, Vector3 localCenter, Vector2 localRadii, float localHalfHeight)
    {
        Vector3 lp = colliderTransform.InverseTransformPoint(worldPoint);

        float dx = lp.x - localCenter.x;
        float dz = lp.z - localCenter.z;
        if (dx*dx / (localRadii.x*localRadii.x) + dz*dz / (localRadii.y*localRadii.y) > 1f)
            return false;

        float dy = lp.y - localCenter.y;
        return dy >= -localHalfHeight && dy <= localHalfHeight;
    }
}