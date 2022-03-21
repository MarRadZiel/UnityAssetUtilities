using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(IconSet))]
public class IconSetEditor : Editor
{
    private IconSet iconSet;

    private string newKey;
    private Texture newTexture;

    private void OnEnable()
    {
        iconSet = target as IconSet;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.BeginHorizontal();
        newKey = EditorGUILayout.TextField(newKey);
        var texture = EditorGUILayout.ObjectField(newTexture, typeof(Texture), allowSceneObjects: false) as Texture;
        if (texture != newTexture)
        {
            newTexture = texture;
            if (texture != null && string.IsNullOrEmpty(newKey))
            {
                newKey = texture.name;
            }
        }
        bool containsKey = iconSet.GetTexture(newKey) != null;
        if (containsKey)
        {
            EditorGUILayout.HelpBox("Key already exist.", MessageType.Warning);
        }
        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newKey) || newTexture == null || containsKey);
        if (GUILayout.Button(new GUIContent("+")))
        {
            iconSet.AddTexture(newKey, newTexture);
            newKey = null;
            newTexture = null;
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        var data = iconSet.GetIconSetDataCopy();
        string toRemove = null;
        foreach (var entry in data)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(entry.Key);
            EditorGUILayout.ObjectField(entry.Value, typeof(Texture2D), allowSceneObjects: false);
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button(new GUIContent("-")))
            {
                toRemove = entry.Key;
            }
            EditorGUILayout.EndHorizontal();
        }
        if (!string.IsNullOrEmpty(toRemove))
        {
            iconSet.RemoveTexture(toRemove);
        }
    }
}
