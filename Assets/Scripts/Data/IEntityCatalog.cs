using System.Collections.Generic;

public interface IEntityCatalog
{
    EntityCatalogEntry GetEntry(int id);
    IReadOnlyList<EntityCatalogEntry> GetAllEntries();
}
