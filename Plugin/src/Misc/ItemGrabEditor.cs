using UnityEngine;

[ExecuteInEditMode]
public class ItemGrabEditor : MonoBehaviour
{
    public Transform parentObject; // Reference to the player's hand or local item holder
    public Transform item; // Reference to the item being held
    public bool previewGrab = false; // Toggle to enable/disable preview
    public Vector3 additionalRotation; // Additional rotation to be applied for preview
    public Vector3 positionOffset; // Additional position offset to be applied for preview
    public AnimationClip previewAnimation; // Animation clip to be played for preview
    public Animator animator; // Animator component reference
    public string targetTransformName = "LocalItemHolder"; // Default target transform name

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator not found on object: " + gameObject.name);
        }

        if (parentObject == null)
        {
            SetParentTransformByName(targetTransformName);
        }
    }

    private void Update()
    {
        UpdateItemTransform();
    }

    private void OnValidate()
    {
        UpdateItemTransform();
    }

    public void UpdateItemTransform()
    {
        if (previewGrab && parentObject != null && item != null)
        {
            // Apply position and rotation offsets
            Vector3 targetPosition = parentObject.position + parentObject.TransformDirection(positionOffset);
            Quaternion targetRotation = parentObject.rotation * Quaternion.Euler(additionalRotation);

            // Apply the transformation for preview
            item.position = targetPosition;
            item.rotation = targetRotation;
        }
    }

    public void SetParentTransformByName(string targetName)
    {
        Transform parentTransform = this.transform;
        Transform result = FindTransformByName(parentTransform, targetName);

        if (result != null)
        {
            parentObject = result;
            Debug.Log("Transform found: " + result.name);
        }
        else
        {
            Debug.Log("Transform not found.");
        }
    }

    public static Transform FindTransformByName(Transform parent, string targetName)
    {
        if (parent == null)
        {
            return null;
        }

        // Check if the current transform's name matches the target name
        if (parent.name.Equals(targetName))
        {
            return parent;
        }

        // Recursively search in each child
        foreach (Transform child in parent)
        {
            Transform found = FindTransformByName(child, targetName);
            if (found != null)
            {
                return found;
            }
        }

        // Return null if the target transform is not found
        return null;
    }

    public void ApplyDefaultRotation()
    {
        if (item != null)
        {
            var grabbableObject = item.gameObject.GetComponent<GrabbableObject>();
            if (grabbableObject != null)
            {
                additionalRotation = grabbableObject.itemProperties.rotationOffset;
            }
        }
    }

    public void FindFirstGrabbableObjectInScene()
    {
        GrabbableObject grabbableObject = FindObjectOfType<GrabbableObject>();
        if (grabbableObject != null)
        {
            item = grabbableObject.transform;
            Debug.Log("GrabbableObject found: " + item.name);
        }
        else
        {
            Debug.Log("No GrabbableObject found in the scene.");
        }
    }

    public void ApplyDefaultPositionOffset()
    {
        if (item != null)
        {
            var grabbableObject = item.gameObject.GetComponent<GrabbableObject>();
            if (grabbableObject != null)
            {
                positionOffset = grabbableObject.itemProperties.positionOffset;
            }
        }
    }
}
