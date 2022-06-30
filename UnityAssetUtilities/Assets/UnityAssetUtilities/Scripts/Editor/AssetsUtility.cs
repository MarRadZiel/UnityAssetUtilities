using UnityEngine;

namespace UnityAssetUtilities
{
    /// <summary>Utility class helping with asset management.</summary>
    public static class AssetsUtility
    {
        /// <summary>Converts path relative to specified basePath to absolute path.</summary>
        /// <param name="basePath">Path to get absolute based on.</param>
        /// <param name="filePath">Path to get absolute of.</param>
        /// <returns>Absolute filepath.</returns>
        public static string RelativePathToAbsolute(string basePath, string filePath)
        {
            return System.IO.Path.GetFullPath($"{basePath}{filePath.Substring(1)}");
        }
        /// <summary>Converts absolute file path to relative to specified basePath. </summary>
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
            absolutePath = absolutePath.Replace('\\', '/').Trim();
            return $"Assets/{absolutePath.Replace(Application.dataPath, string.Empty)}";
        }
        /// <summary>Converts Assets relative path to absolute path.</summary>
        /// <param name="assetsPath">Path that starts with "Assets/"</param>
        /// <returns>Absolute path of specified Assets path.</returns>
        public static string AssetsPathToAbsolutePath(string assetsPath)
        {
            return $"{Application.dataPath}{assetsPath.Replace("Assets/", string.Empty)}";
        }
    }
}