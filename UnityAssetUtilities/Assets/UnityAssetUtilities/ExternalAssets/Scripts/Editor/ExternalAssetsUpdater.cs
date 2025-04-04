using UnityEditor;
using UnityEngine;

namespace UnityAssetUtilities
{
    /// <summary>Checks modification state of external assets in each editor update tick.</summary>
    [InitializeOnLoad]
    public static class ExternalAssetsUpdater
    {
        private static ExternalAssetsManagerSettings _externalAssetsManagerSettings;
        /// <summary>Settings for ExternalAssetsManager.</summary>
        public static ExternalAssetsManagerSettings ExternalAssetsManagerSettings => _externalAssetsManagerSettings;

        private const string defaultSettingsAssetPath = "Assets/Settings/ExternalAssetsManagerSettings.asset";

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
            if (_externalAssetsManagerSettings.autoSynchronization)
            {
                CheckForAssetModifications();
            }
        }

        /// <summary>Load ExternalAssetsManagerSettings ScriptableObject or creates a default one in Assets/Settings folder.</summary>
        public static void LoadExternalAssetsManagerSettings()
        {
            string[] assetGUIDs = AssetDatabase.FindAssets($"t:{nameof(ExternalAssetsManagerSettings)}");
            if (assetGUIDs != null && assetGUIDs.Length > 0)
            {
                _externalAssetsManagerSettings = AssetDatabase.LoadAssetAtPath<ExternalAssetsManagerSettings>(AssetDatabase.GUIDToAssetPath(assetGUIDs[0]));
            }
            else
            {
                _externalAssetsManagerSettings = AssetDatabase.LoadAssetAtPath(defaultSettingsAssetPath, typeof(ExternalAssetsManagerSettings)) as ExternalAssetsManagerSettings;
                if (_externalAssetsManagerSettings == null)
                {
                    CreateExternalAssetsManagerSettingsAsset();
                }
            }
            if (_externalAssetsManagerSettings == null)
            {
                Debug.LogError($"{nameof(ExternalAssetsManagerSettings)} asset couldn't be loaded or created.");
            }
        }
        private static void CreateExternalAssetsManagerSettingsAsset()
        {
            _externalAssetsManagerSettings = ScriptableObject.CreateInstance<ExternalAssetsManagerSettings>();
            if (!AssetDatabase.IsValidFolder("Assets/Settings")) AssetDatabase.CreateFolder("Assets", "Settings");
            foreach (var iconSet in Resources.FindObjectsOfTypeAll<IconSet>())
            {
                if (iconSet.name.Equals("ExternalAssetsIconSet"))
                {
                    _externalAssetsManagerSettings.iconSet = iconSet;
                }
            }
            AssetDatabase.CreateAsset(_externalAssetsManagerSettings, AssetDatabase.GenerateUniqueAssetPath("Assets/Settings/ExternalAssetsManagerSettings.asset"));
            AssetDatabase.SaveAssets();
            Selection.activeObject = _externalAssetsManagerSettings;
        }

