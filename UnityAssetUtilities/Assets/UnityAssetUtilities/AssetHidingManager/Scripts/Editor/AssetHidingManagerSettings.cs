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