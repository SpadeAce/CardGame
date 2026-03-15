using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NamePresetDataTable(.bytes)를 로드하고 id 기반으로 제공한다.
/// DataManager.Instance.NamePreset.Get(id) 형태로 사용.
/// </summary>
public class NamePresetTable
{
    private Dictionary<int, GameData.NamePresetData> _map;
    private List<GameData.NamePresetData> _list;

    internal void Load()
    {
        _list = new List<GameData.NamePresetData>();
        _map = TableLoader.Load(
            "Data/NamePresetData",
            GameData.NamePresetDataTable.Parser,
            table => table.Items,
            row => row.Id
        );
        foreach (var item in _map.Values)
            _list.Add(item);
    }

    /// <summary>
    /// id에 해당하는 NamePresetData 행을 반환한다.
    /// 존재하지 않으면 null.
    /// </summary>
    public GameData.NamePresetData Get(int id)
    {
        _map.TryGetValue(id, out var data);
        return data;
    }

    /// <summary>
    /// 전체 목록에서 랜덤한 이름을 반환한다.
    /// 테이블이 비어 있으면 null.
    /// </summary>
    public string GetRandomName()
    {
        if (_list == null || _list.Count == 0) return null;
        return _list[Random.Range(0, _list.Count)].Name;
    }
}
