using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.ModCompats;
using Dawn;
using Dawn.Utils;
using GameNetcodeStuff;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace CodeRebirth.src.Content.DevTools;

public class DebugStick : GrabbableObject
{
    [field: SerializeField]
    public List<Material> NavMeshMaterials { get; private set; }

    [field: SerializeField]
    public AudioSource Source { get; private set; }

    [field: SerializeField]
    public AudioClip PlaceSound { get; private set; }

    [field: SerializeField]
    public float PlaceDistance { get; private set; } = 20f;

    private Dictionary<DawnMapObjectInfo, HologramCopy> _hologramCopies = new();
    private DawnMapObjectInfo _currentlySelectedHazard;
    private bool _updateNavMesh = false;
    private GameObject? _navMeshObject;

    private void NavMeshSurface_BuildNavMesh(On.Unity.AI.Navigation.NavMeshSurface.orig_BuildNavMesh orig, Unity.AI.Navigation.NavMeshSurface self)
    {
        orig(self);
        VisualiseNavMesh();
    }

    private bool CanPlaceHologram([NotNullWhen(true)] out RaycastHit raycastHit)
    {
        PlayerControllerB playerControllerB = GameNetworkManager.Instance.localPlayerController;
        Ray ray = new(playerControllerB.gameplayCamera.transform.position, playerControllerB.gameplayCamera.transform.forward);
        if (!Physics.Raycast(ray, out raycastHit, PlaceDistance, StartOfRound.Instance.collidersAndRoomMaskAndDefault | MoreLayerMasks.HazardMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        if (raycastHit.collider.gameObject.layer == LayerMask.NameToLayer("MapHazard"))
        {
            return false;
        }
        return true;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _currentlySelectedHazard = LethalContent.MapObjects.Values.First();
        foreach (DawnMapObjectInfo mapObjectInfo in LethalContent.MapObjects.Values)
        {
            _hologramCopies[mapObjectInfo] = new HologramCopy();
            _hologramCopies[mapObjectInfo].SetUpHologram(mapObjectInfo.GetMapObjectPrefab());
        }

        FixControlTips();
        // Q and E to cycle through list of hazards in LethalContent.MapObjects.Values, and update the hologram to match the currently selected hazard
        // Rotate hazard being pointed at with left and arrow right keys.
        // Move it left right forward and back with IJKL keys
        // Up and down with U and O keys.
        // Up and down Arrow keys to rotate up or down.
        // Z to tp to a random hazard of the current selection.
        // Remove all map hazards with R key.
        // Visualise NavMesh with P key.
    }

    private static bool doneOnce = false;
    private void FixControlTips()
    {
        if (doneOnce)
        {
            return;
        }
        doneOnce = true;

        int extraTooltips = 10;
        TextMeshProUGUI finalTooltip = HUDManager.Instance.controlTipLines[3];
        List<TextMeshProUGUI> tooltips = HUDManager.Instance.controlTipLines.ToList();
        for (int i = 0; i < extraTooltips; i++)
        {
            GameObject newTooltip = Instantiate(HUDManager.Instance.controlTipLines[3].gameObject, finalTooltip.transform.parent);
            TextMeshProUGUI newTooltipText = newTooltip.GetComponent<TextMeshProUGUI>();
            newTooltipText.text = string.Empty;
            ((RectTransform)newTooltip.transform).anchoredPosition3D -= new Vector3(0f, 20.5f * (i + 1), 0f);
            tooltips.Add(newTooltipText);
        }

        HUDManager.Instance.controlTipLines = tooltips.ToArray();
    }

    public void CycleSelectedHazard(int direction)
    {
        _hologramCopies[GetCurrentHazard()].HologramObject.SetActive(false);
        _currentlySelectedHazard = direction > 0 ? GetNextHazard() : GetPreviousHazard();
    }

    public DawnMapObjectInfo GetPreviousHazard()
    {
        List<DawnMapObjectInfo> mapObjects = LethalContent.MapObjects.Values.ToList();
        int currentIndex = mapObjects.IndexOf(GetCurrentHazard());
        int newIndex = (currentIndex - 1) % mapObjects.Count;
        if (newIndex < 0)
        {
            newIndex += mapObjects.Count;
        }
        return mapObjects[newIndex];
        
    }

    public DawnMapObjectInfo GetCurrentHazard() => _currentlySelectedHazard;
    public DawnMapObjectInfo GetNextHazard()
    {
        List<DawnMapObjectInfo> mapObjects = LethalContent.MapObjects.Values.ToList();
        int currentIndex = mapObjects.IndexOf(GetCurrentHazard());
        int newIndex = (currentIndex + 1) % mapObjects.Count;
        if (newIndex < 0)
        {
            newIndex += mapObjects.Count;
        }
        return mapObjects[newIndex];
    }


    public override void Update()
    {
        base.Update();
        if (!isHeld || isPocketed || playerHeldBy == null || !playerHeldBy.IsLocalPlayer() || playerHeldBy.inSpecialMenu || playerHeldBy.inTerminalMenu)
        {
            _hologramCopies[GetCurrentHazard()].HologramObject.SetActive(false);
            return;
        }

        if (CanPlaceHologram(out RaycastHit raycastHit))
        {
            _hologramCopies[GetCurrentHazard()].UpdateTick(raycastHit);
            Mouse? mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                _hologramCopies[GetCurrentHazard()].HandleSpawningOriginal(GetCurrentHazard(), raycastHit);
                Source.PlayOneShot(PlaceSound);
            }
        }
        else
        {
            _hologramCopies[GetCurrentHazard()].HologramObject.SetActive(false);
        }

        Keyboard? keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.qKey.wasPressedThisFrame)
        {
            CycleSelectedHazard(-1);
            SetHazardTooltips();
        }
        else if (keyboard.eKey.wasPressedThisFrame)
        {
            CycleSelectedHazard(1);
            SetHazardTooltips();
        }
        else if (keyboard.rKey.wasPressedThisFrame)
        {
            DeleteAllMapObjectsSpawned();
        }
        else if (keyboard.xKey.wasPressedThisFrame)
        {
            ResetAllRotationAndPositionEdits();
        }
        else if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            RotateLeftRight(keyboard.leftArrowKey);
        }
        else if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            RotateLeftRight(keyboard.rightArrowKey);
        }
        else if (keyboard.upArrowKey.wasPressedThisFrame)
        {
            RotateUpDown(keyboard.upArrowKey);
        }
        else if (keyboard.downArrowKey.wasPressedThisFrame)
        {
            RotateUpDown(keyboard.downArrowKey);
        }
        else if (keyboard.zKey.wasPressedThisFrame)
        {
            TeleportToRandomHazard(GetCurrentHazard());
        }
        else if (keyboard.iKey.wasPressedThisFrame)
        {
            MoveForwardBack(keyboard.iKey);
        }
        else if (keyboard.kKey.wasPressedThisFrame)
        {
            MoveForwardBack(keyboard.kKey);
        }
        else if (keyboard.jKey.wasPressedThisFrame)
        {
            MoveLeftRight(keyboard.jKey);
        }
        else if (keyboard.lKey.wasPressedThisFrame)
        {
            MoveLeftRight(keyboard.lKey);
        }
        else if (keyboard.uKey.wasPressedThisFrame)
        {
            MoveUpDown(keyboard.uKey);
        }
        else if (keyboard.oKey.wasPressedThisFrame)
        {
            MoveUpDown(keyboard.oKey);
        }
        else if (keyboard.pKey.wasPressedThisFrame)
        {
            ToggleNavMesh();
        }
    }

    private void ToggleNavMesh()
    {
        _updateNavMesh = !_updateNavMesh;
        VisualiseNavMesh();
    }

    private void VisualiseNavMesh()
    {
        if (!_updateNavMesh)
        {
            if (_navMeshObject != null)
            {
                Destroy(_navMeshObject);
            }
            return;
        }

        if (_navMeshObject != null)
        {
            Destroy(_navMeshObject);
        }

        _navMeshObject = new GameObject("NavMesh Visualiser");

        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

        Vector3[] originalVertices = triangulation.vertices;
        int[] originalIndices = triangulation.indices;
        int[] originalAreas = triangulation.areas;

        List<Vector3> vertices = [.. originalVertices];
        List<int> indices = [];
        List<int> triangleAreas = [];

        Dictionary<(int, int), int> midpointCache = new();

        int GetMidpointIndex(int a, int b)
        {
            if (a > b)
            {
                (a, b) = (b, a);
            }

            if (midpointCache.TryGetValue((a, b), out int cachedIndex))
            {
                return cachedIndex;
            }

            Vector3 midpoint = (vertices[a] + vertices[b]) * 0.5f;
            int newIndex = vertices.Count;
            vertices.Add(midpoint);
            midpointCache[(a, b)] = newIndex;
            return newIndex;
        }

        for (int tri = 0; tri < originalAreas.Length; tri++)
        {
            int i0 = originalIndices[tri * 3];
            int i1 = originalIndices[tri * 3 + 1];
            int i2 = originalIndices[tri * 3 + 2];

            int m01 = GetMidpointIndex(i0, i1);
            int m12 = GetMidpointIndex(i1, i2);
            int m20 = GetMidpointIndex(i2, i0);

            int area = originalAreas[tri];

            AddTriangle(i0, m01, m20, area);
            AddTriangle(m01, i1, m12, area);
            AddTriangle(m20, m12, i2, area);
            AddTriangle(m01, m12, m20, area);
        }

        void AddTriangle(int a, int b, int c, int area)
        {
            indices.Add(a);
            indices.Add(b);
            indices.Add(c);
            triangleAreas.Add(area);
        }

        const float rayStartHeight = 0.5f;
        const float sampleDistance = 1f;
        const float rayDistance = 5f;
        const float visualOffset = 0.03f;

        SnapVerticesToGroundHeightOnly(vertices, rayStartHeight, sampleDistance, rayDistance, visualOffset, LayerMask.GetMask("Default", "Room", "NavigationSurface" /*MoreLayerMasks.DefaultRoomAndNavigationSurfaceMask*/));

        MeshFilter meshFilter = _navMeshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = _navMeshObject.AddComponent<MeshRenderer>();

        Dictionary<int, List<int>> submeshIndices = [];

        for (int tri = 0; tri < triangleAreas.Count; tri++)
        {
            int area = triangleAreas[tri];

            if (!submeshIndices.ContainsKey(area))
            {
                submeshIndices.Add(area, []);
            }

            submeshIndices[area].Add(indices[tri * 3]);
            submeshIndices[area].Add(indices[tri * 3 + 1]);
            submeshIndices[area].Add(indices[tri * 3 + 2]);
        }

        Mesh mesh = new()
        {
            vertices = vertices.ToArray(),
            subMeshCount = submeshIndices.Count
        };

        int submesh = 0;
        foreach (KeyValuePair<int, List<int>> entry in submeshIndices)
        {
            mesh.SetTriangles(entry.Value, submesh++);
        }

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        meshFilter.sharedMesh = mesh;

        List<Material> materials = new();
        foreach (KeyValuePair<int, List<int>> entry in submeshIndices)
        {
            materials.Add(NavMeshMaterials[entry.Key]);
        }

        meshRenderer.SetSharedMaterials(materials);
    }

    private static void SnapVerticesToGroundHeightOnly(List<Vector3> vertices, float rayStartHeight, float sampleDistance, float rayDistance, float visualOffset, int groundMask)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 vertex = vertices[i];
            Vector3 rayStart = vertex + Vector3.up * rayStartHeight;

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit rayHit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                vertices[i] = new Vector3(vertex.x, rayHit.point.y + visualOffset, vertex.z);
            }
            else if (NavMesh.SamplePosition(rayStart, out NavMeshHit navHit, sampleDistance, NavMesh.AllAreas))
            {
                vertices[i] = new Vector3(vertex.x, navHit.position.y + visualOffset, vertex.z);
            }
            else
            {
                vertices[i] = new Vector3(vertex.x, vertex.y + visualOffset, vertex.z);
            }
        }
    }

    private void ResetAllRotationAndPositionEdits()
    {
        foreach (HologramCopy hologramCopy in _hologramCopies.Values)
        {
            hologramCopy.ResetRotationAndPositionEdits();
        }
    }

    private void TeleportToRandomHazard(DawnMapObjectInfo currentlySelectedHazard)
    {
        List<Vector3> validPositions = FindObjectsOfType<DawnMapObjectNamespacedKeyContainer>().Where(x => x.Value == currentlySelectedHazard.Key).Select(x => x.transform.position).ToList();
        if (validPositions.Count > 0)
        {
            playerHeldBy.transform.position = validPositions[UnityEngine.Random.Range(0, validPositions.Count)] + Vector3.up * 5f;
        }
    }

    private void MoveUpDown(KeyControl keyControl)
    {
        _hologramCopies[GetCurrentHazard()].EditPositionOffset(Vector3.up, keyControl, keyControl == Keyboard.current.uKey ? 0.1f : -0.1f);
    }

    private void MoveLeftRight(KeyControl keyControl)
    {
        _hologramCopies[GetCurrentHazard()].EditPositionOffset(Vector3.right, keyControl, keyControl == Keyboard.current.jKey ? -0.1f : 0.1f);
    }

    private void MoveForwardBack(KeyControl keyControl)
    {
        _hologramCopies[GetCurrentHazard()].EditPositionOffset(Vector3.forward, keyControl, keyControl == Keyboard.current.iKey ? 0.1f : -0.1f);
    }

    private void RotateUpDown(KeyControl keyControl)
    {
        _hologramCopies[GetCurrentHazard()].RotateUpDown(keyControl, keyControl == Keyboard.current.upArrowKey ? 10 : -10);
    }

    private void RotateLeftRight(KeyControl keyControl)
    {
        _hologramCopies[GetCurrentHazard()].RotateLeftRight(keyControl, keyControl == Keyboard.current.leftArrowKey ? 10 : -10);
    }

    public override void EquipItem()
    {
        base.EquipItem();
        if (ImperiumCompat.Enabled)
        {
            ImperiumCompat.ToggleInputs(false);
        }

        SetHazardTooltips();
    }

    public override void GrabItem()
    {
        base.GrabItem();
        On.Unity.AI.Navigation.NavMeshSurface.BuildNavMesh += NavMeshSurface_BuildNavMesh;
        On.Unity.AI.Navigation.NavMeshSurface.UpdateNavMesh += NavMeshSurface_UpdateNavMesh;
        // On.Unity.AI.Navigation.NavMeshSurface.UpdateDataIfTransformChanged += NavMeshSurface_UpdateDataIfTransformChanged;
        On.Unity.AI.Navigation.NavMeshSurface.OnDisable += NavMeshSurface_OnDisable;
        On.Unity.AI.Navigation.NavMeshSurface.OnEnable += NavMeshSurface_OnEnable;
    }

    private void NavMeshSurface_UpdateDataIfTransformChanged(On.Unity.AI.Navigation.NavMeshSurface.orig_UpdateDataIfTransformChanged orig, Unity.AI.Navigation.NavMeshSurface self)
    {
        orig(self);
        VisualiseNavMesh();
    }

    private void NavMeshSurface_OnEnable(On.Unity.AI.Navigation.NavMeshSurface.orig_OnEnable orig, Unity.AI.Navigation.NavMeshSurface self)
    {
        orig(self);
        VisualiseNavMesh();
    }

    private void NavMeshSurface_OnDisable(On.Unity.AI.Navigation.NavMeshSurface.orig_OnDisable orig, Unity.AI.Navigation.NavMeshSurface self)
    {
        orig(self);
        VisualiseNavMesh();
    }

    private AsyncOperation NavMeshSurface_UpdateNavMesh(On.Unity.AI.Navigation.NavMeshSurface.orig_UpdateNavMesh orig, Unity.AI.Navigation.NavMeshSurface self, NavMeshData data)
    {
        AsyncOperation result = orig(self, data);
        result.completed += delegate { VisualiseNavMesh(); };
        return result;
    }

    public override void PocketItem()
    {
        base.PocketItem();
        if (ImperiumCompat.Enabled)
        {
            ImperiumCompat.ToggleInputs(true);
        }
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
        if (ImperiumCompat.Enabled)
        {
            ImperiumCompat.ToggleInputs(true);
        }

        On.Unity.AI.Navigation.NavMeshSurface.BuildNavMesh -= NavMeshSurface_BuildNavMesh;
        On.Unity.AI.Navigation.NavMeshSurface.UpdateNavMesh -= NavMeshSurface_UpdateNavMesh;
        // On.Unity.AI.Navigation.NavMeshSurface.UpdateDataIfTransformChanged -= NavMeshSurface_UpdateDataIfTransformChanged;
        On.Unity.AI.Navigation.NavMeshSurface.OnDisable -= NavMeshSurface_OnDisable;
        On.Unity.AI.Navigation.NavMeshSurface.OnEnable -= NavMeshSurface_OnEnable;
    }

    public void SetHazardTooltips()
    {
        HUDManager.Instance.ClearControlTips();

        List<string> tooltips =
        [
            "Cycle Selection : [Q & E]",
            "Previous: " + CutoffString(FormatWordsNicely(GetPreviousHazard().Key.Key.Replace("_", " ")).Replace(" ", ""), 17),
            "Current: " + CutoffString(FormatWordsNicely(GetCurrentHazard().Key.Key.Replace("_", " ")).Replace(" ", ""), 17),
            "Next: " + CutoffString(FormatWordsNicely(GetNextHazard().Key.Key.Replace("_", " ")).Replace(" ", ""), 21),
            "Remove all Hazards : [R]",
        ];

        HUDManager.Instance.ChangeControlTipMultiple(tooltips.ToArray(), false, null);
    }

    public string FormatWordsNicely(string text)
    {
        var splitWords = text.Split(' ');
        int i = 0;
        foreach (string word in splitWords)
        {
            splitWords[i] = word.ToCapitalized();
            i++;
        }

        text = string.Join(" ", splitWords);
        return text;
    }
    
    public string CutoffString(string text, int maxLength)
    {
        if (text.Length > maxLength)
        {
            return text[..maxLength] + "...";
        }
        return text;
    }

    public void DeleteAllMapObjectsSpawned()
    {
        DawnMapObjectNamespacedKeyContainer[] allMapHazards = FindObjectsOfType<DawnMapObjectNamespacedKeyContainer>();
        for (int i = 0; i < allMapHazards.Length; i++)
        {
            GameObject gameObject = allMapHazards[i].gameObject;
            if (gameObject.GetComponent<NetworkObject>())
            {
                foreach (NetworkObject networkObject in gameObject.GetComponentsInChildren<NetworkObject>())
                {
                    if (!networkObject.IsSpawned)
                        continue;

                    networkObject.Despawn(true);
                }
                continue;
            }

            GameObject.Destroy(gameObject);
        }
    }
}