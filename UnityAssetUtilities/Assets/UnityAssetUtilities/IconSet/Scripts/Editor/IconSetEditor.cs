using UnityEditor;
using UnityEngine;

namespace UnityAssetUtilities
{
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
            bool modified = false;

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
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newKey) || newTexture == null || containsKey || !newTexture.isReadable);
            if (GUILayout.Button(new GUIContent("+")))
            {
                Undo.RecordObject(iconSet, "Added icon to set");
                Object ob = Object.Instantiate(newTexture);
                ob.name = ob.name.Replace("(Clone)", string.Empty);
                if (iconSet.HasTexture(ob.name))
                {
                    Object oldTexture = iconSet[ob.name];
                    AssetDatabase.RemoveObjectFromAsset(oldTexture);
                    iconSet.RemoveTexture(ob.name);
                    DestroyImmediate(oldTexture);
                }
                ob.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(ob, iconSet);
                iconSet.AddTexture(ob.name, (Texture)ob);
                EditorUtility.SetDirty(iconSet);
                AssetDatabase.SaveAssetIfDirty(iconSet);
                AssetDatabase.Refresh();

                newKey = null;
                newTexture = null;
                modified = true;
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            if (newTexture != null && !newTexture.isReadable)
            {
                EditorGUILayout.HelpBox("Please set selected Texture asset as readable before adding it to set.", MessageType.Warning);
            }

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
                Undo.RecordObject(iconSet, "Removed icon from set");
                var textureObject = iconSet.GetTexture(toRemove);
                AssetDatabase.RemoveObjectFromAsset(textureObject);
                DestroyImmediate(textureObject);
                iconSet.RemoveTexture(toRemove);
                modified = true;
            }
            if (modified)
            {
                EditorUtility.SetDirty(iconSet);
            }
        }
    }
}