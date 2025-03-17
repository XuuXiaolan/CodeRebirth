using CodeRebirth.src.MiscScripts.PathFinding;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class SmallRigo : MonoBehaviour
{
    public SmartAgentNavigator smartAgentNavigator = null!;

    public void DoPathingToPosition(Vector3 position)
    {
        smartAgentNavigator.DoPathingToDestination(position);
    }
}