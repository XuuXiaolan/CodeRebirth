using UnityEngine;
using UnityEngine.VFX;

public class MonarchBeamController : MonoBehaviour
{
    [SerializeField]
    private VisualEffect _monarchParticle = null!;
    [SerializeField]
    private SkinnedMeshRenderer _wingMesh = null!;
    [SerializeField]
    private Transform _endBeamTransform = null!;

    public void OnValidate()
    {
        _monarchParticle.SetSkinnedMeshRenderer("wingMesh", _wingMesh);
    }

    public void Start()
    {
        _monarchParticle.SetSkinnedMeshRenderer("wingMesh", _wingMesh);
    }

    public void SetBeamPosition(Vector3 position)
    {
        _endBeamTransform.position = position;
    }
}