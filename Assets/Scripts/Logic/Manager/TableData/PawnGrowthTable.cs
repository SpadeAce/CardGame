using System.Collections.Generic;

/// <summary>
/// PawnGrowthDataTable(.bytes)를 로드하고 id 기반으로 제공한다.
/// DataManager.Instance.PawnGrowth.Get(id) 형태로 사용.
/// </summary>
public class PawnGrowthTable
{
    private Dictionary<int, GameData.PawnGrowthData> _map;

    internal void Load()
    {
        _map = TableLoader.Load(
            "Data/PawnGrowthData",
            GameData.PawnGrowthDataTable.Parser,
            table => table.Items,
            row => row.Id
        );
    }

    /// <summary>
    /// id에 해당하는 PawnGrowthData 행을 반환한다.
    /// 존재하지 않으면 null.
    /// </summary>
    public GameData.PawnGrowthData Get(int id)
    {
        _map.TryGetValue(id, out var data);
        return data;
    }

    /// <summary>
    /// classType과 level이 일치하는 행을 반환한다.
    /// 존재하지 않으면 null.
    /// </summary>
    public GameData.PawnGrowthData GetByClassLevel(GameData.ClassType classType, int level)
    {
        foreach (var data in _map.Values)
            if (data.ClassType == classType && data.Level == level) return data;
        return null;
    }
}
