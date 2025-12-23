
using Dawn.Utils;
using Unity.Netcode;

namespace CodeRebirth.src.Content.Unlockables;

public class MoneyCounter : NetworkSingleton<MoneyCounter>
{
    private NetworkVariable<int> _totalMoneyStored = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkPostSpawn()
    {
        base.OnNetworkPostSpawn();

        UpdateVisuals();
    }

    public void AddMoney(int amount)
    {
        _totalMoneyStored.Value += amount;
        UpdateVisuals();
    }

    public int MoneyStored()
    {
        return _totalMoneyStored.Value;
    }

    public void RemoveMoney(int amount)
    {
        _totalMoneyStored.Value -= amount;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // sync counter visual.
    }
}