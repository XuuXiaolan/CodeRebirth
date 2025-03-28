using AntlerShed.SkinRegistry;
using UnityEngine;

namespace CodeRebirthESKR.SkinRegistry;
public class JimothySkin : Skin
{
    public string Label => throw new System.NotImplementedException();

    public string Id => throw new System.NotImplementedException();

    public string EnemyId => throw new System.NotImplementedException();

    public Texture2D Icon => throw new System.NotImplementedException();

    public Skinner CreateSkinner()
    {
        //Return your skinner implementation from here
        Skinner skinner = new JimothySkinner();
        return skinner;
    }
}