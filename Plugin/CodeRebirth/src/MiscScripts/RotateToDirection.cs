using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class RotateToDirection : MonoBehaviour
{
    [field: SerializeField]
    public Vector3 Direction { get; private set; } = new Vector3(-90f, 0f, -45f);

    [field: SerializeField]
    public bool RotateOnAwake { get; private set; } = true;

    private void Awake()
    {
        if (RotateOnAwake)
        {
            Rotate();
        }
    }

    public void Rotate()
    {
        transform.rotation = Quaternion.Euler(Direction);
    }
}