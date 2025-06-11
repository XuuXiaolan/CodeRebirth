using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using INab.CommonVFX;

namespace INab.Common
{
    [ExecuteAlways]
    public class InteractiveEffect : MonoBehaviour
    {
        #region Shader Keywords

        /// <summary>
        /// List of shader keyword names corresponding to different mask Signed Distance Field (SDF) types.
        /// These types include plane, box, sphere, ellipse, solid angle, and round cone.
        /// </summary>
        private static List<string> typeKeywords = new List<string>()
        {
            "_TYPE_PLANE",
            "_TYPE_BOX",
            "_TYPE_ELLIPSE"
        };


        #endregion

        #region Visual Effect Graph Properties

        [SerializeField, Tooltip("Indicates whether to use VFX Graph Effect.")]
        public bool useVFXGraphEffect = true;

        [SerializeField, Tooltip("Reference to the Visual Effect component.")]
        public VisualEffect visualEffect;

        [SerializeField, Tooltip("Reference to the VFX Property Binder component.")]
        public VFXPropertyBinder propertyBinder;

        [SerializeField, Tooltip("Transform of the mesh used for the effect.")]
        public Transform meshTransform;

        [SerializeField, Tooltip("Renderer of the mesh used for the effect.")]
        public Renderer meshRenderer;

        /// <summary>
        /// Determines if the mesh renderer is a skinned mesh renderer.
        /// </summary>
        public bool IsSkinnedMesh
        {
            get
            {
                return meshRenderer is SkinnedMeshRenderer;
            }
            private set { }
        }

        /// <summary>
        /// Determines if the mesh in the mesh renderer is Readable.
        /// </summary>
        public bool IsMeshReadable
        {
            get
            {
                bool readable;

                if (meshRenderer == null) return true;

                if (IsSkinnedMesh)
                {
                    var renderer = meshRenderer as SkinnedMeshRenderer;
                    readable = renderer.sharedMesh.isReadable;
                }
                else
                {
                    var filter = meshRenderer.GetComponent<MeshFilter>();
                    readable = filter.sharedMesh.isReadable;
                }

                return readable;
            }
            private set { }
        }

        [SerializeField, Tooltip("Instance of the VFX Uniform Mesh Baker.")]
        public VFXUniformMeshBaker meshBaker = new VFXUniformMeshBaker();

        [Range(0.01f, 10f), SerializeField, Tooltip("Multiply sample count by this value to control density of the particles. Keep this as low as possible.")]
        public float sampleCountMultiplier = 1f;

        public Coroutine EffectCoroutine;
        public Coroutine EditorCoroutine;

        #endregion

        #region Interactive Mask Properties

        [SerializeField, Tooltip("Interactive effect mask.")]
        public InteractiveEffectMask mask;

        [SerializeField, Tooltip("Type of SDF mask. Choose from Plane, Box, Sphere, etc.")]
        private InteractiveEffectMaskType maskType = InteractiveEffectMaskType.Ellipse;

        public InteractiveEffectMaskType MaskType
        {
            get { return maskType; }
            private set { }
        }

        [SerializeField, Tooltip("Automatically updates material properties from the materials list if enabled.")]
        private bool controlMaterialsProperties = false;

        public bool ControlMaterialsProperties
        {
            get { return controlMaterialsProperties; }
            private set { }
        }

        [SerializeField, Tooltip("Effect shader style. For 'Smooth', set material to transparent.")]
        private ShaderType shaderType = ShaderType.Burn;

        public ShaderType ShaderType
        {
            get { return shaderType; }
            private set { }
        }

        #endregion

        #region Effect Settings

        [SerializeField, Tooltip("List of materials used by the interactive effect.")]
        public List<Material> materials = new List<Material>();

        [SerializeField, Tooltip("Effect animation curve.")]
        public AnimationCurve effectCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [SerializeField, Tooltip("Duration of the effect.")]
        public float duration = 2f;

        [SerializeField, Tooltip("Time that the VFX graphs will be turned off after the effect is finished.")]
        public float vfxEventOffset = 2f;

        [SerializeField, Tooltip("Change the effect stage to 0 on initial.")]
        public bool resetOnInitial = true;

        [SerializeField, Tooltip("Ensures that the materials are only affected on the selected mesh renderer. This may breaks SRP batching.")]
        public bool useInstancedMaterials = false;

        public bool isFinished = false;
        protected float initialLerpValue { get; } = 0;

