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
            bool sourceFileExists = System.IO.File.Exists(externalAsset.ExternalFilePath);
            if (sourceFileExists)
            {
                string absolutePath = AssetsUtility.AssetsRelativeToAbsolutePath(externalAsset.AssetPath);
                bool assetExists = System.IO.File.Exists(absolutePath);
                if (assetExists)
                {
                    System.IO.FileInfo sourceFileInfo = new System.IO.FileInfo(externalAsset.ExternalFilePath);
                    System.IO.FileInfo assetFileInfo = new System.IO.FileInfo(absolutePath);
                    if (sourceFileInfo.LastWriteTime > assetFileInfo.LastWriteTime)
                    {
                        if (EditorUtility.DisplayDialog("External asset modified", $"External asset version is newer than asset at path: {externalAsset.AssetPath}\nShould asset be updated?", "Yes", "No"))
                        {
                            try
                            {
                                System.IO.File.Delete(absolutePath);
                                System.IO.File.Copy(externalAsset.ExternalFilePath, absolutePath);
                                AssetDatabase.Refresh();
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogError($"Error during external asset update.\n{e}");
                            }
                        }
                    }
                }
                else
                {
                    if (EditorUtility.DisplayDialog("External asset modified", $"External asset version is newer than asset at path: {externalAsset.AssetPath}\nShould asset be updated?", "Yes", "No"))
                    {
                        try
                        {
                            System.IO.File.Copy(externalAsset.ExternalFilePath, absolutePath);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"Error during external asset update.\n{e}");
                        }
                    }
                    AssetDatabase.Refresh();
                }
            }
            else
            {
                Debug.LogError($"Source file at {sourceFileExists} doesn't exist.");
            }
        }
    }
}
