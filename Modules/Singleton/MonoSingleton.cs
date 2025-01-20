using UnityEngine;

namespace DCFrame {
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T> {

        protected static T mInstance = null;
        public static T Instance {
            get {
                if (mInstance == null) {
                    mInstance = FindObjectOfType<T>();
                    if (FindObjectsOfType<T>().Length > 1) {
                        Debug.LogWarning("More than 1");
                        return mInstance;
                    }
                    if (mInstance == null) {
                        var instanceName = typeof(T).Name;
                        var instanceObj = GameObject.Find(instanceName);
                        if (!instanceObj) {
                            instanceObj = new GameObject(instanceName);
                        }
                        mInstance = instanceObj.AddComponent<T>();
                        DontDestroyOnLoad(instanceObj);
                    } else {
                        Debug.LogFormat("Already exist: {0}", mInstance.name);
                    }
                }
                return mInstance;
            }
        }
        protected virtual void OnDestroy() {
            mInstance = null;
        }
    }
}