using AntlerShed.SkinRegistry;
using UnityEngine;

namespace CodeRebirthESKR.SkinRegistry.Jimothy;
[CreateAssetMenu(fileName = "JimothySkinDefinition", menuName = "CodeRebirthESKR/JimothySkinDefinition", order = 1)]
public class JimothySkin : BaseSkin
{
    public override string EnemyId => "jimbob";

    public override Skinner CreateSkinner()
    {
        return new JimothySkinner(this);
    }
}