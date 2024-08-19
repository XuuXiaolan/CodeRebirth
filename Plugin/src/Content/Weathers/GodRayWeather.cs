using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Rendering.HighDefinition;
using CodeRebirth.src.Util.Extensions;
using System.Collections;
using CodeRebirth.src.MiscScripts;

namespace CodeRebirth.src.Content.Weathers;
struct GodRaySkyEffect(Color colour, Vector3 position, float radius, float falloff, Vector3 bottomPosition)
{
    public Color colour = colour;
    public Vector3 topPosition = position;
    public Vector3 bottomPosition = bottomPosition;
    public float radius = radius;
    public float falloff = falloff;
}

struct GodRaySpotlightData
{
    public Vector3 location;
    public Quaternion rotation;
    public float angle;
    public Color colour;

    public GodRaySpotlightData(Vector3 topPoint, float topRadius, Vector3 bottomPoint, float bottomRadius, Color colour)
    {
        location = CalculateSpotlightLocation(topPoint, topRadius, bottomPoint, bottomRadius);
    rotation = Quaternion.FromToRotation(Vector3.forward, (bottomPoint - topPoint).normalized);

        Vector2 towardsLight = new Vector2(location.x - topPoint.x, location.z - topPoint.z);
        Func<Vector2, Vector2, Vector2, (float a, float b, float c)> distances = (a, b, c) => ((c - b).magnitude, (c - a).magnitude, (b - a).magnitude);
        // a^2 = b^2 + c^2 - 2bc cos(A)
        // b^2 + c^2 - a^2 = 2bc cos(A)
        // A = acos( (b^2 + c^2 - a^2) / 2bc )
        Func<float, float, float, float> triangleAngle = (a, b, c) => Mathf.Acos((b * b + c * c - a * a) / (2 * b * c));
        (float a, float b, float c) d = distances(new Vector2(towardsLight.magnitude, location.y - topPoint.y), Vector3.zero, Vector2.right * topRadius);
        float halfTheta = triangleAngle(d.a, d.b, d.c);
        angle = halfTheta * 2;

        this.colour = colour;
    }

    /// <summary>
    /// Calculates the distance and location of the spotlight based on the given top and bottom points.
    /// </summary>
    private readonly Vector3 CalculateSpotlightLocation(Vector3 topPoint, float topRadius, Vector3 bottomPoint, float bottomRadius)
    {
        if (Mathf.Approximately(topRadius, bottomRadius))
        {
            // If the radii are approximately equal, return the top point as the location
            return topPoint;
        }
        else
        {
            float dx = (topPoint - bottomPoint).magnitude;
            float dr = bottomRadius - topRadius;
            float x = dx * topRadius / dr;
            return topPoint + (topPoint - bottomPoint).normalized * x;
        }
    }
}

/// <summary>
/// Creates the data for a GodRay, which a GodRayManager can spawn.
/// </summary>
/// <param name="skyColour">The colour of this GodRay in the sky texture.</param>
/// <param name="topPosition">The position of this GodRay in the sky.</param>
/// <param name="topRadius">The base radius of the GodRay in the sky texture</param>
/// <param name="topFalloff">The size of the area in the sky around the GodRay which is lit up. Must be positive (non-zero).</param>
public class GodRay(Color skyColour, Vector2 topPosition, float topRadius, float topFalloff, Vector3 bottomPosition, float bottomRadius, Color lightColour)
{
    public Color SkyColour { get; private set; } = skyColour;
    public Color LightColour { get; private set; } = lightColour;
    public Vector2 TopPosition { get; private set; } = topPosition;
    public float TopRadius { get; private set; } = topRadius;
    public float TopFalloff { get; private set; } = topFalloff;
    public Vector3 BottomPosition { get; private set; } = bottomPosition;
    public float BottomRadius { get; private set; } = bottomRadius;

    internal GodRaySkyEffect SkyEffect(float skyHeight) => new(SkyColour, new Vector3(TopPosition.x, skyHeight, TopPosition.y), TopRadius, TopFalloff, BottomPosition);
    internal GodRaySpotlightData SpotlightData(float skyHeight) => new(new Vector3(TopPosition.x, skyHeight, TopPosition.y), TopRadius, BottomPosition, BottomRadius, LightColour);
}