        #endregion

        #region Transform Properties

        [Range(0, 1), SerializeField, Tooltip("Lerp value for transforms.")]
        private float effectStateTest = 0.5f;

        public float EffectStateTest
        {
            get { return effectStateTest; }
            private set { }
        }

        [SerializeField, Tooltip("Indicates if position transform is used.")]
        public bool usePositionTransform = false;

        [SerializeField, Tooltip("Initial position for the transform.")]
        public Vector3 initialPosition;

        [SerializeField, Tooltip("Final position for the transform.")]
        public Vector3 finalPosition;

        [SerializeField, Tooltip("Indicates if scale transform is used.")]
        public bool useScaleTransform = false;

        [SerializeField, Tooltip("Initial scale for the transform.")]
        public Vector3 initialScale;

        [SerializeField, Tooltip("Final scale for the transform.")]
        public Vector3 finalScale;

        [SerializeField, Tooltip("Indicates if rotation transform is used.")]
        public bool useRotationTransform = false;

        [SerializeField, Tooltip("Initial rotation for the transform.")]
        public Vector3 initialRotation;

        [SerializeField, Tooltip("Final rotation for the transform.")]
        public Vector3 finalRotation;

        #endregion

        #region Materials Properties

        [SerializeField, Tooltip("Rate at which the properties are scaled.")]
        private float propertiesScaleRate = 1;
        public float PropertiesScaleRate
        {
            get { return propertiesScaleRate; }
            set
            {
                propertiesScaleRate = value;
                UpdateAllMaterialProperties();
            }
        }

        [SerializeField, Tooltip("Inverts the effect effect.")]
        private bool invert = false;
        public bool Invert
        {
            get { return invert; }
            set
            {
                invert = value;
                UpdateAllMaterialProperties(nameof(Invert), Invert);
            }
        }

        [SerializeField, Tooltip("Guide texture for the effect pattern.")]
        private Texture2D guideTexture = null;
        public Texture2D GuideTexture
        {
            get { return guideTexture; }
            set
            {
                guideTexture = value;
                UpdateAllMaterialProperties(nameof(GuideTexture), GuideTexture);
            }
        }

        [SerializeField, Tooltip("Tiling rate of the guide texture.")]
        private float guideTiling = 1;
        public float GuideTiling
        {
            get { return guideTiling; }
            set
            {
                guideTiling = value;
                UpdateAllMaterialProperties(nameof(GuideTiling), GuideTiling);
            }
        }

        [SerializeField, Range(0, 1), Tooltip("Intensity of the guide texture effect.")]
        private float guideStrength = 0.5f;
        public float GuideStrength
        {
            get { return guideStrength; }
            set
            {
                guideStrength = value;
                UpdateAllMaterialProperties(nameof(GuideStrength), GuideStrength);
            }
        }

        // ================= Back Color =================

        [SerializeField, Tooltip("Enables a background color for the effect.")]
        private bool useBackColor = false;
        public bool UseBackColor
        {
            get { return useBackColor; }
            set
            {
                useBackColor = value;
                UpdateAllMaterialProperties(nameof(UseBackColor), UseBackColor);
            }
        }

        [SerializeField, ColorUsage(true, true), Tooltip("Background color for the effect effect.")]
        private Color backColor = Color.black;
        public Color BackColor
        {
            get { return backColor; }
            set
            {
                backColor = value;
                UpdateAllMaterialProperties(nameof(BackColor), BackColor);
            }
        }

        // ================= Burn =================

        [SerializeField, Range(0, 1), Tooltip("Controls the sharpness of the burn effect.")]
        private float burnHardness = 0.5f;
        public float BurnHardness
        {
            get { return burnHardness; }
            set
            {
                burnHardness = value;
                UpdateAllMaterialProperties(nameof(BurnHardness), BurnHardness);
            }
        }

        [SerializeField, Range(0, 2), Tooltip("Determines the offset of the burn effect.")]
        private float burnOffset = 0.5f;
        public float BurnOffset
        {
            get { return burnOffset; }
            set
            {
                burnOffset = value;
                UpdateAllMaterialProperties(nameof(BurnOffset), BurnOffset);
            }
        }

        [SerializeField, Range(0, 1), Tooltip("Offset for the ember effect within the burn area.")]
        private float emberOffset = 0.1f;
        public float EmberOffset
        {
            get { return emberOffset; }
            set
            {
                emberOffset = value;
                UpdateAllMaterialProperties(nameof(EmberOffset), EmberOffset);
            }
        }

