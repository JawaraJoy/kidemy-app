using UnityEngine;

namespace Rush
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T s_Instance;

        public static T Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindFirstObjectByType<T>();

                    if (s_Instance == null)
                    {
                        GameObject go = new(typeof(T).Name);
                        s_Instance = go.AddComponent<T>();
                    }
                }

                return s_Instance;
            }
        }

        protected virtual void Awake()
        {
            if (s_Instance == null)
            {
                s_Instance = this as T;
                return;
            }

            if (s_Instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
}