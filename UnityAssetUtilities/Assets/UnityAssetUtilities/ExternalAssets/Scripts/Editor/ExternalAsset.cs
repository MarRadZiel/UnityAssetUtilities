using UnityEngine;

namespace UnityAssetUtilities
{
    /// <summary>Describes binding between external file and Unity asset.</summary>
    [System.Serializable]
    public class ExternalAsset
    {
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

        /// <summary>Checks if destination asset is up-to-date with source file.</summary>
        /// <param name="refresh">Should file information be refreshed before checking modification state.</param>
        /// <returns>True if asset is up-to-date.</returns>
        public bool IsAssetUpToDate(bool refresh = true)
        {
            if (refresh) RefreshFileInfos();
            return SourceFileInfo.LastWriteTime <= AssetFileInfo.LastWriteTime;
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
}