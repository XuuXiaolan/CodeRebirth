using System.Collections;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.Content.MoonAddons;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class OxydeCrashingShip : FallingObjectBehaviour
{
    [Header("Scrap")]
    [SerializeField]
    private Transform _itemSpawnTransform = null!;

    [Header("Audio")]
    [SerializeField]
    private AudioSource _ImpactAudio = null!;
    [SerializeField]
    private AudioSource _NormalTravelAudio = null!;
    [SerializeField]
    private AudioSource _CloseTravelAudio = null!;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Vector3 spawnPosition = this.transform.position;
        this.transform.position = new Vector3(9999, 9999, 9999);
        System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
        StartCoroutine(WaitToDoYourThing(spawnPosition, random));

    }

    public IEnumerator WaitToDoYourThing(Vector3 spawnPosition, System.Random random)
    {
        yield return new WaitForSeconds(random.NextFloat(180f, 360f));
        Vector3 origin = CalculateRandomSkyOrigin(Direction.East, spawnPosition, random);
        SetupFallingObject(origin, spawnPosition, 25);
    }

    protected override void OnImpact()
    {
        base.OnImpact();
        StartCoroutine(Impact()); // Start the impact effects
    }

    protected override void OnSetup()
    {
        base.OnSetup();
        StartCoroutine(UpdateAudio()); // Make sure audio works correctly on the first frame.
    }

    public void Start()
    {
        _NormalTravelAudio.Play();
    }

    private IEnumerator UpdateAudio()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.25f);
            if (GameNetworkManager.Instance.localPlayerController.isInsideFactory)
            {
                _NormalTravelAudio.volume = 0;
                _CloseTravelAudio.volume = 0;
                _ImpactAudio.volume = 0.05f;
            }
            else
            {
                _NormalTravelAudio.volume = Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value;
                _CloseTravelAudio.volume = Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value;
                _ImpactAudio.volume = Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value;
            }
            if (GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom && StartOfRound.Instance.hangarDoorsClosed)
            {
                _NormalTravelAudio.volume = Plugin.ModConfig.ConfigMeteorShowerInShipVolume.Value * Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value;
                _CloseTravelAudio.volume = Plugin.ModConfig.ConfigMeteorShowerInShipVolume.Value * Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value;
                _ImpactAudio.volume = Plugin.ModConfig.ConfigMeteorShowerInShipVolume.Value * Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value;
            }
            if (((1 - Progress) * _travelTime) <= 4.106f && !_CloseTravelAudio.isPlaying)
            {
                _NormalTravelAudio.volume = 0.5f * Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value;
                _CloseTravelAudio.Play();
            }
        }
    }

    private IEnumerator Impact()
    {
        _ImpactAudio.Play();
        CRUtilities.CreateExplosion(transform.position, true, 50, 0, 25, 4, null, null, 100f);

        if (!IsServer) yield break;
        if (MoonAddonsHandler.Instance.MoonUnlocker == null) yield break;
        CodeRebirthUtils.Instance.SpawnScrap(MoonAddonsHandler.Instance.MoonUnlocker.ItemDefinitions.GetCRItemDefinitionWithItemName("Oxyde")?.item, _itemSpawnTransform.position, false, true, 0);
    }
}