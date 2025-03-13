using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Util;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.HighDefinition;

namespace CodeRebirth.src.MiscScripts;
public class DetectLightInSurroundings : MonoBehaviour
{
    [HideInInspector] public float lightValueOnThisObject = 0f;
    [HideInInspector] public UnityEvent<float> OnLightValueChange = new();

    public void OnEnable()
    {
        LightUpdateManager.detectLightInSurroundings.Add(this);
    }

    public void OnDisable()
    {
        LightUpdateManager.detectLightInSurroundings.Remove(this);
    }

    public IEnumerator UpdateLightValue()
    {
        List<(Light light, HDAdditionalLightData hDAdditionalLightData)> roundLightData = new();
        lightValueOnThisObject = 0f;
        roundLightData.Clear();
        roundLightData.AddRange(CodeRebirthUtils.currentRoundLightData);
        foreach ((Light light, HDAdditionalLightData hdLightData) in roundLightData)
        {
            if (light == null || hdLightData == null || !light.enabled) continue;
            if (GameNetworkManager.Instance.localPlayerController.nightVision == light) continue;
            Vector3 nodePosition = transform.position;

            float attenuation = CalculateAttenuation(light, nodePosition);
            if (attenuation == 0) continue;
            float contribution = light.intensity * attenuation;
            lightValueOnThisObject += contribution;
            // Plugin.ExtendedLogging($"influencing light: {light.name} with contribution: {contribution}");
            yield return null;
        }

        // Use the calculated lightValue as needed
        OnLightValueChange.Invoke(lightValueOnThisObject);
    }

    private float CalculateAttenuation(Light light, Vector3 samplePosition)
    {
        float lightRange = light.range;
        if (light.type == LightType.Directional)
        {
            if (light.shadows != LightShadows.None && Physics.Raycast(samplePosition, -light.transform.forward, out _, float.PositiveInfinity, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                return 0f;
            }
            return 1f;
        }
        else if (light.type == LightType.Spot)
        {
            float angleToNode = Vector3.Angle(light.transform.forward, samplePosition - light.transform.position);
            if (angleToNode > light.spotAngle / 2f)
            {
                // Node is outside the spot light's cone
                return 0f;
            }
        }

        float distance = Vector3.Distance(samplePosition, light.transform.position);
        if (distance > lightRange)
        {
            return 0f;
        }

        if (light.shadows != LightShadows.None && Physics.Linecast(light.transform.position, samplePosition, out _, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
        {
            return 0f;
        }
        // Example using inverse square law with range consideration
        float attenuation = 1f / (distance * distance);
        return attenuation;
    }
}