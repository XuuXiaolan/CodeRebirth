using UnityEngine;
using UnityEngine.VFX;

public class MonarchBeamController : MonoBehaviour
{
    public Transform _raycastDirectionBeamTransform = null!;
    public Transform _startBeamTransform = null!;
    public AudioClip _beamSound = null!;
    public VisualEffect? _monarchParticle = null;

    [SerializeField]
    private SkinnedMeshRenderer _wingMesh = null!;
    [SerializeField]
    private Transform _endBeamTransform = null!;

    public void OnValidate()
    {
        if (_monarchParticle == null)
            return;

        _monarchParticle.SetSkinnedMeshRenderer("wingMesh", _wingMesh);
    }

    public void Start()
    {
        if (_monarchParticle == null)
            return;

        _monarchParticle.SetSkinnedMeshRenderer("wingMesh", _wingMesh);
    }

    public void SetBeamPosition(Vector3 position)
    {
        _endBeamTransform.position = position;
    }
}