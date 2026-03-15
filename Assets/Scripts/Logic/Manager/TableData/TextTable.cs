using System.Collections.Generic;

/// <summary>
/// TextDataTable(.bytes)를 로드하고 IdAlias(string) 기반으로 제공한다.
/// TextManager 내부에서 사용. 직접 접근은 TextManager.Instance.Get(alias)을 통해 한다.
/// </summary>
public class TextTable
{
    private Dictionary<string, GameData.TextData> _map;

    internal void Load()
    {
        _map = TableLoader.Load<GameData.TextDataTable, GameData.TextData, string>(
            "Data/TextData",
            GameData.TextDataTable.Parser,
            table => table.Items,
            row => row.IdAlias
        );
    }

    public GameData.TextData Get(string alias)
    {
        _map.TryGetValue(alias, out var data);
        return data;
    }
}
