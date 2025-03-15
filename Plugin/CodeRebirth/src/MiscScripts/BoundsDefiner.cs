using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class BoundsDefiner : MonoBehaviour
{
    public Color boundColor = Color.blue; // Customizable color for bounds
    public Bounds bounds = new(Vector3.zero, Vector3.one); // Default bounds

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = boundColor;

        // Save the original matrix so we can restore it later.
        Matrix4x4 oldGizmosMatrix = Gizmos.matrix;
        
        // Set the Gizmos matrix to include the GameObject's position and rotation.
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        
        // Now draw the cube in local space.
        Gizmos.DrawWireCube(bounds.center, bounds.size);
        
        // Restore the original Gizmos matrix.
        Gizmos.matrix = oldGizmosMatrix;
    }

    public bool BoundsContainTransform(Transform transform)
    {
        return new Bounds(transform.position + bounds.center, bounds.size).Contains(transform.position);
    }
}