using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityCatalog", menuName = "Game/Entity Catalog")]
public class EntityCatalog : ScriptableObject, IEntityCatalog
{
    public List<EntityCatalogEntry> entries = new();

    private Dictionary<int, EntityCatalogEntry> _lookup;

    private void OnEnable() => RebuildLookup();

    /// <summary>
    /// 룩업 딕셔너리를 재빌드합니다. 에디터에서 entries 변경 후 호출하세요.
    /// </summary>
    public void RebuildLookup()
    {
        _lookup = new Dictionary<int, EntityCatalogEntry>();
        foreach (var e in entries)
        {
            if (e.id > 0)
                _lookup[e.id] = e;
        }
    }

    public EntityCatalogEntry GetEntry(int id)
    {
        if (id <= 0) return null;
        if (_lookup == null) RebuildLookup();
        return _lookup.TryGetValue(id, out var e) ? e : null;
    }

    public IReadOnlyList<EntityCatalogEntry> GetAllEntries() => entries;
}
