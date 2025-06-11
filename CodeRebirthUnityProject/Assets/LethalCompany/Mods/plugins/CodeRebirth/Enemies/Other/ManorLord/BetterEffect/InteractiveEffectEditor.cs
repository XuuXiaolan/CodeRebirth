using UnityEditor;
using UnityEngine;
using INab.CommonVFX;

namespace INab.Common
{
    [CustomEditor(typeof(InteractiveEffect))]
    [CanEditMultipleObjects]
    public class InteractiveEffectEditor : Editor
    {
        #region Properties

        private SerializedProperty effectCurve;
        private SerializedProperty duration;
        private SerializedProperty isFinished;
        private SerializedProperty resetOnInitial;

        private SerializedProperty useVFXGraphEffect;
        private SerializedProperty vfxEventOffset;

        private SerializedProperty visualEffect;
        private SerializedProperty propertyBinder;
        private SerializedProperty meshTransform;
        private SerializedProperty meshRenderer;

        private SerializedProperty sampleCountMultiplier;

        private SerializedProperty mask;
        private SerializedProperty maskType;
        private SerializedProperty materials;
        private SerializedProperty useInstancedMaterials;

        private SerializedProperty controlMaterialsProperties;
        private SerializedProperty shaderType;

        
        private SerializedProperty propertiesScaleRate;

        private SerializedProperty invert;
        private SerializedProperty guideTexture;
        private SerializedProperty guideTiling;
        private SerializedProperty guideStrength;
        private SerializedProperty useBackColor;
        private SerializedProperty backColor;

        private SerializedProperty burnHardness;
        private SerializedProperty burnOffset;
        private SerializedProperty emberOffset;
        private SerializedProperty emberSmoothness;
        private SerializedProperty emberWidth;
        private SerializedProperty emberColor;
        private SerializedProperty burnColor;

        private SerializedProperty useDithering;
        private SerializedProperty edgeColor;
        private SerializedProperty edgeWidth;
        private SerializedProperty edgeSmoothness;
        private SerializedProperty affectAlbedo;
        private SerializedProperty glareColor;
        private SerializedProperty glareGuideStrength;
        private SerializedProperty glareWidth;
        private SerializedProperty glareSmoothness;
        private SerializedProperty glareOffset;

        private SerializedProperty effectStateTest;

        private SerializedProperty usePositionTransform;
        private SerializedProperty initialPosition;
        private SerializedProperty finalPosition;

        private SerializedProperty useScaleTransform;
        private SerializedProperty initialScale;
        private SerializedProperty finalScale;

        private SerializedProperty useRotationTransform;
        private SerializedProperty initialRotation;
        private SerializedProperty finalRotation;

        // Flags to control delayed updates
        public bool waitForType = false;
        public bool waitForEffectStateTest = false;
        public bool waitForSampleCountMultiplier = false;

        protected InteractiveEffect ourTarget;

        protected GUIStyle indentedButtonStyle;
        protected GUIStyle indentedButtonStyleDouble;
        protected GUIStyle defaultButtonStyle;
        protected GUIStyle centeredBoldLabel;

        // We could use static, but it gets resetted when game is played
        //protected static bool FoldoutInteractiveSettings = true;
        public static bool FoldoutInteractiveSettings
        {
            get { return SessionState.GetBool("FoldoutInteractiveSettings", false); }
            set { SessionState.SetBool("FoldoutInteractiveSettings", value); }
        }

        public static bool FoldoutInteractiveTesting
        {
            get { return SessionState.GetBool("FoldoutInteractiveTesting", false); }
            set { SessionState.SetBool("FoldoutInteractiveTesting", value); }
        }

        public static bool FoldoutGeneral
        {
            get { return SessionState.GetBool("FoldoutGeneral", false); }
            set { SessionState.SetBool("FoldoutGeneral", value); }
        }

        public static bool FoldoutMaterialsSettings
        {
            get { return SessionState.GetBool("FoldoutMaterialsSettings", false); }
            set { SessionState.SetBool("FoldoutMaterialsSettings", value); }
        }

        #endregion

        #region Methods

