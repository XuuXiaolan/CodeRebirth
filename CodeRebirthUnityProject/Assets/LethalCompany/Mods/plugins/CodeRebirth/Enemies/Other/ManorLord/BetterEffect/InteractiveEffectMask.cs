using UnityEngine;

namespace INab.Common
{
    /// <summary>
    /// Mask script.
    /// </summary>
    [ExecuteAlways]
    public class InteractiveEffectMask : MonoBehaviour
    {
        #region UserExposed

        [SerializeField, Tooltip("Configuration settings for the mask's visual guides and collision detection.")]
        public InteractiveEffectMaskSettings maskSettings;

        #endregion

        #region Internal

        /// <summary>
        /// INTERAL: Determines the mask's shape and associated behavior.
        /// </summary>
        public InteractiveEffectMaskType Type
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
                if (usePreview)
                {
                    UpdatePreview();
                }
            }
        }
        [SerializeField, Tooltip("Determines the mask's shape and associated behavior.")]
        private InteractiveEffectMaskType type = InteractiveEffectMaskType.Ellipse;


        /// <summary>
        /// INTERAL: Enables a visual preview of the mask in the scene.
        /// </summary>
        public bool UsePreview
        {
            get
            {
                return usePreview;
            }
            set
            {
                usePreview = value;
            }
        }
        [SerializeField, Tooltip("Enables a visual preview of the mask in the scene.")]
        protected bool usePreview = false;

        /// <summary>
        /// INTERAL: Indicates whether the mask has a preview renderer component.
        /// </summary>
        public bool HasMaskPreview
        {
            get
            {
                if (maskPreviewRenderer != null) return true;
                else return false;
            }
            private set { }
        }

        /// <summary>
        /// INTERAL: Disables the mask preview when the application is playing.
        /// </summary>
        public bool OnlyEditorPreview
        {
            get
            {
                return onlyEditorPreview;
            }
            set
            {
                onlyEditorPreview = value;
            }
        }
        [SerializeField, Tooltip("Disables the mask preview when the application is playing.")]
        protected bool onlyEditorPreview = false;

        /// <summary>
        /// INTERAL: Mask preview mesh filter component.
        /// </summary>
        public MeshFilter maskPreviewFilter;

        /// <summary>
        /// INTERAL: Mask preview mesh renderer component.
        /// </summary>
        public MeshRenderer maskPreviewRenderer;

        #endregion

        #region privateMethods

        private void Start()
        {
            if (onlyEditorPreview && Application.isPlaying)
            {
                if (maskPreviewRenderer) maskPreviewRenderer.enabled = false;
            }
            else
            {
                if (maskPreviewRenderer) maskPreviewRenderer.enabled = true;
            }
        }

        private void Update()
        {
            if(!HasMaskPreview && UsePreview)
            {
                UpdatePreview();
            }
        }

        #endregion

        #region InternalMethods


        /// <summary>
        /// INTERAL: Updates the collider based on the mask type. 
        /// </summary>
        public void UpdatePreview()
        {
            DestroyPreview();

            if (gameObject.GetComponent<MeshFilter>() != null)
            {
                maskPreviewFilter = gameObject.GetComponent<MeshFilter>();
            }
            else
            {
                maskPreviewFilter = gameObject.AddComponent<MeshFilter>();
            }

            if (gameObject.GetComponent<MeshRenderer>() != null)
            {
                maskPreviewRenderer = gameObject.GetComponent<MeshRenderer>();
            }
            else
            {
                maskPreviewRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            maskPreviewRenderer.material = maskSettings.PreviewMaterial;
            maskPreviewRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            maskPreviewRenderer.receiveShadows = false;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(gameObject);
#endif

            switch (type)
            {
                case InteractiveEffectMaskType.Plane:
                    maskPreviewFilter.sharedMesh = maskSettings.PreviewPlaneMesh;
                    break;
                case InteractiveEffectMaskType.Box:
                    maskPreviewFilter.sharedMesh = maskSettings.PreviewBoxMesh;
                    break;
                case InteractiveEffectMaskType.Ellipse:
                    maskPreviewFilter.sharedMesh = maskSettings.PreviewSphereMesh;
                    break;
            }

        }

        /// <summary>
        /// INTERAL: Destroys the existing preview
        /// </summary>
        public void DestroyPreview()
        {
            if (Application.isPlaying)
            {
                Destroy(maskPreviewFilter);
                Destroy(maskPreviewRenderer);
            }
            else
            {
                DestroyImmediate(maskPreviewFilter);
                DestroyImmediate(maskPreviewRenderer);
            }

            maskPreviewFilter = null;
            maskPreviewRenderer = null;
        }

        #endregion

    }
}