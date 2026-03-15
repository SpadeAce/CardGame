using System.Collections.Generic;

/// <summary>
/// CardEffectDataTable(.bytes)를 로드하고 id 기반으로 제공한다.
/// DataManager.Instance.CardEffect.Get(id) 형태로 사용.
/// </summary>
public class CardEffectTable
{
    private Dictionary<int, GameData.CardEffectData> _map;

    internal void Load()
    {
        _map = TableLoader.Load(
            "Data/CardEffectData",
            GameData.CardEffectDataTable.Parser,
            table => table.Items,
            row => row.Id
        );
    }

    /// <summary>
    /// id에 해당하는 CardEffectData 행을 반환한다.
    /// 존재하지 않으면 null.
    /// </summary>
    public GameData.CardEffectData Get(int id)
    {
        _map.TryGetValue(id, out var data);
        return data;
    }
}
