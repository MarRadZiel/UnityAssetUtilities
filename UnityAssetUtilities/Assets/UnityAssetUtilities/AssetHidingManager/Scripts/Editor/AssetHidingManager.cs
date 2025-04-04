using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityAssetUtilities
{
    public static class AssetHidingManager
    {
        public const string hiddenAssetPrefix = ".";
        public static readonly int hiddenAssetPrefixLength = hiddenAssetPrefix.Length;

        /// <summary>Sets asset's hiding state.</summary>
        /// <param name="path">Absolute path to asset file.</param>
        /// <param name="hide">True if asset should be hidden. False if asset should be visible.</param>
        /// <param name="refreshAssetDatabase">If true <see cref="AssetDatabase.Refresh()"/> will be called if asset hiding state has changed.</param>
        /// <returns>True if asset was hidden/unhidden, false otherwise.</returns>
        public static bool SetAssetHidden(string path, bool hide, bool refreshAssetDatabase = true)
        {
            bool success = false;
            if (!string.IsNullOrEmpty(path))
            {
                string filename = Path.GetFileName(path);
                bool isFile = Path.HasExtension(path) && !string.IsNullOrEmpty(filename.Replace(Path.GetExtension(filename), string.Empty));
                string metaFilePath = $"{path}{AssetsUtility.metaFileExtension}";
                string targetPath = GetHiddenAssetPath(path, hide);
                string targetMetaFilePath = GetHiddenAssetPath(metaFilePath, hide);

                success = true;

                if (isFile)
                {
                    if (File.Exists(path) && !File.Exists(targetPath))
                    {
                        File.Move(path, targetPath);
                    }
                    else success = false;
                }
                else
                {
                    if (Directory.Exists(path) && !Directory.Exists(targetPath))
                    {
                        Directory.Move(path, targetPath);
                    }
                    else success = false;

                }
                if (File.Exists(metaFilePath) && !File.Exists(targetMetaFilePath))
                {
                    File.Move(metaFilePath, targetMetaFilePath);
                }
                else success = false;

                if (!success)
                {
                    Debug.LogError($"Could not {(hide ? "hide" : "unhide")} asset because of naming problem.\nThis could be caused by already existing file with same name as target {(hide ? "hidden" : "unhidden")} asset. This also applies to assets .meta files.");
                }
            }
            if (success && refreshAssetDatabase)
            {
                AssetDatabase.Refresh();
            }
            return success;
        }

        /// <summary>Returns asset's path adjusted to desired hiding state.</summary>
        /// <param name="assetPath">Asset path to be adjusted.</param>
        /// <param name="hidden">Desired hiding state.</param>
        /// <returns>Adjusted asset path.</returns>
        public static string GetHiddenAssetPath(string assetPath, bool hidden)
        {
            string fileName = Path.GetFileName(assetPath);
            bool isHidden = IsAssetHidden(fileName);

            if (hidden)
            {
                if (!isHidden)
                {
                    return Path.Combine(Path.GetDirectoryName(assetPath), $"{hiddenAssetPrefix}{fileName}");
                }
            }
            else
            {
                if (isHidden)
                {
                    return Path.Combine(Path.GetDirectoryName(assetPath), fileName.Substring(hiddenAssetPrefixLength));
                }
            }
            return assetPath;
        }

        /// <summary>Checks if asset at specified path is hidden.</summary>
        /// <param name="assetPath">Asset path to check.</param>
        /// <returns>True if asset is hidden, false otherwise.</returns>
        public static bool IsAssetAtPathHidden(string assetPath)
        {
            return IsAssetHidden(Path.GetFileName(assetPath));
        }
        /// <summary>Checks if asset with specified file name is hidden.</summary>
        /// <param name="assetFileName">Asset file name to check.</param>
        /// <returns>True if asset is hidden, false otherwise.</returns>
        public static bool IsAssetHidden(string assetFileName)
        {
            return assetFileName.StartsWith(hiddenAssetPrefix);
        }
    }
}