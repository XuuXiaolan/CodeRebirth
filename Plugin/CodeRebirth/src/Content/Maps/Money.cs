using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class Money : NetworkBehaviour
{
    [HideInInspector] public int value = 0;
    public void Start()
    {
        value = 1;
    }
}