using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Stores information about external file and corresponding assets.</summary>
public class ExternalAssetsManagerSettings : ScriptableObject
{
    [SerializeField]
    [HideInInspector]
    public bool autoSynchronization = true;

    [SerializeField]
    [HideInInspector]
    public bool notifyBeforeUpdate = true;

    [SerializeField]
    public IconSet iconSet;

    [SerializeField]
    [HideInInspector]
    private List<ExternalAsset> externalAssets = new List<ExternalAsset>();

    public int ExternalAssetsCount => externalAssets.Count;
    public IEnumerable<ExternalAsset> ExternalAssets => externalAssets;

    private void OnEnable()
    {
        foreach (var externalAsset in externalAssets)
        {
            externalAsset.RegenerateAbsoluteFilePaths();
        }
    }

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

    public void RegisterExternalAsset(string externalFilePath, string assetPath)
    {
        externalAssets.Add(new ExternalAsset(externalFilePath, assetPath));
    }
    public void UnregisterExternalAsset(ExternalAsset externalAsset)
    {
        externalAssets.Remove(externalAsset);
    }
}

[System.Serializable]
public class ExternalAsset
{
    [SerializeField]
    private bool _autoUpdate;
    public bool AutoUpdate { get => _autoUpdate; set => _autoUpdate = value; }

    [SerializeField]
    private bool _notifyBeforeUpdate;
    public bool NotifyBeforeUpdate { get => _notifyBeforeUpdate; set => _notifyBeforeUpdate = value; }

    [SerializeField]
    private string _externalFilePath;
    private string _externalFileAbsolutePath;
    public string ExternalFilePath => _externalFileAbsolutePath;

    [SerializeField]
    private string _assetPath;
    private string _assetAbsolutePath;
    public string AssetPath => _assetPath;

    public System.IO.FileInfo SourceFileInfo { get; private set; }
    public System.IO.FileInfo AssetFileInfo { get; private set; }


    public ExternalAsset(string externalFilePath, string assetPath)
    {
        _autoUpdate = true;
        _notifyBeforeUpdate = true;
        _externalFileAbsolutePath = externalFilePath;
        _assetPath = assetPath;
        _externalFilePath = AssetsUtility.AbsolutePathToRelative(Application.dataPath, _externalFileAbsolutePath);
        _assetAbsolutePath = AssetsUtility.AssetsPathToAbsolutePath(_assetPath);
    }


    public void RegenerateAbsoluteFilePaths()
    {
        _externalFileAbsolutePath = AssetsUtility.RelativePathToAbsolute(Application.dataPath, _externalFilePath);
        _assetAbsolutePath = AssetsUtility.AssetsPathToAbsolutePath(_assetPath);
    }

    public bool IsAssetUpToDate(bool refresh = true)
    {
        if (refresh) RefreshFileInfos();
        return SourceFileInfo.LastWriteTime <= AssetFileInfo.LastWriteTime;
    }

    public void RefreshFileInfos()
    {
        if (string.IsNullOrEmpty(_externalFileAbsolutePath) || string.IsNullOrEmpty(_assetAbsolutePath)) RegenerateAbsoluteFilePaths();

        if (SourceFileInfo == null) SourceFileInfo = new System.IO.FileInfo(_externalFileAbsolutePath);
        else SourceFileInfo.Refresh();
        if (AssetFileInfo == null) AssetFileInfo = new System.IO.FileInfo(_assetAbsolutePath);
        else AssetFileInfo.Refresh();
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
}