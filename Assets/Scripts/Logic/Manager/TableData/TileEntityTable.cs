using System.Collections.Generic;

/// <summary>
/// TileEntityDataTable(.bytes)를 로드하고 id 기반으로 제공한다.
/// DataManager.Instance.TileEntity.Get(id) 형태로 사용.
/// </summary>
public class TileEntityTable
{
    private Dictionary<int, GameData.TileEntityData> _map;

    internal void Load()
    {
        _map = TableLoader.Load(
            "Data/TileEntityData",
            GameData.TileEntityDataTable.Parser,
            table => table.Items,
            row => row.Id
        );
    }

    /// <summary>
    /// id에 해당하는 TileEntityData 행을 반환한다.
    /// 존재하지 않으면 null.
    /// </summary>
    public GameData.TileEntityData Get(int id)
    {
        _map.TryGetValue(id, out var data);
        return data;
    }
}
