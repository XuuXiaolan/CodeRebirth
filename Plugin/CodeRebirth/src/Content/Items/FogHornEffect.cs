using UnityEngine;
using UnityEngine.VFX;

namespace CodeRebirth.src.Content.Items;
public class FogHornEffect : MonoBehaviour
{
    [SerializeField]
    private VisualEffect _fogEffect;

    public void Start()
    {
        _fogEffect.gameObject.transform.SetParent(null);
    }

    public void Update()
    {
        _fogEffect.SetVector3("ObjectForward", transform.forward);
        _fogEffect.SetVector3("ObjectUp", transform.up);
    }
}
