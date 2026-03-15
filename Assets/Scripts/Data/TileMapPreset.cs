using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TilePresetData
{
    public Vector2Int position;
    public TileType tileType;
    public SpawnPointType spawnPoint;
    public int spawnId;     // PawnId (Player) 또는 MonsterId (Enemy)
    public int entityId;    // EntityCatalog ID (0 = 없음)
}

[System.Serializable]
public class EdgePresetData
{
    public Vector2Int posA;
    public Vector2Int posB;
    public int entityId;    // EntityCatalog ID (0 = 없음)
}

[CreateAssetMenu(fileName = "TileMapPreset", menuName = "Game/TileMap Preset")]
public class TileMapPreset : ScriptableObject
{
    public int width;
    public int height;
    public float tileSize = 1.05f;
    public int turnLimit = 0;       // 0 = 제한 없음
    public int maxPawnCount = 0;    // 0 = 스폰 포인트 수만큼
    public List<TilePresetData> tiles = new List<TilePresetData>();
    public List<EdgePresetData> edges = new List<EdgePresetData>();

    public TilePresetData GetTileData(Vector2Int position)
    {
        return tiles.Find(t => t.position == position);
    }

    public string ToJson() => JsonUtility.ToJson(this, true);
    public void FromJson(string json) => JsonUtility.FromJsonOverwrite(json, this);
}
