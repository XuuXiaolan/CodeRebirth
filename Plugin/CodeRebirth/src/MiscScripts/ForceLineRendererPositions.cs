using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class ForceLineRendererPositions : MonoBehaviour
{
    [SerializeField]
    private LineRenderer _lineRenderer = null!;

    [SerializeField]
    private Transform[] _transforms = [];

    private void Awake()
    {
        _lineRenderer.positionCount = _transforms.Length;
    }

    private void Update()
    {
        for (int i = 0; i < _transforms.Length; i++)
        {
            _lineRenderer.SetPosition(i, _transforms[i].position);
        }
    }
}