using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class BillboardScript : MonoBehaviour
{
    private Transform parentObject;
    [Tooltip("Locks the rotational up axis of the billboard.")]
    public bool lockUpAxis = true;
    private Camera camera = null!;

    private void Start()
    {
        camera = GameNetworkManager.Instance.localPlayerController.gameplayCamera;
        parentObject = transform.parent;
        transform.rotation = parentObject.rotation;

    }

    private void Update()
    {
        if (lockUpAxis)
        {
            if (parentObject.transform.eulerAngles == Vector3.zero)
            {
                gameObject.transform.eulerAngles = new Vector3(gameObject.transform.eulerAngles.x, camera.transform.eulerAngles.y + 180, gameObject.transform.eulerAngles.z);

            }
            else
            {
                gameObject.transform.eulerAngles = new Vector3(camera.transform.eulerAngles.x, parentObject.transform.eulerAngles.y, camera.transform.eulerAngles.z);
            }
        }
        else
        {
            gameObject.transform.LookAt(camera.transform.position);
        }
    }
}
