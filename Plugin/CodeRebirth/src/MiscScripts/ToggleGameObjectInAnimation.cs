using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class ToggleGameObjectInAnimation : MonoBehaviour
{
    public GameObject targetGameObject;

    public void ToggleGameObject(int isActive)
    {
        if (targetGameObject != null)
        {
            targetGameObject.SetActive(isActive == 1);
        }
    }
}