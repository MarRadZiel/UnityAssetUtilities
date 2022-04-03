using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityAssetUtilities
{
    /// <summary>Stores information about external file and corresponding assets.</summary>
    public class ExternalAssetsManagerSettings : ScriptableObject
    {
        /// <summary>Should ExternalAssets be automatically updated when modification is detected.</summary>
        [SerializeField]
        [HideInInspector]
        public bool autoSynchronization = true;

        /// <summary>Should notification be displayed when ExternalAsset modification is detected.</summary>
        [SerializeField]
        [HideInInspector]
        public bool notifyBeforeUpdate = true;

        /// <summary>Icon set used by ExternalAssetsManager window.</summary>
        [SerializeField]
        [HideInInspector]
        public IconSet iconSet;

        [SerializeField]
        [HideInInspector]
        private List<ExternalAsset> externalAssets = new List<ExternalAsset>();

        /// <summary>Number of registered external assets.</summary>
        public int ExternalAssetsCount => externalAssets.Count;
        /// <summary>Enumerable collection of registered external assets.</summary>
        public IEnumerable<ExternalAsset> ExternalAssets => externalAssets;

        private void OnEnable()
        {
            foreach (var externalAsset in externalAssets)
            {
                externalAsset.RegenerateAbsoluteFilePaths();
            }
        }

        /// <summary>Checks if specified asset already has any related ExternalAsset.</summary>
        /// <param name="assetPath">Assets-relative asset path.</param>
        /// <returns>True if there is already realted ExternalAsset.</returns>
        public bool ContainsAsset(string assetPath)
        {
            foreach (var externalAsset in externalAssets)
            {
                if (externalAsset.AssetPath.Equals(assetPath))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Registers new ExternalAsset.</summary>
        /// <param name="externalFilePath">Source file path of this ExternalAsset. Should be absolute.</param>
        /// <param name="assetPath">Destination asset path of this ExternalAsset. Should be Assets-relative.</param>
        public void RegisterExternalAsset(string externalFilePath, string assetPath)
        {
            externalAssets.Add(new ExternalAsset(externalFilePath, assetPath));
        }
        /// <summary>Unregisters specified ExternalAsset.</summary>
        /// <param name="externalAsset">ExternalAsset to unregister.</param>
        public void UnregisterExternalAsset(ExternalAsset externalAsset)
        {
            externalAssets.Remove(externalAsset);
        }
    }

    [CustomEditor(typeof(ExternalAssetsManagerSettings))]
    public class ExternalAssetsManagerSettingsEditor : Editor
    {
        ExternalAssetsManagerSettings settings;

        private void OnEnable()
        {
            settings = (ExternalAssetsManagerSettings)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.LabelField($"External asset count: {settings.ExternalAssetsCount}");
            EditorGUI.BeginDisabledGroup(true);
            foreach (var externalAsset in settings.ExternalAssets)
            {
                EditorGUILayout.ObjectField(AssetDatabase.LoadAssetAtPath(externalAsset.AssetPath, typeof(Object)), typeof(Object), allowSceneObjects: false);
            }
            EditorGUI.EndDisabledGroup();
        }

        [MenuItem("Assets/Create/Unity Asset Utilities/Settings/External Assets Manager Settings")]
        private static void Create()
        {
            string[] assetGUIDs = AssetDatabase.FindAssets($"t:{nameof(ExternalAssetsManagerSettings)}");
            if (assetGUIDs == null || assetGUIDs.Length == 0)
            {
                var asset = ScriptableObject.CreateInstance<ExternalAssetsManagerSettings>();
                foreach (var iconSet in Resources.FindObjectsOfTypeAll<IconSet>())
                {
                    if (iconSet.name.Equals("ExternalAssetsIconSet"))
                    {
                        asset.iconSet = iconSet;
                    }
                }
                var path = $"{AssetDatabase.GetAssetPath(Selection.activeObject)}/ExternalAssetsManagerSettings.asset";
                ProjectWindowUtil.CreateAsset(asset, path);
            }
            else
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGUIDs[0]);
                var asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(ExternalAssetsManagerSettings));
                if (asset != null) EditorGUIUtility.PingObject(asset);
                EditorUtility.DisplayDialog("Asset already exists", $"{nameof(ExternalAssetsManagerSettings)} asset already exists. New one won't be created.", "Ok");
            }
        }
    }
}