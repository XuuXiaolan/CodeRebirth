using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.MiscScripts;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Items;
/// <summary>
/// Follows you, kicked like a football, any damage it takes deals some damage (20-30) to the player.
/// </summary>
[RequireComponent(typeof(SmartAgentNavigator))]
public class PuppeteersVoodoo : NetworkBehaviour, IHittable
{
    public NavMeshAgent agent = null!;
    public SmartAgentNavigator smartAgentNavigator = null!;
    [HideInInspector] public Puppeteer puppeteerCreatedBy = null!;
    [HideInInspector] public PlayerControllerB playerControlled = null!;

    [Header("Voodoo Damage Settings")]
    [Tooltip("Multiplier for the damage transferred to the linked player.")]
    [SerializeField]
    private int damageTransferMultiplier = 20; // 20-30 as described, can be randomized or set
    public void Start()
    {
        if (puppeteerCreatedBy == null)
        {

            return;
        }
        smartAgentNavigator.SetAllValues(puppeteerCreatedBy.isOutside);
    }

    public void Update()
    {
        if (puppeteerCreatedBy == null)
        {
            // puppeteer is dead.
        }
    }

    public void OnDollDamaged(int damage, CauseOfDeath causeOfDeath)
    {
        if (playerControlled != null)
        {
            // We assume your PlayerControllerB has a method to handle direct damage:
            int finalDamage = Mathf.RoundToInt(damage * damageTransferMultiplier);
            playerControlled.DamagePlayer(finalDamage, true, true, causeOfDeath); 
        }
    }

    public void BreakDoll()
    {
    }

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        OnDollDamaged(force, CauseOfDeath.Bludgeoning);
        return true;
    }
}