using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class ToggleGameObjectInAnimation : MonoBehaviour
{
    public GameObject targetGameObject;

    public void ToggleGameObject(bool isActive)
    {
        if (targetGameObject != null)
        {
            targetGameObject.SetActive(isActive);
        }
    }
}