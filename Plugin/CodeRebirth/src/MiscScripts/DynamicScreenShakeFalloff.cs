using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
[CreateAssetMenu(fileName = "DynamicScreenShakeFalloff", menuName = "CodeRebirth/DynamicScreenShakeFalloff", order = 1)]
public class DynamicScreenShakeFalloff : ScriptableObject
{
    public float dynamicIncreaseFromBigToSmall = 0;
    public float dynamicIncreaseFromLongToBig = 0;
    public float dynamicIncreaseFromVeryStrongToLong = 0;
}