        public virtual void OnEnable()
        {
            useVFXGraphEffect = serializedObject.FindProperty("useVFXGraphEffect");
            vfxEventOffset = serializedObject.FindProperty("vfxEventOffset");


            visualEffect = serializedObject.FindProperty("visualEffect");
            propertyBinder = serializedObject.FindProperty("propertyBinder");
            meshTransform = serializedObject.FindProperty("meshTransform");
            meshRenderer = serializedObject.FindProperty("meshRenderer");

            effectCurve = serializedObject.FindProperty("effectCurve");
            duration = serializedObject.FindProperty("duration");
            isFinished = serializedObject.FindProperty("isFinished");
            resetOnInitial = serializedObject.FindProperty("resetOnInitial");

            sampleCountMultiplier = serializedObject.FindProperty("sampleCountMultiplier");

            mask = serializedObject.FindProperty("mask");
            maskType = serializedObject.FindProperty("maskType");
            materials = serializedObject.FindProperty("materials");
            useInstancedMaterials = serializedObject.FindProperty("useInstancedMaterials");
            
            controlMaterialsProperties = serializedObject.FindProperty("controlMaterialsProperties");
            shaderType = serializedObject.FindProperty("shaderType");


            propertiesScaleRate = serializedObject.FindProperty("propertiesScaleRate");

            invert = serializedObject.FindProperty("invert");
            backColor = serializedObject.FindProperty("backColor");
            useBackColor = serializedObject.FindProperty("useBackColor");

            guideTexture = serializedObject.FindProperty("guideTexture");
            guideTiling = serializedObject.FindProperty("guideTiling");
            guideStrength = serializedObject.FindProperty("guideStrength");
            burnHardness = serializedObject.FindProperty("burnHardness");
            burnOffset = serializedObject.FindProperty("burnOffset");
            emberOffset = serializedObject.FindProperty("emberOffset");
            emberSmoothness = serializedObject.FindProperty("emberSmoothness");
            emberWidth = serializedObject.FindProperty("emberWidth");
            emberColor = serializedObject.FindProperty("emberColor");
            burnColor = serializedObject.FindProperty("burnColor");

            useDithering = serializedObject.FindProperty("useDithering");
            edgeColor = serializedObject.FindProperty("edgeColor");
            edgeWidth = serializedObject.FindProperty("edgeWidth");
            edgeSmoothness = serializedObject.FindProperty("edgeSmoothness");
            affectAlbedo = serializedObject.FindProperty("affectAlbedo");
            glareColor = serializedObject.FindProperty("glareColor");
            glareGuideStrength = serializedObject.FindProperty("glareGuideStrength");
            glareWidth = serializedObject.FindProperty("glareWidth");
            glareSmoothness = serializedObject.FindProperty("glareSmoothness");
            glareOffset = serializedObject.FindProperty("glareOffset");

            effectStateTest = serializedObject.FindProperty("effectStateTest");

            usePositionTransform = serializedObject.FindProperty("usePositionTransform");
            initialPosition = serializedObject.FindProperty("initialPosition");
            finalPosition = serializedObject.FindProperty("finalPosition");

            useScaleTransform = serializedObject.FindProperty("useScaleTransform");
            initialScale = serializedObject.FindProperty("initialScale");
            finalScale = serializedObject.FindProperty("finalScale");

            useRotationTransform = serializedObject.FindProperty("useRotationTransform");
            initialRotation = serializedObject.FindProperty("initialRotation");
            finalRotation = serializedObject.FindProperty("finalRotation");
        }

        public void UpdateKeywords()
        {
            if (waitForType)
            {
                ourTarget.ChangeMaskType(ourTarget.MaskType);
                waitForType = false;
            }

            if (waitForEffectStateTest)
            {
                ourTarget.UpdateMaskTransform(ourTarget.EffectStateTest);
                waitForEffectStateTest = false;
            }
        }

        public override void OnInspectorGUI()
        {
            ourTarget = target as InteractiveEffect;
            serializedObject.Update();

            // Custom inspector implementation
            Inspector();

            serializedObject.ApplyModifiedProperties();

            // Update materials based on property changes
            UpdateKeywords();
        }

