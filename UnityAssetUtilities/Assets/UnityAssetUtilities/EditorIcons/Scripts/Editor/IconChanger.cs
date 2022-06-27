using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Static class providing methods for asset's icon changing.</summary>
public static class IconChanger
{
    private const string iconEntryPrefix = "icon: ";
    private const string iconEntrySufix = "}";
    private const string iconEntryInstanceId = "{instanceID: <INSTANCEID>";
    private const string iconEntryFileId = "{fileID: <FILEID>, guid: <GUID>, type: 0";

    private static Object selectedAsset;

    [MenuItem("Assets/Change icon", isValidateFunction: true)]
    private static bool ChangeIconValidation()
    {
        return Selection.objects != null && Selection.objects.Length == 1;
    }

    [MenuItem("Assets/Change icon")]
    private static void ChangeIcon()
    {
        selectedAsset = Selection.objects[0];
        if (selectedAsset != null)
        {
            IconChangerEditorWindow.ShowWindow();
        }
    }

    /// <summary>Changes icon of an selected asset.</summary>
    /// <param name="asset">Asset to change icon of.</param>
    /// <param name="guid">GUID of file where the icon is defined.</param>
    /// <param name="fileId">FileID of the icon.</param>
    public static void ChangeIcon(Object asset, string guid, string fileId)
    {
        if (asset != null)
        {
            string metaFilePath = AssetDatabase.GetTextMetaFilePathFromAssetPath(AssetDatabase.GetAssetPath(asset));
            ChangeIcon(metaFilePath, guid, fileId);
            AssetDatabase.Refresh();
        }
    }
    /// <summary>Changes icon of an selected asset.</summary>
    /// <param name="asset">Asset to change icon of.</param>
    /// <param name="iconAsset">Target icon.</param>
    public static void ChangeIcon(Object asset, Texture2D iconAsset)
    {
        if (asset != null)
        {
            string metaFilePath = AssetDatabase.GetTextMetaFilePathFromAssetPath(AssetDatabase.GetAssetPath(asset));
            if (iconAsset != null)
            {
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(iconAsset, out string guid, out long localId))
                {
                    ChangeIcon(metaFilePath, guid, localId.ToString());
                    AssetDatabase.Refresh();
                }
            }
            else
            {
                ChangeIcon(metaFilePath, null, null);
                AssetDatabase.Refresh();
            }
        }
    }

    private static void ChangeIcon(string metaFilePath, string iconAssetGuid, string iconFileId)
    {
        try
        {
            if (File.Exists(metaFilePath))
            {
                if (iconFileId == null) iconFileId = "0";
                bool isDefaultIcon = iconFileId.Equals("0");
                int prefixIndex = -1;

                string content = File.ReadAllText(metaFilePath);

                prefixIndex = content.IndexOf(iconEntryPrefix);

                if (prefixIndex >= 0)
                {
                    string firstPart = content.Substring(0, prefixIndex);
                    string tempSecondPart = content.Substring(prefixIndex);
                    int sufixIndex = tempSecondPart.IndexOf(iconEntrySufix) + prefixIndex;
                    string secondPart = content.Substring(sufixIndex);

                    if (isDefaultIcon)
                    {
                        content = $"{firstPart}{iconEntryPrefix}{iconEntryInstanceId.Replace("<INSTANCEID>", iconFileId)}{secondPart}";
                    }
                    else
                    {
                        content = $"{firstPart}{iconEntryPrefix}{iconEntryFileId.Replace("<FILEID>", iconFileId).Replace("<GUID>", iconAssetGuid)}{secondPart}";
                    }
                }
                else if (!isDefaultIcon)
                {
                    if (!content.EndsWith("\n")) content += "\n";
                    content += $"{iconEntryPrefix}{iconEntryFileId.Replace("<FILEID>", iconFileId).Replace("<GUID>", iconAssetGuid)}{iconEntrySufix}";
                }
                File.WriteAllText(metaFilePath, content);
            }
            else
            {
                Debug.LogError($"Meta file at path: {metaFilePath} doesn't exist!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
    }

    private class IconChangerEditorWindow : EditorWindow
    {
        private enum Tab
        {
            ByReference,
            ByFileId
        }

        private static Texture2D iconAsset;
        private static string guid = "0";
        private static string fileId = "0";

        private Tab currentTab = Tab.ByReference;

        public static void ShowWindow()
        {
            var window = ScriptableObject.CreateInstance<IconChangerEditorWindow>();
            window.titleContent = new GUIContent("Icon changer");
            window.minSize = new Vector2(300.0f, window.minSize.y + 10.0f);
            window.maxSize = new Vector2(window.maxSize.x, window.minSize.y + 10.0f);
            window.ShowUtility();
        }

        private void OnGUI()
        {
            if (selectedAsset != null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(new GUIContent($"Changing icon of"), selectedAsset, typeof(Object), allowSceneObjects: false);
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Toggle(currentTab == Tab.ByReference, new GUIContent("By asset reference"), EditorStyles.toolbarButton)) currentTab = Tab.ByReference;
                if (GUILayout.Toggle(currentTab == Tab.ByFileId, new GUIContent("By GUID and FileID"), EditorStyles.toolbarButton)) currentTab = Tab.ByFileId;
                EditorGUILayout.EndHorizontal();

                if (currentTab == Tab.ByReference)
                {
                    var iconAssetNew = EditorGUILayout.ObjectField(iconAsset, typeof(Texture2D), allowSceneObjects: false) as Texture2D;
                    if (iconAssetNew != null && iconAssetNew != iconAsset)
                    {
                        iconAsset = iconAssetNew;
                    }
                    EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
                }
                else
                {
                    guid = EditorGUILayout.TextField(new GUIContent("Icon Asset Guid"), guid);
                    fileId = EditorGUILayout.TextField(new GUIContent("Icon File Id"), fileId);
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Confirm")))
                {
                    if (currentTab == Tab.ByReference)
                    {
                        ChangeIcon(selectedAsset, iconAsset);
                    }
                    else
                    {
                        ChangeIcon(selectedAsset, guid, fileId);
                    }
                    Close();
                }
                if (GUILayout.Button(new GUIContent("Reset to default")))
                {
                    ChangeIcon(selectedAsset, null);
                    Close();
                }
                if (GUILayout.Button(new GUIContent("Cancel")))
                {
                    Close();
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                Close();
            }
        }
    }
}
