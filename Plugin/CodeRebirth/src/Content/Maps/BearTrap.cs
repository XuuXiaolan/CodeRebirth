using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dawn;
using Dawn.Utils;
using GameNetcodeStuff;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;

public class BearTrap : CodeRebirthHazard, IHittable
{
    public Animator trapAnimator = null!;
    public Collider trapCollider = null!;
    public float delayBeforeReset = 3.0f;
    public InteractTrigger trapTrigger = null!;
    public AudioSource trapAudioSource = null!;
    public AudioClip resetTrapEndSound = null!;
    public AudioClip triggerSound = null!;
    public AudioClip resetTrapSound = null!;
    public AudioClip poppingTireSound = null!;

    private float damagePlayerTimer = 0f;
    private Vector3 caughtPosition = Vector3.zero;
    internal PlayerControllerB? playerCaught = null;
    private EnemyAI? enemyCaught = null;
    private bool isTriggered = false;
    private bool canTrigger = true;
    internal bool byProduct = false;
    private static readonly int IsTrapTriggered = Animator.StringToHash("isTrapTriggered");
    private static readonly int IsTrapResetting = Animator.StringToHash("isTrapResetting");

    private static readonly List<BearTrap> Instances = new();

    internal static void Init()
    {
        IL.StormyWeather.LightningStrikeRandom += AddBearTrapsToPossibleNodes;
        On.StormyWeather.BeginDay += AddBearTrapsToPossibleNodes;
    }

    // TODO: adjust for beartraps only attracting lightning or being added to list if they're triggered?
    private static void AddBearTrapsToPossibleNodes(On.StormyWeather.orig_BeginDay orig, StormyWeather self)
    {
        orig(self);
        List<GameObject> newList = self.outsideNodes.ToList();
        newList.AddRange(Instances.Select(x => x.gameObject));
        self.outsideNodes = newList.ToArray();
    }

    private static void AddBearTrapsToPossibleNodes(ILContext il)
    {
        ILCursor cursor = new(il);
        if (!cursor.TryGotoNext(
            MoveType.Before,
            il => il.MatchLdarg(0),
            il => il.MatchLdfld<StormyWeather>(nameof(StormyWeather.seed)),
            il => il.MatchLdcI4(0),
            il => il.MatchLdarg(0),
            il => il.MatchLdfld<StormyWeather>(nameof(StormyWeather.outsideNodes)),
            il => il.MatchLdlen(),
            il => il.MatchConvI4(),
            il => il.MatchCallvirt(out _),
            il => il.MatchStloc(1)
        ))
        {
            Plugin.Logger.LogWarning($"Could not match StormyWeather.LightningStrikeRandom (1), Meaning bear traps will not be targetted for lightning strikes.");
            return;
        }

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitLdfld<StormyWeather>(nameof(StormyWeather.outsideNodes));
        cursor.EmitDelegate((GameObject[] outsideNodes) =>
        {
            if (Instances.Count <= 0)
            {
                return;
            }

            List<GameObject> newList = outsideNodes.ToList();
            newList.AddRange(Instances.Select(x => x.gameObject));
            newList.ToArray();
        });

        if (!cursor.TryGotoNext(
            MoveType.After,
            il => il.MatchCall(out _),
            il => il.MatchLdloc(0),
            il => il.MatchLdcR4(15),
            il => il.MatchLdarg(0),
            il => il.MatchLdfld<StormyWeather>(nameof(StormyWeather.navHit)),
            il => il.MatchLdarg(0),
            il => il.MatchLdfld<StormyWeather>(nameof(StormyWeather.seed)),
            il => il.MatchLdcI4(out _),
            il => il.MatchLdcR4(1),
            il => il.MatchCallvirt(out _),
            il => il.MatchStloc(0)
        ))
        {
            Plugin.Logger.LogWarning($"Could not match StormyWeather.LightningStrikeRandom (2), Meaning bear traps will not be properly targetted for lightning strikes.");
            return;
        }

        cursor.Emit(OpCodes.Ldloc_1);
        cursor.Emit(OpCodes.Ldloc_0);
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitLdfld<StormyWeather>(nameof(StormyWeather.outsideNodes));
        cursor.EmitDelegate((int index, Vector3 position, GameObject[] outsideNodes) =>
        {
            if (!outsideNodes[index].TryGetComponent(out BearTrap _))
            {
                return;
            }

            position = outsideNodes[index].transform.position;
            Plugin.ExtendedLogging($"Lightning strike redirected to hit {outsideNodes[index].name} at {position}");
        });
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Instances.Add(this);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Instances.Remove(this);
    }

