using System.IO;
using UnityEditor;
using UnityEngine;

public static class IconChanger
{
    private const string proxyAssetPath = "Assets/temporaryIconChangerProxyAsset.mat";

    private static Material proxyAsset;

    private const string iconEntryPrefix = "icon: ";
    private const string iconEntrySufix = "}";
    private const string iconEntryInstanceId = "{instanceID: <INSTANCEID>";
    private const string iconEntryFileId = "{fileID: <FILEID>, guid: <GUID>, type: 0";

    private static Object selectedObject;

    [MenuItem("Assets/Change icon", isValidateFunction: true)]
    private static bool ChangeIconValidation()
    {
        return Selection.objects != null && Selection.objects.Length == 1;
    }

    [MenuItem("Assets/Change icon")]
    private static void ChangeIcon()
    {
        selectedObject = Selection.objects[0];
        if (selectedObject != null)
        {
            IconChangerEditorWindow.ShowWindow();
        }
    }

    private static void OnIconConfirmed(string guid, string fileId)
    {
        if (selectedObject != null)
        {
            string metaFilePath = AssetDatabase.GetTextMetaFilePathFromAssetPath(AssetDatabase.GetAssetPath(selectedObject));
            ChangeIcon(metaFilePath, guid, fileId);
            AssetDatabase.Refresh();
        }
    }


    private static void LoadOrCreateProxyAsset()
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
    private static void DeleteProxyAsset()
    {
        if (proxyAsset != null)
        {
            Object.DestroyImmediate(proxyAsset, true);
            AssetDatabase.DeleteAsset(proxyAssetPath);
            AssetDatabase.Refresh();
        }
    }
    private static string GetFileId(Texture icon)
    {
        proxyAsset.mainTexture = icon;
        EditorUtility.SetDirty(proxyAsset);
        AssetDatabase.SaveAssetIfDirty(proxyAsset);
        var serializedAsset = File.ReadAllText(proxyAssetPath);
        var index = serializedAsset.IndexOf("_MainTex:", System.StringComparison.Ordinal);
        if (index == -1)
            return string.Empty;

        const string FileId = "fileID:";
        var startIndex = serializedAsset.IndexOf(FileId, index) + FileId.Length;
        var endIndex = serializedAsset.IndexOf(',', startIndex);
        return serializedAsset.Substring(startIndex, endIndex - startIndex).Trim();
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
        private static Texture2D iconAsset;
        private static string guid = "0";
        private static string fileId = "0";

        public static void ShowWindow()
        {
            var window = ScriptableObject.CreateInstance<IconChangerEditorWindow>();
            window.titleContent = new GUIContent("Icon changer");
            window.ShowUtility();
        }

        private void OnGUI()
        {
            if (selectedObject != null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(new GUIContent($"Changing icon of"), selectedObject, typeof(Object), allowSceneObjects: false);
                EditorGUI.EndDisabledGroup();
                var iconAssetNew = EditorGUILayout.ObjectField(iconAsset, typeof(Texture2D), allowSceneObjects: false) as Texture2D;
                if (iconAssetNew != null && iconAssetNew != iconAsset)
                {
                    iconAsset = iconAssetNew;
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(iconAsset, out string _guid, out long localId))
                    {
                        guid = _guid;
                        fileId = localId.ToString();
                    }
                }
                guid = EditorGUILayout.TextField(new GUIContent("Icon Asset Guid"), guid);
                fileId = EditorGUILayout.TextField(new GUIContent("Icon File Id"), fileId);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Confirm"))
                {
                    OnIconConfirmed(guid, fileId);
                    Close();
                }
                if (GUILayout.Button("Reset to default"))
                {
                    OnIconConfirmed(null, null);
                    Close();
                }
                if (GUILayout.Button("Cancel"))
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

