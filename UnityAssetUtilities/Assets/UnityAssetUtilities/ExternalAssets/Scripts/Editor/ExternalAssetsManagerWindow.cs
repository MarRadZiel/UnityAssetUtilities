using UnityEngine;
using UnityEditor;

namespace UnityAssetUtilities
{
    public class ExternalAssetsManagerWindow : EditorWindow
    {
        public enum Tab
        {
            Invalid = -1,
            Browser,
            About,

            Count
        }

        [SerializeField]
        private Tab currentTab = Tab.Browser;

        [SerializeField]
        private Vector2 mainScrollPos;

        [SerializeField]
        private Vector2 browseAssetsScrollPos;

        private string newExternalAssetPath;
        private string newExternalAssetTargetPath;

        private Color defaultGUIBackgroundColor;

        private static readonly Vector2 maxButtonSize = new Vector2(EditorGUIUtility.singleLineHeight * 2.1f, EditorGUIUtility.singleLineHeight * 2.1f);

        private const float minCreationButtonsWidth = 150.0f;
        private const float minCreationFieldsWidth = 150.0f;
        private const float minBrowserHeaderWidth = 150.0f;
        private const float minBrowserFieldsWidth = 150.0f;

        private Texture AddExternalAssetIcon => EditorGUIUtility.isProSkin ? ExternalAssetsUpdater.ExternalAssetsManagerSettings.iconSet["externalAssets_add_icon"] : ExternalAssetsUpdater.ExternalAssetsManagerSettings.iconSet["externalAssets_add_icon_dark"];
        private Texture RemoveExternalAssetIcon => EditorGUIUtility.isProSkin ? ExternalAssetsUpdater.ExternalAssetsManagerSettings.iconSet["externalAssets_remove_icon"] : ExternalAssetsUpdater.ExternalAssetsManagerSettings.iconSet["externalAssets_remove_icon_dark"];
        private Texture RefreshExternalAssetIcon => EditorGUIUtility.isProSkin ? ExternalAssetsUpdater.ExternalAssetsManagerSettings.iconSet["externalAssets_refresh_icon"] : ExternalAssetsUpdater.ExternalAssetsManagerSettings.iconSet["externalAssets_refresh_icon_dark"];
        private Texture ToggleExternalAssetNotificationsIcon => EditorGUIUtility.isProSkin ? ExternalAssetsUpdater.ExternalAssetsManagerSettings.iconSet["externalAssets_notifications_icon"] : ExternalAssetsUpdater.ExternalAssetsManagerSettings.iconSet["externalAssets_notifications_icon_dark"];
        private Texture ToggleExternalAssetSynchronizationIcon => EditorGUIUtility.isProSkin ? ExternalAssetsUpdater.ExternalAssetsManagerSettings.iconSet["externalAssets_synchronize_icon"] : ExternalAssetsUpdater.ExternalAssetsManagerSettings.iconSet["externalAssets_synchronize_icon_dark"];



        private void OnEnable()
        {
            defaultGUIBackgroundColor = GUI.backgroundColor;
        }

