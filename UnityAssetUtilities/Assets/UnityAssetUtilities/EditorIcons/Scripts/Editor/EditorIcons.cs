using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityAssetUtilities
{
    /// <summary>Stores information about extracted editor icons.</summary>
    public class EditorIcons : ScriptableObject, ISerializationCallbackReceiver
    {
        [HideInInspector]
        [SerializeField]
        private List<Texture2D> icons = new List<Texture2D>();
        [HideInInspector]
        [SerializeField]
        private List<string> iconFileIds = new List<string>();
        [HideInInspector]
        [SerializeField]
        private List<float> iconBrightness = new List<float>();


        public List<EditorIconEntry> IconData = new List<EditorIconEntry>();

        private const string proxyAssetPath = "Assets/temporaryEditorIconsProxyAsset.mat";

        private static Material proxyAsset;


        public void ExtractEditorIcons()
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
            OnAfterDeserialize();
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

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            IconData = new List<EditorIconEntry>();
            for (int i = 0; i < icons.Count && i < iconFileIds.Count && i < iconBrightness.Count; ++i)
            {
                IconData.Add(new EditorIconEntry
                {
                    icon = icons[i],
                    fileId = iconFileIds[i],
                    brightness = iconBrightness[i]
                });
            }
        }

        public class EditorIconEntry
        {
            public Texture2D icon;
            public string fileId;
            public float brightness;
        }
    }

    [CustomEditor(typeof(EditorIcons))]
    public class EditorIconsEditor : Editor
    {
        [SerializeField]
        private Vector2 scrollPosition;

        private EditorIcons editorIcons;

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

        private void OnEnable()
        {
            editorIcons = (EditorIcons)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DrawIcons();
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
            foreach (var entry in editorIcons.IconData)
            {
                EditorGUILayout.BeginHorizontal();
                Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(50f), GUILayout.Width(50f));
                if (entry.brightness < 0.5f)
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
                GUI.DrawTexture(rect, entry.icon, ScaleMode.ScaleToFit);
                EditorGUILayout.SelectableLabel(entry.icon.name, GUILayout.MaxWidth(200f));
                EditorGUILayout.SelectableLabel(entry.fileId, GUILayout.MaxWidth(200f));
                EditorGUILayout.SelectableLabel($"{entry.icon.width}x{entry.icon.height}", GUILayout.Width(50f));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        [MenuItem("Assets/Create/Unity Asset Utilities/Editor Icons")]
        private static void Create()
        {
            string[] assetGUIDs = AssetDatabase.FindAssets($"t:{nameof(EditorIcons)}");
            if (assetGUIDs == null || assetGUIDs.Length == 0)
            {
                if (EditorUtility.DisplayDialog("Confirmation", $"{nameof(EditorIcons)} asset is about to be created. During creation the editor icons will be extracted. This can take up to several minutes. Do you really want to create it now?", "Yes", "No"))
                {
                    var asset = ScriptableObject.CreateInstance<EditorIcons>();
                    var path = $"{AssetDatabase.GetAssetPath(Selection.activeObject)}/EditorIcons.asset";
                    ProjectWindowUtil.CreateAsset(asset, path);
                    asset.ExtractEditorIcons();
                    EditorUtility.SetDirty(asset);
                    AssetDatabase.SaveAssetIfDirty(asset);
                }
            }
            else
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGUIDs[0]);
                var asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(EditorIcons));
                if (asset != null) EditorGUIUtility.PingObject(asset);
                EditorUtility.DisplayDialog("Asset already exists", $"{nameof(EditorIcons)} asset already exists. New one won't be created.", "Ok");
            }
        }
    }
}