        public virtual void Inspector()
        {
            defaultButtonStyle = new GUIStyle(EditorStyles.miniButton);
            defaultButtonStyle.margin = new RectOffset(0, 0, 0, 0);

            indentedButtonStyle = new GUIStyle(EditorStyles.miniButton);
            indentedButtonStyle.margin = new RectOffset(35, 0, 0, 0);
            indentedButtonStyle.fontStyle = FontStyle.Bold;

            indentedButtonStyleDouble = new GUIStyle(EditorStyles.miniButton);
            indentedButtonStyleDouble.margin = new RectOffset(48, 0, 0, 0);

            centeredBoldLabel = new GUIStyle(EditorStyles.boldLabel);
            centeredBoldLabel.alignment = TextAnchor.MiddleCenter;

            DrawGeneral();
            DrawMaterials();
            DrawTesting();
            DrawInteractiveSettings();
            DrawMaterialsSettings();
        }

        #endregion

        #region InspectorMethods

        // TODO: better helping boxes
        private void DrawGeneral()
        {
            FoldoutGeneral = EditorGUILayout.BeginFoldoutHeaderGroup(FoldoutGeneral, "General", EditorStyles.foldoutHeader);
            if(FoldoutGeneral)
            {
                EditorGUI.indentLevel++;

                // ==================================================================
                // Visual Effect Graph 
                // ==================================================================

                EditorGUILayout.PropertyField(useVFXGraphEffect);

                if(useVFXGraphEffect.boolValue)
                {
                    EditorGUILayout.LabelField("Visual Effect Graph", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(meshRenderer);
                    if(!ourTarget.IsMeshReadable)
                    {
                        EditorGUILayout.HelpBox("The mesh in the Mesh Renderer is not readable. Enable 'Read/Write' in the Mesh Inspector to fix this.", MessageType.Error);

                    }
                    if (ourTarget.meshRenderer == null)
                    {
                        if (GUILayout.Button("Find Renderer In Parent", indentedButtonStyleDouble))
                        {
                            ourTarget._FindRendererInParent();
                        }
                        EditorGUILayout.HelpBox("Please assign a Mesh Renderer.", MessageType.Error);
                        EditorGUILayout.Space();
                    }

                    EditorGUILayout.PropertyField(meshTransform);
                    if (ourTarget.meshTransform == null)
                    {
                        string message = "Assign a Mesh Transform (transform of the mesh renderer).";
                        if (ourTarget.IsSkinnedMesh)
                        {
                            message = "Assign the Mesh Transform (the root bone of the skinned mesh).";
                        }
                        EditorGUILayout.HelpBox(message, MessageType.Error);
                        EditorGUILayout.Space();
                    }

                    EditorGUILayout.PropertyField(visualEffect);

                    if (ourTarget.visualEffect == null)
                    {
                        if (GUILayout.Button("Setup Vfx Graph Game Object", indentedButtonStyle))
                        {
                            ourTarget._SetupVfxGraphGameObject();
                        }
                        EditorGUILayout.HelpBox("Please assign a Visual Effect Graph.", MessageType.Error);
                    }
                    else if (!ourTarget.visualEffect.HasGraphicsBuffer(VFXUniformMeshBaker.GraphicsBufferName))
                    {
                        EditorGUILayout.HelpBox("Change the VFX graph asset template to the one provided by INab Studio.", MessageType.Error);

                    }
                    EditorGUILayout.PropertyField(vfxEventOffset);


                    EditorGUILayout.PropertyField(propertyBinder);
                    if (ourTarget.propertyBinder == null)
                    {
                        EditorGUILayout.HelpBox("Please assign a Property Binder.", MessageType.Error);
                        EditorGUILayout.Space();
                    }

                    EditorGUILayout.PropertyField(sampleCountMultiplier);


                    Color oldGUIColor = GUI.backgroundColor;
                    int sampleCount = ourTarget.meshBaker.SampleCount;

                    float colorsIntensity = 2f;

                    Gradient gradient = new Gradient();
                    GradientColorKey[] colorKeys = new GradientColorKey[2];
                    colorKeys[0] = new GradientColorKey(Color.green * colorsIntensity, 0.35f); // Lower count color
                    colorKeys[1] = new GradientColorKey(Color.red * colorsIntensity, 1f);   // 20000 count color
                    GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
                    alphaKeys[0] = new GradientAlphaKey(1f, 0f);
                    alphaKeys[1] = new GradientAlphaKey(1f, 1f);
                    gradient.SetKeys(colorKeys, alphaKeys);

                    float t = Mathf.Clamp01(sampleCount / 20000f);
                    Color sampleCountColor = gradient.Evaluate(t);
                    GUI.backgroundColor = sampleCountColor;

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.IntField("Sample Count", sampleCount);
                    EditorGUI.EndDisabledGroup();
                    GUI.backgroundColor = oldGUIColor;

                    if (sampleCount > 20000)
                    {
                        EditorGUILayout.HelpBox("Sample count exceeds 20,000. Reduce the Sample Count Multiplier to lower the sample count.", MessageType.Warning);
                    }


                    if (ourTarget.sampleCountMultiplier != sampleCountMultiplier.floatValue)
                    {
                        waitForSampleCountMultiplier = true;
                    }

                    if (waitForSampleCountMultiplier)
                    {
                        if (GUILayout.Button("Bake", indentedButtonStyle))
                        {
                            ourTarget._BakeUniformMesh();
                            waitForSampleCountMultiplier = false;
                        }
                    }
                    EditorGUILayout.Space();
                    EditorGUI.indentLevel--;
                }

                // ==================================================================

                // ==================================================================
                // Interactive Mask
                // ==================================================================

                EditorGUILayout.LabelField("Interactive Mask", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(mask);
                if (ourTarget.mask == null)
                {
                    if (GUILayout.Button("Setup Mask Game Object", indentedButtonStyle))
                    {
                        ourTarget._SetupMaskGameObject();
                    }
                    EditorGUILayout.HelpBox("Please assign a Mask.", MessageType.Error);

                }
                EditorGUILayout.PropertyField(maskType);

                EditorGUILayout.Space();
                EditorGUI.indentLevel--;
                // ==================================================================


                EditorGUI.indentLevel--;

                if (((int)ourTarget.MaskType) != maskType.enumValueIndex)
                {
                    waitForType = true;
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        private void DrawMaterials()
        {
            if (FoldoutGeneral)
            {
                EditorGUI.indentLevel++;

                // ==================================================================
                // Materials list
                // ==================================================================

                EditorGUILayout.PropertyField(useInstancedMaterials);
                if(useInstancedMaterials.boolValue)
                {
                    EditorGUILayout.HelpBox("The materials listed below are for editor testing only. Instanced materials will be automatically found at runtime. ", MessageType.Info);
                }

                EditorGUILayout.PropertyField(materials, true);

                EditorGUI.indentLevel++;

                bool hasMaterials = ourTarget.materials.Count > 0 && ourTarget.materials[0] != null;

                if (!hasMaterials)
                {
                    EditorGUILayout.HelpBox("No materials found in the materials list. Please add materials.", MessageType.Error);
                }

                if (!ourTarget._DoMaterialsUseProperShaders())
                {
                    EditorGUILayout.HelpBox("Not all materials from the list use the Interactive Effect shaders. Change the shaders used by the materials to 'Interactive Effect Burn' or 'Interactive Effect Smooth'.", MessageType.Error);
                }

                using (var horizontalScope = new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Find In Renderer", indentedButtonStyleDouble))
                    {
                        Undo.RecordObject(ourTarget, "Find Materials In Renderer");
                        ourTarget.GetRendererMaterials();
                    }

                    if (GUILayout.Button("Clear", defaultButtonStyle))
                    {
                        Undo.RecordObject(ourTarget, "Clear Materials");
                        ourTarget._ClearMaterialsList();
                    }
                }

                EditorGUILayout.Space();
                EditorGUI.indentLevel--;

                // ==================================================================

                // ==================================================================
                // Setup 
                // ==================================================================

                EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);

                if (GUILayout.Button("Update And Setup Effect", indentedButtonStyleDouble))
                {
                    ourTarget.UpdateAndSetupEffect();
                }
                EditorGUILayout.HelpBox("After configuring the settings, or if something isn't working, try clicking the button.", MessageType.Info);

                EditorGUILayout.Space();
                // ==================================================================

                EditorGUI.indentLevel--;
            }
        }
        private void DrawTesting()
        {
            FoldoutInteractiveTesting = EditorGUILayout.BeginFoldoutHeaderGroup(FoldoutInteractiveTesting, "Testing", EditorStyles.foldoutHeader);
            if (FoldoutInteractiveTesting)
            {
                EditorGUI.indentLevel++;

                // ==================================================================
                // Visual Effect Graph Events
                // ==================================================================

                if (useVFXGraphEffect.boolValue)
                {

                    EditorGUILayout.LabelField("Visual Effect Graph Events", EditorStyles.boldLabel);

                    using (var horizontalScope = new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Play Event", indentedButtonStyleDouble))
                        {
                            ourTarget.SendPlayEvent();
                        }

                        if (GUILayout.Button("Stop Event", defaultButtonStyle))
                        {
                            ourTarget.SendStopEvent();
                        }
                    }
                    EditorGUILayout.Space();
                }
                // ==================================================================

                // ==================================================================
                // Other
                // ==================================================================

                EditorGUILayout.LabelField("Other", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(effectStateTest);

                if (ourTarget.EffectStateTest != effectStateTest.floatValue)
                {
                    waitForEffectStateTest = true;
                }

                Color oldGUIColor = GUI.backgroundColor;

                if (ourTarget.EditorCoroutine == null)
                {
                    if (GUILayout.Button("Play Effect", indentedButtonStyleDouble))
                    {
                        ourTarget.PlayEffect();
                    }
                    if (GUILayout.Button("Effect Loop", indentedButtonStyleDouble))
                    {
                        ourTarget.EditorCoroutine = ourTarget.StartCoroutine(ourTarget.AutoEffectCoroutine());
                    }
                }
                else
                {
                    GUI.backgroundColor = Color.red;

                    if (GUILayout.Button("Stop " + "Effect Loop", indentedButtonStyleDouble))
                    {
                        if (ourTarget.EditorCoroutine != null)
                        {
                            ourTarget.StopCoroutine(ourTarget.EditorCoroutine);
                            ourTarget.StopAllCoroutines();
                        }
                        ourTarget.EditorCoroutine = null;
                    }

                    GUI.backgroundColor = oldGUIColor;
                }

                EditorGUILayout.Space();
                EditorGUI.indentLevel--;
                // ==================================================================

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();


        }

        // TODO add indedts etc to make the editor unified

        private void DrawInteractiveSettings()
        {
            FoldoutInteractiveSettings = EditorGUILayout.BeginFoldoutHeaderGroup(FoldoutInteractiveSettings, "Interactive Settings", EditorStyles.foldoutHeader);
            if (FoldoutInteractiveSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(effectCurve);
                EditorGUILayout.PropertyField(duration);
                EditorGUILayout.PropertyField(resetOnInitial);

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(isFinished);
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Mask Transform", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(usePositionTransform);
                if (usePositionTransform.boolValue)
                {
                    EditorGUILayout.PropertyField(initialPosition);
                    EditorGUILayout.PropertyField(finalPosition);
                    EditorGUILayout.Space();
                }

                EditorGUILayout.PropertyField(useScaleTransform);
                if (useScaleTransform.boolValue)
                {
                    EditorGUILayout.PropertyField(initialScale);
                    EditorGUILayout.PropertyField(finalScale);
                    EditorGUILayout.Space();
                }

                EditorGUILayout.PropertyField(useRotationTransform);
                if (useRotationTransform.boolValue)
                {
                    EditorGUILayout.PropertyField(initialRotation);
                    EditorGUILayout.PropertyField(finalRotation);
                    EditorGUILayout.Space();
                }
                EditorGUILayout.Space();


                if (ourTarget.initialPosition != initialPosition.vector3Value)
                {
                    waitForEffectStateTest = true;
                }
                if (ourTarget.finalPosition != finalPosition.vector3Value)
                {
                    waitForEffectStateTest = true;
                }

                if (ourTarget.initialScale != initialScale.vector3Value)
                {
                    waitForEffectStateTest = true;
                }
                if (ourTarget.finalScale != finalScale.vector3Value)
                {
                    waitForEffectStateTest = true;
                }

                if (ourTarget.initialRotation != initialRotation.vector3Value)
                {
                    waitForEffectStateTest = true;
                }
                if (ourTarget.finalRotation != finalRotation.vector3Value)
                {
                    waitForEffectStateTest = true;
                }

                if (GUILayout.Button("Set Initial Transform", indentedButtonStyleDouble))
                {
                    Undo.RecordObject(ourTarget, "Set Initial Transform");
                    ourTarget._SetInitialMaskTransform();
                }

                if (GUILayout.Button("Set Final Transform", indentedButtonStyleDouble))
                {
                    Undo.RecordObject(ourTarget, "Set Final Transform");
                    ourTarget._SetFinalMaskTransform();
                }
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Transforms are in local space and relative to the mask's parent.", MessageType.Info);
                EditorGUI.indentLevel--;

                EditorGUILayout.Space();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

        }


        private void DrawMaterialsSettings()
        {
            FoldoutMaterialsSettings = EditorGUILayout.BeginFoldoutHeaderGroup(FoldoutMaterialsSettings, "Materials Settings", EditorStyles.foldoutHeader);
            if (FoldoutMaterialsSettings)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(controlMaterialsProperties);

                if (controlMaterialsProperties.boolValue)
                {
                    string shaderTypeName = "Interactive Effect ";

                    string alphaThresholdMessage = " Alpha Clipping Threshold must be set to a very small value, like 0.0001.";
                    switch (shaderType.intValue)
                    {
                        case 0:
                            shaderTypeName = "Burn";
                            break;
                        case 1:
                            shaderTypeName = "Smooth";
                            if (!useDithering.boolValue)
                            {
                                EditorGUILayout.HelpBox("If useDithering is off, material surface maskType must be Transparent.", MessageType.Info);
                            }
                            break;
                    }

                    EditorGUILayout.HelpBox("All materials need to use Interactive Effect " + shaderTypeName + " shader.", MessageType.Info);
                    EditorGUILayout.HelpBox("Alpha Clipping needs to be on." + alphaThresholdMessage, MessageType.Info);
                }

                EditorGUILayout.Space();

                if (ourTarget.ControlMaterialsProperties)
                {
                EditorGUILayout.Space();
                    if (ourTarget.visualEffect != null)
                    {
                        bool hasMaterials = ourTarget.materials.Count > 0 && ourTarget.materials[0] != null;
                        EditorGUI.BeginDisabledGroup(!hasMaterials);
                        if (GUILayout.Button("Pass Material Properties To Vfx Graph", indentedButtonStyleDouble))
                        {
                            ourTarget.PassMaterialPropertiesToGraph();
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.PropertyField(propertiesScaleRate);
                    
                    if (ourTarget.ShaderType == 0)
                    {
                        DrawBurnTypeProperties();
                    }
                    else
                    {
                        DrawSmoothTypeProperties();
                    }
                }
                EditorGUILayout.Space();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

        }
        private void DrawBurnTypeProperties()
        {
            EditorGUILayout.LabelField("Edge Properties", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(invert);
            EditorGUILayout.PropertyField(shaderType);

            EditorGUILayout.PropertyField(guideTexture);
            EditorGUILayout.PropertyField(guideTiling);
            EditorGUILayout.PropertyField(guideStrength);

            EditorGUILayout.PropertyField(burnHardness);
            EditorGUILayout.PropertyField(burnOffset);
            EditorGUILayout.PropertyField(burnColor);
            EditorGUILayout.PropertyField(emberOffset);
            EditorGUILayout.PropertyField(emberSmoothness);
            EditorGUILayout.PropertyField(emberWidth);
            EditorGUILayout.PropertyField(emberColor);
            EditorGUILayout.PropertyField(useBackColor);
            if (useBackColor.boolValue) EditorGUILayout.PropertyField(backColor);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
        private void DrawSmoothTypeProperties()
        {
            EditorGUILayout.LabelField("Edge Properties", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(invert);
            EditorGUILayout.PropertyField(shaderType);

            EditorGUILayout.PropertyField(guideTexture);
            EditorGUILayout.PropertyField(guideTiling);
            EditorGUILayout.PropertyField(guideStrength);

            EditorGUILayout.PropertyField(useDithering);
            EditorGUILayout.PropertyField(edgeColor);
            EditorGUILayout.PropertyField(edgeWidth);
            EditorGUILayout.PropertyField(edgeSmoothness);
            EditorGUILayout.PropertyField(affectAlbedo);
            EditorGUILayout.PropertyField(glareColor);
            EditorGUILayout.PropertyField(glareGuideStrength);
            EditorGUILayout.PropertyField(glareWidth);
            EditorGUILayout.PropertyField(glareSmoothness);
            EditorGUILayout.PropertyField(glareOffset);

            EditorGUILayout.PropertyField(useBackColor);
            if (useBackColor.boolValue) EditorGUILayout.PropertyField(backColor);

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        #endregion

    }
}