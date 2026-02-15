using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class GameObjectEvents : MonoBehaviour
{
    public GameObject[] gameObjects = [];

    public void EnableGameObject(int index)
    {
        gameObjects[index].SetActive(true);
    }

    public void DisableGameObject(int index)
    {
        gameObjects[index].SetActive(false);
    }
}