using GameNetcodeStuff;

namespace CodeRebirth.src.Content.Maps;
public class BoomTrap : BearTrap
{
    public void Awake()
    {
        byProduct = true;
    }

    public override void TriggerTrap(PlayerControllerB player)
    {
        base.TriggerTrap(player);

    }

    public override void TriggerTrap(EnemyAI enemy)
    {
        base.TriggerTrap(enemy);

    }
}