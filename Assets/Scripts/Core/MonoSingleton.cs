using System.Reflection;
using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static bool _shuttingDown = false;
    private static object _lock = new object();
    private static GameObject _singletonRoot = null;
    private static T _instance = null;
    public static T Instance
    {
        get
        {
            if (_shuttingDown)
                return null;

            lock(_lock)
            {
                if(_instance == null)
                {
                    _instance = FindFirstObjectByType<T>();

                    if (_instance == null)
                    {
                        if (_singletonRoot == null)
                        {
                            var singletonRoot = GameObject.Find("ManagerRoot") ?? new GameObject();
                            singletonRoot.name = "ManagerRoot";
                            DontDestroyOnLoad(singletonRoot);
                            _singletonRoot = singletonRoot;
                        }

                        var path = typeof(T).GetCustomAttribute<AssetPathAttribute>()?.Path;

                        if (!string.IsNullOrEmpty(path))
                        {
                            var prefab = Resources.Load<T>(path);

                            if (prefab == null)
                                return null;

                            _instance = Instantiate(prefab, _singletonRoot.transform);

                            if (_instance == null)
                            {
                                var singletonObject = new GameObject();
                                _instance = singletonObject.AddComponent<T>();
                                singletonObject.name = typeof(T).ToString();
                                singletonObject.transform.parent = _singletonRoot.transform;
                            }
                            else
                                _instance.name = prefab.name;

                            //DontDestroyOnLoad(_instance.gameObject);
                        }
                        else
                        {
                            var singletonObject = new GameObject();
                            _instance = singletonObject.AddComponent<T>();
                            singletonObject.name = typeof(T).ToString();
                            singletonObject.transform.parent = _singletonRoot.transform;

                            //DontDestroyOnLoad(singletonObject);
                        }
                    }
                }

                return _instance;
            }
        }
    }

    public static bool HasInstance
    {
        get
        {
            return _instance != null;
        }
    }

    private void OnApplicationQuit()
    {
        _shuttingDown = true;
    }

    private void OnDestroy()
    {
        _shuttingDown = true;
    }
}
