using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityAssetUtilities
{
    /// <summary>Utility class helping with asset management.</summary>
    public static class AssetsUtility
    {
        public const string assetsFolderName = "Assets";
        public const string metaFileExtension = ".meta";

        /// <summary><see cref="Application.dataPath"/> with directory separators unified to the platform-speciffic ones.</summary>
        public static readonly string unifiedApplicationDataPath = UnifyDirectorySeparators(Application.dataPath);


        /// <summary>Converts path relative to specified basePath to absolute path.</summary>
        /// <param name="basePath">Path to get absolute based on.</param>
        /// <param name="filePath">Path to get absolute of.</param>
        /// <returns>Absolute filepath.</returns>
        public static string RelativePathToAbsolute(string basePath, string filePath)
        {
            return System.IO.Path.GetFullPath($"{basePath}{filePath.Substring(1)}");
        }
        /// <summary>Converts absolute file path to relative to specified basePath.</summary>
        /// <param name="basePath">Path to get relative based on.</param>
        /// <param name="filePath">Path to get relative of.</param>
        /// <returns>filepath relative to basePath.</returns>
        public static string AbsolutePathToRelative(string basePath, string filePath)
        {
            basePath = basePath.Replace('\\', '/').Trim();
            filePath = filePath.Replace('\\', '/').Trim();
            string relativePath = ".";
            if (filePath.Contains(basePath)) return filePath.Replace(basePath.TrimEnd('/'), relativePath);
            relativePath += "/..";
            string lastRelativePath;
            do
            {
                lastRelativePath = relativePath;
                basePath = System.IO.Path.GetDirectoryName(basePath).Replace('\\', '/').Trim();
                if (!filePath.Contains(basePath))
                {
                    relativePath += "/..";
                }
                else
                {
                    return filePath.Replace(basePath.TrimEnd('/'), relativePath);
                }
            }
            while (!lastRelativePath.Equals(relativePath));
            return filePath;
        }

        /// <summary>Converts absolute path to Assets path (starting with "Assets/").</summary>
        /// <param name="absolutePath">Path to get Assets path of.</param>
        /// <returns>Path as Assets path.</returns>
        public static string AbsolutePathToAssetsPath(string absolutePath)
        {
            return System.IO.Path.Combine(assetsFolderName, UnifyDirectorySeparators(absolutePath).Replace(unifiedApplicationDataPath, string.Empty).TrimStart(System.IO.Path.DirectorySeparatorChar));
        }
        /// <summary>Converts Assets relative path to absolute path.</summary>
        /// <param name="assetsPath">Path that starts with "Assets/"</param>
        /// <returns>Absolute path of specified Assets path.</returns>
        public static string AssetsPathToAbsolutePath(string assetsPath)
        {
            return System.IO.Path.Combine(unifiedApplicationDataPath, UnifyDirectorySeparators(assetsPath).Substring(assetsFolderName.Length).TrimStart(System.IO.Path.DirectorySeparatorChar));
        }

        /// <summary>Returns path to meta file of specified asset.</summary>
        /// <param name="assetPath">Path to the asset.</param>
        /// <returns>Path to asset's meta file.</returns>
        public static string GetMetaFilePath(string assetPath)
        {
            return $"{assetPath}{metaFileExtension}";
        }

        /// <summary>Unifies path's diretory separators to platform-speciffic ones.</summary>
        /// <param name="path">Path to unify separators for.</param>
        /// <returns>Path with unified directory separators.</returns>
        public static string UnifyDirectorySeparators(string path)
        {
            return path.Replace('/', System.IO.Path.DirectorySeparatorChar).Replace('\\', System.IO.Path.DirectorySeparatorChar);
        }

        /// <summary>Returns the first asset object of type type at given GUID.</summary>
        /// <typeparam name="T">Data type of the asset.</typeparam>
        /// <param name="guid">GUID of the asset to load.</param>
        /// <returns>The asset matching the parameters.</returns>
        public static T LoadAssetAtGUID<T>(string guid) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
        }
    }
}