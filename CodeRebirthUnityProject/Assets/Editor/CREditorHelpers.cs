using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace com.github.xuuxiaolan.crassetbundlebuilder
{
    public class EditorHelpers
    {
        public static Color DefaultBackgroundColor => GetDefaultBackgroundColor();

        private static Color GetDefaultBackgroundColor()
        {
            var method = typeof(EditorGUIUtility)
                .GetMethod("GetDefaultBackgroundColor", BindingFlags.NonPublic | BindingFlags.Static);
            return (Color)method.Invoke(null, null);
        }

        public static Color GetAlternatingColor(int arrayIndex)
        {
            return (arrayIndex % 2 == 0) ? new Color(0.8f, 0.8f, 1f, 1f) : new Color(1f, 0.8f, 0.8f, 1f);
        }

        public static GUIStyle GetNewStyle(bool enableRichText = true, int fontSize = -1)
        {
            GUIStyle newStyle = new GUIStyle { richText = enableRichText };

            if (fontSize != -1)
                newStyle.fontSize = fontSize;

            return newStyle;
        }

        public static GUIStyle GetNewStyle(Color backgroundColor, bool enableRichText = true, int fontSize = -1)
        {
            GUIStyle newStyle = GetNewStyle(enableRichText, fontSize);
            newStyle.normal.background = GetColoredTexture(backgroundColor);
            return newStyle;
        }

        private static Texture2D GetColoredTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        public static List<GameObject> GetPrefabsWithType(Type type)
        {
            List<GameObject> returnList = new List<GameObject>();
            IEnumerable<GameObject> allPrefabs = AssetDatabase.FindAssets("t:GameObject")
                .Select(x => AssetDatabase.GUIDToAssetPath(x))
                .Select(x => AssetDatabase.LoadAssetAtPath<GameObject>(x));

            foreach (GameObject prefab in allPrefabs)
            {
                if (prefab.GetComponent(type) != null || prefab.GetComponentInChildren(type) != null)
                    returnList.Add(prefab);
            }

            return returnList;
        }

        public static void SerializeDictionary<K, V>(ref Dictionary<K, V> dictionary, ref List<K> keys, ref List<V> values)
        {
            if (dictionary == null)
                dictionary = new Dictionary<K, V>();
            keys.Clear();
            values.Clear();
            foreach (KeyValuePair<K, V> kvp in dictionary)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }

        public static void DeserializeDictionary<K, V>(ref Dictionary<K, V> dictionary, ref List<K> keys, ref List<V> values)
        {
            if (dictionary == null)
                dictionary = new Dictionary<K, V>();
            else
                dictionary.Clear();

            for (int i = 0; i != Math.Min(keys.Count, values.Count); i++)
                dictionary.Add(keys[i], values[i]);
        }
    }

    public static class BackgroundStyle
    {
        public static GUIStyle Get(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            GUIStyle style = new GUIStyle();
            texture.SetPixel(0, 0, color);
            texture.Apply();
            style.normal.background = texture;
            return style;
        }
    }

    public static class StringExtensions
    {
        public static string Colorize(this string text, Color color)
        {
            Color32 c = color;
            return $"<color=#{c.r:X2}{c.g:X2}{c.b:X2}{c.a:X2}>{text}</color>";
        }

        public static string ToBold(this string text)
        {
            return $"<b>{text}</b>";
        }
    }
}