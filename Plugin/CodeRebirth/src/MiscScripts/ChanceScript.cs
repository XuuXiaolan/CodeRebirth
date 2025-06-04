using CodeRebirth.src.Util;
using UnityEngine;
using UnityEngine.Events;

namespace CodeRebirth.src.MiscScripts;
[DefaultExecutionOrder(-999)]
public class ChanceScript : MonoBehaviour
{
    [SerializeField]
    private UnityEvent _onChance = new();
    [SerializeField]
    [Range(0, 100)]
    private int _chance = 50;

    public void Start()
    {
        int randomNumber = CodeRebirthUtils.Instance.CRRandom.Next(0, 100) + 1;
        Plugin.ExtendedLogging($"Random Number: {randomNumber}");
        if (randomNumber > _chance)
            return;

        _onChance.Invoke();
    }
}
