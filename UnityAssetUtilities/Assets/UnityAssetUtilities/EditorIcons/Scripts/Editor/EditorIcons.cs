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

        [HideInInspector]
        [SerializeField]
        private List<bool> iconExtractionStatus = new List<bool>();

        /// <summary>Stores extracted editor icons information.</summary>
        public List<EditorIconEntry> IconData = new List<EditorIconEntry>();

        private Dictionary<Texture2D, int> indexMap = new Dictionary<Texture2D, int>();


        private const string proxyAssetPath = "Assets/temporaryEditorIconsProxyAsset.mat";

        private static Material proxyAsset;

        /// <summary>Performs editor icons information extraction.</summary>
        public void ExtractEditorIconsInfo()
        {
            if (proxyAsset == null) LoadOrCreateProxyAsset();
            var editorAssetBundle = GetEditorAssetBundle();
            var iconsPath = GetIconsPath();
            foreach (var assetName in EnumerateIcons(editorAssetBundle, iconsPath))
            {
                var icon = editorAssetBundle.LoadAsset<Texture2D>(assetName);
                if (icon == null)
                    continue;

                iconExtractionStatus.Add(false);
                icons.Add(icon);
                iconFileIds.Add(GetFileId(icon));
                iconBrightness.Add(GetBrightness(icon));
            }
            DeleteProxyAsset();
            OnAfterDeserialize();
        }

        /// <summary>Saves copy of specified editor icon as this ScriptableObject subasset.</summary>
        /// <param name="icon">Editor icon reference.</param>
        /// <param name="saveAsset">Should this asset be set as dirty and saved.</param>
        public void ExtractIcon(Texture2D icon, bool saveAsset)
        {
            ExtractIcon(indexMap[icon], saveAsset);
        }
        private void ExtractIcon(int index, bool saveAsset)
        {
            if (iconExtractionStatus.Count > index && iconExtractionStatus[index]) return;

            Texture2D iconCopy = new Texture2D(icons[index].width, icons[index].height, icons[index].format, icons[index].mipmapCount, true);
            Graphics.CopyTexture(icons[index], iconCopy);
            UnityEngine.Object ob = Instantiate(iconCopy);
            ob.name = icons[index].name;
            AssetDatabase.AddObjectToAsset(ob, this);
            iconExtractionStatus[index] = true;
            if (saveAsset)
            {
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssetIfDirty(this);
            }
        }

        /// <summary>Saves copy of all editor icons as this ScriptableObject subassets.</summary>
        public void ExtractAllIcons()
        {
            for (int i = 0; i < icons.Count; ++i)
            {
                ExtractIcon(i, saveAsset: false);
            }
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        /// <summary>Checks if specified icon was already extracted.</summary>
        /// <param name="icon">Editor icon reference to check.</param>
        /// <returns>True if icon was already extracted.</returns>
        public bool WasExtracted(Texture2D icon)
        {
            return WasExtracted(indexMap[icon]);
        }
        private bool WasExtracted(int index)
        {
            return iconExtractionStatus[index];
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
            indexMap = new Dictionary<Texture2D, int>();
            IconData = new List<EditorIconEntry>();
            for (int i = 0; i < icons.Count && i < iconFileIds.Count && i < iconBrightness.Count; ++i)
            {
                IconData.Add(new EditorIconEntry
                {
                    icon = icons[i],
                    fileId = iconFileIds[i],
                    brightness = iconBrightness[i]
                });
                indexMap.Add(icons[i], i);
            }
        }

        /// <summary>Stores information about single extracted editor icon.</summary>
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
        private enum SearchType
        {
            Name,
            FileID
        }

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

        private SearchType searchType = SearchType.Name;
        private string searchString = string.Empty;
        private bool matchCase = false;


        private void OnEnable()
        {
            editorIcons = (EditorIcons)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.SelectableLabel($"GUID: 0000000000000000d000000000000000");

            if (GUILayout.Button(new GUIContent("Extract all icons", "Saves copy of all icon assets as EditorIcons subassets.")))
            {
                editorIcons.ExtractAllIcons();
            }
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            searchString = GUILayout.TextField(searchString, GUI.skin.FindStyle("ToolbarSeachTextField"));
            EditorGUI.BeginDisabledGroup(searchType != SearchType.Name);
            matchCase = GUILayout.Toggle(matchCase, new GUIContent("Aa", "Match case"), EditorStyles.toolbarButton, GUILayout.Width(25.0f));
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Toggle(searchType == SearchType.Name, new GUIContent("By name"), EditorStyles.toolbarButton, GUILayout.Width(100.0f))) searchType = SearchType.Name;
            if (GUILayout.Toggle(searchType == SearchType.FileID, new GUIContent("By FileID"), EditorStyles.toolbarButton, GUILayout.Width(100.0f))) searchType = SearchType.FileID;
            EditorGUILayout.EndHorizontal();

            DrawIcons();
        }

        private void DrawIcons()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.GetControlRect(GUILayout.Height(1f), GUILayout.Width(50f));
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontStyle = FontStyle.Bold;
            EditorGUILayout.SelectableLabel("Name", headerStyle, GUILayout.MaxWidth(200f));
            EditorGUILayout.SelectableLabel("FileID", headerStyle, GUILayout.MaxWidth(200f));
            EditorGUILayout.SelectableLabel("Size", headerStyle, GUILayout.Width(50f));
            EditorGUILayout.LabelField(GUIContent.none, GUILayout.Width(50f));
            EditorGUILayout.EndHorizontal();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            bool isSearching = !string.IsNullOrWhiteSpace(searchString);
            string lowercaseSearchingString = searchString.ToLower();
            foreach (var entry in editorIcons.IconData)
            {
                if (isSearching)
                {
                    if (searchType == SearchType.Name)
                    {
                        if (matchCase)
                        {
                            if (!entry.icon.name.Contains(searchString)) continue;
                        }
                        else
                        {
                            if (!entry.icon.name.ToLower().Contains(lowercaseSearchingString)) continue;
                        }
                    }
                    else if (searchType == SearchType.FileID)
                    {
                        if (!entry.fileId.ToLower().Contains(lowercaseSearchingString)) continue;
                    }
                }
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
                EditorGUILayout.LabelField(new GUIContent("{entry.icon.width}x{entry.icon.height}"), GUILayout.Width(50f));
                EditorGUI.BeginDisabledGroup(editorIcons.WasExtracted(entry.icon));
                if (GUILayout.Button(new GUIContent("Extract", "Saves copy of icon asset as EditorIcons sub asset."), GUILayout.Width(50f)))
                {
                    editorIcons.ExtractIcon(entry.icon, true);
                }
                EditorGUI.EndDisabledGroup();
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
                if (EditorUtility.DisplayDialog("Confirmation", $"{nameof(EditorIcons)} asset is about to be created. During creation the editor icons information will be extracted. This can take up to several minutes. Do you really want to create it now?", "Yes", "No"))
                {
                    EditorIcons asset = ScriptableObject.CreateInstance<EditorIcons>();
                    var path = $"{AssetDatabase.GetAssetPath(Selection.activeObject)}/EditorIcons.asset";
                    ProjectWindowUtil.CreateAsset(asset, path);
                    asset.ExtractEditorIconsInfo();
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