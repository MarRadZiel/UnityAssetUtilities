using UnityEngine;

public static class AssetsUtility
{
    public static string AbsolutePathToAssetsRelative(string path)
    {
        return $"Assets/{path.Replace(Application.dataPath, "")}";
    }
    public static string AssetsRelativeToAbsolutePath(string path)
    {
        return $"{Application.dataPath}{path.Replace("Assets/", "")}";
    }
}
