using System.Collections.Generic;
using UnityEngine;

namespace UnityAssetUtilities
{
    /// <summary>Stores Texture assets that can be retrieved by their key string.</summary>
    [CreateAssetMenu(fileName = "IconSet", menuName = "Icon Set", order = 100)]
    public class IconSet : ScriptableObject, ISerializationCallbackReceiver
    {
        [HideInInspector]
        [SerializeField]
        private List<string> keys;
        [HideInInspector]
        [SerializeField]
        private List<Texture> textures;

        private Dictionary<string, Texture> iconSetData = new Dictionary<string, Texture>();

        /// <summary>Gets texture by its key.</summary>
        /// <param name="key">Key string of texture to get.</param>
        /// <returns>Texture if exists for specified key. Null otherwise.</returns>
        public Texture this[string key]
        {
            get => GetTexture(key);
        }

        /// <summary>Adds new texture to set.</summary>
        /// <param name="key">Key string to store texture at.</param>
        /// <param name="texture">Texture to add.</param>
        public void AddTexture(string key, Texture texture)
        {
            if (!string.IsNullOrEmpty(key) && iconSetData != null && !iconSetData.ContainsKey(key))
            {
                iconSetData.Add(key, texture);
            }
        }

        /// <summary>Checks if any texture is store at specified key.</summary>
        /// <param name="key">Key string of texture to check.</param>
        /// <returns>True if texture exists for specified key.</returns>
        public bool HasTexture(string key)
        {
            if (!string.IsNullOrEmpty(key) && iconSetData != null && iconSetData.ContainsKey(key))
            {
                return true;
            }
            return false;
        }

        /// <summary>Gets texture by its key.</summary>
        /// <param name="key">Key string of texture to get.</param>
        /// <returns>Texture if exists for specified key. Null otherwise.</returns>
        public Texture GetTexture(string key)
        {
            if (!string.IsNullOrEmpty(key) && iconSetData != null && iconSetData.ContainsKey(key))
            {
                return iconSetData[key];
            }
            return null;
        }

        /// <summary>Gets copy of the whole set data.</summary>
        /// <returns>Copy of stored data.</returns>
        public Dictionary<string, Texture> GetIconSetDataCopy()
        {
            return new Dictionary<string, Texture>(iconSetData);
        }


        /// <summary>Removes texture by its key.</summary>
        /// <param name="key">Key string of texture to remove.</param>
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
}