public class GodRayManager : CodeRebirthWeathers
{
    List<GodRaySkyEffect> godRayEffects = new();
    List<HDAdditionalLightData> godRaySpotlights = new();
    List<GodRay> godRays = new();

    public Material godRayMaterial = null!;
    public GameObject godRayParent = null!;
    public Material sphereMaterial = null!;
    ComputeBuffer? rayBuffer;
    Camera localCamera = null!;
    float scale;
    Vector3 previousPosition;
    public float timeBetweenGodRaySpawns;
    [SerializeField]
    public List<Color> rayColours;
    System.Random godRayRandom = new();
    public static GodRayManager? Instance { get; private set; }
    public static bool Active => Instance != null;

    private void OnEnable()
    {
        Instance = this;
        timeBetweenGodRaySpawns = 10f;
        rayColours = new List<Color> { new Color(1f, 0f, 0f, 0.25f) };
        godRayRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        localCamera = GameNetworkManager.Instance.localPlayerController.gameplayCamera;
        transform.position = localCamera.transform.position;
        SetScale(localCamera.farClipPlane);
        RegenerateRayComputeBuffer();
        previousPosition = localCamera.transform.position;
        StartCoroutine(UpdateGodRays());
    }

    private IEnumerator UpdateGodRays()
    {
        Vector3 centre = CalculateCenterOfPoints(RoundManager.Instance.outsideAINodes.Select(node => node.transform.position).ToList());

        while (godRays.Count() <= 1)
        {
            yield return new WaitForSeconds(godRayRandom.NextFloat(0.75f, 1.25f) * timeBetweenGodRaySpawns);

            Vector3 bottomPosition = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(centre, 100f, default, godRayRandom);
            Vector2 position = godRays.Count() == 0 ? Vector2.zero : new Vector2(bottomPosition.x, bottomPosition.z);
            Color rayColour = rayColours[godRayRandom.Next(0, rayColours.Count)];

            AddGodRay(new GodRay(rayColour, position, godRayRandom.NextFloat(2f, 4f), godRayRandom.NextFloat(2f, 5f), new Vector3(bottomPosition.x, 0, bottomPosition.z), godRayRandom.NextFloat(3f, 8f), rayColour));
        }
    }

    private void SetScale(float scale)
    {
        scale *= 0.99f;
        transform.localScale = Vector3.one * scale * 2;
        transform.position = localCamera.transform.position;
        this.scale = scale * 2;
        sphereMaterial.SetFloat("_skyBoxRadius", scale);
        for (int i = 0; i < godRaySpotlights.Count; i++)
        {
            float skyHeight = localCamera.farClipPlane * 0.99f + localCamera.transform.position.y;
            GodRaySpotlightData spotlightData = godRays[i].SpotlightData(skyHeight);
            float distance = (godRays[i].BottomPosition - spotlightData.location).magnitude + 5f;
            godRaySpotlights[i].range = distance;

            HDAdditionalLightData light = godRaySpotlights[i].GetComponent<HDAdditionalLightData>();
            light.range = localCamera.farClipPlane * 2;

            float innerAnglePercent = Mathf.Clamp(spotlightData.angle * Mathf.Rad2Deg, 1f, 179f);
            if (spotlightData.angle * Mathf.Rad2Deg < 1f) innerAnglePercent = spotlightData.angle * Mathf.Rad2Deg * 100f;
            light.SetSpotAngle(innerAnglePercent, innerAnglePercent);
            light.shapeRadius = 0;
            light.luxAtDistance = (spotlightData.location - godRays[i].BottomPosition).magnitude;
            light.SetIntensity(100, LightUnit.Lux);
            godRaySpotlights[i].gameObject.transform.rotation = spotlightData.rotation;
            godRaySpotlights[i].gameObject.transform.position = spotlightData.location;
            GenerateSpotlightMesh(godRaySpotlights[i].gameObject, distance, godRays[i].BottomRadius, godRays[i].LightColour, false, 40);

            // godRayEffects[i] = godRays[i].SkyEffect(localCamera.farClipPlane * 0.99f + localCamera.transform.position.y);
        }
    }

