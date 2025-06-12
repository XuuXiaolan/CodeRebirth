namespace CodeRebirth.src.MiscScripts.DissolveEffect;
/// <summary>
/// Represents the types of masks available for world dissolve effects.
/// Each type corresponds to a different geometric shape or form.
/// </summary>
public enum InteractiveEffectMaskType
{
    Plane,       // A flat, 2D surface mask
    Box,         // A cubic or rectangular mask
    Ellipse,     // An elliptical mask
}

/// <summary>
/// Defines the available styles for world dissolve shaders.
/// Each style provides a unique visual effect for the dissolve transition.
/// </summary>
public enum ShaderType
{
    Burn,            // A burning effect
    Smooth,          // A smooth, gradual dissolve
}