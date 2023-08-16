using System.Collections.Generic;
using UnityEngine;

namespace CoreLib
{
    public class VirtualAssetBundle
    {
        private Dictionary<string, Object> loadedObjects = new Dictionary<string, Object>();

        public void Register(Object obj)
        {
            if (!loadedObjects.ContainsKey(obj.name))
            {
                loadedObjects.Add(obj.name, obj);
            }
        }

        public T Get<T>(string name)
            where T : Object
        {
            if (loadedObjects.ContainsKey(name))
            {
                return (T)loadedObjects[name];
            }

            return null;
        }
        
        public bool TryGet<T>(string name, out T res)
        where T : Object
        {
            if (loadedObjects.ContainsKey(name))
            {
                res = (T)loadedObjects[name];
                return true;
            }

            res = null;
            return false;
        }
    }
}