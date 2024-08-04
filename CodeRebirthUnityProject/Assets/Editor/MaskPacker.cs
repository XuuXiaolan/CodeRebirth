/*
 * (CC0 1.0 Universal) You're free to use these game assets in any project, personal or commercial.
 * There's no need to ask permission before using these. 
 * Giving attribution is not required, but is greatly appreciated!
 * 
 * Do what you want with it, use it, abuse it, I hope this
 * is useful to you and improves your workflow.
*/
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Rendering;

public class MaskPacker : EditorWindow
{
    [SerializeField]
    private ComputeShader fastPack;                 //Use a compute shader to greatly speed up packing time

    private static EditorWindow window;             //Main Window


    private Texture2D inputMetallic;                //Input textures
    private Texture2D inputAmbientOcclusion;
    private Texture2D inputDetailMask;
    private Texture2D inputSmooth_Rough;

    private Texture2D previewAlbedo, previewNormal;

    private RenderTexture blitMetallic;
    private RenderTexture blitAmbientOcclusion;
    private RenderTexture blitDetailMask;
    private RenderTexture blitSmooth_Rough;

    private float defaultMetal;                     //Slider values if no map is provided
    private float defaultAO = 1f;
    private float defaultDetail;
    private float defaultSmooth;

    private Vector2 scrollPos;                      //For Scrolling
    private GUIStyle regularStyle, regularSmall, smallWarn, regularWarn;    //Font styles
    private RenderTexture packedTexture;
    private Texture2D finalTexture;
    private Vector2Int textureDimensions;
    private bool isRough;
    private Editor previewMatViewer;
    private Material previewMat;
    private bool showPreview = true;
    private bool hdrpShaderFound;

    private readonly string hdrpShaderPath = "HDRP/Lit";
    //Show the window
    [MenuItem("Tools/Mask Packer")]
    public static void ShowWindow()
    {
        window = GetWindow(typeof(MaskPacker), false, "Mask Packer");
    }
    private void OnEnable()
    {
        InitGUIStyles();
        textureDimensions = Vector2Int.zero;
        Shader hdrp = Shader.Find(hdrpShaderPath);
        if(hdrp != null)
            previewMat = new Material(hdrp);
        hdrpShaderFound = previewMat != null;
    }
    //If for some reason the window becomes null get it again.
    private void OnInspectorUpdate()
    {
        if (!window)
            window = GetWindow(typeof(MaskPacker), false, "Mask Packer");
    }

