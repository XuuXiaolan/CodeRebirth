using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace CodeRebirth.src.MiscScripts
{
    public class DetectLightInSurroundings : MonoBehaviour
    {
        private float lightValue = 0f;
        private static List<GameObject> lights = new List<GameObject>();
        private readonly float refreshRate = 10f;

        private void Start() // keep in mind that the player also has a lightsource attached to them, this isnt exclusive to just interior lights, but you could try to exclude all lights that arent in the interior for some small performance gains (probably almost 0 gain lol).
        {
            // Populate the list of lights only once
            if (lights.Count == 0)
            {
                HDAdditionalLightData[] potentialLights = FindObjectsOfType<HDAdditionalLightData>();
                foreach (HDAdditionalLightData light in potentialLights)
                {
                    if (!lights.Contains(light.gameObject) && light.gameObject.transform.position.y <= -20)
                    {
                        lights.Add(light.gameObject);
                    }
                }
            }

            StartCoroutine(UpdateLightValue());
        }

        private IEnumerator UpdateLightValue()
        {
            while (true)
            {
                //Stopwatch timer = new Stopwatch();
                //timer.Start();
                // Reset light value before calculation
                lightValue = 0f;

                foreach (GameObject lightObj in lights)
                {
                    HDAdditionalLightData hdLightData = lightObj.GetComponent<HDAdditionalLightData>();
                    Light lightComponent = lightObj.GetComponent<Light>();

                    if (hdLightData != null && lightComponent != null && lightComponent.enabled)
                    {
                        // Get positions
                        Vector3 lightPosition = lightObj.transform.position;
                        Vector3 nodePosition = transform.position;

                        // Direction and distance from light to node
                        Vector3 direction = nodePosition - lightPosition;
                        float distance = direction.magnitude;

                        // Check if the node is within the light's range
                        float lightRange = lightComponent.range;
                        if (distance <= lightRange)
                        {
                            // Perform a raycast to check for occlusion
                            RaycastHit hit;
                            if (!Physics.Raycast(lightPosition, direction.normalized, out hit, distance, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                            {
                                // No obstruction; proceed to calculate light contribution
                                float attenuation = CalculateAttenuation(distance, lightRange);

                                // Get light intensity (considering light type and HDRP settings)
                                float intensity = GetLightIntensity(hdLightData, lightComponent, direction);

                                // Accumulate light value
                                float contribution = intensity * attenuation;
                                lightValue += contribution;
                            }
                        }
                    }
                }

                // Use the calculated lightValue as needed
                //Plugin.ExtendedLogging($"Light value at node {gameObject.name} and position {transform.position.x} {transform.position.y} {transform.position.z}: {lightValue}");
                //timer.Stop();
                //Plugin.ExtendedLogging($"{timer.ElapsedMilliseconds}ms + {timer.ElapsedTicks} ticks");
                yield return new WaitForSeconds(refreshRate);
            }
        }

        /// <summary>
        /// Calculates attenuation based on distance and light range.
        /// </summary>
        private float CalculateAttenuation(float distance, float range)
        {
            // Example using inverse square law with range consideration
            float attenuation = 1f / (distance * distance);
            return attenuation;
        }

        /// <summary>
        /// Retrieves the light intensity, adjusting for light type and direction.
        /// </summary>
        private float GetLightIntensity(HDAdditionalLightData hdLightData, Light lightComponent, Vector3 directionToNode)
        {
            float intensity = hdLightData.intensity;

            // Adjust intensity based on light type
            switch (lightComponent.type)
            {
                case LightType.Point:
                    // Point light; no additional adjustments needed
                    break;

                case LightType.Spot:
                    // Adjust for spot angle
                    float angleToNode = Vector3.Angle(lightComponent.transform.forward, directionToNode);
                    if (angleToNode > lightComponent.spotAngle / 2f)
                    {
                        // Node is outside the spot light's cone
                        return 0f;
                    }
                    break;

                case LightType.Directional:
                    // For directional lights, intensity doesn't attenuate with distance
                    intensity = hdLightData.intensity;
                    break;

                case LightType.Area:
                    // Area lights are more complex; simplifying as point light for this example
                    break;
            }

            return intensity;
        }
    }
}
