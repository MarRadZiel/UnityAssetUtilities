using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "IconSet", menuName = "Icon Set")]
public class IconSet : ScriptableObject, ISerializationCallbackReceiver
{
    [HideInInspector]
    [SerializeField]
    private List<string> keys;
    [HideInInspector]
    [SerializeField]
    private List<Texture> textures;

    private Dictionary<string, Texture> iconSetData;


    public Texture this[string key]
    {
        get => GetTexture(key);
    }

    public void AddTexture(string key, Texture texture)
    {
        if (!string.IsNullOrEmpty(key) && iconSetData != null && !iconSetData.ContainsKey(key))
        {
            iconSetData.Add(key, texture);
        }
    }

    public Texture GetTexture(string key)
    {
        if (!string.IsNullOrEmpty(key) && iconSetData != null && iconSetData.ContainsKey(key))
        {
            return iconSetData[key];
        }
        return null;
    }

    public Dictionary<string, Texture> GetIconSetDataCopy()
    {
        return new Dictionary<string, Texture>(iconSetData);
    }

    public void RemoveTexture(string key)
    {
        if (!string.IsNullOrEmpty(key) && iconSetData != null)
        {
            iconSetData.Remove(key);
        }
    }


    public void OnAfterDeserialize()
    {
        if (keys != null && textures != null)
        {
            if (iconSetData == null) iconSetData = new Dictionary<string, Texture>();
            else iconSetData.Clear();

            for (int i = 0; i < keys.Count && i < textures.Count; ++i)
            {
                iconSetData.Add(keys[i], textures[i]);
            }
        }
    }

    public void OnBeforeSerialize()
    {
        if (iconSetData != null)
        {
            if (keys == null) keys = new List<string>();
            else keys.Clear();
            if (textures == null) textures = new List<Texture>();
            else textures.Clear();

            foreach (var entry in iconSetData)
            {
                keys.Add(entry.Key);
                textures.Add(entry.Value);
            }
        }
    }
}