    private void Update()
    {
        Vector3 currentCameraPosition = localCamera.transform.position;
        if (scale != localCamera.farClipPlane * 2 || previousPosition != currentCameraPosition)
        {
            SetScale(localCamera.farClipPlane);
            previousPosition = currentCameraPosition;
        }

        this.transform.position = currentCameraPosition;
    }

    /// <summary>
    /// Removes a GodRay, deleting all associated data
    /// so it is no longer rendered.
    /// </summary>
    /// <param name="ray">The ray to remove</param>
    /// <returns>true if the ray has been removed, or false otherwise</returns>
    private bool RemoveGodRay(GodRay ray)
    {
        Plugin.ExtendedLogging($"Removing GodRay at TopPosition: {ray.TopPosition}, BottomPosition: {ray.BottomPosition}");
        int index = godRays.IndexOf(ray);
        if (index != -1)
        {
            godRays.RemoveAt(index);
            godRayEffects.RemoveAt(index);
            Destroy(godRaySpotlights[index]);
            godRaySpotlights.RemoveAt(index);
            RegenerateRayComputeBuffer();
            return true;
        }
        return false;
    }
    /// <summary>
    /// Spawns a GodRay.
    /// </summary>
    /// <param name="ray">The GodRay to add.</param>
    /// <returns>The ray that was added (a reference to the given argument "ray")</returns>
    private GodRay AddGodRay(GodRay ray)
    {
        float skyHeight = localCamera.farClipPlane * 0.99f + localCamera.transform.position.y;
        godRays.Add(ray);
        godRayEffects.Add(ray.SkyEffect(skyHeight));
        GodRaySpotlightData spotlightData = ray.SpotlightData(skyHeight);
        GameObject lightGameObject = new GameObject();
        HDAdditionalLightData light = lightGameObject.AddHDLight(HDLightTypeAndShape.ConeSpot);
        light.range = localCamera.farClipPlane * 2;

        float innerAnglePercent = Mathf.Clamp(spotlightData.angle * Mathf.Rad2Deg, 1f, 179f);
        if (spotlightData.angle * Mathf.Rad2Deg < 1f) innerAnglePercent = spotlightData.angle * Mathf.Rad2Deg * 100f;
        light.SetSpotAngle(innerAnglePercent, innerAnglePercent);
        light.shapeRadius = 0;
        light.luxAtDistance = (spotlightData.location - ray.BottomPosition).magnitude;
        light.SetIntensity(100, LightUnit.Lux);

        light.color = spotlightData.colour;
        lightGameObject.transform.parent = godRayParent.transform;
        lightGameObject.transform.position = spotlightData.location;
        lightGameObject.transform.rotation = spotlightData.rotation;
        GenerateSpotlightMesh(lightGameObject, (ray.BottomPosition - spotlightData.location).magnitude, ray.BottomRadius, spotlightData.colour, pointCount: 40);
        godRaySpotlights.Add(light);
        RegenerateRayComputeBuffer();
        return ray;
    }

    private Vector3 CalculateCenterOfPoints(List<Vector3> points)
    {
        if (points == null || points.Count == 0) return Vector3.zero;

        Vector3 sum = Vector3.zero;
        foreach (var point in points)
        {
            sum += point;
        }
        return sum / points.Count;
    }

