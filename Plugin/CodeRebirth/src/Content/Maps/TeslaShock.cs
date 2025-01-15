using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

namespace CodeRebirth.src.Content.Maps;
public class TeslaShock : CodeRebirthHazard
{
    public float distanceFromPlayer = 5f;
    public int playerDamageAmount = 40;
    public int playerZapDamageAmount = 10;
    public float pushMultiplier = 10f;
    public Transform startChainPoint = null!;
    public VisualEffect vfx = null!;
    public AudioSource teslaIdleAudioSource = null!;
    public AudioClip teslaSlowIdleSound = null!;
    public AudioClip teslaFastIdleSound = null!;
    public AudioSource teslaAudioSource = null!;
    public AudioClip teslaTouchSound = null!;
    public AudioClip teslaFirstBigChargeSound = null!;
    public AudioClip teslaConsecutiveChargeSound = null!;
    public List<AudioClip> teslaZapSounds = null!;

    private GrabbableObject chargedItemPlayerWasHolding;
    private PlayerControllerB? targetPlayer;
    private List<(GameObject obj, LineRenderer[] renderers)> lineRendererObjects = new();
    private int activeLineRenderers = 0;

    public override void Start()
    {
        base.Start();
        Plugin.ExtendedLogging("Tesla Turret initialized");
        vfx.SetFloat("SpawnRate", 10f);
        teslaIdleAudioSource.clip = teslaSlowIdleSound;
        teslaIdleAudioSource.Stop();
        teslaIdleAudioSource.Play();
        GameObject scanNode = new("ScanNode")
        {
            layer = LayerMask.NameToLayer("ScanNode")
        };
        scanNode.transform.position = this.transform.position + new Vector3(0, 2, 0);
        scanNode.transform.SetParent(this.transform, true);
        scanNode.transform.localScale = new Vector3(1, 1, 1);
        ScanNodeProperties scanNodePorperties = scanNode.AddComponent<ScanNodeProperties>();
        scanNodePorperties.maxRange = 13;
        scanNodePorperties.minRange = 0;
        scanNodePorperties.requiresLineOfSight = true;
        scanNodePorperties.headerText = "Tesla Shock";
        scanNodePorperties.subText = "My PC hated making the visuals for this thing...";
        scanNodePorperties.nodeType = 1;
        BoxCollider boxCollider = scanNode.AddComponent<BoxCollider>();
        boxCollider.center = new Vector3(0, 2, 0);
        boxCollider.size = new Vector3(1, 0.83f, 1);
    }

    private void Update()
    {
        UpdateAudio();
        if (targetPlayer != null) return;
        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
        {
            if (!ShouldContinueCharging(player)) continue;
            targetPlayer = player;
            teslaIdleAudioSource.clip = teslaFastIdleSound;
            teslaIdleAudioSource.Stop();
            teslaIdleAudioSource.Play();
            StartCoroutine(ExplodePlayerAfterDelay(player));
            break;
        }
    }

    private void UpdateAudio()
    {
        teslaIdleAudioSource.volume = Plugin.ModConfig.ConfigTeslaShockVolume.Value;
        teslaAudioSource.volume = Plugin.ModConfig.ConfigTeslaShockVolume.Value;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 3 && other.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
        {
            Vector3 direction = (player.transform.position - this.transform.position).normalized;
            Vector3 force = direction * pushMultiplier;
            player.DamagePlayer(50, true, false, CauseOfDeath.Blast, 0, false, force);

            teslaAudioSource.PlayOneShot(teslaTouchSound);
        }
    }

    private IEnumerator ExplodePlayerAfterDelay(PlayerControllerB affectedPlayer)
    {
        while (ShouldContinueCharging(affectedPlayer))
        {
            Plugin.ExtendedLogging($"Charging {affectedPlayer.playerUsername}");
            yield return StartCoroutine(ChargePlayer(affectedPlayer));
            List<Transform> validTargets = new();
            if (!ShouldContinueCharging(affectedPlayer))
            {
                float distanceFromItem = Vector3.Distance(this.transform.position, chargedItemPlayerWasHolding.transform.position);
                if (distanceFromItem <= distanceFromPlayer && !Physics.Raycast(this.transform.position, (chargedItemPlayerWasHolding.transform.position - transform.position).normalized, out _, distanceFromItem, StartOfRound.Instance.collidersAndRoomMask | LayerMask.GetMask("InteractableObject"), QueryTriggerInteraction.Ignore))
                {
                    validTargets.Add(startChainPoint);
                    validTargets.Add(chargedItemPlayerWasHolding.transform);
                    
                    DrawChainLines(validTargets);
                    DamageTargets(validTargets, affectedPlayer);

                    yield return StartCoroutine(HandleConsecutiveChargeEffects());
                }
            }
            else
            {
                List<PlayerControllerB> sortedPlayers = GetPlayersSortedByDistance(affectedPlayer);
                List<EnemyAI> sortedEnemies = GetEnemiesSortedByDistance(affectedPlayer);
                List<Transform> sortedTargets = GetTargetsSortedByDistanceIncludingAffectedPlayer(affectedPlayer, sortedPlayers, sortedEnemies);

                validTargets = GetValidChainTargets(sortedTargets);
            
                DrawChainLines(validTargets);
                DamageTargets(validTargets, affectedPlayer);

                yield return StartCoroutine(HandleConsecutiveChargeEffects());
            }
        }
        ResetEffects();
    }

