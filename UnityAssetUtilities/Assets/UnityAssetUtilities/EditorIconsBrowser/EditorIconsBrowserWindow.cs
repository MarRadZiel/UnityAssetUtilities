using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityAssetUtilities
{
    public class EditorIconsBrowserWindow : EditorWindow
    {
        [SerializeField]
        private Vector2 scrollPosition;

        [SerializeField]
        private List<Texture2D> icons = new List<Texture2D>();
        [SerializeField]
        private List<string> iconFileIds = new List<string>();
        [SerializeField]
        private List<float> iconBrightness = new List<float>();

        private const string proxyAssetPath = "Assets/temporaryEditorIconsProxyAsset.mat";

        private static Material proxyAsset;

        private Texture2D _blackTexture;
        private Texture2D BlackTexture
        {
            get
            {
                if (_blackTexture == null)
                {
                    _blackTexture = new Texture2D(1, 1);
                    _blackTexture.SetPixel(0, 0, Color.black);
                    _blackTexture.Apply();
                }
                return _blackTexture;
            }
        }


        [MenuItem("Tools/Editor Icons Browser")]
        private static void ShowWindow()
        {
            var window = GetWindow<EditorIconsBrowserWindow>();
            window.titleContent = new GUIContent("Editor icons browser");
            window.Show();
        }

        private void OnDisable()
        {
            DeleteProxyAsset();
        }

        private void OnGUI()
        {
            if (icons == null || icons.Count <= 0)
            {
                EditorGUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Load icons"))
                {
                    GetIconsData();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
            }
            else
            {
                DrawIcons();
            }
        }

        private void DrawIcons()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.GetControlRect(GUILayout.Height(1f), GUILayout.Width(50f));
            EditorGUILayout.SelectableLabel("Name", GUILayout.MaxWidth(200f));
            EditorGUILayout.SelectableLabel("FileID", GUILayout.MaxWidth(200f));
            EditorGUILayout.SelectableLabel("Size", GUILayout.Width(50f));
            EditorGUILayout.EndHorizontal();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            for (int i = 0; i < icons.Count && i < iconFileIds.Count && i < iconBrightness.Count; ++i)
            {
                EditorGUILayout.BeginHorizontal();
                Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(50f), GUILayout.Width(50f));
                if (iconBrightness[i] < 0.5f)
                {
                    GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture, ScaleMode.ScaleToFit);
                }
                else
                {
                    GUI.DrawTexture(rect, BlackTexture, ScaleMode.ScaleToFit);
                }
                rect.x += 1;
                rect.y += 1;
                rect.width -= 2;
                rect.height += 2;
                GUI.DrawTexture(rect, icons[i], ScaleMode.ScaleToFit);
                EditorGUILayout.SelectableLabel(icons[i].name, GUILayout.MaxWidth(200f));
                EditorGUILayout.SelectableLabel(iconFileIds[i], GUILayout.MaxWidth(200f));
                EditorGUILayout.SelectableLabel($"{icons[i].width}x{icons[i].height}", GUILayout.Width(50f));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void GetIconsData()
        {
            if (proxyAsset == null) LoadOrCreateProxyAsset();
            var editorAssetBundle = GetEditorAssetBundle();
            var iconsPath = GetIconsPath();
            foreach (var assetName in EnumerateIcons(editorAssetBundle, iconsPath))
            {
                var icon = editorAssetBundle.LoadAsset<Texture2D>(assetName);
                if (icon == null)
                    continue;
                icons.Add(icon);
                iconFileIds.Add(GetFileId(icon));
                iconBrightness.Add(GetBrightness(icon));
            }
            DeleteProxyAsset();
        }
        private void LoadOrCreateProxyAsset()
        {
            proxyAsset = AssetDatabase.LoadAssetAtPath<Material>(proxyAssetPath);
            if (proxyAsset == null)
            {
                proxyAsset = new Material(Shader.Find("Unlit/Texture"));
                AssetDatabase.CreateAsset(proxyAsset, proxyAssetPath);
            }

            proxyAsset.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
            EditorUtility.SetDirty(proxyAsset);
            AssetDatabase.SaveAssetIfDirty(proxyAsset);
        }
        private void DeleteProxyAsset()
        {
            if (proxyAsset != null)
            {
                DestroyImmediate(proxyAsset, true);
                AssetDatabase.DeleteAsset(proxyAssetPath);
                AssetDatabase.Refresh();
            }
        }

        private float GetBrightness(Texture2D tex)
        {
            Texture2D copyTex = new Texture2D(tex.width, tex.height, tex.format, tex.mipmapCount, true);
            Graphics.CopyTexture(tex, copyTex);
            long pixels = 0;
            float r = 0;
            float g = 0;
            float b = 0;
            foreach (var pixel in copyTex.GetPixels())
            {
                if (pixel.a <= 0) continue;
                r += pixel.r;
                g += pixel.g;
                b += pixel.b;
                ++pixels;
            }
            DestroyImmediate(copyTex, true);
            return ((r + g + b) / pixels) / 3;
        }

        private static IEnumerable<string> EnumerateIcons(AssetBundle editorAssetBundle, string iconsPath)
        {
            foreach (var assetName in editorAssetBundle.GetAllAssetNames())
            {
                if (assetName.StartsWith(iconsPath, StringComparison.OrdinalIgnoreCase) == false)
                    continue;
                if (assetName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) == false &&
                    assetName.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) == false)
                    continue;

                yield return assetName;
            }
        }

        private static string GetFileId(Texture icon)
        {
            proxyAsset.mainTexture = icon;
            EditorUtility.SetDirty(proxyAsset);
            AssetDatabase.SaveAssetIfDirty(proxyAsset);
            var serializedAsset = File.ReadAllText(proxyAssetPath);
            var index = serializedAsset.IndexOf("_MainTex:", StringComparison.Ordinal);
            if (index == -1)
                return string.Empty;

            const string FileId = "fileID:";
            var startIndex = serializedAsset.IndexOf(FileId, index) + FileId.Length;
            var endIndex = serializedAsset.IndexOf(',', startIndex);
            return serializedAsset.Substring(startIndex, endIndex - startIndex).Trim();
        }

        private static AssetBundle GetEditorAssetBundle()
        {
            var editorGUIUtility = typeof(EditorGUIUtility);
            var getEditorAssetBundle = editorGUIUtility.GetMethod(
                "GetEditorAssetBundle",
                BindingFlags.NonPublic | BindingFlags.Static);

            return (AssetBundle)getEditorAssetBundle.Invoke(null, new object[] { });
        }

        private static string GetIconsPath()
        {
#if UNITY_2018_3_OR_NEWER
            return UnityEditor.Experimental.EditorResources.iconsPath;
#else
            var assembly = typeof(EditorGUIUtility).Assembly;
            var editorResourcesUtility = assembly.GetType("UnityEditorInternal.EditorResourcesUtility");

            var iconsPathProperty = editorResourcesUtility.GetProperty(
                "iconsPath",
                BindingFlags.Static | BindingFlags.Public);

            return (string)iconsPathProperty.GetValue(null, new object[] { });
#endif
        }
    }
}