        private static void CheckForAssetModifications()
        {
            foreach (var externalAsset in ExternalAssetsManagerSettings.ExternalAssets)
            {
                if (externalAsset.AutoUpdate || externalAsset.RequestUpdate)
                {
                    bool manualUpdate = externalAsset.RequestUpdate;
                    externalAsset.RequestUpdate = false;
                    externalAsset.RefreshFileInfos();

                    if (externalAsset.Mode == ExternalAssetMode.SourceToAsset && !externalAsset.SourceFileInfo.Exists)
                    {
                        Debug.LogError($"Source file at {externalAsset.SourceFileInfo.FullName} doesn't exist. Source file is mandatory in {externalAsset.Mode} mode.");
                    }
                    else if (externalAsset.Mode == ExternalAssetMode.AssetToSource && !externalAsset.AssetFileInfo.Exists)
                    {
                        Debug.LogError($"Asset file at {externalAsset.AssetFileInfo.FullName} doesn't exist. Asset file is mandatory in {externalAsset.Mode} mode.");
                    }
                    else if (externalAsset.Mode == ExternalAssetMode.TwoWay && !externalAsset.SourceFileInfo.Exists && !externalAsset.AssetFileInfo.Exists)
                    {
                        Debug.LogError($"Both source file at {externalAsset.SourceFileInfo.FullName} and asset files at {externalAsset.AssetFileInfo.FullName} doesn't exist. Source or asset file is mandatory in {externalAsset.Mode} mode.");
                    }
                    else if (externalAsset.Mode == ExternalAssetMode.SourceToAsset || externalAsset.Mode == ExternalAssetMode.TwoWay)
                    {
                        if (externalAsset.AssetFileInfo.Exists)
                        {
                            if (externalAsset.GetAssetState(refresh: false) == ExternalAssetState.OlderThanSource)
                            {
                                if (manualUpdate || !ExternalAssetsManagerSettings.notifyBeforeUpdate || !externalAsset.NotifyBeforeUpdate || EditorUtility.DisplayDialog("External asset modified", $"External asset version is newer than asset at path: {externalAsset.AssetPath}\nShould asset be updated? If you refuse, automatic update will be disabled.", "Yes", "No"))
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
                            if (externalAsset.Mode == ExternalAssetMode.TwoWay)
                            {
                                if (externalAsset.GetAssetState(refresh: false) == ExternalAssetState.NewerThanSource)
                                {
                                    if (manualUpdate || !ExternalAssetsManagerSettings.notifyBeforeUpdate || !externalAsset.NotifyBeforeUpdate || EditorUtility.DisplayDialog("Project asset modified", $"Project asset version is newer than asset at path: {externalAsset.SourceFileInfo}\nShould external asset be updated? If you refuse, automatic update will be disabled.", "Yes", "No"))
                                    {
                                        try
                                        {
                                            externalAsset.SourceFileInfo.Delete();
                                            externalAsset.AssetFileInfo.CopyTo(externalAsset.SourceFileInfo.FullName);
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
                        }
                        else
                        {
                            if (manualUpdate || !ExternalAssetsManagerSettings.notifyBeforeUpdate || !externalAsset.NotifyBeforeUpdate || EditorUtility.DisplayDialog("External asset modified", $"There is no corresponding asset yet.\nShould it be created now? If you refuse, automatic update will be disabled.", "Yes", "No"))
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
                    else if (externalAsset.Mode == ExternalAssetMode.AssetToSource)
                    {
                        if (externalAsset.SourceFileInfo.Exists)
                        {
                            if (externalAsset.GetAssetState(refresh: false) == ExternalAssetState.NewerThanSource)
                            {
                                if (manualUpdate || !ExternalAssetsManagerSettings.notifyBeforeUpdate || !externalAsset.NotifyBeforeUpdate || EditorUtility.DisplayDialog("Project asset modified", $"Project asset version is newer than asset at path: {externalAsset.SourceFileInfo}\nShould external asset be updated? If you refuse, automatic update will be disabled.", "Yes", "No"))
                                {
                                    try
                                    {
                                        externalAsset.SourceFileInfo.Delete();
                                        externalAsset.AssetFileInfo.CopyTo(externalAsset.SourceFileInfo.FullName);
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
                            if (manualUpdate || !ExternalAssetsManagerSettings.notifyBeforeUpdate || !externalAsset.NotifyBeforeUpdate || EditorUtility.DisplayDialog("Project asset modified", $"There is no corresponding external asset yet.\nShould it be created now? If you refuse, automatic update will be disabled.", "Yes", "No"))
                            {
                                try
                                {
                                    externalAsset.AssetFileInfo.CopyTo(externalAsset.SourceFileInfo.FullName);
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
                }
            }
        }
    }
}