    private void GenerateSpotlightMesh(GameObject light, float distance, float bottomRadius, Color colour, bool generateNewMesh = true, int pointCount = 100)
    {
        if (pointCount < 3 || pointCount >= 254) throw new ArgumentOutOfRangeException(nameof(pointCount));

        // Adjust the distance to a more reasonable length
        float adjustedDistance = distance * 1.05f;

        // Generate the visual mesh (this is purely for visual representation)
        Vector3[] points = new Vector3[pointCount + 1];
        int[] indices = new int[pointCount * 3];
        points[0] = Vector3.zero; // Start at the light's position
        for (int i = 0; i < pointCount; i++)
        {
            float theta = (float)i / pointCount * Mathf.PI * 2f;
            Vector3 offset = new Vector3(Mathf.Cos(theta), Mathf.Sin(theta), 0) * bottomRadius;
            points[i + 1] = new Vector3(0, 0, adjustedDistance) + offset;
            indices[i * 3 + 0] = 0;
            indices[i * 3 + 1] = 1 + ((i + 1) % pointCount);
            indices[i * 3 + 2] = i + 1;
        }

        MeshFilter? meshFilter = generateNewMesh ? light.AddComponent<MeshFilter>() : light.GetComponent<MeshFilter>();
        MeshRenderer? meshRenderer = generateNewMesh ? light.AddComponent<MeshRenderer>() : light.GetComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        if (!generateNewMesh) Destroy(meshFilter.mesh);
        meshFilter.mesh = mesh;
        mesh.vertices = points;
        mesh.triangles = indices;
        light.GetComponent<Renderer>().material = godRayMaterial;
        light.GetComponent<Renderer>().material.color = new Color(colour.r, colour.g, colour.b, 30f / 255f);
        mesh.RecalculateNormals();

        // Calculate the correct bottom position after the adjustment
        Vector3 bottomPosition = light.transform.position + light.transform.forward * adjustedDistance;

        // Create a small sphere collider at the center of the bottom position
        GameObject sphereColliderObject = new GameObject("TriggerCollider");
        sphereColliderObject.transform.parent = light.transform;

        // Set the position of the collider directly to match the bottom position
        sphereColliderObject.transform.position = bottomPosition;

        // Adjust the sphere collider's radius to match the bottom radius
        SphereCollider sphereCollider = sphereColliderObject.AddComponent<SphereCollider>();
        sphereCollider.radius = bottomRadius;
        sphereCollider.isTrigger = true;

        // Attach the BetterCooldownTrigger script to the sphere collider object
        BetterCooldownTrigger cdt = sphereColliderObject.AddComponent<BetterCooldownTrigger>();
        SetupCooldownTrigger(cdt);
    }

    private void SetupCooldownTrigger(BetterCooldownTrigger cdt)
    {
        cdt.deathAnimation = BetterCooldownTrigger.DeathAnimation.Burnt;
        cdt.forceDirection = BetterCooldownTrigger.ForceDirection.Up;
        cdt.causeOfDeath = CauseOfDeath.Burning;
        cdt.forceMagnitudeAfterDamage = 0f;
        cdt.forceMagnitudeAfterDeath = 50f;
        cdt.triggerForEnemies = false;
        cdt.sharedCooldown = true;
        cdt.playDefaultPlayerDamageSFX = true;
        cdt.forceDirectionFromThisObject = false;
        cdt.soundAttractsDogs = false;
        cdt.damageDuration = 0f;
        cdt.damageToDealForPlayers = 5;
        cdt.damageToDealForEnemies = 1;
        cdt.damageIntervalForPlayers = 1f;
        cdt.damageIntervalForEnemies = 20f;
        cdt.damageClip = new List<AudioClip>();
        cdt.damageAudioSources = new List<AudioSource>();
    }

    private void RegenerateRayComputeBuffer()
    {
        if (godRayEffects.Count > 0)
        {
            if (rayBuffer == null || rayBuffer.count != godRayEffects.Count)
            {
                rayBuffer?.Release();
                rayBuffer = new ComputeBuffer(godRayEffects.Count, sizeof(float) * 12);
            }
            rayBuffer.SetData(godRayEffects, 0, 0, godRayEffects.Count);
            sphereMaterial.SetBuffer("_rays", rayBuffer);
        }
        else
        {
            rayBuffer?.Release();
            rayBuffer = null;
        }
        sphereMaterial.SetInt("_rayCount", godRayEffects.Count);
    }

    private void OnDisable()
    {
        Instance = null;
        StopAllCoroutines();
        foreach (GodRay ray in godRays.ToList())
        {
            RemoveGodRay(ray);
        }
        godRays.Clear();
        godRayEffects.Clear();
        godRaySpotlights.Clear();
        rayBuffer?.Release();
        rayBuffer = null;
    }
}