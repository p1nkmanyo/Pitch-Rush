using System.Collections.Generic;
using UnityEngine;

namespace PitchRush
{
    public class PooledObject : MonoBehaviour
    {
        public int poolId;
    }

    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance { get; private set; }

        private Dictionary<int, Queue<GameObject>> poolDictionary = new Dictionary<int, Queue<GameObject>>();
        private Dictionary<int, GameObject> prefabMap = new Dictionary<int, GameObject>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            int key = prefab.GetInstanceID();

            // Create pool if it doesn't exist
            if (!poolDictionary.ContainsKey(key))
            {
                poolDictionary.Add(key, new Queue<GameObject>());
                prefabMap.Add(key, prefab);
            }

            GameObject obj = null;

            // Get from pool or instantiate if empty
            if (poolDictionary[key].Count > 0)
            {
                obj = poolDictionary[key].Dequeue();
                
                // Safety check in case object was destroyed externally
                while (obj == null && poolDictionary[key].Count > 0)
                {
                    obj = poolDictionary[key].Dequeue();
                }
            }

            if (obj == null)
            {
                obj = Instantiate(prefab);
                PooledObject pooledComponent = obj.AddComponent<PooledObject>();
                pooledComponent.poolId = key;
            }

            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            return obj;
        }

        public void Despawn(GameObject obj)
        {
            if (obj == null) return;

            PooledObject pooledComponent = obj.GetComponent<PooledObject>();
            if (pooledComponent == null)
            {
                // Fallback: if it's not a pooled object, destroy it normally
                Destroy(obj);
                return;
            }

            int key = pooledComponent.poolId;
            obj.SetActive(false);

            if (poolDictionary.ContainsKey(key))
            {
                poolDictionary[key].Enqueue(obj);
            }
            else
            {
                // Pool was destroyed or not tracked
                Destroy(obj);
            }
        }
    }
}
