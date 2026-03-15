using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Spawn : MonoBehaviour
{
    [SerializeField] private bool _autoSpawn;
    [SerializeField] private GameObject _prefab;
    [SerializeField, HideInInspector] private string _cachedPath;

    private GameObject _spawnedObject;
    private string _spawnedPath;

    private void Start()
    {
        if (_autoSpawn)
        {
            Get();
        }
    }

    public T GetFromPath<T>(string path, Transform parent = null) where T : Component
    {
        // 이미 동일한 경로의 오브젝트가 생성되어 있다면 재사용
        if (_spawnedObject != null && _spawnedPath == path)
        {
            T cachedComponent = _spawnedObject.GetComponent<T>();
            if (cachedComponent != null)
                return cachedComponent;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if(prefab == null)
        {
            Debug.LogError($"[Spawn] 프리팹이 지정되지 않았습니다. ({path})");
            return null;
        }

        // 기존에 생성된 다른 오브젝트가 있다면 제거
        Despawn();

        GameObject instance = Instantiate(prefab, parent ?? transform);
        T component = instance.GetComponent<T>();

        if (component == null)
        {
            Debug.LogError($"[Spawn] 프리팹에 {typeof(T).Name} 컴포넌트가 없습니다. ({prefab.name})");
            Destroy(instance);
            return null;
        }

        _spawnedObject = instance;
        _spawnedPath = path;

        return component;
    }

    public GameObject GetFromPath(string path, Transform parent = null)
    {
        // 이미 동일한 경로의 오브젝트가 생성되어 있다면 재사용
        if (_spawnedObject != null && _spawnedPath == path)
            return _spawnedObject;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if(prefab == null)
        {
            Debug.LogError($"[Spawn] 프리팹이 지정되지 않았습니다. ({path})");
            return null;
        }

        // 기존에 생성된 다른 오브젝트가 있다면 제거
        Despawn();

        _spawnedObject = Instantiate(prefab, parent ?? transform);
        _spawnedPath = path;

        return _spawnedObject;
    }

    /// <summary>
    /// 프리팹을 인스턴스화하고 T 컴포넌트를 반환한다.
    /// </summary>
    public T Get<T>(Transform parent = null) where T : Component
    {
        // 이미 현재 설정된 프리팹과 동일한 오브젝트가 있다면 재사용
        if (_spawnedObject != null && _spawnedPath == _cachedPath)
        {
            T cachedComponent = _spawnedObject.GetComponent<T>();
            if (cachedComponent != null)
                return cachedComponent;
        }

        if (_prefab == null)
        {
            _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_cachedPath);

            if(_prefab == null)
            {
                Debug.LogError($"[Spawn] 프리팹이 지정되지 않았습니다. ({gameObject.name})");
                return null;
            }
        }

        // 기존에 생성된 다른 오브젝트가 있다면 제거
        Despawn();

        GameObject instance = Instantiate(_prefab, parent ?? transform);
        T component = instance.GetComponent<T>();

        if (component == null)
        {
            Debug.LogError($"[Spawn] 프리팹에 {typeof(T).Name} 컴포넌트가 없습니다. ({_prefab.name})");
            Destroy(instance);
            return null;
        }

        _spawnedObject = instance;
        _spawnedPath = _cachedPath;

        return component;
    }

    /// <summary>
    /// 프리팹을 인스턴스화하고 GameObject를 반환한다.
    /// </summary>
    public GameObject Get(Transform parent = null)
    {
        // 이미 현재 설정된 프리팹과 동일한 오브젝트가 있다면 재사용
        if (_spawnedObject != null && _spawnedPath == _cachedPath)
            return _spawnedObject;

        if (_prefab == null)
        {
            _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_cachedPath);

            if(_prefab == null)
            {
                Debug.LogError($"[Spawn] 프리팹이 지정되지 않았습니다. ({gameObject.name})");
                return null;
            }
        }

        // 기존에 생성된 다른 오브젝트가 있다면 제거
        Despawn();

        _spawnedObject = Instantiate(_prefab, parent ?? transform);
        _spawnedPath = _cachedPath;

        return _spawnedObject;
    }

    public void Despawn()
    {
        if (_spawnedObject != null)
        {
            if (Application.isPlaying)
                Destroy(_spawnedObject);
            else
                DestroyImmediate(_spawnedObject);
            
            _spawnedObject = null;
            _spawnedPath = string.Empty;
        }
    }

    private void OnDestroy()
    {
        Despawn();
    }

#if UNITY_EDITOR
    [ContextMenu("Clear Prefab Link")]
    public void ClearPrefabLink()
    {
        _prefab = null;
        _cachedPath = string.Empty;
        Debug.Log("[Spawn] 프리팹 링크와 캐시된 경로를 완전히 제거했습니다.");
    }

    private void OnValidate()
    {
        if (_prefab != null)
        {
            // 프리팹이 유효하면 경로 캐싱
            string path = AssetDatabase.GetAssetPath(_prefab);
            if (!string.IsNullOrEmpty(path))
                _cachedPath = path;
        }
        else if (!string.IsNullOrEmpty(_cachedPath))
        {
            // 프리팹 링크가 소실되었으면 캐싱된 경로로 복구 시도
            _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_cachedPath);

            if (_prefab != null)
                Debug.Log($"[Spawn] 프리팹 링크 복구 성공: {_cachedPath}");
            else
                Debug.LogWarning($"[Spawn] 프리팹 링크 복구 실패: {_cachedPath}");
        }
    }
#endif
}
