using System.Collections.Generic;

/// <summary>
/// StageRewardDataTable(.bytes)를 로드하고 id 기반으로 제공한다.
/// DataManager.Instance.StageReward.Get(id) 형태로 사용.
/// </summary>
public class StageRewardTable
{
    private Dictionary<int, GameData.StageRewardData> _map;

    internal void Load()
    {
        _map = TableLoader.Load(
            "Data/StageRewardData",
            GameData.StageRewardDataTable.Parser,
            table => table.Items,
            row => row.Id
        );
    }

    /// <summary>
    /// id에 해당하는 StageRewardData 행을 반환한다.
    /// 존재하지 않으면 null.
    /// </summary>
    public GameData.StageRewardData Get(int id)
    {
        _map.TryGetValue(id, out var data);
        return data;
    }

    /// <summary>
    /// level에 해당하는 StageRewardData 행을 반환한다.
    /// 존재하지 않으면 null.
    /// </summary>
    public GameData.StageRewardData GetByLevel(int level)
    {
        foreach (var data in _map.Values)
            if (data.Level == level) return data;
        return null;
    }
}
