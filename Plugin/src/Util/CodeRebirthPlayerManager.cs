using System.Collections.Generic;
using UnityEngine;

namespace CodeRebirth.Util.PlayerManager;
public enum CodeRebirthStatusEffects
{
    None,
    Windy,
    // Add other status effects here
}
public class CodeRebirthPlayerManager : MonoBehaviour
{
    public bool ridingHoverboard = false;
    public Dictionary<CodeRebirthStatusEffects, bool> statusEffects = new Dictionary<CodeRebirthStatusEffects, bool>();

}