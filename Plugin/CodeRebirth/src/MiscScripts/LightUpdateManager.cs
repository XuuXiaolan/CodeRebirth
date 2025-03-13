using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Util;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class LightUpdateManager : MonoBehaviour
{
    public static HashSet<DetectLightInSurroundings> detectLightInSurroundings = new();

    public void Start()
    {
        StartCoroutine(DoUpdateDetectLightInSurroundings());
    }

    public IEnumerator DoUpdateDetectLightInSurroundings()
    {
        List<DetectLightInSurroundings> list = new();
        while (true)
        {
            yield return new WaitUntil(() => CodeRebirthUtils.currentRoundLightData.Count > 0 && detectLightInSurroundings.Count > 0);
            list.Clear();
            list.AddRange(detectLightInSurroundings);
            foreach (var detectLight in list)
            {
                yield return StartCoroutine(detectLight.UpdateLightValue());
                yield return new WaitForSeconds(0.5f);
            }
            // Stuff in list --> HashSet
            // Removing stuff in list --> HashSet
            // Maintain unique collection --> HashSet
        }
    }
}