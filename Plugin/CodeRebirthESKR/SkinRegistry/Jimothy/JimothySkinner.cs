using AntlerShed.SkinRegistry;
using CodeRebirth.src.Content.Enemies;
using UnityEngine;

namespace CodeRebirthESKR.SkinRegistry.Jimothy;
public class JimothySkinner(JimothySkin skinData) : Skinner, JimothyEventHandler
{
    protected JimothySkin SkinData { get; } = skinData;

    public void Apply(GameObject enemy)
    {
        Transporter transporter = enemy.GetComponent<Transporter>();
        EnemySkinRegistry.RegisterEnemyEventHandler(transporter, this);

        //Perform any logic here to modify the appearance of the enemy. All of it must be client-side.
        //This is also the point where an EventHandler is registered if your skinner makes use of it. To do so, call EnemySkinRegistry.RegisterEventHandler(enemy, MyEventHandler)
    }

    public void Remove(GameObject enemy)
    {
        Transporter transporter = enemy.GetComponent<Transporter>();
        EnemySkinRegistry.RemoveEnemyEventHandler(transporter, this);
        //Restore the enemy to its vanilla appearance, undoing all of the changes done by Apply.
        //Unregister the event handler by calling RemoveEventHandler(enemy) if you registered one.
    }
}