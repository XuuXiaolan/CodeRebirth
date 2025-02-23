using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if UNITY_EDITOR // => Ignore from here to next endif if not in editor
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEditor;
using UnityEditor.SearchService;
#endif
using UnityEngine;

namespace ModelReplacement.AvatarBodyUpdater
{
    [Serializable]
    [ExecuteInEditMode]
    [AddComponentMenu("Model Replacement Properties")]
    public class OffsetBuilder : MonoBehaviour {
        
        
        public Vector3 rootPositionOffset = new Vector3(0, 0, 0);
        public Vector3 rootScale = new Vector3(1, 1, 1);
        public Vector3 itemPositonOffset = new Vector3(0, 0, 0);
        public Quaternion itemRotationOffset = Quaternion.identity;
        public GameObject itemHolder;
        public bool UseNoPostProcessing = false;

#if UNITY_EDITOR // => Ignore from here to next endif if not in editor
        public Transform rootTransform => GetPlayerTransformFromBoneName("spine");
        [HideInInspector]
        public GameObject playerObject;
        [HideInInspector]
        public GameObject item;

        private bool initializedPreview => playerObject != null && item != null;

        [HideInInspector]
        [SerializeField]
        private bool initialized;

        public bool renderPlayer {
            get => playerObject.GetComponentInChildren<SkinnedMeshRenderer>().enabled;
            set => playerObject.GetComponentInChildren<SkinnedMeshRenderer>().enabled = value;
        }
        public bool renderItem {
            get => item.GetComponentInChildren<Renderer>().enabled;
            set => item.GetComponentInChildren<Renderer>().enabled = value;
        }

        private ModelReplacementProject prevProject;
        public ModelReplacementProject Project;
  
        public string assetName = "";
        public string assetPath = "";

        private void OnValidate()
        {
            if(Project == prevProject) { return; }
            if(prevProject != null) { prevProject.ReportPrefabRemoval(this); }
            prevProject = Project;
            if (Project != null) { Project.ReportPrefabAddition(this); }
        }

        public void SavePrefab2()
        {
            if (!Directory.Exists("Assets/ModelReplacementSDK/AssetsToBuild"))
            {
                Directory.CreateDirectory("Assets/ModelReplacementSDK/AssetsToBuild");
            }
            assetPath = "Assets/ModelReplacementSDK/AssetsToBuild/" + assetName + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(gameObject, assetPath);

        }


        public IEnumerator SavePrefab()
        {
            yield return null;
            if (!Directory.Exists("Assets/ModelReplacementSDK/AssetsToBuild"))
            {
                Directory.CreateDirectory("Assets/ModelReplacementSDK/AssetsToBuild");
            }
            assetPath = "Assets/ModelReplacementSDK/AssetsToBuild/" + assetName + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(gameObject, assetPath);
            yield return null;
        }
        public void SaveAssetBundles()
        {
            StartCoroutine(SaveAssetBundle());
        }

        private IEnumerator SaveAssetBundle()
        {
            string assetBundleName = "mrapi_assetbundle";

            if(Project != null) { yield break; }
            if (assetName == "")
            {
                Debug.LogError($"Asset Name must be set");
                yield break; 
            }
           
            yield return SavePrefab();

            AssetImporter assetImporter = AssetImporter.GetAtPath(assetPath);
            assetImporter.assetBundleName = assetBundleName;

            string assetBundleDirectory = "Assets/AssetBundles";
            if (!Directory.Exists(assetBundleDirectory))
            {
                Directory.CreateDirectory(assetBundleDirectory);
            }
            BuildPipeline.BuildAssetBundles(assetBundleDirectory,BuildAssetBundleOptions.None,BuildTarget.StandaloneWindows);
            EditorUtility.RevealInFinder(assetBundleDirectory + "/" + assetBundleName);
        }

        private void InitializePreview() {
            if (initializedPreview) {
                return;
            }

            if (playerObject == null)
            {
                var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath("b9abac1e5ff1cb94483598ed4877ae91"));
                playerObject = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                playerObject.hideFlags = HideFlags.DontSave;
            }
            if(playerObject != null) { renderPlayer = false; }
           

