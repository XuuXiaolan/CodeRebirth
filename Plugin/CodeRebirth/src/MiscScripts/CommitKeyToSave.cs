using System.Collections.Generic;
using Dawn;
using Dusk;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public enum SaveTypes
{
    Contract,
    Save,
    Plugin
}

public class CommitKeyToSave : NetworkBehaviour
{
    internal static readonly NamespacedKey CodeRebirthLoreKey = NamespacedKey.From("code_rebirth", "lore_collection");

    [field: SerializeField]
    public SaveTypes SaveType { get; private set; } = SaveTypes.Contract;

    [field: SerializeField]
    public string LoreEntryName { get; private set; } = string.Empty;

    [field: SerializeField, InspectorName("Namespace"), DefaultKeySource("GetLoreEntryName", false)]
    public NamespacedKey NamespacedKey { get; private set; }

    [field: SerializeField, TextArea(2, 10)]
    public string FoundEntryText { get; private set; } = "Found journal entry: '" + "'";
    [field: SerializeField]
    public bool SaveOnServer { get; private set; } = true;

    [field: SerializeField]
    public bool SaveImmediately { get; private set; } = false;

    private static HashSet<NamespacedKey> _savedContractKeys = new();
    private static HashSet<NamespacedKey> _savedSaveKeys = new();
    private static HashSet<NamespacedKey> _savedPluginKeys = new();

    private static bool _shouldSaveContract = false;
    private static bool _shouldSaveSave = false;
    private static bool _shouldSavePlugin = false;

    internal static void Init()
    {
        On.GameNetworkManager.SaveItemsInShip += SaveKeys;
        On.StartOfRound.AutoSaveShipData += SaveKeys;
    }

    private static void SaveKeys(On.StartOfRound.orig_AutoSaveShipData orig, StartOfRound self)
    {
        orig(self);
        SaveKeys();
    }

    private static void SaveKeys(On.GameNetworkManager.orig_SaveItemsInShip orig, GameNetworkManager self)
    {
        orig(self);
        SaveKeys();
    }

    public void CommitKey()
    {
        if (SaveOnServer && !IsServer)
        {
            return;
        }

        CommitNamespaceIntoFile(NamespacedKey);
    }

    public void Destroy()
    {
        if (!IsServer)
        {
            return;
        }

        NetworkObject.Despawn(true);
    }

    private void CommitNamespaceIntoFile(NamespacedKey key)
    {
        switch (SaveType)
        {
            case SaveTypes.Contract:
                _savedContractKeys = DawnLib.GetCurrentContract()!.GetOrCreateDefault<HashSet<NamespacedKey>>(CodeRebirthLoreKey);
                if (!_savedContractKeys.Add(key))
                {
                    return;
                }
                _shouldSaveContract = true;
                break;
            case SaveTypes.Save:
                _savedSaveKeys = DawnLib.GetCurrentSave()!.GetOrCreateDefault<HashSet<NamespacedKey>>(CodeRebirthLoreKey);
                if (!_savedSaveKeys.Add(key))
                {
                    return;
                }
                _shouldSaveSave = true;
                break;
            case SaveTypes.Plugin:
                _savedPluginKeys =  Plugin.PersistentDataContainer.GetOrCreateDefault<HashSet<NamespacedKey>>(CodeRebirthLoreKey);
                if (!_savedPluginKeys.Add(key))
                {
                    return;
                }
                _shouldSavePlugin = true;
                break;
        }

        HUDManager.Instance.DisplayGlobalNotification(FoundEntryText);
        if (!SaveImmediately && !StartOfRound.Instance.inShipPhase)
        {
            return;
        }

        SaveKeys();
    }

    private static void SaveKeys()
    {
        if (_shouldSaveContract)
        {
            DawnLib.GetCurrentContract()!.Set(CodeRebirthLoreKey, _savedContractKeys);
        }

        if (_shouldSaveSave)
        {
            DawnLib.GetCurrentSave()!.Set(CodeRebirthLoreKey, _savedSaveKeys);
        }

        if (_shouldSavePlugin)
        {
            Plugin.PersistentDataContainer.Set(CodeRebirthLoreKey, _savedPluginKeys);
        }

        _shouldSaveContract = false;
        _shouldSaveSave = false;
        _shouldSavePlugin = false;
    }

    public string GetLoreEntryName()
    {
        string normalizedName = NamespacedKey.NormalizeStringForNamespacedKey(LoreEntryName, false);
        return normalizedName;
    }
}