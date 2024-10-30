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
    }

    private void Update()
    {
        if (targetPlayer != null) return;
        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
        {
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
        while (Vector3.Distance(affectedPlayer.transform.position, transform.position) <= distanceFromPlayer && !affectedPlayer.isPlayerDead && PlayerCarryingSomethingConductive(affectedPlayer))
        {
            teslaAudioSource.PlayOneShot(teslaChargeSound);
            yield return new WaitForSeconds(teslaChargeSound.length);
            teslaAudioSource.PlayOneShot(teslaZapSounds[UnityEngine.Random.Range(0, teslaZapSounds.Count - 1)]);
            if (Vector3.Distance(affectedPlayer.transform.position, transform.position) > distanceFromPlayer || affectedPlayer.isPlayerDead || !PlayerCarryingSomethingConductive(affectedPlayer))
            {
                continue;
            }
            affectedPlayer.DamagePlayer(playerDamageAmount, true, false, CauseOfDeath.Blast, 0, false, default);

            List<PlayerControllerB> playersSortedByDistance = StartOfRound.Instance.allPlayerScripts
                .Where(player => player != affectedPlayer && player.isPlayerControlled && !player.isPlayerDead)
                .ToList();

            // First sort by distance to the affected player
            playersSortedByDistance = playersSortedByDistance
                .OrderBy(player => Vector3.Distance(player.gameplayCamera.transform.position, affectedPlayer.gameplayCamera.transform.position))
                .ToList();

            List<PlayerControllerB> finalSortedPlayers =
            [
                // Start with the closest player to the affected player
                playersSortedByDistance[0],
            ];
            playersSortedByDistance.RemoveAt(0);

            while (playersSortedByDistance.Count > 0)
            {
                // Get the last added player's position
                Vector3 lastPlayerPosition = finalSortedPlayers.Last().gameplayCamera.transform.position;

                // Find the closest player to the last added player
                var closestPlayer = playersSortedByDistance
                    .OrderBy(player => Vector3.Distance(player.gameplayCamera.transform.position, lastPlayerPosition))
                    .First();

                // Add the closest player to the final sorted list and remove it from the remaining players
                finalSortedPlayers.Add(closestPlayer);
                playersSortedByDistance.Remove(closestPlayer);
            }

            List<EnemyAI> enemiesSortedByDistance = RoundManager.Instance.SpawnedEnemies
                .Where(enemy => !enemy.isEnemyDead)
                .ToList();

            // First sort by distance to the target player
            enemiesSortedByDistance = enemiesSortedByDistance
                .OrderBy(enemy => Vector3.Distance(enemy.transform.position, affectedPlayer.gameplayCamera.transform.position))
                .ToList();

            List<EnemyAI> finalSortedEnemies =
            [
                // Start with the closest enemy to the target player
                enemiesSortedByDistance[0],
            ];
            enemiesSortedByDistance.RemoveAt(0);

            while (enemiesSortedByDistance.Count > 0)
            {
                // Get the last added enemy's position
                Vector3 lastEnemyPosition = finalSortedEnemies.Last().transform.position;

                // Find the closest enemy to the last added enemy
                var closestEnemy = enemiesSortedByDistance
                    .OrderBy(enemy => Vector3.Distance(enemy.transform.position, lastEnemyPosition))
                    .First();

                // Add the closest enemy to the final sorted list and remove it from the remaining enemies
                finalSortedEnemies.Add(closestEnemy);
                enemiesSortedByDistance.Remove(closestEnemy);
            }
            // Combine players and enemies into a single list ordered by proximity
            List<Transform> targetsSortedByDistance =
            [
                startChainPoint, affectedPlayer.gameplayCamera.transform,
                .. playersSortedByDistance.Select(player => player.gameplayCamera.transform),
                .. enemiesSortedByDistance.Select(enemy => enemy.transform),
            ];

            targetsSortedByDistance.OrderBy(x => Vector3.Distance(x.position, startChainPoint.position));


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
                        startPos -= targetsSortedByDistance[i].up * 0.5f;
                    }

                    if (playersSortedByDistance.Any(p => p.gameplayCamera.transform == targetsSortedByDistance[i + 1]))
                    {
                        endPos -= targetsSortedByDistance[i + 1].up * 0.5f;
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
                        enemy?.HitEnemy(3, null, true, -1);
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
            
            teslaAudioSource.PlayOneShot(teslaFastChargeSound);
            vfx.SetFloat("SpawnRate", 20f);
            yield return new WaitForSeconds(teslaFastChargeSound.length / 2);
            vfx.SetFloat("SpawnRate", 40);
            yield return new WaitForSeconds(teslaFastChargeSound.length / 2);
        }
        teslaIdleAudioSource.clip = teslaSlowIdleSound;
        teslaIdleAudioSource.Stop();
        teslaIdleAudioSource.Play();
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