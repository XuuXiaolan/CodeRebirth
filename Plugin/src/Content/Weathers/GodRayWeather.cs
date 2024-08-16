using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Rendering.HighDefinition;
using Unity.Netcode;
using CodeRebirth.src.Util.Extensions;
using System.Collections;

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
        // dx * r/dr
        float dx = (topPoint - bottomPoint).magnitude;
        float dr = bottomRadius - topRadius;
        float x = dx * topRadius / dr;
        location = topPoint + (topPoint - bottomPoint).normalized * x;

        rotation = Quaternion.FromToRotation(Vector3.forward, bottomPoint - topPoint);

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
    // these lists must be kept aligned
    // so that godRayEffects[i] os the effect for godRays[i],
    // for example.
    // you might see the number of lists and call it "a bad idea",
    // but I call it "ECS", which sounds much fancier.
    List<GodRaySkyEffect> godRayEffects = [];
    List<HDAdditionalLightData> godRaySpotlights = [];
    List<GodRay> godRays = [];

    public Material godRayMaterial = null!;
    public GameObject godRayParent = null!;

    public Material sphereMaterial = null!;
    ComputeBuffer? rayBuffer;
    Camera localCamera = null!;
    float scale;
    Vector3 previousPosition;
    public float timeBetweenGodRaySpawns;
    public float minX, maxX, minZ, maxZ;
    [SerializeField]
    public List<Color> rayColours;
    System.Random godRayRandom = new();
	public static GodRayManager? Instance { get; private set; }
	public static bool Active => Instance != null;
    private void OnEnable()
    {
        Instance = this;
        minX = -40;
        maxX = 40;
        minZ = -40;
        maxZ = 40;
        timeBetweenGodRaySpawns = 10f;
        rayColours = [
            new(1f, 0f, 0f, 0.25f),
            new(1f, 1f, 0f, 0.25f),
            new(1f, 0.65f, 0, 0.25f)
        ];
        godRayRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        localCamera = GameNetworkManager.Instance.localPlayerController.gameplayCamera;
        transform.position = localCamera.transform.position;
        SetScale(localCamera.farClipPlane);
        RegenerateRayComputeBuffer();
        previousPosition = localCamera.transform.position;
        StartCoroutine(UpdateGodRays());
    }

    IEnumerator UpdateGodRays() {
        while (this.godRays.Count() <= 10) {
            yield return new WaitForSeconds(godRayRandom.NextFloat(0.75f, 1.25f) * timeBetweenGodRaySpawns);
            Vector2 position = this.godRays.Count() == 0 ? Vector2.zero : new Vector2(godRayRandom.NextFloat(minX, maxX), godRayRandom.NextFloat(minZ, maxZ));
            Vector2 bottomPosition = new Vector2(godRayRandom.NextFloat(minX, maxX), godRayRandom.NextFloat(minZ, maxZ));
            Color rayColour = rayColours[godRayRandom.Next(0, rayColours.Count)];
            this.AddGodRay(new GodRay(rayColour, position, godRayRandom.NextFloat(2f, 4f), godRayRandom.NextFloat(2f, 5f), new Vector3 (bottomPosition.x, 4.5f, bottomPosition.y), 8f, rayColour));
        }
    }
    void SetScale(float scale)
    {
        // just to prevent the furthest vertex from being clipped
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

            float innerAnglePercent = 100f;
            if (spotlightData.angle * Mathf.Rad2Deg < 1f) innerAnglePercent = spotlightData.angle * Mathf.Rad2Deg * 100f;
            light.SetSpotAngle(spotlightData.angle * Mathf.Rad2Deg, innerAnglePercent);
            light.shapeRadius = 0;
            light.luxAtDistance = (spotlightData.location - godRays[i].BottomPosition).magnitude;
            light.SetIntensity(7000, LightUnit.Lux);

            godRaySpotlights[i].gameObject.transform.rotation = spotlightData.rotation;
            godRaySpotlights[i].gameObject.transform.position = spotlightData.location;
            GenerateSpotlightMesh(godRaySpotlights[i].gameObject, distance, godRays[i].BottomRadius, godRays[i].LightColour, false, 40);

            // godRayEffects[i] = godRays[i].SkyEffect(localCamera.farClipPlane * 0.99f + localCamera.transform.position.y);
        }
    }

    private void Update()
    {
        this.transform.position = localCamera.transform.position;
        if (scale != localCamera.farClipPlane * 2) SetScale(localCamera.farClipPlane);
        else if (previousPosition != localCamera.transform.position)
        {
            SetScale(localCamera.farClipPlane);
            previousPosition = localCamera.transform.position;
        }
    }

    /// <summary>
    /// Removes a GodRay, deleting all associated data
    /// so it is no longer rendered.
    /// </summary>
    /// <param name="ray">The ray to remove</param>
    /// <returns>true if the ray has been removed, or false otherwise</returns>
    public bool RemoveGodRay(GodRay ray)
    {
        int index;
        if ((index = godRays.IndexOf(ray)) != -1)
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
    public GodRay AddGodRay(GodRay ray)
    {
        float skyHeight = localCamera.farClipPlane * 0.99f + localCamera.transform.position.y;
        godRays.Add(ray);
        godRayEffects.Add(ray.SkyEffect(skyHeight));
        GodRaySpotlightData spotlightData = ray.SpotlightData(skyHeight);
        GameObject lightGameObject = new GameObject();
        HDAdditionalLightData light = lightGameObject.AddHDLight(HDLightTypeAndShape.ConeSpot);
        light.range = localCamera.farClipPlane * 2;

        float innerAnglePercent = 100f;
        if (spotlightData.angle * Mathf.Rad2Deg < 1f) innerAnglePercent = spotlightData.angle * Mathf.Rad2Deg * 100f;
        light.SetSpotAngle(spotlightData.angle * Mathf.Rad2Deg, innerAnglePercent);
        light.shapeRadius = 0;
        light.luxAtDistance = (spotlightData.location - ray.BottomPosition).magnitude;
        light.SetIntensity(7000, LightUnit.Lux);

        light.color = spotlightData.colour;
        lightGameObject.transform.parent = godRayParent.transform;
        lightGameObject.transform.position = spotlightData.location;
        lightGameObject.transform.rotation = spotlightData.rotation;
        GenerateSpotlightMesh(lightGameObject, (ray.BottomPosition - spotlightData.location).magnitude, ray.BottomRadius, spotlightData.colour, pointCount: 40);
        godRaySpotlights.Add(light);
        RegenerateRayComputeBuffer();
        return ray;
    }

    void GenerateSpotlightMesh(GameObject light, float distance, float bottomRadius, Color colour, bool generateNewMesh = true, int pointCount = 100)
    {
        if (pointCount < 3 || pointCount >= 254) throw new ArgumentOutOfRangeException(nameof(pointCount));
        Vector3[] points = new Vector3[pointCount + 1];
        int[] indices = new int[pointCount * 3];
        points[0] = Vector3.zero;
        for (int i = 0; i < pointCount; i++)
        {
            float theta = (float)i / pointCount * Mathf.PI * 2f;
            Vector3 offset = new Vector3(Mathf.Cos(theta), Mathf.Sin(theta), 0) * bottomRadius;
            points[i + 1] = new Vector3(0, 0, distance) + offset;
            indices[i * 3 + 0] = 0;
            indices[i * 3 + 1] = 1 + ((i + 1) % pointCount);
            indices[i * 3 + 2] = i + 1;
        }

        MeshFilter? meshFilter = generateNewMesh ? light.AddComponent<MeshFilter>() : light.GetComponent<MeshFilter>();
        MeshRenderer? meshRenderer = generateNewMesh ? light.AddComponent<MeshRenderer>() : light.GetComponent<MeshRenderer>();
        MeshCollider? meshCollider = generateNewMesh ? light.AddComponent<MeshCollider>() : light.GetComponent<MeshCollider>();
        meshCollider.convex = true;
        meshCollider.isTrigger = true;

        Mesh mesh = new Mesh();
        if (!generateNewMesh) Destroy(meshFilter.mesh);
        meshFilter.mesh = mesh;
        mesh.vertices = points;
        mesh.triangles = indices;
        light.GetComponent<Renderer>().material = godRayMaterial;
        light.GetComponent<Renderer>().material.color = new Color(colour.r, colour.g, colour.b, 56f / 255f);
        mesh.RecalculateNormals();

        meshCollider.sharedMesh = mesh;
    }

    void RegenerateRayComputeBuffer()
    {
        if (rayBuffer != null)
        {
            rayBuffer.Release();
            rayBuffer = null;
        }

        if (godRayEffects.Count != 0)
        {
            rayBuffer = new ComputeBuffer(godRayEffects.Count, sizeof(float) * 12);
            rayBuffer.SetData(godRayEffects, 0, 0, godRayEffects.Count);
            sphereMaterial.SetBuffer("_rays", rayBuffer);
        }

        sphereMaterial.SetInt("_rayCount", godRayEffects.Count);
    }

    private void OnDisable()
    {
        Instance = null;
        foreach (GodRay ray in godRays)
        {
            this.RemoveGodRay(ray);
        }
        godRays.Clear();
        godRayEffects.Clear();
        godRaySpotlights.Clear();
        rayBuffer?.Release();
    }
}