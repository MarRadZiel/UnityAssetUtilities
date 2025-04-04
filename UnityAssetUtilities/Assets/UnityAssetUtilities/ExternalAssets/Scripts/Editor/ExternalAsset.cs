using UnityEditor;
using UnityEngine;

namespace UnityAssetUtilities
{
    /// <summary>Describes binding between external file and Unity asset.</summary>
    [System.Serializable]
    public class ExternalAsset
    {
        [SerializeField]
        private ExternalAssetMode _mode;
        /// <summary>Mode of this external asset.</summary>
        public ExternalAssetMode Mode { get => _mode; set => _mode = value; }

        [SerializeField]
        private bool _autoUpdate;
        /// <summary>Should this asset be automatically updated.</summary>
        public bool AutoUpdate { get => _autoUpdate; set => _autoUpdate = value; }

        [SerializeField]
        private bool _notifyBeforeUpdate;
        /// <summary>Should notification be displayed when modification is detected for this asset.</summary>
        public bool NotifyBeforeUpdate { get => _notifyBeforeUpdate; set => _notifyBeforeUpdate = value; }

        [SerializeField]
        private string _externalFilePath;
        private string _externalFileAbsolutePath;
        /// <summary>Absolute path of an external file.</summary>
        public string ExternalFilePath => _externalFileAbsolutePath;

        [SerializeField]
        private string _assetPath;
        private string _assetAbsolutePath;
        /// <summary>Assets-relative path of an asset.</summary>
        public string AssetPath => _assetPath;

        private bool _requestUpdate;
        /// <summary>Should this asset be manually updated.</summary>
        public bool RequestUpdate { get => _requestUpdate; set => _requestUpdate = value; }

        /// <summary>Source file FileInfo instance.</summary>
        public System.IO.FileInfo SourceFileInfo { get; private set; }
        /// <summary>Asset file FileInfo instance.</summary>
        public System.IO.FileInfo AssetFileInfo { get; private set; }


        /// <summary>Creates new External Asset.</summary>
        /// <param name="externalFilePath">Source file path of this ExternalAsset. Should be absolute.</param>
        /// <param name="assetPath">Destination asset path of this ExternalAsset. Should be Assets-relative.</param>
        public ExternalAsset(string externalFilePath, string assetPath)
        {
            _autoUpdate = true;
            _notifyBeforeUpdate = true;
            _externalFileAbsolutePath = externalFilePath;
            _assetPath = assetPath;
            _externalFilePath = AssetsUtility.AbsolutePathToRelative(Application.dataPath, _externalFileAbsolutePath);
            _assetAbsolutePath = AssetsUtility.AssetsPathToAbsolutePath(_assetPath);
        }

        /// <summary>Recreates absolute path to source file and destination asset.</summary>
        public void RegenerateAbsoluteFilePaths()
        {
            _externalFileAbsolutePath = AssetsUtility.RelativePathToAbsolute(Application.dataPath, _externalFilePath);
            _assetAbsolutePath = AssetsUtility.AssetsPathToAbsolutePath(_assetPath);
        }

        /// <summary>Returns this ExternalAssets state.</summary>
        /// <param name="refresh">Should file information be refreshed before checking modification state.</param>
        /// <returns>State of this ExternalAsset.</returns>
        public ExternalAssetState GetAssetState(bool refresh = true)
        {
            if (refresh) RefreshFileInfos();
            int comparison = SourceFileInfo.LastWriteTime.CompareTo(AssetFileInfo.LastWriteTime);
            if (comparison == 0) return ExternalAssetState.SameAsSource;
            else if (comparison < 0) return ExternalAssetState.NewerThanSource;
            else return ExternalAssetState.OlderThanSource;
        }
        
        /// <summary>Refreshes file information stored in source file and destination asset FileInfo instances.</summary>
        public void RefreshFileInfos()
        {
            if (string.IsNullOrEmpty(_externalFileAbsolutePath) || string.IsNullOrEmpty(_assetAbsolutePath)) RegenerateAbsoluteFilePaths();

            if (SourceFileInfo == null) SourceFileInfo = new System.IO.FileInfo(_externalFileAbsolutePath);
            else SourceFileInfo.Refresh();
            if (AssetFileInfo == null) AssetFileInfo = new System.IO.FileInfo(_assetAbsolutePath);
            else AssetFileInfo.Refresh();
        }
    }

    public enum ExternalAssetState
    {
        OlderThanSource,
        SameAsSource,
        NewerThanSource,
    }
    public enum ExternalAssetMode
    {
        /// <summary>Asset is updated based on source file.</summary>
        SourceToAsset,
        /// <summary>Source file is updated based on Asset.</summary>
        AssetToSource,
        /// <summary>Asset is updated based on source file and source file is updated based on Asset</summary>
        TwoWay,
    }
}