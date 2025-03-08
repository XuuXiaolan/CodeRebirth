using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class BoundsDefiner : MonoBehaviour
{
    public Color boundColor = Color.blue; // Customizable color for bounds
    public Bounds bounds = new(Vector3.zero, Vector3.one); // Default bounds

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = boundColor;
        Gizmos.DrawWireCube(bounds.center, bounds.extents * 2);
    }
#endif

    public bool BoundsContainTransform(Transform transform)
    {
        return bounds.Contains(transform.position);
    }
}