        [SerializeField, Range(0, 1), Tooltip("Smoothness of the ember edges.")]
        private float emberSmoothness = 0.1f;
        public float EmberSmoothness
        {
            get { return emberSmoothness; }
            set
            {
                emberSmoothness = value;
                UpdateAllMaterialProperties(nameof(EmberSmoothness), EmberSmoothness);
            }
        }

        [SerializeField, Range(0, 1), Tooltip("Width of the ember effect.")]
        private float emberWidth = 0;
        public float EmberWidth
        {
            get { return emberWidth; }
            set
            {
                emberWidth = value;
                UpdateAllMaterialProperties(nameof(EmberWidth), EmberWidth);
            }
        }

        [SerializeField, ColorUsage(true, true), Tooltip("Color of the ember effect.")]
        private Color emberColor = new Color(10f, 1.8f, 0.2f);
        public Color EmberColor
        {
            get { return emberColor; }
            set
            {
                emberColor = value;
                UpdateAllMaterialProperties(nameof(EmberColor), EmberColor);
            }
        }

        [SerializeField, ColorUsage(true, true), Tooltip("Primary color for the burn effect.")]
        private Color burnColor = Color.black;
        public Color BurnColor
        {
            get { return burnColor; }
            set
            {
                burnColor = value;
                UpdateAllMaterialProperties(nameof(BurnColor), BurnColor);
            }
        }

        // ================= Smooth =================

        [SerializeField, Tooltip("Whether to use dithering to fake transparency.")]
        private bool useDithering = false;
        public bool UseDithering
        {
            get { return useDithering; }
            set
            {
                useDithering = value;
                UpdateAllMaterialProperties(nameof(UseDithering), UseDithering);
            }
        }

        [SerializeField, ColorUsage(true, true), Tooltip("Color of the edge.")]
        private Color edgeColor = Color.blue;
        public Color EdgeColor
        {
            get { return edgeColor; }
            set
            {
                edgeColor = value;
                UpdateAllMaterialProperties(nameof(EdgeColor), EdgeColor);
            }
        }

        [SerializeField, Range(0, 0.2f), Tooltip("Width of the edge.")]
        private float edgeWidth = 0;
        public float EdgeWidth
        {
            get { return edgeWidth; }
            set
            {
                edgeWidth = value;
                UpdateAllMaterialProperties(nameof(EdgeWidth), EdgeWidth);
            }
        }

        [SerializeField, Range(0, 1), Tooltip("Smoothness of the edge.")]
        private float edgeSmoothness = 0.05f;
        public float EdgeSmoothness
        {
            get { return edgeSmoothness; }
            set
            {
                edgeSmoothness = value;
                UpdateAllMaterialProperties(nameof(EdgeSmoothness), EdgeSmoothness);
            }
        }

        [SerializeField, Tooltip("Determines if the albedo is affected.")]
        private bool affectAlbedo = false;
        public bool AffectAlbedo
        {
            get { return affectAlbedo; }
            set
            {
                affectAlbedo = value;
                UpdateAllMaterialProperties(nameof(AffectAlbedo), AffectAlbedo);
            }
        }

        [SerializeField, ColorUsage(true, true), Tooltip("Color of the glare effect.")]
        private Color glareColor = Color.blue;
        public Color GlareColor
        {
            get { return glareColor; }
            set
            {
                glareColor = value;
                UpdateAllMaterialProperties(nameof(GlareColor), GlareColor);
            }
        }

        [SerializeField, Range(0, 3), Tooltip("Strength of the guide texture for the glare effect.")]
        private float glareGuideStrength = 1;
        public float GlareGuideStrength
        {
            get { return glareGuideStrength; }
            set
            {
                glareGuideStrength = value;
                UpdateAllMaterialProperties(nameof(GlareGuideStrength), GlareGuideStrength);
            }
        }

        [SerializeField, Range(0, 1), Tooltip("Width of the glare effect.")]
        private float glareWidth = 0;
        public float GlareWidth
        {
            get { return glareWidth; }
            set
            {
                glareWidth = value;
                UpdateAllMaterialProperties(nameof(GlareWidth), GlareWidth);
            }
        }

