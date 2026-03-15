using System.Collections.Generic;

/// <summary>
/// ShopDataTable(.bytes)를 로드하고 id/level 기반으로 제공한다.
/// DataManager.Instance.Shop.Get(id) / GetByLevel(level) 형태로 사용.
/// </summary>
public class ShopTable
{
    private Dictionary<int, GameData.ShopData> _map;
    private Dictionary<int, GameData.ShopData> _levelMap;
    private int _maxLevel;

    internal void Load()
    {
        _map = TableLoader.Load(
            "Data/ShopData",
            GameData.ShopDataTable.Parser,
            table => table.Items,
            row => row.Id
        );

        _levelMap = new Dictionary<int, GameData.ShopData>();
        foreach (var data in _map.Values)
        {
            _levelMap[data.Level] = data;
            if (data.Level > _maxLevel) _maxLevel = data.Level;
        }
    }

    /// <summary>
    /// id에 해당하는 ShopData 행을 반환한다.
    /// 존재하지 않으면 null.
    /// </summary>
    public GameData.ShopData Get(int id)
    {
        _map.TryGetValue(id, out var data);
        return data;
    }

    /// <summary>
    /// level에 해당하는 ShopData 행을 반환한다.
    /// 존재하지 않으면 null.
    /// </summary>
    public GameData.ShopData GetByLevel(int level)
    {
        _levelMap.TryGetValue(level, out var data);
        return data;
    }

    /// <summary>
    /// 데이터에 입력된 ShopData의 최고 레벨을 반환한다.
    /// </summary>
    public int GetMaxLevel() => _maxLevel;
}
