using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class CRBundleWindow : EditorWindow
{
    internal class BundleBuildSettings
    {
        public string BundleName { get; private set; }
        public string DisplayName { get; private set; }

        public bool FoldOutOpen { get; set; } = false;
        public bool Build { get; set; } = false;
        public bool Blacklisted { get; set; } = false;
        public bool ChangedSinceLastBuild { get; set; } = true;

        public long TotalSize { get; private set; } = 0;
        public long LastBuildSize { get; set; } = 0;
        public long BuiltBundleSize { get; private set; } = 0;

        public bool AssetsFoldOut { get; set; } = false;

        public List<AssetDetails> Assets { get; private set; } = new List<AssetDetails>();

        internal BundleBuildSettings(string bundleName)
        {
            BundleName = bundleName;
            DisplayName = ConvertToDisplayName(bundleName);

            foreach (string asset in AssetDatabase.GetAssetPathsFromAssetBundle(bundleName))
            {
                FileInfo fileInfo = new FileInfo(asset);
                if (fileInfo.Exists)
                {
                    long fileSize = fileInfo.Length;
                    TotalSize += fileSize;
                    Assets.Add(new AssetDetails { Path = asset, Size = fileSize });
                }
            }

            FileInfo bundleFileInfo = new FileInfo(Path.Combine(buildOutputPath, bundleName));
            if (bundleFileInfo.Exists)
                BuiltBundleSize = bundleFileInfo.Length;
        }

        internal class AssetDetails
        {
            public string Path { get; set; }
            public long Size { get; set; }
        }
    }

    [FilePath("Project/CRBundleWindowSettings.asset", FilePathAttribute.Location.PreferencesFolder)]
    public class CRBundleWindowSettings : ScriptableSingleton<CRBundleWindowSettings>
    {
        public bool buildAllBundles;
        public string buildOutputPath;
        public bool buildOnlyChanged;
        public Dictionary<string, bool> assetBundlesDict = new Dictionary<string, bool>();
        public Dictionary<string, bool> buildOptionsDict = new Dictionary<string, bool>();
        public SortOption assetSortOption = SortOption.Size; // Default sorting by size

        public void Save()
        {
            Save(true);
        }
    }

    public enum SortOption
    {
        Alphabetical,
        Size,
        ReverseAlphabetical,
        ReverseSize
    }

    static string ConvertToDisplayName(string bundleName)
    {
        return Regex.Replace(bundleName, "(\\B[A-Z])", " $1")
                    .Replace("assets", " Assets")
                    .Split(' ')
                    .Select(word => char.ToUpper(word[0]) + word.Substring(1))
                    .Aggregate((a, b) => a + " " + b);
    }

    static string GetReadableFileSize(long bytes)
    {
        if (bytes <= 0) return "N/A";

        string[] suffixes = { "B", "KB", "MB", "GB" };
        int i;
        double doubleBytes = bytes;

        for (i = 0; i < suffixes.Length && bytes >= 1024; i++, bytes /= 1024)
        {
            doubleBytes = bytes / 1024.0;
        }

        return string.Format("{0:0.##} {1}", doubleBytes, suffixes[i]);
    }

    internal static Dictionary<string, BundleBuildSettings> bundles = new();

    static string buildOutputPath => CRBundleWindowSettings.instance.buildOutputPath;
    static bool buildOnlyChanged => CRBundleWindowSettings.instance.buildOnlyChanged;

    static bool fileSizeChangesFoldoutOpen = false;

    internal static bool logChangedFiles = false;

    private SortOption _assetSortOption => CRBundleWindowSettings.instance.assetSortOption;

    Vector2 scrollPosition;

    [MenuItem("Code Rebirth/Bundle Builder")]
    static void Open()
    {
        GetWindow<CRBundleWindow>("CR Bundle Builder");

        CRBundleWindowSettings.instance.buildOutputPath = EditorPrefs.GetString("build_output", CRBundleWindowSettings.instance.buildOutputPath);
        CRBundleWindowSettings.instance.buildOnlyChanged = EditorPrefs.GetBool("build_changed", CRBundleWindowSettings.instance.buildOnlyChanged);
        CRBundleWindowSettings.instance.assetSortOption = (SortOption)EditorPrefs.GetInt("asset_sort_option", (int)CRBundleWindowSettings.instance.assetSortOption);
        CRBundleWindowSettings.instance.Save();

        Refresh();
    }

    static void Refresh()
    {
        Debug.Log("Refreshing bundler.");

        AssetDatabase.SaveAssets();

        bundles.Clear();
        foreach (string bundle in AssetDatabase.GetAllAssetBundleNames())
        {
            BundleBuildSettings settings = new BundleBuildSettings(bundle);
            settings.Build = EditorPrefs.GetBool($"{bundle}_build", false);
            settings.Blacklisted = EditorPrefs.GetBool($"{bundle}_blacklisted", false);
            settings.ChangedSinceLastBuild = EditorPrefs.GetBool($"{bundle}_changed", true);
            settings.LastBuildSize = EditorPrefs.GetInt($"{bundle}_size", 0);
            bundles[bundle] = settings;
        }
    }

    void OnGUI()
    {
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 14
        };

        GUIStyle boldLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold
        };

        GUIStyle assetFoldoutStyle = new GUIStyle(EditorStyles.foldout)
        {
            fontStyle = FontStyle.Bold
        };

        if (GUILayout.Button("Refresh"))
        {
            Refresh();
        }

        var assetSortOption = (SortOption)EditorGUILayout.EnumPopup("Sort Assets By:", _assetSortOption);
        EditorPrefs.SetInt("asset_sort_option", (int)assetSortOption);
        CRBundleWindowSettings.instance.assetSortOption = assetSortOption;
        CRBundleWindowSettings.instance.Save();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (BundleBuildSettings bundle in bundles.Values)
        {
            string prefix = bundle.ChangedSinceLastBuild ? "(*) " : "";

            EditorGUI.indentLevel = 0;
            Rect foldoutRect = GUILayoutUtility.GetRect(20f, 25f, GUILayout.ExpandWidth(true)); // Increased height for better spacing
            bundle.FoldOutOpen = EditorGUI.Foldout(foldoutRect, bundle.FoldOutOpen, "", true, headerStyle);

            // Adding a custom icon for asset bundles
            Texture2D bundleIcon = EditorGUIUtility.IconContent("d_Folder Icon").image as Texture2D;
            Rect iconRect = new Rect(foldoutRect.x + 15, foldoutRect.y + 3, 20, 20); // Lower the icon positioning
            GUI.DrawTexture(iconRect, bundleIcon);

            // Drawing the label next to the icon
            Rect labelRect = new Rect(iconRect.xMax + 5, foldoutRect.y, foldoutRect.width - iconRect.width, foldoutRect.height);
            EditorGUI.LabelField(labelRect, $"{prefix}{bundle.DisplayName}", headerStyle);

            if (Event.current.type == EventType.MouseDown && foldoutRect.Contains(Event.current.mousePosition))
            {
                bundle.FoldOutOpen = !bundle.FoldOutOpen;
                Event.current.Use();
            }

            if (!bundle.FoldOutOpen)
            {
                continue;
            }

            EditorGUILayout.LabelField("Total Size", GetReadableFileSize(bundle.TotalSize), boldLabelStyle);
            EditorGUILayout.LabelField("Previous Total Size", GetReadableFileSize(bundle.LastBuildSize), boldLabelStyle);
            EditorGUILayout.LabelField("Built Bundle Size", GetReadableFileSize(bundle.BuiltBundleSize), boldLabelStyle);

            bool build = bundle.Build;
            build = EditorGUILayout.Toggle("Build", build);
            if (build != bundle.Build)
            {
                bundle.Build = build;
                EditorPrefs.SetBool($"{bundle.BundleName}_build", build);
            }

            bool blacklisted = bundle.Blacklisted;
            blacklisted = EditorGUILayout.Toggle("Blacklist", blacklisted);
            if (blacklisted != bundle.Blacklisted)
            {
                bundle.Blacklisted = blacklisted;
                EditorPrefs.SetBool($"{bundle.BundleName}_blacklisted", blacklisted);
            }

            EditorGUI.indentLevel++;
            Rect assetFoldoutRect = GUILayoutUtility.GetRect(20f, 25f, GUILayout.ExpandWidth(true));
            bundle.AssetsFoldOut = EditorGUI.Foldout(assetFoldoutRect, bundle.AssetsFoldOut, "Assets in Bundle", true, assetFoldoutStyle);

            if (Event.current.type == EventType.MouseDown && assetFoldoutRect.Contains(Event.current.mousePosition))
            {
                bundle.AssetsFoldOut = !bundle.AssetsFoldOut;
                Event.current.Use();
            }

            if (bundle.AssetsFoldOut)
            {
                SortAssets(bundle.Assets);

                foreach (var asset in bundle.Assets)
                {
                    EditorGUILayout.BeginHorizontal();
                    Texture2D icon = AssetDatabase.GetCachedIcon(asset.Path) as Texture2D;
                    if (icon != null)
                    {
                        GUILayout.Label(icon, GUILayout.Width(24), GUILayout.Height(24), GUILayout.ExpandWidth(false)); // Adjust icon size and positioning
                    }
                    EditorGUILayout.LabelField(Path.GetFileName(asset.Path), GUILayout.ExpandWidth(true));
                    EditorGUILayout.LabelField(GetReadableFileSize(asset.Size), GUILayout.Width(150), GUILayout.ExpandWidth(false));
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Build Settings", headerStyle);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Select All for Build"))
        {
            foreach (BundleBuildSettings bundle in bundles.Values)
            {
                if (!bundle.Blacklisted)
                {
                    bundle.Build = true;
                    EditorPrefs.SetBool($"{bundle.BundleName}_build", true);
                }
            }
        }
        if (GUILayout.Button("Unselect All for Build"))
        {
            foreach (BundleBuildSettings bundle in bundles.Values)
            {
                bundle.Build = false;
                EditorPrefs.SetBool($"{bundle.BundleName}_build", false);
            }
        }

        GUILayout.EndHorizontal();

        EditorGUIUtility.labelWidth = 300;
        CRBundleWindowSettings.instance.buildOnlyChanged = EditorGUILayout.Toggle("Build Only Changed (Experimental): ", CRBundleWindowSettings.instance.buildOnlyChanged, GUILayout.ExpandWidth(true));
        EditorPrefs.SetBool("build_changed", CRBundleWindowSettings.instance.buildOnlyChanged);

        EditorGUIUtility.labelWidth = 150;

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        CRBundleWindowSettings.instance.buildOutputPath = EditorGUILayout.TextField("Build Output Directory:", CRBundleWindowSettings.instance.buildOutputPath, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("Select", EditorStyles.miniButton))
        {
            CRBundleWindowSettings.instance.buildOutputPath = EditorUtility.OpenFolderPanel("Select Build Output Directory", CRBundleWindowSettings.instance.buildOutputPath, "");
            EditorPrefs.SetString("build_output", CRBundleWindowSettings.instance.buildOutputPath);
        }

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Build"))
        {
            BuildBundles();
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        logChangedFiles = EditorGUILayout.Toggle("Log Changed Files", logChangedFiles);
        EditorPrefs.SetBool("log_changed_files", logChangedFiles);
    }

    void SortAssets(List<BundleBuildSettings.AssetDetails> assets)
    {
        switch (_assetSortOption)
        {
            case SortOption.Size:
                assets.Sort((a, b) => b.Size.CompareTo(a.Size));
                break;
            case SortOption.Alphabetical:
                assets.Sort((a, b) => a.Path.CompareTo(b.Path));
                break;
            case SortOption.ReverseAlphabetical:
                assets.Sort((a, b) => b.Path.CompareTo(a.Path));
                break;
            case SortOption.ReverseSize:
                assets.Sort((a, b) => a.Size.CompareTo(b.Size));
                break;
        }
    }

    void BuildBundles()
    {
        Debug.Log("Getting ready for build.");

        AssetBundleBuild[] bundleBuilds = bundles.Values
            .Where(bundle => !bundle.Blacklisted && (CRBundleWindowSettings.instance.buildOnlyChanged ? bundle.ChangedSinceLastBuild : bundle.Build))
            .Select(bundle =>
            {
                bundle.ChangedSinceLastBuild = false;
                bundle.LastBuildSize = bundle.TotalSize;

                EditorPrefs.SetBool($"{bundle.BundleName}_changed", false);
                EditorPrefs.SetInt($"{bundle.BundleName}_size", (int)bundle.TotalSize);

                return new AssetBundleBuild
                {
                    assetBundleName = bundle.BundleName,
                    assetNames = AssetDatabase.GetAssetPathsFromAssetBundle(bundle.BundleName)
                };
            })
            .ToArray();

        Debug.Log($"Building {bundleBuilds.Length} bundle(s)!");
        BuildPipeline.BuildAssetBundles(CRBundleWindowSettings.instance.buildOutputPath, bundleBuilds, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);

        Debug.Log("Performing cleanup.");
        Refresh();
        string directoryName = Path.GetFileName(CRBundleWindowSettings.instance.buildOutputPath);
        string emptyBundlePath = Path.Combine(CRBundleWindowSettings.instance.buildOutputPath, directoryName);
        if (File.Exists(emptyBundlePath))
        {
            File.Delete(emptyBundlePath);
        }

        foreach (string file in Directory.GetFiles(CRBundleWindowSettings.instance.buildOutputPath, "*.manifest", SearchOption.TopDirectoryOnly))
        {
            File.Delete(file);
        }

        Debug.Log("Build completed and cleanup done.");
    }
}