        [SerializeField, Range(0, 2), Tooltip("Smoothness of the glare effect.")]
        private float glareSmoothness = 0.3f;
        public float GlareSmoothness
        {
            get { return glareSmoothness; }
            set
            {
                glareSmoothness = value;
                UpdateAllMaterialProperties(nameof(GlareSmoothness), GlareSmoothness);
            }
        }

        [SerializeField, Range(-2, 2), Tooltip("Offset for the glare effect.")]
        private float glareOffset = 0;
        public float GlareOffset
        {
            get { return glareOffset; }
            set
            {
                glareOffset = value;
                UpdateAllMaterialProperties(nameof(GlareOffset), GlareOffset);
            }
        }


        #endregion

        #region Material Properties Methods

        private void UpdateAllMaterialProperties(string propertyName, Color color)
        {
            foreach (var material in materials)
            {
                material.SetColor("_" + propertyName, color);
            }
        }

        private void UpdateAllMaterialProperties(string propertyName, float vector)
        {
            foreach (var material in materials)
            {
                material.SetFloat("_" + propertyName, vector);
            }
        }

        private void UpdateAllMaterialProperties(string propertyName, Vector3 vector)
        {
            foreach (var material in materials)
            {
                material.SetVector("_" + propertyName, vector);
            }
        }

        private void UpdateAllMaterialProperties(string propertyName, bool value)
        {
            foreach (var material in materials)
            {
                material.SetInt("_" + propertyName, value ? 1 : 0);
            }
        }

        private void UpdateAllMaterialProperties(string propertyName, Texture value)
        {
            if (value == null) return;
            foreach (var material in materials)
            {
                material.SetTexture("_" + propertyName, value);
            }
        }

        /// <summary>
        /// Updates all materials's effect type properties attached to this script.
        /// </summary>
        private void UpdateAllMaterialProperties()
        {
            foreach (var material in materials)
            {
                material.SetInt("_" + nameof(Invert), Invert ? 1 : 0);
                material.SetTexture("_" + nameof(GuideTexture), GuideTexture);
                material.SetFloat("_" + nameof(GuideTiling), GuideTiling / propertiesScaleRate);
                material.SetFloat("_" + nameof(GuideStrength), GuideStrength * propertiesScaleRate);
                material.SetInt("_" + nameof(UseBackColor), UseBackColor ? 1 : 0);
                material.SetColor("_" + nameof(BackColor), BackColor);
            }

            if (shaderType == ShaderType.Burn)
            {
                foreach (var material in materials)
                {
                    material.SetFloat("_" + nameof(BurnHardness), BurnHardness);
                    material.SetFloat("_" + nameof(BurnOffset), BurnOffset * propertiesScaleRate);

                    material.SetFloat("_" + nameof(EmberOffset), EmberOffset * propertiesScaleRate);
                    material.SetFloat("_" + nameof(EmberSmoothness), Mathf.Clamp01(EmberSmoothness * propertiesScaleRate));
                    material.SetFloat("_" + nameof(EmberWidth), EmberWidth * propertiesScaleRate);

                    material.SetColor("_" + nameof(EmberColor), EmberColor);
                    material.SetColor("_" + nameof(BurnColor), BurnColor);

                }
            }
            else if (shaderType == ShaderType.Smooth)
            {
                foreach (var material in materials)
                {
                    material.SetInt("_" + nameof(UseDithering), UseDithering ? 1 : 0);
                    material.SetColor("_" + nameof(EdgeColor), EdgeColor);
                    material.SetFloat("_" + nameof(EdgeWidth), EdgeWidth * propertiesScaleRate);
                    material.SetFloat("_" + nameof(EdgeSmoothness), Mathf.Clamp01(EdgeSmoothness * propertiesScaleRate));
                    material.SetInt("_" + nameof(AffectAlbedo), AffectAlbedo ? 1 : 0);
                    material.SetColor("_" + nameof(GlareColor), GlareColor);
                    material.SetFloat("_" + nameof(GlareGuideStrength), GlareGuideStrength);
                    material.SetFloat("_" + nameof(GlareWidth), GlareWidth * propertiesScaleRate);
                    material.SetFloat("_" + nameof(GlareSmoothness), GlareSmoothness * propertiesScaleRate);
                    material.SetFloat("_" + nameof(GlareOffset), GlareOffset * propertiesScaleRate);
                }
            }
        }


