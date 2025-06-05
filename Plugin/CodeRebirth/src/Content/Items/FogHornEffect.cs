using UnityEngine;
using UnityEngine.VFX;

namespace CodeRebirth.src.Content.Items;
public class FogHornEffect : MonoBehaviour
{
    [SerializeField]
    private VisualEffect _fogEffect;

    private static readonly int _objectForwardHash = Shader.PropertyToID("ObjectForward");
    private static readonly int _objectUpHash = Shader.PropertyToID("ObjectUp");

    public void Update()
    {
        _fogEffect.SetVector3(_objectForwardHash, -transform.up);
        _fogEffect.SetVector3(_objectUpHash, -transform.forward);
    }
}
