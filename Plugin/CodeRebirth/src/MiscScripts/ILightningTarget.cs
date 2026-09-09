using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public interface ILightningTarget
{
    public bool IsStrikeable { get; }
    public Transform LightningStrikePoint { get; }
}
