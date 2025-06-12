using UnityEditor;
using UnityEngine;
using CodeRebirth.src.MiscScripts.DissolveEffect;

public static class SDFHandlesUtilities
{
    public static void DrawArrow(Transform transform, float size)
    {
        Vector3 position = transform.position;
        Vector3 normalEndPosition = position + transform.up * size;
        Vector3 arrowStartPosition = position + transform.up * size * 0.73f;

        // Draw the line
        Handles.DrawLine(position, normalEndPosition);

        // Draw the arrowhead
        float arrowWide = size * 0.15f;

        // Forward
        Vector3 leftPointForward = arrowStartPosition + transform.forward * -arrowWide;
        Vector3 rightPointForward = arrowStartPosition + transform.forward * arrowWide;

        Handles.DrawLine(arrowStartPosition, leftPointForward);
        Handles.DrawLine(arrowStartPosition, rightPointForward);

        Handles.DrawLine(leftPointForward, normalEndPosition);
        Handles.DrawLine(rightPointForward, normalEndPosition);
    }

    public static void DrawPlane(Transform transform, float size)
    {
        float offset = 0;

        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        Vector3 right = rotation * Vector3.right;
        Vector3 up = rotation * Vector3.up;
        Vector3 forward = rotation * Vector3.forward;

        Vector3 p0 = position + transform.up * offset;
        Vector3 p1 = p0 + right * size / 2f;
        Vector3 p2 = p0 - right * size / 2f;
        Vector3 p3 = p0 + forward * size / 2f;
        Vector3 p4 = p0 - forward * size / 2f;

        Handles.DrawPolyLine(p1 + p3 - p0, p2 + p3 - p0, p2 + p4 - p0, p1 + p4 - p0, p1 + p3 - p0, p2 + p3 - p0, p2 + p4 - p0, p1 + p4 - p0);
    }

    public static void DrawBox(Transform transform)
    {
        // Calculate the transformation matrix based on the object's position, rotation, and scale
        Matrix4x4 cubeTransform = Matrix4x4.TRS(transform.position, transform.rotation, new Vector3(1, 1, 1));

        // Set the transformation matrix for the Handles system
        Handles.matrix = cubeTransform;

        // Draw the wire cube using the Handles system
        Handles.DrawWireCube(Vector3.zero, Vector3.Max(transform.lossyScale, Vector3.zero));
    }

    public static void DrawEllipse(Transform transform)
    {
        int numSegments = 64;

        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        Vector3 right = rotation * Vector3.right * Mathf.Max(transform.lossyScale.x, 0); ;
        Vector3 up = rotation * Vector3.up * Mathf.Max(transform.lossyScale.y, 0); ;
        Vector3 forward = rotation * Vector3.forward * Mathf.Max(transform.lossyScale.z, 0); ;

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < numSegments; j++)
            {
                float angle1 = Mathf.Lerp(0, 2 * Mathf.PI, (float)j / numSegments);
                float angle2 = Mathf.Lerp(0, 2 * Mathf.PI, (float)(j + 1) / numSegments);

                Vector3 axis1 = Vector3.zero;
                Vector3 axis2 = Vector3.zero;

                switch (i)
                {
                    case 0:
                        axis1 = right * Mathf.Cos(angle1) + up * Mathf.Sin(angle1);
                        axis2 = right * Mathf.Cos(angle2) + up * Mathf.Sin(angle2);
                        break;
                    case 1:
                        axis1 = right * Mathf.Cos(angle1) + forward * Mathf.Sin(angle1);
                        axis2 = right * Mathf.Cos(angle2) + forward * Mathf.Sin(angle2);
                        break;
                    case 2:
                        axis1 = up * Mathf.Cos(angle1) + forward * Mathf.Sin(angle1);
                        axis2 = up * Mathf.Cos(angle2) + forward * Mathf.Sin(angle2);
                        break;
                }

                Handles.DrawLine(position + axis1, position + axis2);
            }
        }
    }
}