using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

namespace CodeRebirth.src.Content.Maps;
public class TeslaShock : NetworkBehaviour
{
    public float distanceFromPlayer = 5f;
    public int playerDamageAmount = 40;
    public float delayBeforeExplodePlayer = 3f;
    public float delayBetweenZaps = 2f;
    public float pushMultiplier = 10f;
    public Transform startChainPoint = null!;
    public VisualEffect vfx = null!;

    private PlayerControllerB? targetPlayer;
    private Dictionary<GameObject, List<LineRenderer>> createdLineRenderers = new();
    private int activeLineRenderers = 0;

    private void Start()
    {
        vfx.SetFloat("SpawnRate", 20f);
    }

    private void Update()
    {
        if (targetPlayer != null) return;
        bool somethingConductiveFound = PlayerCarryingSomethingCondutive(GameNetworkManager.Instance.localPlayerController);
        if (!somethingConductiveFound) return;
        if (Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, transform.position) > distanceFromPlayer) return;

        targetPlayer = GameNetworkManager.Instance.localPlayerController;
        SetTargetPlayerClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, targetPlayer));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
        {
            Vector3 direction = (player.transform.position - this.transform.position).normalized;
            Vector3 force = direction * pushMultiplier;
            player.DamagePlayer(playerDamageAmount, true, false, CauseOfDeath.Blast, 0, false, force);
        }
    }

    [ClientRpc]
    private void SetTargetPlayerClientRpc(int playerIndex)
    {
        targetPlayer = StartOfRound.Instance.allPlayerScripts[playerIndex];
        StartCoroutine(ExplodePlayerAfterDelay(delayBeforeExplodePlayer, targetPlayer));
    }

    private IEnumerator ExplodePlayerAfterDelay(float delay, PlayerControllerB affectedPlayer)
    {
        yield return new WaitForSeconds(delay);

        while (Vector3.Distance(affectedPlayer.transform.position, transform.position) <= distanceFromPlayer && !affectedPlayer.isPlayerDead && PlayerCarryingSomethingCondutive(affectedPlayer))
        {
            affectedPlayer.DamagePlayer(playerDamageAmount, true, false, CauseOfDeath.Blast, 0, false, default);

            List<PlayerControllerB> playersSortedByDistance = StartOfRound.Instance.allPlayerScripts
                .Where(player => player != affectedPlayer && player.isPlayerControlled && !player.isPlayerDead && Vector3.Distance(player.transform.position, affectedPlayer.transform.position) <= distanceFromPlayer)
                .OrderBy(player => Vector3.Distance(player.transform.position, affectedPlayer.transform.position))
                .ToList();

            List<EnemyAI> enemiesSortedByDistance = RoundManager.Instance.SpawnedEnemies
                .Where(enemy => !enemy.isEnemyDead && Vector3.Distance(enemy.transform.position, affectedPlayer.transform.position) <= distanceFromPlayer)
                .OrderBy(enemy => Vector3.Distance(enemy.transform.position, affectedPlayer.transform.position))
                .ToList();

            // Combine players and enemies into a single list ordered by proximity
            List<Transform> targetsSortedByDistance =
            [
                startChainPoint, affectedPlayer.gameplayCamera.transform,
                .. playersSortedByDistance.Select(player => player.gameplayCamera.transform),
                .. enemiesSortedByDistance.Select(enemy => enemy.transform),
            ];

            // Draw lines between consecutive targets
            int linesNeeded = targetsSortedByDistance.Count - 1;
            EnsureLineRendererPool(linesNeeded);

            for (int i = 0; i < linesNeeded; i++)
            {
                GameObject lineObject = createdLineRenderers.Keys.ElementAt(i);
                List<LineRenderer> lineRenderers = createdLineRenderers[lineObject];
                foreach (LineRenderer lineRenderer in lineRenderers)
                {
                    lineRenderer.positionCount = 2;

                    Vector3 startPos = targetsSortedByDistance[i].position;
                    Vector3 endPos = targetsSortedByDistance[i + 1].position;

                    if (playersSortedByDistance.Any(p => p.gameplayCamera.transform == targetsSortedByDistance[i]))
                    {
                        startPos -= targetsSortedByDistance[i].up * 0.25f;
                    }

                    if (playersSortedByDistance.Any(p => p.gameplayCamera.transform == targetsSortedByDistance[i + 1]))
                    {
                        endPos -= targetsSortedByDistance[i + 1].up * 0.25f;
                    }

                    lineRenderer.SetPosition(0, startPos);
                    lineRenderer.SetPosition(1, endPos);
                    lineRenderer.enabled = true;
                    StartCoroutine(DisableRendererAfterDelay(lineRenderer, 0.5f));
                }

                // Damage each target in the chain
                if (i > 0) // Skip the first point (startChainPoint)
                {
                    Transform currentTarget = targetsSortedByDistance[i + 1];
                    PlayerControllerB player = playersSortedByDistance.FirstOrDefault(p => p.gameplayCamera.transform == currentTarget);
                    if (player != null)
                    {
                        player?.DamagePlayer(10, true, false, CauseOfDeath.Burning, 0, false, default);
                    }
                    else
                    {
                        EnemyAI enemy = enemiesSortedByDistance.FirstOrDefault(e => e.transform == currentTarget);
                        enemy?.HitEnemy(1, null, true, -1);
                    }
                }
            }

            // Disable any extra line renderers that aren't needed
            for (int i = linesNeeded; i < activeLineRenderers; i++)
            {
                GameObject lineObject = createdLineRenderers.Keys.ElementAt(i);
                List<LineRenderer> lineRenderers = createdLineRenderers[lineObject];
                foreach (LineRenderer lineRenderer in lineRenderers)
                {
                    lineRenderer.enabled = false;
                }
            }

            activeLineRenderers = linesNeeded;
            vfx.SetFloat("SpawnRate", 20f);
            yield return new WaitForSeconds(delayBetweenZaps / 2);
            vfx.SetFloat("SpawnRate", 40);
            yield return new WaitForSeconds(delayBetweenZaps / 2);
        }
        vfx.SetFloat("SpawnRate", 5f);
        // Disable all line renderers when done
        foreach (var lineRendererDict in createdLineRenderers)
        {
            foreach (LineRenderer line in lineRendererDict.Value)
            {
                line.enabled = false;
            }
        }

        activeLineRenderers = 0;
        targetPlayer = null;
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

    private bool PlayerCarryingSomethingCondutive(PlayerControllerB player)
    {
        foreach (var item in player.ItemSlots)
        {
            if (item == null || item.itemProperties == null) continue;
            if (item.itemProperties.isConductiveMetal) return true;
        }
        return false;
    }
}