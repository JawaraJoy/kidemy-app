using System.Collections.Generic;
using UnityEngine;

namespace Rush
{
    public class PoolManager : Singleton<PoolManager>
    {
        [SerializeField]
        private Transform m_PoolRoot;

        private readonly Dictionary<PoolableConfig, Queue<GameObject>> m_Pools = new();

        protected override void Awake()
        {
            base.Awake();

            if (m_PoolRoot == null)
            {
                GameObject root = new("[Pool Root]");
                root.transform.SetParent(transform);

                m_PoolRoot = root.transform;
            }
        }

        public void Prewarm(PoolableConfig config)
        {
            if (config == null)
            {
                return;
            }

            Queue<GameObject> pool = GetOrCreatePool(config);

            int count = config.PrewarmCount;

            for (int i = 0; i < count; i++)
            {
                GameObject instance = CreateInstance(config);

                instance.SetActive(false);

                pool.Enqueue(instance);
            }
        }
        private GameObject Get(PoolableConfig config,Transform parent, bool worldPositionStays)
        {
            GameObject instance = Get(config);

            if (parent != null)
            {
                instance.transform.SetParent(parent, worldPositionStays);
            }

            return instance;
        }

        private GameObject Get(PoolableConfig config)
        {
            if (config == null)
            {
                return null;
            }

            Queue<GameObject> pool = GetOrCreatePool(config);

            GameObject instance;

            if (pool.Count > 0)
            {
                instance = pool.Dequeue();
            }
            else
            {
                instance = CreateInstance(config);
            }

            instance.transform.SetParent(null);
            instance.SetActive(true);

            if (instance.TryGetComponent(out IPoolable poolable))
            {
                poolable.OnPoolGet();
            }

            return instance;
        }

        public T Get<T>(PoolableConfig config) where T : Component
        {
            GameObject instance = Get(config);

            return instance.GetComponent<T>();
        }
        public T Get<T>(PoolableConfig config, Transform parent,bool worldPositionStays) where T : Component
        {
            GameObject instance = Get(config, parent, worldPositionStays);

            return instance.GetComponent<T>();
        }

        public void Release(Component component)
        {
            if (component == null)
            {
                return;
            }

            Release(component.gameObject);
        }

        public void Release(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            PoolObject poolObject = instance.GetComponent<PoolObject>();

            if (poolObject == null)
            {
                Destroy(instance);
                return;
            }

            if (instance.TryGetComponent(out IPoolable poolable))
            {
                poolable.OnPoolRelease();
            }

            instance.transform.SetParent(m_PoolRoot);
            instance.SetActive(false);

            Queue<GameObject> pool = GetOrCreatePool(poolObject.SourceConfig);

            pool.Enqueue(instance);
        }

        private Queue<GameObject> GetOrCreatePool(PoolableConfig config)
        {
            if (m_Pools.TryGetValue(config, out Queue<GameObject> pool))
            {
                return pool;
            }

            pool = new Queue<GameObject>();

            m_Pools.Add(config, pool);

            return pool;
        }

        private GameObject CreateInstance(PoolableConfig config)
        {
            GameObject instance = Instantiate(config.Prefab);

            PoolObject poolObject = null;

            if (instance.TryGetComponent(out PoolObject poolable))
            {
                poolObject = poolable;
            }
            else
            {
                poolObject = instance.AddComponent<PoolObject>();
            }

            poolObject.Init(config);

            return instance;
        }
    }
}