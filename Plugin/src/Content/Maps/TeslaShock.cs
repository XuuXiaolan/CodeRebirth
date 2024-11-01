using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

namespace CodeRebirth.src.Content.Maps;
public class TeslaShock : NetworkBehaviour // have a background audiosource constantly low volume playing a sound
{
    public float distanceFromPlayer = 5f;
    public int playerDamageAmount = 40;
    public float pushMultiplier = 10f;
    public Transform startChainPoint = null!;
    public VisualEffect vfx = null!;
    public AudioSource teslaIdleAudioSource = null!;
    public AudioClip teslaSlowIdleSound = null!;
    public AudioClip teslaFastIdleSound = null!;
    public AudioSource teslaAudioSource = null!;
    public AudioClip teslaTouchSound = null!;
    public AudioClip teslaChargeSound = null!;
    public AudioClip teslaFastChargeSound = null!;
    public List<AudioClip> teslaZapSounds = null!;

    private PlayerControllerB? targetPlayer;
    private Dictionary<GameObject, List<LineRenderer>> createdLineRenderers = new();
    private int activeLineRenderers = 0;

    private void Start()
    {
        vfx.SetFloat("SpawnRate", 20f);
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
        if (targetPlayer != null) return;
        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
        {
            if (!player.isPlayerControlled || player.isPlayerDead || Physics.Raycast(player.transform.position, transform.position - player.transform.position, out RaycastHit hit, distanceFromPlayer, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Collide)) continue;
            bool somethingConductiveFound = PlayerCarryingSomethingConductive(player);
            if (!somethingConductiveFound) continue;
            if (Vector3.Distance(player.transform.position, transform.position) > distanceFromPlayer) continue;
            targetPlayer = player;
            teslaIdleAudioSource.clip = teslaFastIdleSound;
            teslaIdleAudioSource.Stop();
            teslaIdleAudioSource.Play();
            StartCoroutine(ExplodePlayerAfterDelay(player));
            break;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 3 && other.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
        {
            Vector3 direction = (player.transform.position - this.transform.position).normalized;
            Vector3 force = direction * pushMultiplier;
            player.DamagePlayer(playerDamageAmount, true, false, CauseOfDeath.Blast, 0, false, force);

            teslaAudioSource.PlayOneShot(teslaTouchSound);
        }
    }

    private IEnumerator ExplodePlayerAfterDelay(PlayerControllerB affectedPlayer)
    {
        while (ShouldContinueCharging(affectedPlayer))
        {
            yield return StartCoroutine(ChargePlayer(affectedPlayer));

            List<PlayerControllerB> sortedPlayers = GetPlayersSortedByDistance(affectedPlayer);
            List<EnemyAI> sortedEnemies = GetEnemiesSortedByDistance(affectedPlayer);
            List<Transform> sortedTargets = GetTargetsSortedByDistanceIncludingAffectedPlayer(affectedPlayer, sortedPlayers, sortedEnemies);

            List<Transform> validTargets = GetValidChainTargets(sortedTargets);

            DrawChainLines(validTargets);
            DamageTargets(validTargets);

            yield return StartCoroutine(HandleFastChargeEffects());
        }

        ResetEffects();
    }

    private bool ShouldContinueCharging(PlayerControllerB affectedPlayer)
    {
        return Vector3.Distance(affectedPlayer.transform.position, transform.position) <= distanceFromPlayer
            && !affectedPlayer.isPlayerDead
            && PlayerCarryingSomethingConductive(affectedPlayer);
    }

    private IEnumerator ChargePlayer(PlayerControllerB affectedPlayer)
    {
        teslaAudioSource.PlayOneShot(teslaChargeSound);
        yield return new WaitForSeconds(teslaChargeSound.length);
        teslaAudioSource.PlayOneShot(teslaZapSounds[UnityEngine.Random.Range(0, teslaZapSounds.Count - 1)]);
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

    private List<Transform> GetValidChainTargets(List<Transform> sortedTargets)
    {
        List<Transform> validTargets = new List<Transform> { sortedTargets[0] }; // Start with startChainPoint

        for (int i = 1; i < sortedTargets.Count; i++)
        {
            if (Vector3.Distance(validTargets.Last().position, sortedTargets[i].position) <= distanceFromPlayer)
            {
                validTargets.Add(sortedTargets[i]);
            }
            else
            {
                break;
            }
        }

        return validTargets;
    }

    private void DrawChainLines(List<Transform> sortedTargets)
    {
        int linesNeeded = sortedTargets.Count - 1;
        EnsureLineRendererPool(linesNeeded);

        for (int i = 0; i < linesNeeded; i++)
        {
            GameObject lineObject = createdLineRenderers.Keys.ElementAt(i);
            List<LineRenderer> lineRenderers = createdLineRenderers[lineObject];
            foreach (LineRenderer lineRenderer in lineRenderers)
            {
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, sortedTargets[i].position);
                lineRenderer.SetPosition(1, sortedTargets[i + 1].position);
                lineRenderer.enabled = true;
                StartCoroutine(DisableRendererAfterDelay(lineRenderer, 0.5f));
            }
        }

        DisableExtraLineRenderers(linesNeeded);
        activeLineRenderers = linesNeeded;
    }

    private void DamageTargets(List<Transform> sortedTargets)
    {
        for (int i = 1; i < sortedTargets.Count; i++) // Skip the first point (startChainPoint)
        {
            Transform currentTarget = sortedTargets[i];
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(p => p.transform == currentTarget);
            if (player != null)
            {
                player?.DamagePlayer(10, true, false, CauseOfDeath.Burning, 0, false, default);
            }
            else
            {
                EnemyAI enemy = RoundManager.Instance.SpawnedEnemies.FirstOrDefault(e => e.transform == currentTarget);
                enemy?.HitEnemy(3, null, true, -1);
            }
        }
    }

    private IEnumerator HandleFastChargeEffects()
    {
        teslaAudioSource.PlayOneShot(teslaFastChargeSound);
        vfx.SetFloat("SpawnRate", 20f);
        yield return new WaitForSeconds(teslaFastChargeSound.length / 2);
        vfx.SetFloat("SpawnRate", 40);
        yield return new WaitForSeconds(teslaFastChargeSound.length / 2);
    }

    private void ResetEffects()
    {
        teslaIdleAudioSource.clip = teslaSlowIdleSound;
        teslaIdleAudioSource.Stop();
        teslaIdleAudioSource.Play();
        vfx.SetFloat("SpawnRate", 5f);
        DisableAllLineRenderers();
        activeLineRenderers = 0;
        targetPlayer = null;
    }

    private void DisableExtraLineRenderers(int linesNeeded)
    {
        for (int i = linesNeeded; i < activeLineRenderers; i++)
        {
            GameObject lineObject = createdLineRenderers.Keys.ElementAt(i);
            List<LineRenderer> lineRenderers = createdLineRenderers[lineObject];
            foreach (LineRenderer lineRenderer in lineRenderers)
            {
                lineRenderer.enabled = false;
            }
        }
    }

    private void DisableAllLineRenderers()
    {
        foreach (var lineRendererDict in createdLineRenderers)
        {
            foreach (LineRenderer line in lineRendererDict.Value)
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
        while (createdLineRenderers.Count < linesNeeded)
        {
            GameObject newLineObject = Instantiate(MapObjectHandler.Instance.TeslaShock.ChainLightningPrefab, this.transform);
            List<LineRenderer> lineRenderers = newLineObject.GetComponentsInChildren<LineRenderer>().ToList();
            if (lineRenderers.Count > 0)
            {
                createdLineRenderers.Add(newLineObject, lineRenderers);
                foreach (LineRenderer lineRenderer in lineRenderers)
                {
                    lineRenderer.enabled = false;
                }
            }
        }
    }

    private bool PlayerCarryingSomethingConductive(PlayerControllerB player)
    {
        foreach (var item in player.ItemSlots)
        {
            if (item == null || item.itemProperties == null) continue;
            if (item.itemProperties.isConductiveMetal) return true;
        }
        return false;
    }
}