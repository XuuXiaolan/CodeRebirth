using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public enum RaycastDirection
{
    Forward,
    Back,
    Up,
    Down,
    Left,
    Right
}

public class MoveObjectToRaycastResult : MonoBehaviour
{
    [field: SerializeField]
    public RaycastDirection RaycastDirection { get; private set; }
    [field: SerializeField]
    public bool WorldDirection { get; private set; }
    [field: SerializeField]
    public LayerMask LayerMask { get; private set; }

    [field: SerializeField]
    public QueryTriggerInteraction QueryTriggerInteraction { get; private set; }

    [field: SerializeField]
    public float Distance { get; private set; }

    [field: SerializeField]
    public bool DoOnAwake { get; private set; }

    public void Awake()
    {
        if (DoOnAwake)
        {
            MoveToRaycastResult();
        }
    }

    public void MoveToRaycastResult()
    {
        Ray ray = new(transform.position, CalculateForceDirection());
        if (Physics.Raycast(ray, out RaycastHit hit, Distance, LayerMask, QueryTriggerInteraction))
        {
            Plugin.ExtendedLogging($"Starting from: {transform.position}, hit: {hit.point} with normal: {hit.normal} and Direction: {ray.direction}");
            transform.position = hit.point;
        }
    }

    private Vector3 CalculateForceDirection()
    {
        Vector3 raycastDirectionVector = Vector3.zero;
        switch (RaycastDirection)
        {
            case RaycastDirection.Forward:
                raycastDirectionVector = WorldDirection ? Vector3.forward : transform.forward;
                break;
            case RaycastDirection.Back:
                raycastDirectionVector = WorldDirection ? Vector3.back : -transform.transform.forward;
                break;
            case RaycastDirection.Up:
                raycastDirectionVector = WorldDirection ? Vector3.up : transform.transform.up;
                break;
            case RaycastDirection.Down:
                raycastDirectionVector = WorldDirection ? Vector3.down : -transform.transform.up;
                break;
            case RaycastDirection.Right:
                raycastDirectionVector = WorldDirection ? Vector3.right : transform.transform.right;
                break;
            case RaycastDirection.Left:
                raycastDirectionVector = WorldDirection ? Vector3.left : -transform.transform.right;
                break;
        }

        return raycastDirectionVector;
    }
}