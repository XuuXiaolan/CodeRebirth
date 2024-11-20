using UnityEditor;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using ModelReplacement.AvatarBodyUpdater;

namespace Assets.Editor
{
    public class SetupAvatarOffset
    {
        [MenuItem("ModelReplacementSDK/Setup Model")]
        static void setupModel()
        {
            var obj = Selection.activeObject as GameObject;
            if (obj == null) { return; }
            if (!obj.GetComponentInChildren<SkinnedMeshRenderer>())
            {
                Debug.LogError($"Model {obj.name} must have at least one SkinnedMeshRenderer");
            }
            var ani = obj.GetComponentInChildren<Animator>();
            if (!ani)
            {
                Debug.LogError($"Model {obj.name} must have an Animator");
            }
            if (!ani.isHuman)
            {
                Debug.LogError($"Model {obj.name} must have a humanoid avatar setup.");
            }



            if (!ani.gameObject.GetComponent<OffsetBuilder>())
            {
                ani.gameObject.AddComponent<OffsetBuilder>();
                Debug.Log($"Model {obj.name} setup complete. Set each humanoid avatar bone to the correct rotation offset.");
            }
            else
            {
                Debug.Log($"Model {obj.name} setup unnecessary. Already has an OffsetBuilder");
            }



        }
    }

}