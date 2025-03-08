using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class BoundsDefiner : MonoBehaviour
{
    public Color boundColor = Color.blue; // Customizable color for bounds
    public Bounds bounds = new(Vector3.zero, Vector3.one); // Default bounds

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = boundColor;
        Gizmos.DrawWireCube(this.transform.position + bounds.center, bounds.extents * 2);
    }

    public bool BoundsContainTransform(Transform transform)
    {
        return new Bounds(transform.position + bounds.center, bounds.size).Contains(transform.position);
    }
}