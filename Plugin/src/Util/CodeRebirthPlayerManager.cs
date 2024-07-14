using System;
using System.Collections.Generic;
using UnityEngine;

namespace CodeRebirth.Util.PlayerManager;
public enum CodeRebirthStatusEffects
{
    None,
    Water,
    Electric,
    Fire,
    Smoke,
    Windy,
    Blood,
    // Add other status effects here
}

public enum CodeRebirthItemUsages {
    Wallet,
    Hoverboard,
}

public class CodeRebirthPlayerManager : MonoBehaviour
{
    public bool ridingHoverboard = false;
    public bool holdingWallet = false;
    public bool flingingAway = false;
    public Dictionary<CodeRebirthStatusEffects, bool> statusEffects = new Dictionary<CodeRebirthStatusEffects, bool>();
    public GameObject[] playerParticles = new GameObject[6];
    public Dictionary<CodeRebirthItemUsages, bool> ItemUsages = new Dictionary<CodeRebirthItemUsages, bool>(); //todo, USE THISSS

    public event Action<CodeRebirthStatusEffects, bool> OnStatusEffectChanged = null!;

    public void Awake()
    {
        ItemUsages.Add(CodeRebirthItemUsages.Wallet, false);
        ItemUsages.Add(CodeRebirthItemUsages.Hoverboard, false);
        statusEffects.Add(CodeRebirthStatusEffects.None, false);
        statusEffects.Add(CodeRebirthStatusEffects.Water, false);
        statusEffects.Add(CodeRebirthStatusEffects.Electric, false);
        statusEffects.Add(CodeRebirthStatusEffects.Fire, false);
        statusEffects.Add(CodeRebirthStatusEffects.Smoke, false);
        statusEffects.Add(CodeRebirthStatusEffects.Windy, false);
        statusEffects.Add(CodeRebirthStatusEffects.Blood, false);
        InitPlayerParticles();
    }

    public void InitPlayerParticles()
    {
        playerParticles[0] = Instantiate(Plugin.Assets.WaterPlayerParticles, this.gameObject.transform.position, Quaternion.identity, this.gameObject.transform);
        playerParticles[1] = Instantiate(Plugin.Assets.ElectricPlayerParticles, this.gameObject.transform.position, Quaternion.identity, this.gameObject.transform);
        playerParticles[2] = Instantiate(Plugin.Assets.FirePlayerParticles, this.gameObject.transform.position, Quaternion.identity, this.gameObject.transform);
        playerParticles[3] = Instantiate(Plugin.Assets.SmokePlayerParticles, this.gameObject.transform.position, Quaternion.identity, this.gameObject.transform);
        playerParticles[4] = Instantiate(Plugin.Assets.WindPlayerParticles, this.gameObject.transform.position, Quaternion.identity, this.gameObject.transform);
        playerParticles[5] = Instantiate(Plugin.Assets.BloodPlayerParticles, this.gameObject.transform.position, Quaternion.identity, this.gameObject.transform);
        foreach (GameObject particleSystem in playerParticles)
        {
            particleSystem.gameObject.SetActive(false);
        }
    }

    public void UpdateStatusEffect(CodeRebirthStatusEffects effect, bool isActive)
    {
        if (statusEffects.ContainsKey(effect))
        {
            statusEffects[effect] = isActive;
            OnStatusEffectChanged?.Invoke(effect, isActive);
        }
    }

    private void OnEnable()
    {
        OnStatusEffectChanged += HandleStatusEffectChanged;
    }

    private void OnDisable()
    {
        OnStatusEffectChanged -= HandleStatusEffectChanged;
    }

    private void HandleStatusEffectChanged(CodeRebirthStatusEffects effect, bool isActive)
    {
        int index = (int)effect - 1; // Adjust index since None is 0
        if (index >= 0 && index < playerParticles.Length)
        {
            playerParticles[index].SetActive(isActive);
        }
    }

    public void ChangeActiveStatus(CodeRebirthStatusEffects effects, bool isActive)
    {
        UpdateStatusEffect(effects, isActive);
    }
}