    public override void Start()
    {
        base.Start();
        if (!IsServer)
            return;

        if (byProduct)
        {
            float newTrapTime = UnityEngine.Random.Range(trapTrigger.timeToHold - 1.5f, trapTrigger.timeToHold + 0.5f);
            SyncRandomResetTrapTimeClientRpc(newTrapTime);
            return;
        }

        Vector3 position = this.transform.position;
        for (int i = 0; i < UnityEngine.Random.Range(4, 8) - (this is BoomTrap ? 3 : 0); i++)
        {
            Vector3 vector = RoundManager.Instance.GetRandomNavMeshPositionInRadius(position, 10f) + (Vector3.up * 2);
            Physics.Raycast(vector, Vector3.down, out RaycastHit hit, 100, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore);
            if (hit.collider == null)
                continue;

            DawnMapObjectInfo mapObjectInfo = LethalContent.MapObjects[CodeRebirthMapObjectKeys.GravelBearTrap];
            string surfaceTag = hit.collider.tag;
            if (this is BoomTrap || UnityEngine.Random.Range(0, 1000) < 5)
            {
                mapObjectInfo = LethalContent.MapObjects[CodeRebirthMapObjectKeys.BoomTrap];
            }
            else
            {
                if (hit.collider.TryGetComponent(out DawnSurface surface) && surface.TryGetFootstepIndex(hit.point, false, out int footstepSurfaceIndex) && footstepSurfaceIndex != -1)
                {
                    DawnSurfaceInfo surfaceInfo = StartOfRound.Instance.footstepSurfaces[footstepSurfaceIndex].DawnInfo;
                    surfaceTag = surfaceInfo.Surface.surfaceTag;
                }

                if (surfaceTag.Equals("Grass", StringComparison.OrdinalIgnoreCase))
                {
                    mapObjectInfo = LethalContent.MapObjects[CodeRebirthMapObjectKeys.GrassBearTrap];
                }
                else if (surfaceTag.Equals("Snow", StringComparison.OrdinalIgnoreCase))
                {
                    mapObjectInfo = LethalContent.MapObjects[CodeRebirthMapObjectKeys.SnowBearTrap];
                }
            }

            GameObject beartrap = mapObjectInfo.GetMapObjectPrefab()!;
            GameObject spawnedTrap = GameObject.Instantiate(beartrap, hit.point, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
            spawnedTrap.GetComponent<BearTrap>().byProduct = true;
            Plugin.ExtendedLogging($"Spawning {beartrap.name} at {hit.point}");
            spawnedTrap.transform.up = hit.normal;
            spawnedTrap.GetComponent<NetworkObject>().Spawn(true);
            position = spawnedTrap.transform.position;
        }
        this.NetworkObject.Despawn(true);
    }

    private void Update()
    {
        trapTrigger.interactable = isTriggered;
        UpdateAudio();
        if (playerCaught != null)
        {
            damagePlayerTimer -= Time.deltaTime;
            float distanceToPlayer = Vector3.Distance(playerCaught.transform.position, this.transform.position);
            if (distanceToPlayer > 15)
            {
                DoReleaseTrap();
                return;
            }
            playerCaught.transform.position = Vector3.Lerp(playerCaught.transform.position, caughtPosition, 5f * Time.deltaTime);
        }

        if (enemyCaught == null)
        {
            return;
        }

        enemyCaught.agent.velocity = Vector3.zero;
    }

    public void SetWheelFriction(BearTrapWheelProxy bearTrapWheelProxy)
    {
        if (!Plugin.ModConfig.ConfigBearTrapsPopTires.Value)
        {
            return;
        }

        bearTrapWheelProxy.PunctureWheel();
    }

    private void UpdateAudio()
    {
        trapAudioSource.volume = Plugin.ModConfig.ConfigBearTrapVolume.Value;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered || !canTrigger)
        {
            return;
        }

        if (other.gameObject.layer == 30 && other.TryGetComponent(out BearTrapWheelProxy wheelProxy))
        {
            if (wheelProxy.AlreadyPunctured)
            {
                return;
            }
            trapAudioSource.PlayOneShot(poppingTireSound);
            SetWheelFriction(wheelProxy);
        }
        else if (other.gameObject.layer == 3 && other.TryGetComponent(out PlayerControllerB player) && player.IsLocalPlayer)
        {
            TriggerTrapServerRpc(player);
        }
        else if (other.gameObject.layer == 19 && other.TryGetComponent(out EnemyAICollisionDetect enemyAICollisionDetect))
        {
            if (enemyAICollisionDetect.mainScript.enemyType.EnemySize == EnemySize.Giant)
            {
                return;
            }

            TriggerTrap(enemyAICollisionDetect.mainScript);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TriggerTrapServerRpc(PlayerControllerReference playerControllerReference)
    {
        TriggerTrapClientRpc(playerControllerReference);
    }

    [ClientRpc]
    private void TriggerTrapClientRpc(PlayerControllerReference playerControllerReference)
    {
        TriggerTrap(playerControllerReference);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TriggerTrapServerRpc()
    {
        TriggerTrapClientRpc();
    }

    [ClientRpc]
    private void TriggerTrapClientRpc()
    {
        TriggerTrap();
    }

    public virtual void TriggerTrap(PlayerControllerB player)
    {
        playerCaught = player;
        playerCaught.disableMoveInput = true;
        if (damagePlayerTimer <= 0f)
        {
            damagePlayerTimer = 0.5f;
            playerCaught.DamagePlayer(25, true, true, CauseOfDeath.Crushing, 0, false, default);
        }
        caughtPosition = playerCaught.transform.position;

        TriggerTrap();
    }

    public virtual void TriggerTrap()
    {
        trapAudioSource.Stop();
        trapAudioSource.clip = triggerSound;
        trapAudioSource.Play();
        isTriggered = true;
        trapAnimator.SetBool(IsTrapTriggered, true);
        StartCoroutine(ResetBooleanAfterDelay(IsTrapTriggered, 0.5f));
        trapCollider.enabled = false;
    }

    public virtual void TriggerTrap(EnemyAI enemy)
    {
        enemyCaught = enemy;
        enemyCaught.HitEnemy(0, null, false, -1);

        TriggerTrap();

        StartCoroutine(DelayReleasingTrap(10f * (enemy.enemyType.EnemySize == EnemySize.Medium ? 0.5f : 1f) * enemy.enemyType.stunTimeMultiplier));
    }

    [ServerRpc(RequireOwnership = false)]
    private void DelayReleasingTrapServerRpc(float timer)
    {
        DelayReleasingTrapClientRpc(timer);
    }

    [ClientRpc]
    private void DelayReleasingTrapClientRpc(float timer)
    {
        StartCoroutine(DelayReleasingTrap(timer));
    }

    private IEnumerator DelayReleasingTrap(float timer)
    {
        trapTrigger.gameObject.SetActive(false);
        yield return new WaitForSeconds(timer);

        DoReleaseTrapEarly();
        yield return new WaitForSeconds(trapTrigger.timeToHold);
        trapTrigger.gameObject.SetActive(true);
        DoReleaseTrap();
    }

    public void ReleaseTrapEarly()
    {
        Plugin.ExtendedLogging("release trap early");
        DoReleaseTrapEarlyServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void DoReleaseTrapEarlyServerRpc()
    {
        DoReleaseTrapEarlyClientRpc();
    }

    [ClientRpc]
    private void DoReleaseTrapEarlyClientRpc()
    {
        DoReleaseTrapEarly();
    }

    public void DoReleaseTrapEarly()
    {
        trapAnimator.SetBool(IsTrapResetting, true);
        trapAudioSource.Stop();
        trapAudioSource.clip = resetTrapSound;
        trapAudioSource.Play();
        StartCoroutine(ResetBooleanAfterDelay(IsTrapResetting, 0.5f));
    }

    public void OnCancelReleaseTrap()
    {
        Plugin.ExtendedLogging("Canceling trap release");
        DoOnCancelReleaseTrapServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void DoOnCancelReleaseTrapServerRpc()
    {
        DoOnCancelReleaseTrapClientRpc();
    }

    [ClientRpc]
    public void DoOnCancelReleaseTrapClientRpc()
    {
        if (!isTriggered || playerCaught == null)
        {
            return;
        }

        TriggerTrap(playerCaught);
    }

    public void ReleaseTrap(PlayerControllerB player)
    {
        if (!player.IsLocalPlayer)
        {
            return;
        }

        Plugin.ExtendedLogging("release trap");
        DoReleaseTrapServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void DoReleaseTrapServerRpc()
    {
        DoReleaseTrapClientRpc();
    }

    [ClientRpc]
    public void DoReleaseTrapClientRpc()
    {
        DoReleaseTrap();
    }

    public void DoReleaseTrap()
    {
        trapAudioSource.Stop();
        trapAudioSource.clip = resetTrapEndSound;
        trapAudioSource.Play();

        trapAnimator.SetBool(IsTrapResetting, true);
        trapCollider.enabled = true;

        if (playerCaught != null)
        {
            playerCaught.disableMoveInput = false;
            playerCaught = null;
        }

        if (enemyCaught != null)
        {
            enemyCaught = null;
        }

        isTriggered = false;
        StartCoroutine(DelayForReuse());

        // Reset `isTrapTriggered` if it hasn't been reset properly before
        trapAnimator.SetBool(IsTrapTriggered, false);

        StartCoroutine(ResetBooleanAfterDelay(IsTrapResetting, 0.5f));
    }

    private IEnumerator DelayForReuse()
    {
        canTrigger = false;
        yield return new WaitForSeconds(delayBeforeReset);
        canTrigger = true;
    }

    private IEnumerator ResetBooleanAfterDelay(int parameterHash, float delay)
    {
        yield return new WaitForSeconds(delay);
        trapAnimator.SetBool(parameterHash, false);
    }

    [ClientRpc]
    private void SyncRandomResetTrapTimeClientRpc(float resetTime)
    {
        trapTrigger.timeToHold = resetTime;
    }

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (isTriggered)
        {
            return false;
        }

        TriggerTrapServerRpc();

        if (this is not BoomTrap)
        {
            DelayReleasingTrapServerRpc(60f);
        }

        return true;
    }
}