        /// <summary>
        /// Disables all type keywords in material.
        /// </summary>
        /// <param name="material"></param>
        private void DisableMaterialTypeKeyword(Material material)
        {
            foreach (var keyword in material.enabledKeywords)
            {
                if (typeKeywords.Contains(keyword.name))
                {
                    material.DisableKeyword(keyword);
                }
            }
        }

        /// <summary>
        /// Enables type keyword based on current mask type.
        /// </summary>
        /// <param name="material"></param>
        private void EnableMaterialTypeKeyword(Material material)
        {
            material.EnableKeyword(typeKeywords[(int)maskType]);
        }


        #endregion

        #region Sdf Transform Properties

        // Common
        private Vector4 position;
        private Vector4 scaleVector;

        // Plane Rotation
        private Vector4 upVector;

        // Common Rotation
        private Vector4 forwardVector;
        private Vector4 rightVector;

        #endregion

        #region Sdf Transform Methods

        private void ResetSdfTransformToDefault(ref Vector4 position, ref Vector4 forward, ref Vector4 right, ref Vector4 up)
        {
            position = new Vector4(0, 999999, 0, 0);
            forward = Vector3.forward;
            up = Vector3.up;
            right = Vector3.right;
        }

        private void SetSdfTransform(InteractiveEffectMask mask, ref Vector4 position, ref Vector4 scale,
            ref Vector4 forward, ref Vector4 right, ref Vector4 up)
        {
            var maskTransform = mask.transform;

            position = maskTransform.position;
            // Using abs to avoid negative scales
            //scale = new Vector4(Mathf.Abs(maskTransform.lossyScale.x), Mathf.Abs(maskTransform.lossyScale.y), Mathf.Abs(maskTransform.lossyScale.z), 0);
            scale = new Vector4((maskTransform.lossyScale.x), (maskTransform.lossyScale.y), (maskTransform.lossyScale.z), 0);

            switch (maskType)
            {
                case InteractiveEffectMaskType.Plane:
                    up = maskTransform.up;
                    break;
                case InteractiveEffectMaskType.Box:
                    forward = maskTransform.forward;
                    up = maskTransform.up;
                    right = maskTransform.right;
                    scale = scale * 0.5f; // Divide box scale by 2
                    break;
                case InteractiveEffectMaskType.Ellipse:
                    forward = maskTransform.forward;
                    up = maskTransform.up;
                    right = maskTransform.right;

                    float threshold = 0.01f;

                    if (scale.x < threshold && scale.x > -threshold) scale.x = threshold;
                    if (scale.y < threshold && scale.y > -threshold) scale.y = threshold;
                    if (scale.z < threshold && scale.z > -threshold) scale.z = threshold;

                    break;
            }

        }

        private void UpdateSDFTransformProperties()
        {
            foreach (var material in materials)
            {
                if (material == null) return;
                UpdateCommonTransformProperties(material);

                switch (maskType)
                {
                    case InteractiveEffectMaskType.Plane:
                        UpdateUpVectorProperties(material);
                        break;
                    case InteractiveEffectMaskType.Box:
                        UpdateRotationProperties(material);
                        break;
                    case InteractiveEffectMaskType.Ellipse:
                        UpdateRotationProperties(material);
                        break;
                }
            }
        }

        private void UpdateCommonTransformProperties(Material material)
        {
            material.SetVector("_PositionVector", position);
            material.SetVector("_ScaleVector", scaleVector);
        }

        private void UpdateRotationProperties(Material material)
        {
            material.SetVector("_UpVector", upVector);
            material.SetVector("_RightVector", rightVector);
            material.SetVector("_ForwardVector", forwardVector);
        }

        private void UpdateUpVectorProperties(Material material)
        {
            material.SetVector("_UpVector", upVector);
        }

        private void UpdateCommonVFXProperties(VisualEffect particleEffect)
        {
            particleEffect.SetVector4("Mask Position", position);
            particleEffect.SetVector4("Mask Scale", scaleVector);
        }

        private void UpdateRotationVFXProperties(VisualEffect particleEffect)
        {
            particleEffect.SetVector4("Mask Up", upVector);
            particleEffect.SetVector4("Mask Right", rightVector);
            particleEffect.SetVector4("Mask Forward", forwardVector);
        }

        private void UpdateUpVectorVFXProperties(VisualEffect particleEffect)
        {
            particleEffect.SetVector4("Mask Up", upVector);
        }



