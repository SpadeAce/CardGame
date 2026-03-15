using System.Collections.Generic;

/// <summary>
/// StagePresetDataTable(.bytes)를 로드하고 id 기반으로 제공한다.
/// DataManager.Instance.StagePreset.Get(id) 형태로 사용.
/// </summary>
public class StagePresetTable
{
    private Dictionary<int, GameData.StagePresetData> _map;

    internal void Load()
    {
        _map = TableLoader.Load(
            "Data/StagePresetData",
            GameData.StagePresetDataTable.Parser,
            table => table.Items,
            row => row.Id
        );
    }

    /// <summary>
    /// id에 해당하는 StagePresetData 행을 반환한다.
    /// 존재하지 않으면 null.
    /// </summary>
    public GameData.StagePresetData Get(int id)
    {
        _map.TryGetValue(id, out var data);
        return data;
    }

    /// <summary>
    /// Level 값이 일치하는 첫 번째 StagePresetData 행을 반환한다.
    /// 존재하지 않으면 null.
    /// </summary>
    public GameData.StagePresetData GetByLevel(int level)
    {
        foreach (var data in _map.Values)
            if (data.Level == level) return data;
        return null;
    }
}