    private void OnGUI()
    {
        
        if (window)
        {
            window.Repaint();
            GUILayout.BeginArea(new Rect(0, 0, window.position.size.x, window.position.size.y));
            GUILayout.BeginVertical();
            scrollPos = GUILayout.BeginScrollView(scrollPos, false, true, GUILayout.ExpandHeight(true));
        }

        if (!inputMetallic && !inputAmbientOcclusion && !inputDetailMask && !inputSmooth_Rough)
            textureDimensions = Vector2Int.zero;

        #region Info/Instructions
        GUILayout.Space(10f);
        GUILayout.Label("Add grayscale textures to be packed", regularStyle);
        GUILayout.Label("Each texture must have the same dimensions", regularStyle);
        GUILayout.Label("Ensure your textures have sRGB unchecked.", smallWarn);
        #endregion

        #region Metallic
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Space(10f);
        inputMetallic = (Texture2D)EditorGUILayout.ObjectField("Metallic (R)", inputMetallic, typeof(Texture2D), false);
        if (!inputMetallic)
        {
            GUILayout.Label("No Metallic input, use slider to set value", regularSmall);
            defaultMetal = EditorGUILayout.Slider(defaultMetal, 0f, 1f);
            
        }
        else
        {
            if (textureDimensions != Vector2Int.zero && (inputMetallic.width != textureDimensions.x || inputMetallic.height != textureDimensions.y))
            {
                inputMetallic = null;
            }
            if (textureDimensions == Vector2Int.zero)
            {
                textureDimensions.x = inputMetallic.width;
                textureDimensions.y = inputMetallic.height;
            }
        }
        GUILayout.EndVertical();
        #endregion

        #region Ambient Occlusion
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Space(10f);
        inputAmbientOcclusion = (Texture2D)EditorGUILayout.ObjectField("Ambient Occlusion (G)", inputAmbientOcclusion, typeof(Texture2D), false);
        if (!inputAmbientOcclusion)
        {
            GUILayout.Label("No Ambient Occlusion input, use slider to set value", regularSmall);
            defaultAO = EditorGUILayout.Slider(defaultAO, 0f, 1f);

        }
        else
        {
            if (textureDimensions != Vector2Int.zero && (inputAmbientOcclusion.width != textureDimensions.x || inputAmbientOcclusion.height != textureDimensions.y))
            {
                inputAmbientOcclusion = null;
            }
            if (textureDimensions == Vector2Int.zero)
            {
                textureDimensions.x = inputAmbientOcclusion.width;
                textureDimensions.y = inputAmbientOcclusion.height;
            }
        }
        GUILayout.EndVertical();
        #endregion

        #region Detail Mask
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Space(10f);
        inputDetailMask = (Texture2D)EditorGUILayout.ObjectField("Detail Mask (B)", inputDetailMask, typeof(Texture2D), false);
        if (!inputDetailMask)
        {
            GUILayout.Label("No Detail input, use slider to set value", regularSmall);
            defaultDetail = EditorGUILayout.Slider(defaultDetail, 0f, 1f);

        }
        else
        {
            if (textureDimensions != Vector2Int.zero && (inputDetailMask.width != textureDimensions.x || inputDetailMask.height != textureDimensions.y))
            {
                inputDetailMask = null;
            }
            if (textureDimensions == Vector2Int.zero)
            {
                textureDimensions.x = inputDetailMask.width;
                textureDimensions.y = inputDetailMask.height;
            }
        }
        GUILayout.EndVertical();
        #endregion

        #region Smoothness
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Space(10f);
        if (isRough = EditorGUILayout.Toggle("Input Is Roughness Map", isRough))
            inputSmooth_Rough = (Texture2D)EditorGUILayout.ObjectField("Roughness Map (A)", inputSmooth_Rough, typeof(Texture2D), false);
        else
            inputSmooth_Rough = (Texture2D)EditorGUILayout.ObjectField("Smoothness Map (A)", inputSmooth_Rough, typeof(Texture2D), false);
        if (!inputSmooth_Rough)
        {
            GUILayout.Label("No Rough/Smooth input, use slider to set value", regularSmall);
            defaultSmooth = EditorGUILayout.Slider(defaultSmooth, 0f, 1f);
        }
        else
        {
            if (textureDimensions != Vector2Int.zero && (inputSmooth_Rough.width != textureDimensions.x || inputSmooth_Rough.height != textureDimensions.y))
            {
                inputSmooth_Rough = null;
            }
            if (textureDimensions == Vector2Int.zero)
            {
                textureDimensions.x = inputSmooth_Rough.width;
                textureDimensions.y = inputSmooth_Rough.height;
            }
        }
        GUILayout.EndVertical();
        #endregion

        #region Buttons
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Space(10f);
        GUILayout.Label("You may need to change the max size of the newly created LitMask after saving.", regularSmall);
        if (hdrpShaderFound)
        {
            if (GUILayout.Button("Update Preview") && textureDimensions != Vector2Int.zero)
            {
                EditorUtility.DisplayProgressBar("Packing Metallic, please wait...", "", 0f);
                CreatePackedTexture();
                EditorUtility.ClearProgressBar();
            }
        }
        if (GUILayout.Button("Pack Textures") && textureDimensions != Vector2Int.zero)
        { 
            CreatePackedTexture();
            SaveTexture();
            EditorUtility.ClearProgressBar();
        }
        if (GUILayout.Button("Clear All"))
        {
            inputMetallic = inputAmbientOcclusion = inputDetailMask = inputSmooth_Rough = previewAlbedo = previewNormal = null;
            previewMatViewer = null;
        }
        GUILayout.Space(10f);
        GUILayout.EndVertical();
        #endregion

        #region HDRP Present
        if (hdrpShaderFound)
        {
            #region Toggle Preview
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(10f);
            showPreview = GUILayout.Toggle(showPreview, "Show Preview");
            GUILayout.Space(10f);
            GUILayout.EndVertical();
            #endregion

            #region Preview Albedo and Normal

            if (showPreview)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("Albedo and Normal maps are for previewing and don't get packed.", regularSmall);
                previewAlbedo = (Texture2D)EditorGUILayout.ObjectField("Preview Albedo (optional)", previewAlbedo, typeof(Texture2D), false);
                GUILayout.Space(10f);
                previewNormal = (Texture2D)EditorGUILayout.ObjectField("Preview Normal (optional)", previewNormal, typeof(Texture2D), false);
                GUILayout.EndVertical();
            }
            #endregion

            #region Preview Box
            if (previewMat != null && previewMatViewer != null && showPreview)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("Preview", regularStyle);
                GUILayout.Space(10f);
                previewMatViewer.OnPreviewGUI(GUILayoutUtility.GetRect(256, 256), EditorStyles.objectField);
                GUILayout.Label("Preview is displayed with Metallic multiplier set to 1.", regularSmall);
                GUILayout.Space(10f);
                GUILayout.EndVertical();
            }
            #endregion
        }
        #endregion
        else
        #region HDRP Not Present
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(20f);
            GUILayout.Label("Shaders/HDRP/Lit not found.\nYou can still compile maps, but a previewing is disabled (for now).", regularWarn);
            GUILayout.Space(20f);
            GUILayout.EndVertical();
        }
        #endregion

        if (window)
        {
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }

    private void CreatePackedTexture()
    {
        finalTexture = new Texture2D(textureDimensions.x, textureDimensions.y, TextureFormat.ARGB32, false, true);
        int blitKernel = fastPack.FindKernel("ChannelSet");

        #region Metallic
        EditorUtility.DisplayProgressBar("Packing Metallic, please wait...", "", 1f);
        blitMetallic = new RenderTexture(textureDimensions.x, textureDimensions.y, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        if (inputMetallic)
            Graphics.Blit(inputMetallic, blitMetallic);
        else
        {
            blitMetallic.enableRandomWrite = true;
            blitMetallic.Create();

            
            fastPack.SetTexture(blitKernel, "Mask", blitMetallic);
            fastPack.SetFloat("maskCol", defaultMetal);
            fastPack.Dispatch(blitKernel, textureDimensions.x, textureDimensions.y, 1);
        }
        #endregion

        #region Ambient Occlusion
        EditorUtility.DisplayProgressBar("Packing Ambient Occlusion, please wait...", "", 0.25f);
        blitAmbientOcclusion = new RenderTexture(textureDimensions.x, textureDimensions.y, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        if (inputAmbientOcclusion)
            Graphics.Blit(inputAmbientOcclusion, blitAmbientOcclusion);
        else
        {
            blitAmbientOcclusion.enableRandomWrite = true;
            blitAmbientOcclusion.Create();


            fastPack.SetTexture(blitKernel, "Mask", blitAmbientOcclusion);
            fastPack.SetFloat("maskCol", defaultAO);
            fastPack.Dispatch(blitKernel, textureDimensions.x, textureDimensions.y, 1);
        }
        #endregion

        #region DetailMask
        EditorUtility.DisplayProgressBar("Packing Detail, please wait...", "", 0.5f);
        blitDetailMask = new RenderTexture(textureDimensions.x, textureDimensions.y, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        if (inputDetailMask)
            Graphics.Blit(inputDetailMask, blitDetailMask);
        else
        {
            blitDetailMask.enableRandomWrite = true;
            blitDetailMask.Create();

            fastPack.SetTexture(blitKernel, "Mask", blitDetailMask);
            fastPack.SetFloat("maskCol", defaultDetail);
            fastPack.Dispatch(blitKernel, textureDimensions.x, textureDimensions.y, 1);
        }
        #endregion

        #region Smooth/Rough
        EditorUtility.DisplayProgressBar("Packing Smoothness/Roughness, please wait...", "", 0.75f);
        blitSmooth_Rough = new RenderTexture(textureDimensions.x, textureDimensions.y, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        if (inputSmooth_Rough)
            Graphics.Blit(inputSmooth_Rough, blitSmooth_Rough);
        else
        {
            blitSmooth_Rough.enableRandomWrite = true;
            blitSmooth_Rough.Create();

            fastPack.SetTexture(blitKernel, "Mask", blitSmooth_Rough);
            fastPack.SetFloat("maskCol", defaultSmooth);
            fastPack.Dispatch(blitKernel, textureDimensions.x, textureDimensions.y, 1);
        }
        #endregion

        #region Combining Maps
        EditorUtility.DisplayProgressBar("Combining Maps, please wait...", "", 1f);
        //Create the render texture
        if (textureDimensions != Vector2Int.zero)
        {
            packedTexture = new RenderTexture(textureDimensions.x, textureDimensions.x, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            packedTexture.enableRandomWrite = true;
            packedTexture.Create();



            int kernel = fastPack.FindKernel("CSMain");
            fastPack.SetTexture(kernel, "Result", packedTexture);
            fastPack.SetTexture(kernel, "metal", blitMetallic);
            fastPack.SetTexture(kernel, "ambient", blitAmbientOcclusion);
            fastPack.SetTexture(kernel, "detail", blitDetailMask);
            fastPack.SetTexture(kernel, "smooth", blitSmooth_Rough);

            fastPack.SetInt("isRough", isRough ? 1 : 0);
            fastPack.Dispatch(kernel, textureDimensions.x, textureDimensions.y, 1);


            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = packedTexture;
            finalTexture.ReadPixels(new Rect(0, 0, packedTexture.width, packedTexture.height), 0, 0);
            finalTexture.Apply();
            RenderTexture.active = previous;
        }
        #endregion

        if (showPreview && hdrpShaderFound)
        {
            GeneratePreview();
        }
    }
    private void SaveTexture()
    {
        var path = EditorUtility.SaveFilePanelInProject("Save Texture To Directory", "LitMask", "png", "Saved");
        var pngData = finalTexture.EncodeToPNG();

        if (path.Length != 0 && pngData != null)
        {
            File.WriteAllBytes(path, pngData);
            Debug.Log("Texture Saved to: " + path);
            AssetDatabase.Refresh();
        }
        else
            EditorUtility.ClearProgressBar();    
    }
    private void InitGUIStyles()
    {
        regularStyle = new GUIStyle();
        regularStyle.fontSize = 14;
        regularStyle.fontStyle = FontStyle.Normal;
        regularStyle.wordWrap = true;
        regularStyle.alignment = TextAnchor.MiddleCenter;
        if (EditorGUIUtility.isProSkin)
            regularStyle.normal.textColor = new Color(0.76f, 0.76f,0.76f, 1f);
        else
            regularStyle.normal.textColor = Color.black;

        regularSmall = new GUIStyle();
        regularSmall.fontSize = 12;
        regularSmall.fontStyle = FontStyle.Normal;
        regularSmall.wordWrap = true;
        regularSmall.alignment = TextAnchor.MiddleCenter;
        if (EditorGUIUtility.isProSkin)
            regularSmall.normal.textColor = new Color(0.76f, 0.76f, 0.76f, 1f);
        else
            regularSmall.normal.textColor = Color.black;

        smallWarn = new GUIStyle();
        smallWarn.fontSize = 12;
        smallWarn.fontStyle = FontStyle.Normal;
        smallWarn.wordWrap = true;
        smallWarn.alignment = TextAnchor.MiddleCenter;
        if (EditorGUIUtility.isProSkin)
            smallWarn.normal.textColor = new Color(0.90f, 0.65f, 0.10f, 1f);
        else
            smallWarn.normal.textColor = new Color(0.60f, 0.35f, 0.00f, 1f);

        regularWarn = new GUIStyle();
        regularWarn.fontSize = 14;
        regularWarn.fontStyle = FontStyle.Normal;
        regularWarn.wordWrap = true;
        regularWarn.alignment = TextAnchor.MiddleCenter;
        if (EditorGUIUtility.isProSkin)
            regularWarn.normal.textColor = new Color(0.90f, 0.65f, 0.10f, 1f);
        else
            regularWarn.normal.textColor = new Color(0.60f, 0.35f, 0.00f, 1f);
    }
    private void GeneratePreview()
    {
        if (previewAlbedo)
            previewMat.SetTexture("_BaseColorMap", previewAlbedo);

        if (previewNormal)
        {
            previewMat.EnableKeyword("_NORMALMAP");
            previewMat.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
            previewMat.SetTexture("_NormalMap", previewNormal);
        }
        previewMat.SetFloat("_Metallic", 1f);
        previewMat.EnableKeyword("_MASKMAP");
        previewMat.SetTexture("_MaskMap", finalTexture);

        previewMatViewer = Editor.CreateEditor(previewMat);
    }
}