    private bool ShouldContinueCharging(PlayerControllerB affectedPlayer)
    {
        float distanceToPlayer = Vector3.Distance(affectedPlayer.transform.position, this.transform.position);
        return distanceToPlayer <= distanceFromPlayer
            && !affectedPlayer.isPlayerDead
            && affectedPlayer.isPlayerControlled
            && PlayerCarryingSomethingConductive(affectedPlayer)
            && !Physics.Raycast(affectedPlayer.transform.position, (transform.position - affectedPlayer.transform.position).normalized, out _, distanceToPlayer, StartOfRound.Instance.collidersAndRoomMask | LayerMask.GetMask("InteractableObject"), QueryTriggerInteraction.Ignore);
    }

    private IEnumerator ChargePlayer(PlayerControllerB affectedPlayer)
    {
        float timeToCharge = teslaFirstBigChargeSound.length;
        if (affectedPlayer.playerSteamId == Plugin.GLITCH_STEAM_ID)
        {
            timeToCharge /= 5f;
        }
        teslaAudioSource.PlayOneShot(teslaFirstBigChargeSound);
        vfx.SetFloat("SpawnRate", 10);
        yield return new WaitForSeconds(timeToCharge/5);
        vfx.SetFloat("SpawnRate", 20);
        yield return new WaitForSeconds(timeToCharge/5);
        vfx.SetFloat("SpawnRate", 40);
        yield return new WaitForSeconds(timeToCharge/5);
        vfx.SetFloat("SpawnRate", 60);
        yield return new WaitForSeconds(timeToCharge/5);
        vfx.SetFloat("SpawnRate", 80);
        yield return new WaitForSeconds(timeToCharge/5);
        vfx.SetFloat("SpawnRate", 100); // todo: change this to a hash or whatever its called
    }

    private List<PlayerControllerB> GetPlayersSortedByDistance(PlayerControllerB affectedPlayer)
    {
        return StartOfRound.Instance.allPlayerScripts
            .Where(player => player != affectedPlayer && player.isPlayerControlled && !player.isPlayerDead)
            .OrderBy(player => Vector3.Distance(player.transform.position, affectedPlayer.transform.position))
            .ToList();
    }

    private List<EnemyAI> GetEnemiesSortedByDistance(PlayerControllerB affectedPlayer)
    {
        return RoundManager.Instance.SpawnedEnemies
            .Where(enemy => !enemy.isEnemyDead)
            .OrderBy(enemy => Vector3.Distance(enemy.transform.position, affectedPlayer.transform.position))
            .ToList();
    }

    private List<Transform> GetTargetsSortedByDistanceIncludingAffectedPlayer(PlayerControllerB affectedPlayer, List<PlayerControllerB> sortedPlayers, List<EnemyAI> sortedEnemies)
    {
        List<Transform> targets =
        [
            startChainPoint, affectedPlayer.transform,
            .. sortedPlayers.Select(player => player.transform),
            .. sortedEnemies.Select(enemy => enemy.transform),
        ];
        return targets.OrderBy(x => Vector3.Distance(x.position, startChainPoint.position)).ToList();
    }

    private List<Transform> GetValidChainTargets(IEnumerable<Transform> sortedTargets)
    {
        List<Transform> validTargets = new List<Transform> { sortedTargets.First() }; // Start with startChainPoint

        foreach (var target in sortedTargets)
        {
            if (Vector3.Distance(validTargets.Last().position, target.position) > distanceFromPlayer) break;
            validTargets.Add(target);
        }

        return validTargets;
    }

