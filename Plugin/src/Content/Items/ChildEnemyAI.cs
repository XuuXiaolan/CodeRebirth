using System;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Items;
public class ChildEnemyAI : GrabbableObject
{
    public NavMeshAgent agent = null!;
    [NonSerialized] public bool mommyAlive = true;
    public override void Start()
    {
        base.Start();
    }
}