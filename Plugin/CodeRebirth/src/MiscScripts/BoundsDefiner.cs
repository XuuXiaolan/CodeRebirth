using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class BoundsDefiner : MonoBehaviour
{
    public Color boundColor = Color.blue; // Customizable color for bounds
    public Bounds bounds = new(Vector3.zero, Vector3.one); // Default bounds

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = boundColor;
        Matrix4x4 oldGizmosMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
        Gizmos.matrix = oldGizmosMatrix;
    }

    public bool BoundsContainTransform(Transform target)
    {
        Vector3 localPos = transform.InverseTransformPoint(target.position);
        return bounds.Contains(localPos);
    }

    public bool BoundsContainPlayer(PlayerControllerB player)
    {
        return bounds.Contains(player.serverPlayerPosition);
    }
}