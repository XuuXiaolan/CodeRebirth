using System.Collections.Generic;
using UnityEngine;

namespace CodeRebirth.Util.PlayerManager;
public enum CodeRebirthStatusEffects
{
    None,
    Water,
    Electric,
    Fire,
    Smokey,
    // Add other status effects here
}
public class CodeRebirthPlayerManager : MonoBehaviour
{
    public bool ridingHoverboard = false;
    public Dictionary<CodeRebirthStatusEffects, bool> statusEffects = new Dictionary<CodeRebirthStatusEffects, bool>();
    public ParticleSystem playerParticles;

    public CodeRebirthPlayerManager() {
        statusEffects.Add(CodeRebirthStatusEffects.None, false);
        statusEffects.Add(CodeRebirthStatusEffects.Water, false);
        statusEffects.Add(CodeRebirthStatusEffects.Electric, false);
        statusEffects.Add(CodeRebirthStatusEffects.Fire, false);
        statusEffects.Add(CodeRebirthStatusEffects.Smokey, false);
    }
}