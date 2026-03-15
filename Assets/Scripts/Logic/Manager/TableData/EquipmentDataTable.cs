using System.Collections.Generic;

/// <summary>
/// EquipmentDataTable(.bytes)를 로드하고 id 기반으로 제공한다.
/// DataManager.Instance.Equipment.Get(id) 형태로 사용.
/// </summary>
public class EquipmentDataTable
{
    private Dictionary<int, GameData.EquipmentData> _map;

    internal void Load()
    {
        _map = TableLoader.Load(
            "Data/EquipmentData",
            GameData.EquipmentDataTable.Parser,
            table => table.Items,
            row => row.Id
        );
    }

    /// <summary>
    /// id에 해당하는 EquipmentData 행을 반환한다.
    /// 존재하지 않으면 null.
    /// </summary>
    public GameData.EquipmentData Get(int id)
    {
        _map.TryGetValue(id, out var data);
        return data;
    }
}
