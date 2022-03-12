using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ExternalAssetsManagerWindow : EditorWindow
{
    public enum Tab
    {
        Invalid = -1,
        NewEntry,
        Browse,
        About,

        Count
    }

    [SerializeField]
    private Tab currentTab = Tab.About;

    [SerializeField]
    private Vector2 browseAssetsScrollPos;

    private string newExternalAssetPath;
    private string newExternalAssetTargetPath;

    private Color defaultGUIBackgroundColor;



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
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Toggle(currentTab == Tab.NewEntry, new GUIContent("New"), EditorStyles.toolbarButton)) currentTab = Tab.NewEntry;
            if (GUILayout.Toggle(currentTab == Tab.Browse, new GUIContent("Browse"), EditorStyles.toolbarButton)) currentTab = Tab.Browse;
            if (GUILayout.Toggle(currentTab == Tab.About, new GUIContent("About"), EditorStyles.toolbarButton)) currentTab = Tab.About;
            GUILayout.FlexibleSpace();
            bool autoSync = GUILayout.Toggle(ExternalAssetsUpdater.ExternalAssetsManagerSettings.autoSynchronization, new GUIContent("Auto synchronization", "Toggles external asset auto synchronization"));
            if (ExternalAssetsUpdater.ExternalAssetsManagerSettings.autoSynchronization != autoSync)
            {
                ExternalAssetsUpdater.ExternalAssetsManagerSettings.autoSynchronization = autoSync;
                EditorUtility.SetDirty(ExternalAssetsUpdater.ExternalAssetsManagerSettings);
                AssetDatabase.SaveAssetIfDirty(ExternalAssetsUpdater.ExternalAssetsManagerSettings);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            switch (currentTab)
            {
                case Tab.NewEntry:
                    {
                        EditorGUILayout.BeginHorizontal();

                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button(new GUIContent("SET SOURCE FILE"), GUILayout.MaxWidth(200.0f)))
                        {
                            newExternalAssetPath = EditorUtility.OpenFilePanel("Choose external asset source", Application.dataPath, "*");
                        }
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.TextField(newExternalAssetPath);
                        EditorGUI.EndDisabledGroup();
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button(new GUIContent("SET DESTINATION ASSET"), GUILayout.MaxWidth(200.0f)))
                        {
                            newExternalAssetTargetPath = EditorUtility.SaveFilePanel("Choose external asset target", Application.dataPath, System.IO.Path.GetFileName(newExternalAssetPath), "*");
                            newExternalAssetTargetPath = AssetsUtility.AbsolutePathToAssetsPath(newExternalAssetTargetPath);
                        }
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.TextField(newExternalAssetTargetPath);
                        EditorGUI.EndDisabledGroup();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();

                        bool targetAssetAvailable = !ExternalAssetsUpdater.ExternalAssetsManagerSettings.ContainsAsset(newExternalAssetTargetPath);
                        if (targetAssetAvailable)
                        {
                            if (GUILayout.Button(new GUIContent("ADD")))
                            {
                                ExternalAssetsUpdater.ExternalAssetsManagerSettings.RegisterExternalAsset(newExternalAssetPath, newExternalAssetTargetPath);
                                EditorUtility.SetDirty(ExternalAssetsUpdater.ExternalAssetsManagerSettings);
                                AssetDatabase.SaveAssetIfDirty(ExternalAssetsUpdater.ExternalAssetsManagerSettings);
                                newExternalAssetPath = null;
                                newExternalAssetTargetPath = null;
                            }
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("Asset already used!\nChoose a new one or remove the other entry.", MessageType.Error);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    break;
                case Tab.Browse:
                    {
                        if (ExternalAssetsUpdater.ExternalAssetsManagerSettings.ExternalAssetsCount > 0)
                        {
                            ExternalAsset toRemove = null;
                            browseAssetsScrollPos = EditorGUILayout.BeginScrollView(browseAssetsScrollPos);
                            foreach (ExternalAsset externalAsset in ExternalAssetsUpdater.ExternalAssetsManagerSettings.ExternalAssets)
                            {
                                externalAsset.RefreshFileInfos();
                                EditorGUILayout.BeginHorizontal();
                                if (externalAsset.SourceFileInfo.Exists)
                                {
                                    EditorGUI.BeginDisabledGroup(true);
                                    EditorGUILayout.BeginVertical();

                                    EditorGUILayout.BeginHorizontal();
                                    EditorGUILayout.ObjectField(externalAsset.AssetFileInfo.Exists ? AssetDatabase.LoadAssetAtPath(externalAsset.AssetPath, typeof(Object)) : null, typeof(Object), allowSceneObjects: false);
                                    EditorGUILayout.TextField(externalAsset.ExternalFilePath);
                                    EditorGUILayout.EndHorizontal();

                                    EditorGUILayout.BeginHorizontal();
                                    Color defaultGUIColor = GUI.color;
                                    GUI.color = externalAsset.IsAssetUpToDate() ? Color.green : Color.red;
                                    EditorGUILayout.TextField(externalAsset.AssetFileInfo.LastWriteTime.ToString());
                                    EditorGUILayout.TextField(externalAsset.SourceFileInfo.LastWriteTime.ToString());
                                    GUI.color = defaultGUIColor;
                                    EditorGUILayout.EndHorizontal();

                                    EditorGUILayout.EndVertical();
                                    EditorGUI.EndDisabledGroup();

                                    if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("TreeEditor.Trash").image, "Remove external asset"), GUILayout.MaxWidth(25f), GUILayout.Height(EditorGUIUtility.singleLineHeight * 2)))
                                    {
                                        if (EditorUtility.DisplayDialog("Removing external asset", "Are you sure to delete this external asset binding?\nBoth external file and asset won't be deleted.", "Yes", "No"))
                                        {
                                            toRemove = externalAsset;
                                        }
                                    }
                                    if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("TreeEditor.Refresh").image, "Update external asset"), GUILayout.MaxWidth(25f), GUILayout.Height(EditorGUIUtility.singleLineHeight * 2)))
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
                                    EditorGUI.BeginDisabledGroup(!ExternalAssetsUpdater.ExternalAssetsManagerSettings.autoSynchronization);
                                    externalAsset.AutoUpdate = GUILayout.Toggle(externalAsset.AutoUpdate, new GUIContent("Auto", $"Toggles this external asset auto-synchronization.{(ExternalAssetsUpdater.ExternalAssetsManagerSettings.autoSynchronization ? string.Empty : " Auto synchronization must be enabled globally first.")}"), GUI.skin.button, GUILayout.MaxWidth(50f), GUILayout.Height(EditorGUIUtility.singleLineHeight * 2));
                                    EditorGUI.EndDisabledGroup();
                                }
                                else
                                {
                                    GUI.backgroundColor = Color.red;
                                    EditorGUILayout.HelpBox($"File doesn't exist!\n{externalAsset.ExternalFilePath}", MessageType.Error);
                                    if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("TreeEditor.Trash").image, "Remove external asset"), GUILayout.Width(25f), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
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
                            EditorGUILayout.HelpBox($"There is no entries yet. Add new external asset via \"New entry\" tab.", MessageType.Info);
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
        }
    }




    [MenuItem("Tools/External Assets Manager")]
    private static void ShowWindow()
    {
        var window = GetWindow<ExternalAssetsManagerWindow>();
        window.titleContent = new GUIContent("External Assets Manager", EditorGUIUtility.IconContent("d_DefaultAsset Icon").image);
        window.Show();
    }
}