    private void DrawChainLines(IEnumerable<Transform> sortedTargets)
    {
        int linesNeeded = sortedTargets.Count() - 1;
        EnsureLineRendererPool(linesNeeded);
        Vector3? prevPosition = null;
        int index = 0;
        foreach (var target in sortedTargets)
        {
            if (prevPosition != null)
            {
                LineRenderer[] lineRenderers = lineRendererObjects[index].renderers;
                foreach (LineRenderer lineRenderer in lineRenderers)
                {
                    lineRenderer.positionCount = 2;
                    lineRenderer.SetPosition(0, prevPosition.Value);
                    lineRenderer.SetPosition(1, target.position);
                    teslaAudioSource.transform.position = target.position;
                    teslaAudioSource.PlayOneShot(teslaZapSounds[UnityEngine.Random.Range(0, teslaZapSounds.Count - 1)]);
                    lineRenderer.enabled = true;
                    StartCoroutine(DisableRendererAfterDelay(lineRenderer, 0.5f));
                }
            }
            prevPosition = target.position;
        }

        DisableExtraLineRenderers(linesNeeded);
        activeLineRenderers = linesNeeded;
    }

    private void DamageTargets(List<Transform> sortedTargets, PlayerControllerB startingPlayer)
    {
        for (int i = 1; i < sortedTargets.Count; i++) // Skip the first point (startChainPoint)
        {
            Transform currentTarget = sortedTargets[i];
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(p => p.transform == currentTarget);
            EnemyAI enemy = RoundManager.Instance.SpawnedEnemies.FirstOrDefault(e => e.transform == currentTarget);
            if (player != null)
            {
                player.DamagePlayer(80 * (player.playerSteamId == Plugin.GLITCH_STEAM_ID ? 2 : 1), true, false, CauseOfDeath.Burning, 0, false, default);
            }
            else if (enemy != null)
            {
                enemy.HitEnemy(3, startingPlayer, true, -1);
            }
            else
            {
                Plugin.ExtendedLogging("Shooting non player or non enemy target");
            }
        }
    }

    private IEnumerator HandleConsecutiveChargeEffects()
    {
        teslaAudioSource.transform.position = transform.position;
        teslaAudioSource.PlayOneShot(teslaConsecutiveChargeSound);
        teslaIdleAudioSource.clip = teslaSlowIdleSound;
        teslaIdleAudioSource.Stop();
        teslaIdleAudioSource.Play();
        vfx.SetFloat("SpawnRate", 80);
        yield return new WaitForSeconds(teslaConsecutiveChargeSound.length / 5);
        vfx.SetFloat("SpawnRate", 60f);
        yield return new WaitForSeconds(teslaConsecutiveChargeSound.length / 5);
        vfx.SetFloat("SpawnRate", 40);
        yield return new WaitForSeconds(teslaConsecutiveChargeSound.length / 5);
        vfx.SetFloat("SpawnRate", 20f);
        yield return new WaitForSeconds(teslaConsecutiveChargeSound.length / 5);
        vfx.SetFloat("SpawnRate", 0f);
        yield return new WaitForSeconds(teslaConsecutiveChargeSound.length / 5);
        yield return new WaitForSeconds(5f);
    }

    private void ResetEffects()
    {
        vfx.SetFloat("SpawnRate", 10f);
        DisableAllLineRenderers();
        activeLineRenderers = 0;
        targetPlayer = null;
    }

    private void DisableExtraLineRenderers(int linesNeeded)
    {
        for (int i = linesNeeded; i < activeLineRenderers; i++)
        {
            LineRenderer[] lineRenderers = lineRendererObjects[i].renderers;
            foreach (LineRenderer lineRenderer in lineRenderers)
            {
                lineRenderer.enabled = false;
            }
        }
    }

    private void DisableAllLineRenderers()
    {
        foreach (var lineRendererDict in lineRendererObjects)
        {
            foreach (LineRenderer line in lineRendererDict.renderers)
            {
                line.enabled = false;
            }
        }
    }

    private IEnumerator DisableRendererAfterDelay(LineRenderer lineRenderer, float delay)
    {
        yield return new WaitForSeconds(delay);
        lineRenderer.enabled = false;
    }

    private void EnsureLineRendererPool(int linesNeeded)
    {
        while (lineRendererObjects.Count < linesNeeded)
        {
            GameObject newLineObject = Instantiate(MapObjectHandler.Instance.TeslaShock.ChainLightningPrefab, this.transform);
            LineRenderer[] lineRenderers = newLineObject.GetComponentsInChildren<LineRenderer>();
            lineRendererObjects.Add((newLineObject, lineRenderers));
            foreach (LineRenderer lineRenderer in lineRenderers)
            {
                lineRenderer.enabled = false;
            }
        }
    }

    private bool PlayerCarryingSomethingConductive(PlayerControllerB player)
    {
        foreach (var item in player.ItemSlots)
        {
            if (item == null || item.itemProperties == null) continue;
            if (item.itemProperties.isConductiveMetal)
            {
                chargedItemPlayerWasHolding = item;
                return true;
            }
        }
        return false;
    }
}