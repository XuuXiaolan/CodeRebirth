using UnityEngine;
using Unity.Netcode;
using Dawn.Utils;
using CodeRebirth.src.Content.Maps;
using CodeRebirth.src.MiscScripts;

namespace CodeRebirth.src.Content.Enemies;

public class MiliaryPlane : NetworkBehaviour
{
    [field: SerializeField]
    public float FlyingSpeed { get; private set; } = 25f;

    [field: SerializeField]
    public AudioSource AudioSource { get; private set; } = null!;
    [field: SerializeField]
    public AudioClip AmbientFlyingSound { get; private set; } = null!;
    [field: SerializeField]
    public AudioClip WarningSirenSound { get; private set; } = null!;

    private Vector3 DropPosition = Vector3.zero;
    private Vector3 ExplodePosition = Vector3.zero;
    public void Start()
    {
        AudioSource.clip = AmbientFlyingSound;
        AudioSource.Play();
        AudioSource.PlayOneShot(WarningSirenSound);
        if (RoundManager.Instance.outsideAINodes == null)
        {
            RoundManager.Instance.outsideAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
        }

        float furthestDistanceToShip = 0f;
        GameObject? furthestAINode = null;
        foreach (GameObject AINode in RoundManager.Instance.outsideAINodes)
        {
            float distanceToShip = Vector3.Distance(AINode.transform.position, StartOfRound.Instance.shipLandingPosition.position);
            if (distanceToShip > furthestDistanceToShip)
            {
                furthestDistanceToShip = distanceToShip;
                furthestAINode = AINode;
            }
        }

        if (furthestAINode == null)
        {
            Plugin.Logger.LogError("No outside AI nodes found for military plane to spawn at!");
            return;
        }

        Plugin.ExtendedLogging($"Spawning military plane at furthest outside AI node: {furthestAINode.name} at distance {furthestDistanceToShip} from ship");
        this.transform.position = furthestAINode.transform.position + Vector3.up * 250f;
        this.transform.LookAt(StartOfRound.Instance.shipLandingPosition);
        this.transform.rotation = Quaternion.Euler(0, this.transform.rotation.eulerAngles.y, 0);

        GameObject? perfectNodeToFaceTowards = null;
        float closestDot = float.MaxValue;
        foreach (GameObject AINode in RoundManager.Instance.outsideAINodes)
        {
            float distanceToShip = Vector3.Distance(AINode.transform.position, StartOfRound.Instance.shipLandingPosition.position);
            float distanceToFurthestNode = Vector3.Distance(AINode.transform.position, furthestAINode.transform.position);
            if (distanceToShip <= 20f || distanceToFurthestNode <= furthestDistanceToShip)
            {
                continue;
            }

            Vector3 directionToNode = (AINode.transform.position - StartOfRound.Instance.shipLandingPosition.position).normalized;
            float dot = Vector3.Dot(directionToNode, this.transform.forward);
            if (dot < closestDot)
            {
                closestDot = dot;
                perfectNodeToFaceTowards = AINode;
            }
        }

        Vector3 centerOfFlightPath = StartOfRound.Instance.shipLandingPosition.position + Vector3.up * 250f;
        if (perfectNodeToFaceTowards != null)
        {
            Plugin.ExtendedLogging($"Found perfect node for military plane to face towards: {perfectNodeToFaceTowards.name} with dot {closestDot}");
            Vector3 centerOfNodes = (this.transform.position - perfectNodeToFaceTowards.transform.position) / 2;
            centerOfFlightPath = new Vector3(centerOfNodes.x, this.transform.position.y, centerOfNodes.z);
            this.transform.LookAt(perfectNodeToFaceTowards.transform);
            this.transform.rotation = Quaternion.Euler(0, this.transform.rotation.eulerAngles.y, 0);
        }
        else
        {
            Plugin.Logger.LogWarning("No perfect node found for military plane to face towards, defaulting to facing ship");
        }

        if (Physics.Raycast(this.transform.position, this.transform.forward, out RaycastHit hit, 70f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
        {
            ExplodePosition = hit.point;
            DropPosition = hit.point - (this.transform.forward * 20f);
            Plugin.ExtendedLogging($"Calculated drop position for military plane: {DropPosition} based on raycast hit with {hit.collider.name}");
        }
        else
        {
            // Must be 20 to 30 units away from the ship
            System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 12345);
            float requiredDistanceFromShip = random.NextFloat(20f, 30f) * random.NextSign();
            Vector3 directionFromCenter = (this.transform.position - centerOfFlightPath).normalized;
            DropPosition = centerOfFlightPath + directionFromCenter * requiredDistanceFromShip;
            Plugin.ExtendedLogging("Military plane failed to calculate drop position based on raycast, defaulting to ship landing position");
        }
        // Spawn on a random node that's far from the ship
        // do a vector3.dot between all ai nodes to see one that lines up from the direction of it looking towards the ship past the ship
        // make that the flight path, from current node + like 250y in height to that node, crossing by the ship's position
        // drop the boxchute down nearby the ship at a random point along the ship's length, but not too close to the edges
        // precalculate the drop point
        // if flightpath intersects with something after debt collector dropped, explode?
    }

    private bool exploded = false;
    private bool droppedBoxChute = false;

    public void Update()
    {
        this.transform.position += this.transform.forward * Time.deltaTime * FlyingSpeed;
        if (!droppedBoxChute && Vector3.Distance(this.transform.position, DropPosition) <= 0.5f)
        {
            GameObject boxChuteObject = GameObject.Instantiate(MapObjectHandler.Instance.Merchant.BoxChutePrefab, DropPosition, Quaternion.identity);
            BoxChute boxChute = boxChuteObject.GetComponent<BoxChute>();
            boxChute.SetupBoxChute();
        }

        if (!exploded && Vector3.Distance(this.transform.position, ExplodePosition) <= 0.5f)
        {
            CRUtilities.CreateExplosion(ExplodePosition, true, 100, 0f, 100f, 100, null, null, 200f);
            if (IsServer)
            {
                NetworkObject.Despawn(true);
            }
        }
    }
}