            if (item == null)
            {
                var walkieTalkiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath("7e73454e50f98f347aaea162c4ebe382"));
                item = (GameObject)PrefabUtility.InstantiatePrefab(walkieTalkiePrefab);
                foreach (var itemChild in item.GetComponentsInChildren<Transform>()) {
                    itemChild.gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
                }
            }
            
            playerObject.name = $"{playerObject.name}({name})";
            item.name = $"{item.name}({name})";
        }

        private void CleanUpPreview() {
            if (Application.isPlaying) {
                Destroy(item);
                Destroy(playerObject);
            } else {
                DestroyImmediate(item);
                DestroyImmediate(playerObject);
            }
        }

        public void Initialize(bool force) {
            InitializePreview();
            if (initialized && !force) {
                return;
            }

            assetName = name;
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                throw new UnityException("No animator found on the character. It is required.");
            }
            Transform upperChestTransform = animator.GetBoneTransform(HumanBodyBones.UpperChest);
            hasUpperChest = (upperChestTransform != null);
            
            var rht = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (rht == null) {
                throw new UnityException("No right hand found on the character. It is required.");
            }

            if (itemHolder == null) {
                var itemTransform = rht.Find("ItemHolderTransform");
                if (itemTransform == null) {
                    itemHolder = new GameObject("ItemHolderTransform");
                    itemHolder.transform.SetParent(rht);
                    itemHolder.name = "ItemHolderTransform";
                } else {
                    itemHolder = itemTransform.gameObject;
                }
            }

            ScavengerGetter.Get().GetComponentInChildren<Animator>().avatar.humanDescription.skeleton.ToList().ForEach(MapSkeletonBones);
            animator.avatar.humanDescription.skeleton.ToList().ForEach(MapSkeletonBones);
            
            // PopulateFingers();
            baseScale = Vector3.one;
            CalculateScale();
            var playerBodyExtents = playerObject.GetComponentInChildren<SkinnedMeshRenderer>().bounds.extents;
            float scale = playerBodyExtents.y / GetBounds().extents.y;
            baseScale = transform.localScale * scale;
            CalculateScale();
            CalculateRotationOffsets();
            CalculateRootOffset();
            initialized = true;
        }

        private void MapSkeletonBones(SkeletonBone sk)
        {
            var matchingTransforms = GetComponentsInChildren<Transform>().Where(x => x.name == sk.name);
            if (matchingTransforms.Any())
            {
                matchingTransforms.First().localRotation = sk.rotation;
            }
            else
            {
                Debug.Log($"Missing bone {sk.name}");
            }
        }


        public Animator animator;
        public bool hasUpperChest = false;
        private Bounds GetBounds()
        {
            Bounds bounds = new Bounds();
            var allBounds = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>().Select(r => r.bounds);

            float maxX = allBounds.OrderByDescending(x => x.max.x).First().max.x;
            float maxY = allBounds.OrderByDescending(x => x.max.y).First().max.y;
            float maxZ = allBounds.OrderByDescending(x => x.max.z).First().max.z;

            float minX = allBounds.OrderBy(x => x.min.x).First().min.x;
            float minY = allBounds.OrderBy(x => x.min.y).First().min.y;
            float minZ = allBounds.OrderBy(x => x.min.z).First().min.z;

            bounds.SetMinMax(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
            return bounds;
        }

        public Vector3 baseScale = Vector3.one;
       

        public void CalculateScale()
        {
            transform.localScale = Vector3.Scale(baseScale,rootScale);
        }

        public void CalculateRotationOffsets()
        {
            foreach (Transform playerBone in playerObject.GetComponentInChildren<SkinnedMeshRenderer>().bones)
            {
                Transform modelBone = GetAvatarTransformFromBoneName(playerBone.name);
                if (modelBone == null) { continue; }


                Transform humanBone = null;
                var someBones = ScavengerGetter.Get().GetComponentsInChildren<Transform>().Where(x => x.name == playerBone.name.Replace(".", "_"));
                if (someBones.Any()) { humanBone = someBones.First(); }


                if (!modelBone.gameObject.GetComponent<RotationOffset>())
                {
                    var a = modelBone.gameObject.AddComponent<RotationOffset>();

                    if (humanBone )
                    {
                        a.offset = Quaternion.Inverse(playerBone.rotation) * modelBone.rotation;
                    }
                }

            }
        }

        public void CalculateRootOffset()
        {
            //Translation offset done after all rotation offsets are done
            foreach (Transform playerBone in playerObject.GetComponentInChildren<SkinnedMeshRenderer>().bones)
            {
                if (playerBone.name != "spine") continue;
                    
                Transform modelBone = GetAvatarTransformFromBoneName(playerBone.name);
                if (modelBone == null) { continue; }

                Vector3 playerfoot = ScavengerGetter.Get().GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.LeftToes).position;
                Vector3 modelFoot = GetAvatarLowestTransform().position - playerBone.TransformVector(rootPositionOffset);
                Vector3 diff = playerfoot - modelFoot;
                diff.x = 0f;
                rootPositionOffset = playerBone.InverseTransformVector(diff);
            }
        }

        // Update is called once per frame
        void Update() {
            if (!initializedPreview) { return; }
            // PopulateFingers();
            if (playerObject != null) { playerObject.GetComponentInChildren<SkinnedMeshRenderer>().enabled = renderPlayer; }
            if (item != null){ item.GetComponentInChildren<Renderer>().enabled = renderItem; }
                
            CalculateScale();
            animator = GetComponentInChildren<Animator>();


            itemHolder.transform.localPosition = itemPositonOffset;
            Transform playerItemHolder = GetPlayerItemHolder();


            item.transform.rotation = playerItemHolder.rotation;
            item.transform.Rotate(new Vector3(30.01f, 5.9f, 12.54f));
            item.transform.position = itemHolder.transform.position;
            Vector3 vector = new Vector3(-0.012f, 0.085f, 0.01f) * 50;
            vector = playerItemHolder.rotation * vector;
            item.transform.position += vector;

            foreach (Transform playerBone in playerObject.GetComponentInChildren<SkinnedMeshRenderer>().bones)
            {
                Transform modelBone = GetAvatarTransformFromBoneName(playerBone.name);
                if (modelBone == null) { continue; }
                modelBone.rotation = playerBone.rotation;
                var offset = modelBone.GetComponent<RotationOffset>();
                if (offset) { modelBone.rotation *= offset.offset; }

                if (playerBone.name == "spine")
                {
                    modelBone.position = playerBone.position;

                    modelBone.position += playerBone.TransformVector(rootPositionOffset);
                    //modelBone.position += rootPositionOffset;
                }
            }
        }

        private void OnEnable() {
            Initialize(false);
        }

        private void OnDisable() {
            CleanUpPreview();
        }

        private Transform GetAvatarLowestTransform()
        {
            if (animator.GetBoneTransform(HumanBodyBones.LeftToes))
            {
                return animator.GetBoneTransform(HumanBodyBones.LeftToes);
            }
            if (animator.GetBoneTransform(HumanBodyBones.LeftFoot))
            {
                return animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            }
            if (animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg))
            {
                return animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            }
            if (animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg))
            {
                return animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            }
            return animator.GetBoneTransform(HumanBodyBones.Hips);
        }
        private Transform GetAvatarTransformFromBoneName(string boneName)
        {
            //Special logic is required here. The player model has 5 central bones.
            // Spine, spine.001, spine.002, spine.003,   spine.004, corresponding to 
            // Hips   Spine      Chest      UpperChest   Head
            //However spine.002 practically doesn't move, and I wish to support mods that don't have an UpperChest bone. 
            //If they don't have an upperchest bone, instead map spine.003 to the Chest transform on the replacement model.
            if (boneName == "spine.002")
            {
                if (hasUpperChest) { return animator.GetBoneTransform(HumanBodyBones.Chest); }
                else { return null; }
            }
            if (boneName == "spine.003")
            {
                if (hasUpperChest) { return animator.GetBoneTransform(HumanBodyBones.UpperChest); }
                else { return animator.GetBoneTransform(HumanBodyBones.Chest); }
            }
            if (modelToAvatarBone.ContainsKey(boneName))
            {
                return animator.GetBoneTransform(modelToAvatarBone[boneName]);
            }
            return null;
        }

        public Transform GetPlayerTransformFromBoneName(string boneName)
        {
            var a = playerObject.GetComponentInChildren<SkinnedMeshRenderer>().bones.Where(x => x.name == boneName);
            if (a.Any()) { return a.First(); }
            if (boneName == "spine")
            {
                var b = playerObject.GetComponentInChildren<SkinnedMeshRenderer>().bones.Where(x => x.name.Contains("PlayerRagdoll")); //For ragdoll and etc...
                if (b.Any()) { return b.First(); }
            }
            return null;

        }

        public Transform GetPlayerItemHolder()
        {
            var tr = playerObject.GetComponentsInChildren<Transform>().Where(x => (x.name == "ServerItemHolder") || (x.name == "ItemHolder"));
            if(tr.Any()) { return tr.First(); }
            return null;
        }

        //Remove spine.002 and .003 to implement logic
        public static Dictionary<string, HumanBodyBones> modelToAvatarBone = new Dictionary<string, HumanBodyBones>()
            {
                {"spine" , HumanBodyBones.Hips},
                {"spine.001" , HumanBodyBones.Spine},

                // {"spine.002" , HumanBodyBones.Chest},
                //{"spine.003" , HumanBodyBones.UpperChest},

                {"shoulder.L" , HumanBodyBones.LeftShoulder},
                {"arm.L_upper" , HumanBodyBones.LeftUpperArm},
                {"arm.L_lower" , HumanBodyBones.LeftLowerArm},
                {"hand.L" , HumanBodyBones.LeftHand},
                {"finger5.L" , HumanBodyBones.LeftLittleProximal},
                {"finger5.L.001" , HumanBodyBones.LeftLittleIntermediate},
                {"finger4.L" , HumanBodyBones.LeftRingProximal},
                {"finger4.L.001" , HumanBodyBones.LeftRingIntermediate},
                {"finger3.L" , HumanBodyBones.LeftMiddleProximal},
                {"finger3.L.001" , HumanBodyBones.LeftMiddleIntermediate},
                {"finger2.L" , HumanBodyBones.LeftIndexProximal},
                {"finger2.L.001" , HumanBodyBones.LeftIndexIntermediate},
                {"finger1.L" , HumanBodyBones.LeftThumbProximal},
                {"finger1.L.001" , HumanBodyBones.LeftThumbDistal},

                {"shoulder.R" , HumanBodyBones.RightShoulder},
                {"arm.R_upper" , HumanBodyBones.RightUpperArm},
                {"arm.R_lower" , HumanBodyBones.RightLowerArm},
                {"hand.R" , HumanBodyBones.RightHand},
                {"finger5.R" , HumanBodyBones.RightLittleProximal},
                {"finger5.R.001" , HumanBodyBones.RightLittleIntermediate},
                {"finger4.R" , HumanBodyBones.RightRingProximal},
                {"finger4.R.001" , HumanBodyBones.RightRingIntermediate},
                {"finger3.R" , HumanBodyBones.RightMiddleProximal},
                {"finger3.R.001" , HumanBodyBones.RightMiddleIntermediate},
                {"finger2.R" , HumanBodyBones.RightIndexProximal},
                {"finger2.R.001" , HumanBodyBones.RightIndexIntermediate},
                {"finger1.R" , HumanBodyBones.RightThumbProximal},
                {"finger1.R.001" , HumanBodyBones.RightThumbDistal},

                {"spine.004" , HumanBodyBones.Head},

                {"thigh.L" , HumanBodyBones.LeftUpperLeg},
                {"shin.L" , HumanBodyBones.LeftLowerLeg},
                {"foot.L" , HumanBodyBones.LeftFoot},
                {"toe.L" , HumanBodyBones.LeftToes},

                {"thigh.R" , HumanBodyBones.RightUpperLeg},
                {"shin.R" , HumanBodyBones.RightLowerLeg},
                {"foot.R" , HumanBodyBones.RightFoot},
                {"toe.R" , HumanBodyBones.RightToes},
        };
#endif
    }

}