        private void OnGUI()
        {
            if (ExternalAssetsUpdater.ExternalAssetsManagerSettings == null)
            {
                ExternalAssetsUpdater.CreateExternalAssetsManagerSettingsAsset();
            }
            else
            {
                GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                mainScrollPos = EditorGUILayout.BeginScrollView(mainScrollPos);
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                if (GUILayout.Toggle(currentTab == Tab.Browser, new GUIContent("Manage"), EditorStyles.toolbarButton)) currentTab = Tab.Browser;
                if (GUILayout.Toggle(currentTab == Tab.About, new GUIContent("About"), EditorStyles.toolbarButton)) currentTab = Tab.About;
                GUILayout.FlexibleSpace();
                bool autoSync = GUILayout.Toggle(ExternalAssetsUpdater.ExternalAssetsManagerSettings.autoSynchronization, new GUIContent("Auto synchronization", "Toggles external asset auto synchronization."));
                EditorGUILayout.Space();
                EditorGUI.BeginDisabledGroup(!autoSync);
                bool notifications = GUILayout.Toggle(ExternalAssetsUpdater.ExternalAssetsManagerSettings.notifyBeforeUpdate, new GUIContent("Notifications", "Toggles notifications before asset updating."));
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.Space();
                if (ExternalAssetsUpdater.ExternalAssetsManagerSettings.autoSynchronization != autoSync || ExternalAssetsUpdater.ExternalAssetsManagerSettings.notifyBeforeUpdate != notifications)
                {
                    ExternalAssetsUpdater.ExternalAssetsManagerSettings.autoSynchronization = autoSync;
                    ExternalAssetsUpdater.ExternalAssetsManagerSettings.notifyBeforeUpdate = notifications;
                    EditorUtility.SetDirty(ExternalAssetsUpdater.ExternalAssetsManagerSettings);
                    AssetDatabase.SaveAssetIfDirty(ExternalAssetsUpdater.ExternalAssetsManagerSettings);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                switch (currentTab)
                {
                    case Tab.Browser:
                        {
                            //Adding new External Asset
                            EditorGUILayout.LabelField(new GUIContent("New external asset"), headerStyle);
                            EditorGUILayout.BeginHorizontal();

                            EditorGUILayout.BeginVertical();
                            EditorGUILayout.BeginHorizontal();
                            if (GUILayout.Button(new GUIContent("Set source file"), GUILayout.Width(minCreationButtonsWidth)))
                            {
                                newExternalAssetPath = EditorUtility.OpenFilePanel("Choose external asset source", Application.dataPath, "*");
                            }
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.TextField(newExternalAssetPath, GUILayout.MinWidth(minCreationFieldsWidth));
                            EditorGUI.EndDisabledGroup();
                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();
                            if (GUILayout.Button(new GUIContent("Set destination asset"), GUILayout.Width(minCreationButtonsWidth)))
                            {
                                newExternalAssetTargetPath = EditorUtility.SaveFilePanel("Choose external asset destination", Application.dataPath, System.IO.Path.GetFileName(newExternalAssetPath), "*");
                                if (!string.IsNullOrWhiteSpace(newExternalAssetTargetPath))
                                {
                                    newExternalAssetTargetPath = AssetsUtility.AbsolutePathToAssetsPath(newExternalAssetTargetPath);
                                }
                            }
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.TextField(newExternalAssetTargetPath, GUILayout.MinWidth(minCreationFieldsWidth));
                            EditorGUI.EndDisabledGroup();
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndVertical();

                            bool canCreate = false;
                            if (string.IsNullOrWhiteSpace(newExternalAssetPath) || string.IsNullOrWhiteSpace(newExternalAssetTargetPath))
                            {
                                EditorGUILayout.HelpBox("Both source file and destination asset paths must be set.", MessageType.Info);
                            }
                            else if (!System.IO.File.Exists(newExternalAssetPath))
                            {
                                EditorGUILayout.HelpBox("Specified source file doesn't exist!\nChoose a new one.", MessageType.Error);
                            }
                            else
                            {
                                bool targetAssetAvailable = !ExternalAssetsUpdater.ExternalAssetsManagerSettings.ContainsAsset(newExternalAssetTargetPath);
                                if (!targetAssetAvailable)
                                {
                                    EditorGUILayout.HelpBox("Asset already used!\nChoose a new one or remove the other entry.", MessageType.Error);
                                }
                                else
                                {
                                    canCreate = true;
                                }
                            }
                            EditorGUI.BeginDisabledGroup(!canCreate);
                            if (GUILayout.Button(new GUIContent(AddExternalAssetIcon, "Add this external asset."), GUILayout.MaxWidth(maxButtonSize.x), GUILayout.MaxHeight(maxButtonSize.y)))
                            {
                                ExternalAssetsUpdater.ExternalAssetsManagerSettings.RegisterExternalAsset(newExternalAssetPath, newExternalAssetTargetPath);
                                EditorUtility.SetDirty(ExternalAssetsUpdater.ExternalAssetsManagerSettings);
                                AssetDatabase.SaveAssetIfDirty(ExternalAssetsUpdater.ExternalAssetsManagerSettings);
                                newExternalAssetPath = null;
                                newExternalAssetTargetPath = null;
                            }
                            EditorGUI.EndDisabledGroup();
                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

                            //External Asset Browser
                            if (ExternalAssetsUpdater.ExternalAssetsManagerSettings.ExternalAssetsCount > 0)
                            {
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField(new GUIContent("Asset"), headerStyle, GUILayout.MinWidth(minBrowserHeaderWidth));
                                EditorGUILayout.LabelField(new GUIContent("Source File"), headerStyle, GUILayout.MinWidth(minBrowserHeaderWidth));
                                EditorGUILayout.LabelField(new GUIContent("Actions"), headerStyle, GUILayout.Width(4 * (maxButtonSize.x + 2.5f)));
                                EditorGUILayout.EndHorizontal();

                                ExternalAsset toRemove = null;
                                browseAssetsScrollPos = EditorGUILayout.BeginScrollView(browseAssetsScrollPos);
                                foreach (ExternalAsset externalAsset in ExternalAssetsUpdater.ExternalAssetsManagerSettings.ExternalAssets)
                                {
                                    externalAsset.RefreshFileInfos();
                                    EditorGUILayout.BeginHorizontal();
                                    if (externalAsset.SourceFileInfo.Exists)
                                    {
                                        bool isAssetUpToDate = externalAsset.IsAssetUpToDate();
                                        EditorGUI.BeginDisabledGroup(true);
                                        EditorGUILayout.BeginVertical();

                                        EditorGUILayout.BeginHorizontal();
                                        EditorGUILayout.ObjectField(externalAsset.AssetFileInfo.Exists ? AssetDatabase.LoadAssetAtPath(externalAsset.AssetPath, typeof(Object)) : null, typeof(Object), allowSceneObjects: false, GUILayout.MinWidth(minBrowserFieldsWidth));
                                        EditorGUILayout.TextField(externalAsset.ExternalFilePath, GUILayout.MinWidth(minBrowserFieldsWidth));
                                        EditorGUILayout.EndHorizontal();

                                        EditorGUILayout.BeginHorizontal();
                                        Color defaultGUIColor = GUI.color;
                                        GUI.color = isAssetUpToDate ? Color.green : Color.red;
                                        EditorGUILayout.TextField(externalAsset.AssetFileInfo.Exists ? externalAsset.AssetFileInfo.LastWriteTime.ToString() : "No asset file", GUILayout.MinWidth(minBrowserFieldsWidth));
                                        EditorGUILayout.TextField(externalAsset.SourceFileInfo.Exists ? externalAsset.SourceFileInfo.LastWriteTime.ToString() : "No external asset file", GUILayout.MinWidth(minBrowserFieldsWidth));
                                        GUI.color = defaultGUIColor;
                                        EditorGUILayout.EndHorizontal();

                                        EditorGUILayout.EndVertical();
                                        EditorGUI.EndDisabledGroup();

                                        EditorGUI.BeginDisabledGroup(isAssetUpToDate);
                                        if (GUILayout.Button(new GUIContent(RefreshExternalAssetIcon, $"Update this external asset.{(isAssetUpToDate ? "\nAsset is already up-to-date." : string.Empty)}"), GUILayout.MaxWidth(maxButtonSize.x), GUILayout.MaxHeight(maxButtonSize.y)))
                                        {
                                            string absolutePath = AssetsUtility.AssetsPathToAbsolutePath(externalAsset.AssetPath);
                                            try
                                            {
                                                if (System.IO.File.Exists(absolutePath))
                                                {
                                                    System.IO.File.Delete(absolutePath);
                                                }
                                                System.IO.File.Copy(externalAsset.ExternalFilePath, absolutePath);
                                                AssetDatabase.Refresh();
                                            }
                                            catch (System.Exception e)
                                            {
                                                Debug.LogError($"Error during external asset update.\n{e}");
                                            }
                                        }
                                        EditorGUI.EndDisabledGroup();
                                        EditorGUI.BeginDisabledGroup(!ExternalAssetsUpdater.ExternalAssetsManagerSettings.autoSynchronization);
                                        externalAsset.AutoUpdate = GUILayout.Toggle(externalAsset.AutoUpdate, new GUIContent(ToggleExternalAssetSynchronizationIcon, $"Toggle auto-synchronization for this external asset.{(ExternalAssetsUpdater.ExternalAssetsManagerSettings.autoSynchronization ? string.Empty : " Auto synchronization must be enabled globally first.")}"), GUI.skin.button, GUILayout.MaxWidth(maxButtonSize.x), GUILayout.MaxHeight(maxButtonSize.y));
                                        EditorGUI.EndDisabledGroup();
                                        EditorGUI.BeginDisabledGroup(!ExternalAssetsUpdater.ExternalAssetsManagerSettings.autoSynchronization || !ExternalAssetsUpdater.ExternalAssetsManagerSettings.notifyBeforeUpdate);
                                        externalAsset.NotifyBeforeUpdate = GUILayout.Toggle(externalAsset.NotifyBeforeUpdate, new GUIContent(ToggleExternalAssetNotificationsIcon, $"Toggle notification before this external asset update.{(ExternalAssetsUpdater.ExternalAssetsManagerSettings.notifyBeforeUpdate ? string.Empty : " Auto synchronization and notifications must be enabled globally first.")}"), GUI.skin.button, GUILayout.MaxWidth(maxButtonSize.x), GUILayout.MaxHeight(maxButtonSize.y));
                                        EditorGUI.EndDisabledGroup();
                                        if (GUILayout.Button(new GUIContent(RemoveExternalAssetIcon, "Remove this external asset."), GUILayout.MaxWidth(maxButtonSize.x), GUILayout.MaxHeight(maxButtonSize.y)))
                                        {
                                            if (EditorUtility.DisplayDialog("Removing external asset", "Are you sure to delete this external asset binding?\nBoth external file and asset won't be deleted.", "Yes", "No"))
                                            {
                                                toRemove = externalAsset;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        GUI.backgroundColor = Color.red;
                                        EditorGUILayout.HelpBox($"File doesn't exist!\n{externalAsset.ExternalFilePath}", MessageType.Error);
                                        if (GUILayout.Button(new GUIContent(RemoveExternalAssetIcon, "Remove this external asset."), GUILayout.Width(25f), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                                        {
                                            if (EditorUtility.DisplayDialog("Removing external asset", "Are you sure to delete this external asset binding?\nAsset won't be deleted.", "Yes", "No"))
                                            {
                                                toRemove = externalAsset;
                                            }
                                        }
                                        GUI.backgroundColor = defaultGUIBackgroundColor;
                                    }
                                    EditorGUILayout.EndHorizontal();
                                    EditorGUILayout.Space();
                                }
                                EditorGUILayout.EndScrollView();

                                if (toRemove != null)
                                {
                                    ExternalAssetsUpdater.ExternalAssetsManagerSettings.UnregisterExternalAsset(toRemove);
                                    EditorUtility.SetDirty(ExternalAssetsUpdater.ExternalAssetsManagerSettings);
                                    AssetDatabase.SaveAssetIfDirty(ExternalAssetsUpdater.ExternalAssetsManagerSettings);
                                }
                            }
                            else
                            {
                                EditorGUILayout.HelpBox($"There are no entries yet. Add new external asset via \"New external asset\" section.", MessageType.Info);
                            }
                        }
                        break;
                    case Tab.About:
                        {
                            EditorGUILayout.HelpBox($"This is a simple tool allowing to bind project's assets with external files (outside the Assets folder). Changes in external file are automatically propagated to destination asset.", MessageType.Info);
                        }
                        break;
                    default:
                        {
                            currentTab = Tab.About;
                        }
                        break;
                }

                EditorGUILayout.EndScrollView();
            }
        }




        [MenuItem("Tools/External Assets Manager")]
        private static void ShowWindow()
        {
            var window = GetWindow<ExternalAssetsManagerWindow>();
            window.titleContent = new GUIContent("External Assets Manager", EditorGUIUtility.isProSkin ? ExternalAssetsUpdater.ExternalAssetsManagerSettings.iconSet["externalAssets_icon"] : ExternalAssetsUpdater.ExternalAssetsManagerSettings.iconSet["externalAssets_icon_dark"]);
            window.Show();
        }
    }
}