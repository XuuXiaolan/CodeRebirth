using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class OutsideObjectSpawner : MonoBehaviour
{
    public void Awake()
    {
        RoundManager.Instance.SpawnOutsideHazards();
    }
}