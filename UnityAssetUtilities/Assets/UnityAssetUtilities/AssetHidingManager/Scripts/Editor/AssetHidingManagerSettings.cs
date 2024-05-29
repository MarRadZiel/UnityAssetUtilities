using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityAssetUtilities
{
    /// <summary>Stores information about asset hiding manager settings.</summary>
    public class AssetHidingManagerSettings : ScriptableObject
    {
        /// <summary>Icon set used by AssetHidingManager window.</summary>
        [SerializeField]
        [HideInInspector]
        public IconSet iconSet;

        [SerializeField]
        [HideInInspector]
        private bool syncSelection;

        [SerializeField]
        [HideInInspector]
        private List<AssetExtensionIcon> iconForAssetsCache;

        /// <summary>Tries to retrieve asset icon cached by AssetHidingManager.</summary>
        /// <param name="assetExtension">File extension of asset to search icon for.</param>
        /// <param name="icon">Found icon Texture.</param>
        /// <returns>True if icon was found, false otherwise.</returns>
        public bool TryGetIconForAsset(string assetExtension, out Texture icon)
        {
            if (iconForAssetsCache != null)
            {
                assetExtension = assetExtension.Trim().TrimStart('.').ToLower();
                foreach (var cachedIcon in iconForAssetsCache)
                {
                    if (string.Equals(cachedIcon.assetExtension, assetExtension))
                    {
                        icon = cachedIcon.icon;
                        return true;
                    }
                }
            }
            icon = null;
            return false;
        }

        /// <summary>Cache asset icon for AssetHidingManager..</summary>
        /// <param name="assetExtension">File extension of asset to cache icon for.</param>
        /// <param name="icon">Icon to be cached.</param>
        public void CacheAssetIcon(string assetExtension, Texture icon)
        {
            assetExtension = assetExtension.Trim().TrimStart('.').ToLower();

            var cachedIcon = new AssetExtensionIcon
            {
                assetExtension = assetExtension,
                icon = icon
            };
            if (iconForAssetsCache == null)
            {
                iconForAssetsCache = new List<AssetExtensionIcon>() { cachedIcon };
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssetIfDirty(this);
            }
            else
            {
                for (int i = 0; i < iconForAssetsCache.Count; ++i)
                {
                    if (string.Equals(iconForAssetsCache[i].assetExtension, assetExtension))
                    {
                        if (iconForAssetsCache[i].icon != icon)
                        {
                            iconForAssetsCache[i] = cachedIcon;
                            EditorUtility.SetDirty(this);
                            AssetDatabase.SaveAssetIfDirty(this);
                        }
                        return;
                    }
                }

                // If there was no icon for this extension, add a new one
                iconForAssetsCache.Add(cachedIcon);
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssetIfDirty(this);
            }
        }

        /// <summary>Clears asset icon cache for AssetHidingManager..</summary>
        public void ClearAssetIconCache()
        {
            iconForAssetsCache.Clear();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        /// <summary>Retrieves selection synchronization enablement.</summary>
        /// <returns>True if synchronization is enabled, false otherwise.</returns>
        public bool IsSelectionSyncEnabled()
        {
            return syncSelection;
        }
        /// <summary>Set enablement of selection synchronization.</summary>
        /// <param name="enabled">True to enable selection sync, false to disable it.</param>
        /// <returns>State of sync enablement.</returns>
        public bool SetSelectionSync(bool enabled)
        {
            if (syncSelection != enabled)
            {
                syncSelection = enabled;
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssetIfDirty(this);
            }
            return syncSelection;
        }


        [System.Serializable]
        private class AssetExtensionIcon
        {
            public string assetExtension;
            public Texture icon;
        }
    }

    [CustomEditor(typeof(AssetHidingManagerSettings))]
    public class AssetHidingManagerSettingsEditor : Editor
    {
        AssetHidingManagerSettings settings;

        private void OnEnable()
        {
            settings = (AssetHidingManagerSettings)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var iconForAssetsCacheProperty = serializedObject.FindProperty("iconForAssetsCache");
            if (iconForAssetsCacheProperty != null)
            {
                EditorGUILayout.LabelField(new GUIContent($"Cached icons: {iconForAssetsCacheProperty.arraySize}"));
                EditorGUI.BeginDisabledGroup(iconForAssetsCacheProperty.arraySize <= 0);
                if (GUILayout.Button(new GUIContent("Clear icon cache")))
                {
                    settings.ClearAssetIconCache();
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        [MenuItem("Assets/Create/Unity Asset Utilities/Settings/Asset Hiding Manager Settings")]
        private static void Create()
        {
            string[] assetGUIDs = AssetDatabase.FindAssets($"t:{nameof(AssetHidingManagerSettings)}");
            if (assetGUIDs == null || assetGUIDs.Length == 0)
            {
                var asset = ScriptableObject.CreateInstance<AssetHidingManagerSettings>();
                foreach (var iconSet in Resources.FindObjectsOfTypeAll<IconSet>())
                {
                    if (iconSet.name.Equals("AssetHidingManagerIconSet"))
                    {
                        asset.iconSet = iconSet;
                    }
                }
                var path = $"{AssetDatabase.GetAssetPath(Selection.activeObject)}/AssetHidingManagerSettings.asset";
                ProjectWindowUtil.CreateAsset(asset, path);
            }
            else
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGUIDs[0]);
                var asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(AssetHidingManagerSettings));
                if (asset != null) EditorGUIUtility.PingObject(asset);
                EditorUtility.DisplayDialog("Asset already exists", $"{nameof(AssetHidingManagerSettings)} asset already exists. New one won't be created.", "Ok");
            }
        }
    }
}