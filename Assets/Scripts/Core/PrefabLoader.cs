using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// AssetPathAttribute가 지정된 MonoBehaviour 프리팹을 로드한다.
/// reflection 결과를 캐싱하여 반복 호출 비용을 줄인다.
/// 사용법: PrefabLoader.Load&lt;ShopItem&gt;()
/// </summary>
public static class PrefabLoader
{
    private static readonly Dictionary<Type, string> _pathCache = new();

    public static T Load<T>() where T : MonoBehaviour
    {
        var type = typeof(T);
        if (!_pathCache.TryGetValue(type, out var path))
        {
            path = type.GetCustomAttribute<AssetPathAttribute>()?.Path;
            _pathCache[type] = path;
        }
        return Resources.Load<T>(path);
    }
}
