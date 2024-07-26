using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

public class CRBundeWindow : EditorWindow {
    internal class BundleBuildSettings {
        public string BundleName { get; private set; }

        public bool _foldOutOpen = false;
        public bool _build = false;

        public bool changedSinceLastBuild = true;

        public long totalSize = 0;
        public long lastBuildSize = 0;
        public long builtBundleSize = 0;
        
        internal BundleBuildSettings(string bundleName) {
            BundleName = bundleName;
            
            foreach(string asset in AssetDatabase.GetAssetPathsFromAssetBundle(bundleName)) {
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(asset);
                if (fileInfo.Exists)
                    totalSize += fileInfo.Length;
            }

            FileInfo bundleFileInfo = new(Path.Combine(buildOutputPath, bundleName));
            if (bundleFileInfo.Exists)
                builtBundleSize = bundleFileInfo.Length;
        }
    }

    static string GetReadableFileSize(long bytes) {
        if (bytes <= 0) return "N/A";
        
        string[] suffixes = { "B", "KB", "MB" };
        int i;
        double doubleBytes = bytes;

        for (i = 0; i < suffixes.Length && bytes >= 1024; i++, bytes /= 1024)
        {
            doubleBytes = bytes / 1024.0;
        }

        return string.Format("{0:0.##} {1}", doubleBytes, suffixes[i]);
    }
    
    internal static Dictionary<string, BundleBuildSettings> bundles = new();

    static string buildOutputPath = "";
    static bool buildOnlyChanged = false;

    static bool fileSizeChangesFoldoutOpen = false;
    
    [MenuItem("Code Rebirth/Bundle Builder")]
    static void Open() {
        GetWindow<CRBundeWindow>("CR Bundle Builder");

        buildOutputPath = EditorPrefs.GetString("build_output");
        buildOnlyChanged = EditorPrefs.GetBool("build_changed", false);
        
        Refresh();
    }

    static void Refresh() {
        Debug.Log("Refreshing bundler.");
        
        foreach (string bundle in AssetDatabase.GetAllAssetBundleNames()) {
            BundleBuildSettings settings = new BundleBuildSettings(bundle);
            settings._build = EditorPrefs.GetBool($"{bundle}_build", false);
            settings.changedSinceLastBuild = EditorPrefs.GetBool($"{bundle}_changed", true);
            settings.lastBuildSize = EditorPrefs.GetInt($"{bundle}_size", 0);
            bundles[bundle] = settings;
        }
    }
    
    void OnGUI() {
        if (GUILayout.Button("Refresh")) {
            Refresh();
        }

        
        
        foreach (BundleBuildSettings bundle in bundles.Values) {

            string prefix = bundle.changedSinceLastBuild ? "(*) " : "";
            
            bundle._foldOutOpen = EditorGUILayout.BeginFoldoutHeaderGroup(bundle._foldOutOpen, $"{prefix}{bundle.BundleName}");
            if (!bundle._foldOutOpen) {
                EditorGUILayout.EndFoldoutHeaderGroup();
                continue;
            }

            EditorGUILayout.LabelField("Total Size", GetReadableFileSize(bundle.totalSize));
            EditorGUILayout.LabelField("Previous Total Size", GetReadableFileSize(bundle.lastBuildSize));
            EditorGUILayout.LabelField("Built Bundle Size", GetReadableFileSize(bundle.builtBundleSize));
            
            if (buildOnlyChanged) GUI.enabled = false;
            bool build = EditorGUILayout.Toggle("Build", bundle._build);

            if (build != bundle._build) {
                bundle._build = build;
                EditorPrefs.SetBool($"{bundle.BundleName}_build", build);
            }

            GUI.enabled = true;
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        EditorGUILayout.Space();

        BundleBuildSettings[] changedSizeBundles = bundles.Values.Where(bundle => bundle.lastBuildSize != bundle.totalSize).ToArray();
        bool fileSizeChangeActuallyOpen = changedSizeBundles.Length != 0 && fileSizeChangesFoldoutOpen;
        if (changedSizeBundles.Length == 0) GUI.enabled = false;
        fileSizeChangesFoldoutOpen = EditorGUILayout.BeginFoldoutHeaderGroup(fileSizeChangesFoldoutOpen, $"Bundle Size Changes List ({changedSizeBundles.Length} bundle(s))");
        GUI.enabled = true;
        
        if (fileSizeChangeActuallyOpen) {

            foreach (BundleBuildSettings bundle in changedSizeBundles) {
                long sizeChange = bundle.totalSize - bundle.lastBuildSize;
                string sizeChangeText = GetReadableFileSize(sizeChange);
                if (sizeChange > 0) {
                    sizeChangeText = "+" + sizeChangeText;
                }

                EditorGUILayout.LabelField(bundle.BundleName, $"{GetReadableFileSize(bundle.lastBuildSize)} -> {GetReadableFileSize(bundle.totalSize)} ({sizeChangeText})");
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.TextArea("Build Settings", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Select All for Build")) {
            foreach (BundleBuildSettings bundle in bundles.Values) {
                bundle._build = true;
            }
        }
        if (GUILayout.Button("Unselect All for Build")) {
            foreach (BundleBuildSettings bundle in bundles.Values) {
                bundle._build = false;
            }
        }
        
        GUILayout.EndHorizontal();

        EditorGUIUtility.labelWidth = 300; // what
        buildOnlyChanged = EditorGUILayout.Toggle("Build Only Changed (Experimental): ", buildOnlyChanged, GUILayout.ExpandWidth(true));

        EditorGUIUtility.labelWidth = 150;
        
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        buildOutputPath = EditorGUILayout.TextField("Build Output Directory:", buildOutputPath, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("Select", EditorStyles.miniButton)) {
            buildOutputPath = EditorUtility.OpenFolderPanel("Select Build Output Directory", buildOutputPath, "");
        }

        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("Build :3")) {
            BuildBundles();
        }
        
    }

    void BuildBundles() {
        Debug.Log("Getting ready for build.");
        
        EditorPrefs.SetString("build_output", buildOutputPath);
        EditorPrefs.SetBool("build_changed", buildOnlyChanged);
        
        AssetBundleBuild[] bundleBuilds = bundles.Values
             .Where(bundle => {
                 if (buildOnlyChanged) return bundle.changedSinceLastBuild;
                 return bundle._build;
             })
             .Select(bundle => {
                 bundle.changedSinceLastBuild = false;
                 bundle.lastBuildSize = bundle.totalSize;
                 
                 EditorPrefs.SetBool($"{bundle.BundleName}_changed", false);
                 EditorPrefs.SetInt($"{bundle.BundleName}_size", (int)bundle.totalSize);
                 
                 return new AssetBundleBuild {
                     assetBundleName = bundle.BundleName,
                     assetNames = AssetDatabase.GetAssetPathsFromAssetBundle(bundle.BundleName)
                 };
             })
             .ToArray();

        Debug.Log($"Building {bundleBuilds.Length} bundle(s)!");
        BuildPipeline.BuildAssetBundles(buildOutputPath, bundleBuilds, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
        
        Debug.Log("Performing cleanup.");
        Refresh();
        string directoryName = Path.GetFileName(buildOutputPath);
        string emptyBundlePath = Path.Combine(buildOutputPath, directoryName);
        if (File.Exists(emptyBundlePath))
        {
            File.Delete(emptyBundlePath);
        }

        foreach(string file in Directory.GetFiles(buildOutputPath, "*.manifest", SearchOption.TopDirectoryOnly)) {
            File.Delete(file);
        }
    }
}
