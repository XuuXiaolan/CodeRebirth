using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace CodeRebirth.src.MiscScripts.DissolveEffect;
/// <summary>
/// Code based on the VFXTransformBinder.cs from Runtime\Utilities\PropertyBinding\Implementation
/// Passes lossyScale instead of localScale.
/// </summary>
[VFXBinder("Transform/Lossy Transform")]
public class VFXLossyTransformBinder : VFXBinderBase
{
    public string Property { get { return (string)m_Property; } set { m_Property = value; UpdateSubProperties(); } }

    //[VFXPropertyBinding("Lossy Transform"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_Parameter")]
    [VFXPropertyBinding("System.Single")]
    protected ExposedProperty m_Property = "Transform";
    public Transform Target = null;

    private ExposedProperty Position;
    private ExposedProperty Angles;
    private ExposedProperty Scale;

    public override void OnEnable()
    {
        base.OnEnable();
        UpdateSubProperties();
    }

    void OnValidate()
    {
        UpdateSubProperties();
    }

    void UpdateSubProperties()
    {
        Position = m_Property + "_position";
        Angles = m_Property + "_angles";
        Scale = m_Property + "_scale";
    }

    public override bool IsValid(VisualEffect component)
    {
        return Target != null && component.HasVector3((int)Position) && component.HasVector3((int)Angles) && component.HasVector3((int)Scale);
    }

    public override void UpdateBinding(VisualEffect component)
    {
        component.SetVector3((int)Position, Target.position);
        component.SetVector3((int)Angles, Target.eulerAngles);
        component.SetVector3((int)Scale, Target.lossyScale);
    }

    public override string ToString()
    {
        return string.Format("Lossy Scale Transform : '{0}' -> {1}", m_Property, Target == null ? "(null)" : Target.name);
    }
}