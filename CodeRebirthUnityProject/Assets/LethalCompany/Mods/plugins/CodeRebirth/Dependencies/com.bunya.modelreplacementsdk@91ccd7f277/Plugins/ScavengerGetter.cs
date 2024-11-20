using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

public static class ScavengerGetter {
    private static GameObject originalScavenger;
    public static GameObject Get() {
        if (originalScavenger == null) {
            originalScavenger = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath("a024259985699eb4fb6d4585e872c37c"));
        }
        return originalScavenger;
    }
}

#endif