        /// <summary>
        /// Updates all vfx graps's sdf vector properties.
        /// </summary>
        private void UpdateVFXSDFTransformProperties()
        {
            if (visualEffect == null || useVFXGraphEffect == false) return;
            if (!visualEffect.HasGraphicsBuffer(VFXUniformMeshBaker.GraphicsBufferName)) return;

            UpdateCommonVFXProperties(visualEffect);

            switch (MaskType)
            {
                case InteractiveEffectMaskType.Plane:
                    UpdateUpVectorVFXProperties(visualEffect);
                    break;
                case InteractiveEffectMaskType.Box:
                    UpdateRotationVFXProperties(visualEffect);
                    break;
                case InteractiveEffectMaskType.Ellipse:
                    UpdateRotationVFXProperties(visualEffect);
                    break;
            }

        }


        #endregion

        #region Private Methods

        private void UpdateSdfParameters()
        {
            if (mask != null) SetSdfTransform(mask, ref position, ref scaleVector, ref forwardVector, ref rightVector, ref upVector);
        }

        private void UpdateMaskType()
        {
            if (mask == null) return;
            mask.Type = MaskType;

            if (visualEffect == null || useVFXGraphEffect == false) return;

            bool usePlane = false;
            bool useBox = false;
            bool useEllipse = false;

            switch (MaskType)
            {
                case InteractiveEffectMaskType.Plane:
                    usePlane = true;
                    break;
                case InteractiveEffectMaskType.Box:
                    useBox = true;
                    break;
                case InteractiveEffectMaskType.Ellipse:
                    useEllipse = true;
                    break;
            }

            visualEffect.SetBool("Use Plane", usePlane);
            visualEffect.SetBool("Use Box", useBox);
            visualEffect.SetBool("Use Ellipse", useEllipse);

        }

        private void ForceUpdateMaskParameters()
        {
            UpdateSdfParameters();
            UpdateSDFTransformProperties();
            UpdateVFXSDFTransformProperties();
        }

        #endregion

        #region UnityLifecycleMethods

        protected void Start()
        {
            if (resetOnInitial) StartTransformCheck(initialLerpValue);

            if (useInstancedMaterials && Application.isPlaying)
            {
                GetRendererMaterials(true);
            }

            UpdateAndSetupEffect();
        }

        protected void Update()
        {
            if (useVFXGraphEffect)
            {
                if (visualEffect && meshRenderer) meshBaker.Update(visualEffect, meshRenderer);
            }
        }

        protected void LateUpdate()
        {
            ForceUpdateMaskParameters();
        }

        protected void OnDisable()
        {
            meshBaker.OnDisable();
        }

        protected void OnValidate()
        {
            if (enabled == false || gameObject.activeSelf == false) return;

            if (useVFXGraphEffect)
            {
                meshBaker.SampleCountMultiplier = sampleCountMultiplier;
            }
            else
            {
                SendStopEvent();
            }

            // Automatically changes all materials's properties attached to this script.
            if (controlMaterialsProperties)
            {
                UpdateAllMaterialProperties();
            }
        }

        #endregion

        #region Editor Methods

        /// <summary>
        /// Editor only method to bake the mesh for the VFX Graph.
        /// </summary>
        public void _BakeUniformMesh()
        {
            meshBaker.Bake(visualEffect, meshRenderer);
        }

        /// <summary>
        /// Editor only method to set the graphics buffer for the VFX Graph.
        /// </summary>
        public void _SetGraphicsBuffer()
        {
            meshBaker.SetGraphicsBuffer(visualEffect);
        }

        /// <summary>
        /// Editor only method to clear the materials list.
        /// </summary>
        public void _ClearMaterialsList()
        {
            materials.Clear();
        }

        /// <summary>
        /// Editor only method to find the renderer in the parent.
        /// </summary>
        public void _FindRendererInParent()
        {
            meshRenderer = GetComponentInParent<Renderer>();
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.transform.parent.GetComponentInChildren<Renderer>();
            }

            if (meshRenderer == null)
            {
                Debug.LogWarning("No renderer could be found.");
            }
        }

        /// <summary>
        /// Editor only method to setup the VFX Graph GameObject.
        /// </summary>
        public void _SetupVfxGraphGameObject()
        {
            GameObject newGameObject = new GameObject("Vfx Graph");
            newGameObject.transform.parent = transform;
            newGameObject.transform.localPosition = Vector3.zero;
            newGameObject.transform.localRotation = Quaternion.identity;
            newGameObject.transform.localScale = Vector3.one;

            visualEffect = newGameObject.AddComponent<VisualEffect>();
            visualEffect.initialEventName = "";
            propertyBinder = newGameObject.AddComponent<VFXPropertyBinder>();
        }

