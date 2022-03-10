using UnityEditor;
using UnityEngine;

/// <summary>Checks modification state of external assets in each editor update tick.</summary>
[InitializeOnLoad]
public static class ExternalAssetsUpdater
{
    private static ExternalAssetsManagerSettings _externalAssetsManagerSettings;
    public static ExternalAssetsManagerSettings ExternalAssetsManagerSettings => _externalAssetsManagerSettings;


    static ExternalAssetsUpdater()
    {
        LoadExternalAssetsManagerSettings();
        EditorApplication.update += OnUpdate;
    }

    private static void OnUpdate()
    {
        if (!_externalAssetsManagerSettings)
        {
            LoadExternalAssetsManagerSettings();
        }
        CheckForAssetModifications();
    }

    private static void LoadExternalAssetsManagerSettings()
    {
        Debug.Log("Loading External Asset Manager Settings...");
        string[] assetGUIDs = AssetDatabase.FindAssets($"t:{nameof(ExternalAssetsManagerSettings)}");
        if (assetGUIDs != null && assetGUIDs.Length > 0)
        {
            _externalAssetsManagerSettings = AssetDatabase.LoadAssetAtPath<ExternalAssetsManagerSettings>(AssetDatabase.GUIDToAssetPath(assetGUIDs[0]));
            Debug.Log("Settings asset loaded.");
        }
        else
        {
            CreateExternalAssetsManagerSettingsAsset();
            Debug.Log("Created new settings asset.");
        }
    }

    public static void CreateExternalAssetsManagerSettingsAsset()
    {
        _externalAssetsManagerSettings = ScriptableObject.CreateInstance<ExternalAssetsManagerSettings>();
        AssetDatabase.CreateFolder("Assets", "Settings");
        AssetDatabase.CreateAsset(_externalAssetsManagerSettings, AssetDatabase.GenerateUniqueAssetPath("Assets/Settings/ExternalAssetsManagerSettings.asset"));
        AssetDatabase.SaveAssets();
        Selection.activeObject = _externalAssetsManagerSettings;
    }

    private static void CheckForAssetModifications()
    {
        foreach (var externalAsset in ExternalAssetsManagerSettings.ExternalAssets)
        {
            if (externalAsset.AutoUpdate)
            {
                externalAsset.RefreshFileInfos();
                if (externalAsset.SourceFileInfo.Exists)
                {
                    if (externalAsset.AssetFileInfo.Exists)
                    {
                        if (!externalAsset.IsAssetUpToDate(refresh: false))
                        {
                            if (EditorUtility.DisplayDialog("External asset modified", $"External asset version is newer than asset at path: {externalAsset.AssetPath}\nShould asset be updated? If you refuse, automatic update will be disabled.", "Yes", "No"))
                            {
                                try
                                {
                                    externalAsset.AssetFileInfo.Delete();
                                    externalAsset.SourceFileInfo.CopyTo(externalAsset.AssetFileInfo.FullName);
                                    AssetDatabase.Refresh();
                                }
                                catch (System.Exception e)
                                {
                                    Debug.LogError($"Error during external asset update.\n{e}");
                                }
                            }
                            else
                            {
                                externalAsset.AutoUpdate = false;
                                EditorUtility.SetDirty(ExternalAssetsManagerSettings);
                                AssetDatabase.SaveAssetIfDirty(ExternalAssetsManagerSettings);
                            }
                        }
                    }
                    else
                    {
                        if (EditorUtility.DisplayDialog("External asset modified", $"External asset version is newer than asset at path: {externalAsset.AssetPath}\nShould asset be updated? If you refuse, automatic update will be disabled.", "Yes", "No"))
                        {
                            try
                            {
                                externalAsset.SourceFileInfo.CopyTo(externalAsset.AssetFileInfo.FullName);
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogError($"Error during external asset update.\n{e}");
                            }
                        }
                        else
                        {
                            externalAsset.AutoUpdate = false;
                            EditorUtility.SetDirty(ExternalAssetsManagerSettings);
                            AssetDatabase.SaveAssetIfDirty(ExternalAssetsManagerSettings);
                        }
                        AssetDatabase.Refresh();
                    }
                }
                else
                {
                    Debug.LogError($"Source file at {externalAsset.SourceFileInfo.FullName} doesn't exist.");
                }
            }
        }
    }
}
