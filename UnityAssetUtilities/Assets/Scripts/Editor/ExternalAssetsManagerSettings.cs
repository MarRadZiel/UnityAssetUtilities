using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Stores information about external file and corresponding assets.</summary>
public class ExternalAssetsManagerSettings : ScriptableObject
{
    [SerializeField]
    [HideInInspector]
    private List<ExternalAsset> externalAssets = new List<ExternalAsset>();

    public int ExternalAssetsCount => externalAssets.Count;
    public IEnumerable<ExternalAsset> ExternalAssets => externalAssets;


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
    private string _externalFilePath;
    public string ExternalFilePath => _externalFilePath;
    [SerializeField]
    private string _assetPath;
    public string AssetPath => _assetPath;

    public ExternalAsset(string externalFilePath, string assetPath)
    {
        this._externalFilePath = externalFilePath;
        this._assetPath = assetPath;
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