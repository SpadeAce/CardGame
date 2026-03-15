using System.Collections.Generic;

/// <summary>
/// CardDataTable(.bytes)를 로드하고 id 기반으로 제공한다.
/// DataManager.Instance.Card.Get(id) 형태로 사용.
/// </summary>
public class CardTable
{
    private Dictionary<int, GameData.CardData> _map;

    internal void Load()
    {
        _map = TableLoader.Load(
            "Data/CardData",
            GameData.CardDataTable.Parser,
            table => table.Items,
            row => row.Id
        );
    }

    /// <summary>
    /// id에 해당하는 CardData 행을 반환한다.
    /// 존재하지 않으면 null.
    /// </summary>
    public GameData.CardData Get(int id)
    {
        _map.TryGetValue(id, out var data);
        return data;
    }
}
