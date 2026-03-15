using System.Collections.Generic;
using System.Linq;

/// <summary>
/// PawnDataTable(.bytes)를 로드하고 id 기반으로 제공한다.
/// DataManager.Instance.Pawn.Get(id) 형태로 사용.
/// </summary>
public class PawnTable
{
    private Dictionary<int, GameData.PawnData> _map;

    internal void Load()
    {
        _map = TableLoader.Load(
            "Data/PawnData",
            GameData.PawnDataTable.Parser,
            table => table.Items,
            row => row.Id
        );
    }

    /// <summary>
    /// id에 해당하는 PawnData 행을 반환한다.
    /// 존재하지 않으면 null.
    /// </summary>
    public GameData.PawnData Get(int id)
    {
        _map.TryGetValue(id, out var data);
        return data;
    }

    public IEnumerable<GameData.PawnData> GetAll() => _map.Values;
}
