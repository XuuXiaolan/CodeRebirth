using AntlerShed.SkinRegistry;
using UnityEngine;

namespace CodeRebirthESKR.SkinRegistry;
public class JimothySkinner : Skinner
{
    public void Apply(GameObject enemy)
    {
        //Perform any logic here to modify the appearance of the enemy. All of it must be client-side.
        //This is also the point where an EventHandler is registered if your skinner makes use of it. To do so, call EnemySkinRegistry.RegisterEventHandler(enemy, MyEventHandler)
    }

    public void Remove(GameObject enemy)
    {
        //Restore the enemy to its vanilla appearance, undoing all of the changes done by Apply.
        //Unregister the event handler by calling RemoveEventHandler(enemy) if you registered one.
    }
}