        /// <summary>
        /// Editor only method to setup the mask GameObject.
        /// </summary>
        public void _SetupMaskGameObject()
        {
            GameObject newGameObject = new GameObject("Mask");
            newGameObject.transform.parent = transform;
            newGameObject.transform.localPosition = Vector3.zero;
            newGameObject.transform.localRotation = Quaternion.identity;
            newGameObject.transform.localScale = Vector3.one;
            mask = newGameObject.AddComponent<InteractiveEffectMask>();
        }

        /// <summary>
        /// Editor only method that sets the initial mask transform values as the current mask transform in the scene.
        /// </summary>
        public void _SetInitialMaskTransform()
        {
            var maskTransform = mask.transform;
            initialPosition = maskTransform.localPosition;
            initialRotation = maskTransform.localRotation.eulerAngles;
            initialScale = maskTransform.localScale;
        }

        /// <summary>
        /// Editor only method that sets the final mask transform values as the current mask transform in the scene.
        /// </summary>
        public void _SetFinalMaskTransform()
        {
            var maskTransform = mask.transform;
            finalPosition = maskTransform.localPosition;
            finalRotation = maskTransform.localRotation.eulerAngles;
            finalScale = maskTransform.localScale;
        }

        public bool _DoMaterialsUseProperShaders()
        {
            bool value = true;
            foreach (var material in materials)
            {
                var name = material.shader.name;

                int lastSpaceIndex = name.LastIndexOf(' ');
                var nameStripped = name.Substring(0, lastSpaceIndex);

                if (nameStripped != "INab Studio/Interactive Effect")
                {
                    value = false;
                }
            }

            return value;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Refreshes all properties and keywords for the materials managed by this script.
        /// Ensures the latest material settings are applied, including mask types.
        /// </summary>
        public virtual void UpdateAndSetupEffect()
        {
            if (useVFXGraphEffect)
            {
                VFXMeshSetup.SetupRenderer(meshRenderer, visualEffect);
                VFXMeshSetup.SetupPropertyBinder(propertyBinder, meshTransform);
            }

            // Materials parameters
            ForceUpdateMaskParameters();

            // Materials keywords
            ChangeMaskType(maskType);

            OnValidate();

            if (controlMaterialsProperties)
            {
                UpdateAllMaterialProperties();
            }

            PassMaterialPropertiesToGraph();

            if (useVFXGraphEffect)
            {
                _BakeUniformMesh();
            }
        }

        /// <summary>
        /// Changes the type of the mask used, updating all materials managed by the script.
        /// </summary>
        /// <param name="newType">The new mask type to apply.</param>
        public void ChangeMaskType(InteractiveEffectMaskType newType)
        {
            maskType = newType;

            foreach (var material in materials)
            {
                DisableMaterialTypeKeyword(material);
                EnableMaterialTypeKeyword(material);
            }

            UpdateMaskType();
        }

        /// <summary>
        /// Get materials from the mesh renderer.
        /// </summary>
        /// <param name="sharedMaterials"></param>
        public void GetRendererMaterials(bool instancedMaterials = false)
        {
            materials.Clear();
            if (instancedMaterials) materials.AddRange(meshRenderer.materials);
            else materials.AddRange(meshRenderer.sharedMaterials);
        }

        /// <summary>
        /// Start playing the effect.
        /// </summary>
        public void PlayEffect()
        {
            EffectCoroutine = StartCoroutine(EffectEnumerator());
            if (useInstancedMaterials && Application.isPlaying) GetRendererMaterials(true);
        }

        /// <summary>
        /// Play the effect inversed without particles with a custom duration.
        /// </summary>
        public void ReverseEffect(float customDuration)
        {
            EffectCoroutine = StartCoroutine(EffectEnumerator(false, true, customDuration));
            if (useInstancedMaterials && Application.isPlaying) GetRendererMaterials(true);
        }

        /// <summary>
        /// Play the effect inversed without particles.
        /// </summary>
        public void ReverseEffect()
        {
            ReverseEffect(duration);
        }

        #endregion

        #region Mask Transform Control

        private void StartTransformCheck(float lerpValue)
        {
            UpdateMaskTransform(lerpValue);
            ForceUpdateMaskParameters();
        }

        /// <summary>
        /// Manually updates mask transform based on the effect lerp value.
        /// </summary>
        /// <param name="effectLerp">0 - initial | 1 - final</param>
        public void UpdateMaskTransform(float effectLerp)
        {
            if (mask == null) return;
            var maskTransform = mask.transform;

            if (usePositionTransform)
            {
                var lerpPosition = Vector3.Lerp(initialPosition, finalPosition, effectLerp);
                maskTransform.localPosition = lerpPosition;
            }

            if (useScaleTransform)
            {
                var lerpScale = Vector3.Lerp(initialScale, finalScale, effectLerp);
                maskTransform.localScale = lerpScale;
            }

            if (useRotationTransform)
            {
                var lerpRotation = Quaternion.Lerp(Quaternion.Euler(initialRotation), Quaternion.Euler(finalRotation), effectLerp);
                maskTransform.localRotation = lerpRotation;
            }
        }

        private IEnumerator EffectEnumerator(bool useParticles = true, bool inversed = false, float customDuration = 1)
        {
            if (EffectCoroutine != null) StopCoroutine(EffectCoroutine);

            if (useVFXGraphEffect && useParticles) SendPlayEvent();

            if (inversed)
            {
                SendStopEvent();
            }
            else
            {
                customDuration = duration;
            }

            float effectAmount;
            float elapsedTime = 0f;

            while (elapsedTime < customDuration)
            {
                elapsedTime += Time.deltaTime;

                effectAmount = effectCurve.Evaluate(elapsedTime / customDuration);   // Evaluate the curve in reverse for dissolution

                if (inversed) effectAmount = 1 - effectAmount;

                UpdateMaskTransform(effectAmount);

                yield return null;
            }

            isFinished = true;

            // VFX events offset

            float elapsedTimeVFX = 0f;

            while (elapsedTimeVFX < vfxEventOffset)
            {
                elapsedTimeVFX += Time.deltaTime;

                yield return null;
            }

            if (useVFXGraphEffect && useParticles) SendStopEvent();
        }

        #endregion

        #region DebugAndDevelopment

        public IEnumerator AutoEffectCoroutine()
        {
            float coroutnieTimeOffset = 0.8f;

            float timeLasted = duration;

            isFinished = false;
            PlayEffect();

            while (true)
            {
                timeLasted -= Time.deltaTime;

                if (timeLasted < -coroutnieTimeOffset)
                {
                    isFinished = true;
                    PlayEffect();

                    timeLasted = duration;
                }

                yield return null; // Wait for the next frame
            }
        }

        #endregion

        #region Vfx Graph Methods

        /// <summary>
        /// Sends a play event to the VFX Graph.
        /// </summary>
        public void SendPlayEvent()
        {
            if (visualEffect) visualEffect.Play();
        }

        /// <summary>
        /// Sends a stop event to the VFX Graph.
        /// </summary>
        public void SendStopEvent()
        {
            if (visualEffect) visualEffect.Stop();
        }


        /// <summary>
        /// Copies properties (base map, guide properties, etc.) from material from materials list (at index 0) to the effect graph.
        /// </summary>
        public void PassMaterialPropertiesToGraph()
        {
            if (materials.Count < 1)
            {
                Debug.LogWarning("There is no materials to copy properties from in the materials list.");
                return;
            }

            Material material = materials[0];

            if (material == null)
            {
                Debug.LogWarning("First material in materials list is null.");
                return;
            }

            if (visualEffect != null && useVFXGraphEffect == true)
            {
                if (material.GetTexture("_GuideTexture"))
                {
                    visualEffect.SetTexture("Guide Texture", material.GetTexture("_GuideTexture"));
                    visualEffect.SetFloat("Guide Strength", material.GetFloat("_GuideStrength"));
                }
                else
                {
                    visualEffect.SetFloat("Guide Strength", 0);
                }
                visualEffect.SetFloat("Guide Tiling", material.GetFloat("_GuideTiling"));

                if (material.GetTexture("_BaseMap"))
                {
                    visualEffect.SetTexture("Texture", material.GetTexture("_BaseMap"));
                }
                else
                {
                    //Debug.LogWarning("There is no base texture in the effect material.");
                }
                visualEffect.SetVector2("Tiling", material.GetVector("_Tiling"));

            }
        }

        #endregion
    }
}