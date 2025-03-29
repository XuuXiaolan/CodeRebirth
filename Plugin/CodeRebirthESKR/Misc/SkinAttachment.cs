using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CodeRebirthESKR.Misc;
[Serializable]
public struct ArmatureAttachment
{
    public string boneId;
    public GameObject attachment;
    public Vector3 location;
    public Vector3 rotation;
    public Vector3 scale;

    public static List<GameObject> ApplyAttachments(ArmatureAttachment[] attachments, SkinnedMeshRenderer renderer)
    {
        if (renderer !=null && attachments != null)
        {
            List<GameObject> activeAttachments = new();
            foreach (ArmatureAttachment attachment in attachments)
            {
                try
                {
                    Transform parent = renderer.bones.First((tf) => tf.name.Equals(attachment.boneId));
                    GameObject instance = GameObject.Instantiate(attachment.attachment, parent);
                    instance.transform.localPosition = attachment.location;
                    instance.transform.localRotation = Quaternion.Euler(attachment.rotation);
                    instance.transform.localScale = attachment.scale;
                    activeAttachments.Add(instance);
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogError(e.StackTrace);
                }
            }
            return activeAttachments;
        }
        if (attachments == null)
        {
            Plugin.Logger.LogWarning("Armature attachments array was null. This probably either means you're running dev build or uninstalled fixplugintypesserialization for some reason. If you're manually setting your armature attachments to null when extending an enemyskinner, consider passing in an empty array instead ans save yourself the log entry.");
        }
        return new();
    }

    public static void RemoveAttachments(List<GameObject> attachments)
    {
        foreach (GameObject go in attachments)
        {
            GameObject.Destroy(go);
        }
    }
}
