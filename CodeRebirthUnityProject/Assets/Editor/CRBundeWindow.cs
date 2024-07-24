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
    private string buildPath;

    internal class BundleBuildSettings {
        public string BundleName { get; private set; }

        public bool _foldOutOpen = false;
        public bool _build = false;

        public bool changedSinceLastBuild = true;
        
        internal BundleBuildSettings(string bundleName) {
            BundleName = bundleName;
        }
    }
    
    internal static Dictionary<string, BundleBuildSettings> bundles = new();

    static string buildOutputPath = "";
    static bool buildOnlyChanged = false;
    
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
            bundles[bundle] = settings;
        }
    }
    
    void OnGUI() {
        if (GUILayout.Button("Refresh")) {
            Refresh();
        }

        
        
        foreach (BundleBuildSettings bundle in bundles.Values) {
            bundle._foldOutOpen = EditorGUILayout.BeginFoldoutHeaderGroup(bundle._foldOutOpen, bundle.changedSinceLastBuild? $"(*) {bundle.BundleName}" : bundle.BundleName);
            if (!bundle._foldOutOpen) {
                EditorGUILayout.EndFoldoutHeaderGroup();
                continue;
            }

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
                 
                 EditorPrefs.SetBool($"{bundle.BundleName}_changed", false);
                 
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
