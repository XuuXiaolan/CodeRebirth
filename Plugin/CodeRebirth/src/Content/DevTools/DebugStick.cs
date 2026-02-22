using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dawn;
using Dawn.Utils;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CodeRebirth.src.Content.DevTools;

public class DebugStick : GrabbableObject
{
    [field: SerializeField]
    public Material TransparentMaterial { get; private set; }
    [field: SerializeField]
    public float PlaceDistance { get; private set; } = 20f;

    private Dictionary<DawnMapObjectInfo, HologramCopy> _hologramCopies = new();
    private DawnMapObjectInfo _currentlySelectedHazard;

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
            _hologramCopies[mapObjectInfo].SetUpHologram(mapObjectInfo.MapObject, TransparentMaterial);
        }

        // Q and E to cycle through list of hazards in LethalContent.MapObjects.Values, and update the hologram to match the currently selected hazard
        // Rotate hazard being pointed at with left and arrow right keys.
        // Move it left right forward and back with IJKL keys
        // Up and down with U and O keys.
        // Up and down Arrow keys to rotate up or down.
        // Z to tp to a random hazard of the current selection.
        // Remove all map hazards with R key.
    }

    public void CycleSelectedHazard(int direction)
    {
        _hologramCopies[_currentlySelectedHazard].HologramObject.SetActive(false);
        _currentlySelectedHazard = direction > 0 ? GetNextHazard() : GetPreviousHazard();
    }

    public DawnMapObjectInfo GetPreviousHazard()
    {
        List<DawnMapObjectInfo> mapObjects = LethalContent.MapObjects.Values.ToList();
        int currentIndex = mapObjects.IndexOf(_currentlySelectedHazard);
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
        int currentIndex = mapObjects.IndexOf(_currentlySelectedHazard);
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
        if (!isHeld || isPocketed || playerHeldBy == null || !playerHeldBy.IsLocalPlayer())
        {
            _hologramCopies[_currentlySelectedHazard].HologramObject.SetActive(false);
            return;
        }

        if (CanPlaceHologram(out RaycastHit raycastHit))
        {
            _hologramCopies[_currentlySelectedHazard].UpdateTick(raycastHit);
        }
        else
        {
            _hologramCopies[_currentlySelectedHazard].HologramObject.SetActive(false);
        }

        Keyboard? keyboard = Keyboard.current;
        Mouse? mouse = Mouse.current;
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

        if (mouse == null)
        {
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (_currentlySelectedHazard.HasNetworkObject)
            {
                if (IsServer)
                {
                    GameObject gameObject = GameObject.Instantiate(_currentlySelectedHazard.MapObject, raycastHit.point, Quaternion.identity);
                    gameObject.GetComponent<NetworkObject>().Spawn();
                }
            }
            else
            {
                GameObject.Instantiate(_currentlySelectedHazard.MapObject, raycastHit.point, Quaternion.identity);
            }
        }
    }

    public override void EquipItem()
    {
        base.EquipItem();
        SetHazardTooltips();
    }

    public void SetHazardTooltips()
    {
        List<string> tooltips =
        [
            "Press Q and E to cycle through hazards.",
            "Previous Hazard: " + GetPreviousHazard().Key.Key,
            "Current hazard: " + _currentlySelectedHazard.Key.Key,
            "Next Hazard: " + GetNextHazard().Key.Key,
            "Remove all map hazards with R key.",
        ];

        HUDManager.Instance.ChangeControlTipMultiple(tooltips.ToArray(), false, null);
    }

    public void DeleteAllMapObjectsSpawned()
    {
        DawnMapObjectNamespacedKeyContainer[] allMapHazards = FindObjectsOfType<DawnMapObjectNamespacedKeyContainer>();
        for (int i = 0; i < allMapHazards.Length; i++)
        {
            GameObject gameObject = allMapHazards[i].gameObject;
            if (gameObject.TryGetComponent(out NetworkObject networkObject) && networkObject.IsSpawned)
            {
                networkObject.Despawn(true);
                continue;
            }

            GameObject.Destroy(gameObject);
        }
    }
}