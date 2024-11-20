#if UNITY_EDITOR // => Ignore from here to next endif if not in editor
using ModelReplacement.AvatarBodyUpdater;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using UnityEditor;
#endif

using UnityEngine;
[CreateAssetMenu(menuName = "ModelReplacementSDK/Create Project")]
public class ModelReplacementProject : ScriptableObject
{
#if UNITY_EDITOR // => Ignore from here to next endif if not in editor
    private static string MRAPI_Ver = "2.3.1";
    //Download from latest release, make possible to update
    //ModelReplacementApI.dll

    public string ModGUID = "";
    public string ModName = "";
    public string ModShortDescription = "ModelReplacementAPI Template Project";
    public string AssetbundleName = "templatebundle";
    public string ProjectNamespace = "ModelReplacement";

    public string LCPath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Lethal Company\\";
    public string ProjectPath = "";

    public bool GenerateNewProject = false;
    //public bool UpdateModelReplacementAPIOnBuild = true;

    [SerializeField]
    public List<OffsetBuilder> ProjectPrefabs = new List<OffsetBuilder>();
    private List<string> classNamesInUse = new List<string>();
    

    private void CullDuplicatePrefabs()
    {
        HashSet<OffsetBuilder> prefs = new HashSet<OffsetBuilder>(ProjectPrefabs);
        ProjectPrefabs.Clear();
        ProjectPrefabs.AddRange(prefs);
    }
    private void CullNullPrefabs()
    {
        IEnumerable<OffsetBuilder> prefabsToRemove = ProjectPrefabs.Where(x => x == null);
        prefabsToRemove.ToList().ForEach(x => ProjectPrefabs.Remove(x));
    }
    private void GenerateProject()
    {
        List<OffsetBuilder> validPrefabs = new List<OffsetBuilder>();
        foreach(var x in ProjectPrefabs)
        {
            if(x == null) { continue; }
            if (!validPrefabs.Any(y => y.name == x.name))
            {
                validPrefabs.Add(x);
            }
        }



        classNamesInUse.Clear();
        string defaultClassName = "ModelReplacementPlaceholder";
        string defaultPrefabName = "model_name_placeholder";
        if (validPrefabs.Any())
        {
            defaultClassName = GetClassNameFromPrefab(validPrefabs.First().assetName);
        }



        ProjectPath = EditorUtility.OpenFolderPanel("Select Location To Generate Project", "", "");
        Directory.CreateDirectory(ProjectPath + "/Build");
        string plugincs = ProjectTemplateData.plugincs;
        plugincs = plugincs.Replace("$ModNamespace$", ProjectNamespace);
        plugincs = plugincs.Replace("$ModGUID$", ModGUID);
        plugincs = plugincs.Replace("$ModName$", ModName);
        plugincs = plugincs.Replace("$BRClassName$", defaultClassName);
        plugincs = plugincs.Replace("$AssetBundleName$", AssetbundleName);
        plugincs = plugincs.Trim('"');

        string csproj = ProjectTemplateData.csproj;
        csproj = csproj.Replace("$LCPath$", LCPath);
        csproj = csproj.Replace("$AssetBundleName$", AssetbundleName);
        csproj = csproj.Trim('"');

        string manifest = ProjectTemplateData.manifest;
        manifest = manifest.Replace("$ModName$", ModName);
        manifest = manifest.Replace("$ModDescription$", ModShortDescription);
        manifest = manifest.Replace("$MRAPI_CVer$", MRAPI_Ver);
        manifest = manifest.Trim('"');

        string readme = ProjectTemplateData.readme;
        readme = readme.Replace("$ModName$", ModName);
        readme = readme.Replace("$ModDescription$", ModShortDescription);
        readme = readme.Trim('"');

        string modelReplacement = ProjectTemplateData.MRNameSpace;
        modelReplacement = modelReplacement.Replace("$ModNamespace$", ProjectNamespace);

        bool DoDefault = true;
        foreach (var pclass in validPrefabs)
        {
            string mrClass = ProjectTemplateData.MRclass;
            string className = GetClassNameFromPrefab(pclass.assetName);
            mrClass = mrClass.Replace("$PrefabName$", pclass.assetName);
            mrClass = mrClass.Replace("$BRClassName$", className);
            classNamesInUse.Add(className);

            var strings = modelReplacement.Split("$ClassInsert$");

            modelReplacement = strings[0] + mrClass + "$ClassInsert$" + strings[1];
            DoDefault = false;
        }
        if (DoDefault)
        {
            string mrClass = ProjectTemplateData.MRclass;
            mrClass = mrClass.Replace("$PrefabName$", defaultPrefabName);
            mrClass = mrClass.Replace("$BRClassName$", defaultClassName);

            var strings = modelReplacement.Split("$ClassInsert$");

            modelReplacement = strings[0] + mrClass + "$ClassInsert$" + strings[1];
        }
        modelReplacement = modelReplacement.Replace("$ClassInsert$", "");
        modelReplacement = modelReplacement.Trim('"');

        string projectName = new DirectoryInfo(ProjectPath).Name;

        string pluginDir = ProjectPath + "/Plugin.cs";
        string csprojDir = ProjectPath + $"/{projectName}.csproj";
        string manifestDir = ProjectPath + "/Build/manifest.json";
        string readmeDir = ProjectPath + "/Build/README.md";
        string modelRepDir = ProjectPath + "/BodyReplacements.cs";

        File.WriteAllText(pluginDir, plugincs);
        File.WriteAllText(csprojDir, csproj);
        File.WriteAllText(manifestDir, manifest);
        File.WriteAllText(readmeDir, readme);
        File.WriteAllText(modelRepDir, modelReplacement);

        string imgPath = AssetDatabase.GUIDToAssetPath("7e13341ec1957fa448474ed9fd8fd1d4");
        string icon = ProjectPath + "/Build/icon.png";
        try
        {
            System.IO.File.Copy(imgPath, icon, false);
        }
        catch { }

        classNamesInUse.Clear();
        GenerateNewProject = false;
    }



