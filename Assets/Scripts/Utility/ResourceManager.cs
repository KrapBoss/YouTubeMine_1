using UnityEngine;
using System.Collections.Generic;

namespace MemeLab.Utility
{
    public class ResourceManager
    {
        private static ResourceManager instance;
        public static ResourceManager Instance => instance ??= new ResourceManager();

        private Dictionary<string, Object> resourceCache = new Dictionary<string, Object>();

        public T Load<T>(string path) where T : Object
        {
            if (resourceCache.TryGetValue(path, out Object cachedObj))
            {
                return cachedObj as T;
            }

            T resource = Resources.Load<T>(path);
            if (resource != null)
            {
                resourceCache[path] = resource;
            }
            else
            {
                Debug.LogError($"[ResourceManager] 리소스를 찾을 수 없습니다: {path}");
            }
            
            return resource;
        }

        public void ClearCache()
        {
            resourceCache.Clear();
            Resources.UnloadUnusedAssets();
        }
    }
}
