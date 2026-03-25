using UnityEngine;
using Unity.Netcode;
using Dawn.Utils;
using CodeRebirth.src.MiscScripts;
using System.Collections.Generic;
using System.Linq;

namespace CodeRebirth.src.Content.Enemies;

public class MilitaryPlane : NetworkBehaviour
{
    [field: SerializeField]
    public GameObject ExplosionPrefab { get; private set; } = null!;

    [field: SerializeField]
    public GameObject BoxChutePrefab { get; private set; } = null!;

    [field: SerializeField]
    public float FlyingSpeed { get; private set; } = 25f;

    [field: SerializeField]
    public AudioSource AudioSource { get; private set; } = null!;

    [field: SerializeField]
    public AudioSource EngineAudioSource { get; private set; } = null!;
    [field: SerializeField]
    public AudioClip AmbientFlyingSound { get; private set; } = null!;
    [field: SerializeField]
    public AudioClip WarningSirenSound { get; private set; } = null!;

    private Vector3 DropPosition = Vector3.zero;
    private Vector3 ExplodePosition = Vector3.zero;

    private static int militaryPlaneCount = 0;
    public void Awake()
    {
        StartOfRound.Instance.StartNewRoundEvent.AddListener(OnStartNewRound);
    }

    public void OnStartNewRound()
    {
        militaryPlaneCount++;
        AudioSource.PlayOneShot(WarningSirenSound);
        EngineAudioSource.PlayOneShot(AmbientFlyingSound);

        System.Random random = new(StartOfRound.Instance.randomMapSeed + 12345 + militaryPlaneCount);
        if (RoundManager.Instance.outsideAINodes == null)
        {
            RoundManager.Instance.outsideAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
        }

        List<GameObject> orderedNodes = RoundManager.Instance.outsideAINodes.OrderBy(x => Vector3.Distance(x.transform.position, StartOfRound.Instance.shipLandingPosition.position)).ToList();

        if (orderedNodes.Count == 0)
        {
            Plugin.Logger.LogError("No outside AI nodes found for military plane to spawn at!");
            return;
        }

        List<GameObject> furthestNodes =
        [
            .. orderedNodes.Where(x => Vector3.Distance(x.transform.position, StartOfRound.Instance.shipLandingPosition.position) > 40f),
        ];

        GameObject furthestAINode = furthestNodes[random.Next(0, furthestNodes.Count)];

        float furthestDistanceToShip = Vector3.Distance(furthestAINode.transform.position, StartOfRound.Instance.shipLandingPosition.position);
        this.transform.position = furthestAINode.transform.position + Vector3.up * random.NextFloat(200f, 250f);
        this.transform.position = new Vector3(this.transform.position.x, Mathf.Clamp(this.transform.position.y, int.MinValue, StartOfRound.Instance.shipLandingPosition.position.y + 250f), this.transform.position.z);
        this.transform.LookAt(StartOfRound.Instance.shipLandingPosition);
        this.transform.rotation = Quaternion.Euler(0, this.transform.rotation.eulerAngles.y, 0);
        this.transform.position -= this.transform.forward * 100f;

        Vector3 centerOfFlightPath = StartOfRound.Instance.shipLandingPosition.position;
        centerOfFlightPath.y = this.transform.position.y;
        if (Physics.Raycast(this.transform.position, this.transform.forward, out RaycastHit hit, furthestDistanceToShip, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
        {
            ExplodePosition = hit.point;
            DropPosition = this.transform.position + this.transform.forward * (Vector3.Distance(hit.point, this.transform.position) / 2f);
            Plugin.ExtendedLogging($"Calculated drop position for military plane: {DropPosition} based on raycast hit with {hit.collider.name} (Explosion related)");
        }
        else
        {
            if (Physics.Raycast(this.transform.position, this.transform.forward, out hit, 300f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                ExplodePosition = hit.point;
                Plugin.ExtendedLogging($"Calculated explosion position for military plane: {ExplodePosition} based on raycast hit with {hit.collider.name} (Explosion related)");
            }
            // Must be 20 to 30 units away from the ship
            float requiredDistanceFromShip = random.NextFloat(20, 30f) * random.NextSign();
            Vector3 directionFromCenter = (this.transform.position - centerOfFlightPath).normalized;
            DropPosition = centerOfFlightPath + directionFromCenter * requiredDistanceFromShip;
            Plugin.ExtendedLogging("Military plane failed to calculate drop position based on raycast, defaulting to ship landing position");
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        militaryPlaneCount--;
    }

    private bool exploded = false;
    private bool droppedBoxChute = false;

    public void Update()
    {
        this.transform.position += this.transform.forward * Time.deltaTime * FlyingSpeed;
        float dot = Vector3.Dot(this.transform.forward, (DropPosition - this.transform.position).normalized);
        Plugin.ExtendedLogging($"Military plane position: {this.transform.position} | drop position: {DropPosition} | dot: {dot} | exploded: {exploded} | droppedBoxChute: {droppedBoxChute}");
        if (!droppedBoxChute && (Vector3.Distance(this.transform.position, DropPosition) <= 0.5f))
        {
            GameObject boxChuteObject = GameObject.Instantiate(BoxChutePrefab, DropPosition, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
            BoxChute boxChute = boxChuteObject.GetComponent<BoxChute>();
            boxChute.SetupBoxChute();
            droppedBoxChute = true;
        }

        if (!exploded && Vector3.Distance(this.transform.position, ExplodePosition) <= 0.5f)
        {
            exploded = true;
            CRUtilities.CreateExplosion(ExplodePosition, true, 100, 0f, 100f, 100, null, ExplosionPrefab, 200f);
            if (IsServer)
            {
                NetworkObject.Despawn(true);
            }
        }
    }
}