    public void BuildProject2()
    {

        CullNullPrefabs();
        IEnumerable<OffsetBuilder> prefabsToRemove = ProjectPrefabs.Where(x => x.Project != this);
        prefabsToRemove.ToList().ForEach(x => ProjectPrefabs.Remove(x));
        CullDuplicatePrefabs();

        if (GenerateNewProject)
        {
            if (ModGUID == "") { Debug.LogError("You must enter a mod GUID before building."); return; }
            if (ModName == "") { Debug.LogError("You must enter a mod name before building."); return; }
            if (ProjectNamespace == "") { Debug.LogError("You must enter a namespace before building."); return; }
        }
        
        if (AssetbundleName == "") { Debug.LogError("You must enter an assetbundle name before building."); return; }

        bool anyPrefabs = false;
        Debug.Log($"{ProjectPrefabs.Count} {ProjectPrefabs.ToList().Count}");
        foreach (var item in ProjectPrefabs.ToList())
        {
            if (item == null) { continue; }
            Debug.Log($"{item.assetName} {item.isActiveAndEnabled} {item}");
            item.SavePrefab2();
            AssetImporter assetImporter = AssetImporter.GetAtPath(item.assetPath);
            assetImporter.assetBundleName = AssetbundleName;
            anyPrefabs = true;
        }


        ValidateLCPath();
        if (GenerateNewProject)
        {
            GenerateProject();
        }
        else
        {
            if (ProjectPath == "")
            {
                ProjectPath = EditorUtility.OpenFolderPanel("Select Location To Build Assetbundle", "", "");
            }
            ValidateProjectPath();
        }

        
        if (anyPrefabs)
        {
            BuildPipeline.BuildAssetBundles(ProjectPath, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
            string projectName = new DirectoryInfo(ProjectPath).Name;
            if (File.Exists(ProjectPath + $"/{projectName}"))
            {
                File.Delete(ProjectPath + $"/{projectName}");
            }
            if (File.Exists(ProjectPath + $"/{projectName}.manifest"))
            {
                File.Delete(ProjectPath + $"/{projectName}.manifest");
            }
            EditorUtility.RevealInFinder(ProjectPath + "/" + AssetbundleName);
            Debug.LogWarning("Assetbundle Built. ");
        }
        else
        {
            Debug.LogWarning("Did not build assetbundle, no prefabs in project.");
        }
    }



    public void ValidateLCPath()
    {
        if (Directory.Exists(LCPath)) { return; }
        LCPath = EditorUtility.OpenFolderPanel("Select your (..\\steamapps\\common\\Lethal Company) Directory", "", "");
    }
    public void ValidateProjectPath()
    {
        if (Directory.Exists(ProjectPath)) { return; }
        ProjectPath = EditorUtility.OpenFolderPanel("Relocate Project", "", "");
    }
    public void ReportPrefabAddition(OffsetBuilder mr)
    {
        ProjectPrefabs.Add(mr);
        if(mr.Project != this)
        {
            mr.Project.ReportPrefabRemoval(mr);
            mr.Project = this;
        }
        CullDuplicatePrefabs();
    }

    public void ReportPrefabRemoval(OffsetBuilder mr)
    {
        if (ProjectPrefabs.Contains(mr)) { ProjectPrefabs.Remove(mr); }
        if(mr.Project == this) { mr.Project = null; }
        CullDuplicatePrefabs();
    }
    public string GetClassNameFromPrefab(string prefabName)
    {
        string className = "MR" + RemoveSpecialCharacters(prefabName).ToUpper();
        while (classNamesInUse.Contains(className))
        {
            className += "_";
        }
        return className;

    }
    public static string RemoveSpecialCharacters(string str)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char c in str)
        {
            if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_')
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }








    protected void StartCoroutine(IEnumerator _task)
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("Can not run coroutine outside of play mode.");
            return;
        }

        CoWorker coworker = new GameObject("CoWorker_" + _task.ToString()).AddComponent<CoWorker>();
        coworker.Work(_task);
    }

    public class CoWorker : MonoBehaviour
    {
        public void Work(IEnumerator _coroutine)
        {
            StartCoroutine(WorkCoroutine(_coroutine));
        }

        private IEnumerator WorkCoroutine(IEnumerator _coroutine)
        {
            yield return StartCoroutine(_coroutine);
            Destroy(this.gameObject);
        }
    }

#endif
}
