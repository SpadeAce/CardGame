using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 사각형 타일맵 관리자
/// X,Z 평면 기반 좌표 시스템 사용 (Vector2Int: x→X축, y→Z축)
/// </summary>
public class TileManager : MonoSingleton<TileManager>, IResettable
{
    #region Constants
    
    private const string TILE_PREFAB_PATH = "Prefabs/Tile/TileBase";
    private const string EDGE_TILE_PREFAB_PATH = "Prefabs/Tile/TileEdge";
    
    // 사각형 4방향 (X,Z 평면 — Vector2Int.y가 Z축)
    private static readonly Vector2Int[] SquareDirections = new Vector2Int[]
    {
        new Vector2Int(0, 1),   // Forward (+Z)
        new Vector2Int(1, 0),   // Right   (+X)
        new Vector2Int(0, -1),  // Back    (-Z)
        new Vector2Int(-1, 0)   // Left    (-X)
    };

    // TileDirection → 오프셋 매핑
    private static readonly Dictionary<TileDirection, Vector2Int> DirectionToOffset = new Dictionary<TileDirection, Vector2Int>
    {
        { TileDirection.North, new Vector2Int(0, 1) },
        { TileDirection.East,  new Vector2Int(1, 0) },
        { TileDirection.South, new Vector2Int(0, -1) },
        { TileDirection.West,  new Vector2Int(-1, 0) }
    };

    // 오프셋 → TileDirection 매핑
    private static readonly Dictionary<Vector2Int, TileDirection> OffsetToDirection = new Dictionary<Vector2Int, TileDirection>
    {
        { new Vector2Int(0, 1),  TileDirection.North },
        { new Vector2Int(1, 0),  TileDirection.East },
        { new Vector2Int(0, -1), TileDirection.South },
        { new Vector2Int(-1, 0), TileDirection.West }
    };
    
    #endregion

    #region Private Fields

    private float _tileSize = 1f;

    private GameObject _tilePrefab;
    private GameObject _edgeTilePrefab;
    private Dictionary<Vector2Int, SquareTile> _tilesByPosition = new Dictionary<Vector2Int, SquareTile>();
    private Dictionary<int, SquareTile> _tilesById = new Dictionary<int, SquareTile>();
    private Dictionary<EdgeTileKey, EdgeTile> _edgeTiles = new Dictionary<EdgeTileKey, EdgeTile>();
    private int _nextTileId = 0;

    private const string CATALOG_PATH = "Data/EntityCatalog";
    private IEntityCatalog _entityCatalog;
    
    #endregion

    #region Properties
    
    /// <summary>
    /// 타일 모델 크기 (배치 간격, GenerateTileMap 호출 시 설정됨)
    /// </summary>
    public float TileSize => _tileSize;

    /// <summary>
    /// 모든 타일 열거
    /// </summary>
    public IEnumerable<SquareTile> AllTiles => _tilesByPosition.Values;
    
    /// <summary>
    /// 타일 개수
    /// </summary>
    public int TileCount => _tilesByPosition.Count;
    
    #endregion

    #region Public Methods - Generation
    
    /// <summary>
    /// 사각형 타일맵 생성 (가로 × 세로)
    /// </summary>
    /// <param name="width">가로 타일 수</param>
    /// <param name="height">세로 타일 수</param>
    /// <param name="tileSize">타일 모델 크기 (배치 간격)</param>
    public bool GenerateTileMap(int width, int height, float tileSize = 1f)
    {
        if (width <= 0 || height <= 0)
        {
            Debug.LogError("Width and height must be greater than 0");
            return false;
        }

        ClearAllTiles();
        _tileSize = tileSize;
        
        if (!LoadTilePrefab() || !LoadEdgeTilePrefab())
            return false;

        _nextTileId = 0;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector2Int gridPos = new Vector2Int(x, z);
                CreateTile(gridPos);
            }
        }

        // 모든 타일 생성 후 EdgeTile 생성
        GenerateEdgeTiles();

        Debug.Log($"[TileManager] Generated {_tilesByPosition.Count} square tiles, {_edgeTiles.Count} edge tiles ({width}x{height}, tileSize: {_tileSize})");
        return true;
    }

    /// <summary>
    /// 프리셋 기반 타일맵 생성
    /// </summary>
    public bool GenerateTileMapFromPreset(TileMapPreset preset)
    {
        if (preset == null)
        {
            Debug.LogError("[TileManager] Preset is null");
            return false;
        }

        if (preset.width <= 0 || preset.height <= 0)
        {
            Debug.LogError("[TileManager] Preset width and height must be greater than 0");
            return false;
        }

        ClearAllTiles();
        _tileSize = preset.tileSize;

        if (!LoadTilePrefab() || !LoadEdgeTilePrefab())
            return false;

        _nextTileId = 0;

        for (int x = 0; x < preset.width; x++)
        {
            for (int z = 0; z < preset.height; z++)
            {
                Vector2Int gridPos = new Vector2Int(x, z);
                TilePresetData presetData = preset.GetTileData(gridPos);
                TileType type = presetData != null ? presetData.tileType : TileType.Ground;

                if (type == TileType.Empty)
                    continue;

                CreateTile(gridPos, type);
            }
        }

        GenerateEdgeTiles();

        // 엔티티 배치 (EntityCatalog 기반 정적 오브젝트)
        foreach (var tileData in preset.tiles)
        {
            if (tileData.entityId <= 0) continue;

            var tile = GetTile(tileData.position);
            if (tile == null) continue;

            GameObject prefab = ResolvePrefab(tileData.entityId);
            if (prefab == null)
            {
                Debug.LogWarning($"[TileManager] Entity not found in catalog: id={tileData.entityId}");
                continue;
            }

            GameObject entityObj = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            if (entityObj.TryGetComponent(out TileEntity entity))
            {
                if (tile.Slot != null) tile.Slot.SetEntity(entity);
            }
            else
            {
                Destroy(entityObj);
            }
        }

        // EdgeTile 엔티티 배치 (EntityCatalog 기반 정적 오브젝트 — Wall 등)
        if (preset.edges != null)
        {
            foreach (var edgeData in preset.edges)
            {
                if (edgeData.entityId <= 0) continue;

                var key = new EdgeTileKey(edgeData.posA, edgeData.posB);
                if (!_edgeTiles.TryGetValue(key, out var edge)) continue;

                GameObject prefab = ResolvePrefab(edgeData.entityId);
                if (prefab == null)
                {
                    Debug.LogWarning($"[TileManager] Edge entity not found in catalog: id={edgeData.entityId}");
                    continue;
                }

                GameObject entityObj = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                if (entityObj.TryGetComponent(out TileEntity entity))
                {
                    if (edge.Slot != null) edge.Slot.SetEntity(entity);
                    else Destroy(entityObj);
                }
                else
                {
                    Destroy(entityObj);
                }
            }
        }

        Debug.Log($"[TileManager] Generated tilemap from preset: {_tilesByPosition.Count} tiles ({preset.width}x{preset.height})");
        return true;
    }

    /// <summary>
    /// 프리셋에서 스폰 포인트 목록 가져오기
    /// </summary>
    public List<TilePresetData> GetSpawnPoints(TileMapPreset preset, SpawnPointType type)
    {
        var result = new List<TilePresetData>();
        foreach (var tile in preset.tiles)
        {
            if (tile.spawnPoint == type)
                result.Add(tile);
        }
        return result;
    }

    public void ResetAll()
    {
        ClearAllTiles();
    }

    /// <summary>
    /// 모든 타일 제거
    /// </summary>
    public void ClearAllTiles()
    {
        foreach (var edge in _edgeTiles.Values)
        {
            if (edge != null)
            {
                edge.Clear();
                Destroy(edge.gameObject);
            }
        }
        _edgeTiles.Clear();

        foreach (var tile in _tilesByPosition.Values)
        {
            if (tile != null)
            {
                tile.Clear();
                Destroy(tile.gameObject);
            }
        }

        _tilesByPosition.Clear();
        _tilesById.Clear();
        _nextTileId = 0;
        _entityCatalog = null;
    }
    
    #endregion

    #region Public Methods - Tile Access
    
    /// <summary>
    /// 그리드 위치로 타일 가져오기
    /// </summary>
    public SquareTile GetTile(Vector2Int gridPosition)
    {
        return _tilesByPosition.TryGetValue(gridPosition, out var tile) ? tile : null;
    }

    /// <summary>
    /// 타일 ID로 타일 가져오기
    /// </summary>
    public SquareTile GetTileById(int id)
    {
        return _tilesById.TryGetValue(id, out var tile) ? tile : null;
    }

    /// <summary>
    /// 인접 타일 가져오기 (4방향)
    /// </summary>
    public List<SquareTile> GetAdjacentTiles(Vector2Int gridPosition)
    {
        List<SquareTile> adjacentTiles = new List<SquareTile>(4);

        foreach (var dir in SquareDirections)
        {
            Vector2Int neighborPos = gridPosition + dir;
            if (_tilesByPosition.TryGetValue(neighborPos, out var neighbor))
            {
                adjacentTiles.Add(neighbor);
            }
        }

        return adjacentTiles;
    }

    /// <summary>
    /// 범위 내 모든 타일 가져오기
    /// </summary>
    public List<SquareTile> GetTilesInRange(Vector2Int center, int range)
    {
        List<SquareTile> tilesInRange = new List<SquareTile>();

        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                Vector2Int pos = center + new Vector2Int(x, z);
                if (_tilesByPosition.TryGetValue(pos, out var tile))
                {
                    tilesInRange.Add(tile);
                }
            }
        }

        return tilesInRange;
    }
    
    #endregion

    #region Public Methods - EdgeTile Access

    /// <summary>
    /// 특정 타일의 특정 방향 EdgeTile 가져오기
    /// </summary>
    public EdgeTile GetEdgeTile(SquareTile tile, TileDirection direction)
    {
        if (tile == null || direction == TileDirection.None)
            return null;

        return GetEdgeTile(tile.GridPosition, direction);
    }

    /// <summary>
    /// 그리드 좌표 + 방향으로 EdgeTile 가져오기
    /// </summary>
    public EdgeTile GetEdgeTile(Vector2Int gridPos, TileDirection direction)
    {
        if (direction == TileDirection.None)
            return null;

        if (!DirectionToOffset.TryGetValue(direction, out var offset))
            return null;

        Vector2Int neighborPos = gridPos + offset;
        var key = new EdgeTileKey(gridPos, neighborPos);
        return _edgeTiles.TryGetValue(key, out var edge) ? edge : null;
    }

    /// <summary>
    /// 특정 타일의 모든 인접 EdgeTile 가져오기
    /// </summary>
    public List<EdgeTile> GetEdgeTilesForTile(Vector2Int gridPosition)
    {
        List<EdgeTile> edges = new List<EdgeTile>(4);

        foreach (var dir in SquareDirections)
        {
            var key = new EdgeTileKey(gridPosition, gridPosition + dir);
            if (_edgeTiles.TryGetValue(key, out var edge))
            {
                edges.Add(edge);
            }
        }

        return edges;
    }

    #endregion

    #region Public Methods - Utilities
    
    /// <summary>
    /// 두 타일 간 거리 계산 (맨해튼 거리, X,Z 평면)
    /// </summary>
    public int GetDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    /// <summary>
    /// BFS로 이동 가능한 타일 집합을 반환한다.
    /// 조건: IsWalkable == true, 슬롯 비어 있음 (점유된 타일은 진입 불가)
    /// </summary>
    /// <summary>
    /// BFS로 이동 가능한 타일 집합을 반환한다.
    /// - 빈 타일: 진입 및 정지 가능 → reachable에 추가
    /// - 같은 종류(friendlyDataType) Actor: 통과만 가능, 정지 불가 → reachable 미추가, BFS 계속
    /// - 다른 종류 점유 타일: 진입 불가 → BFS 차단
    /// </summary>
    public HashSet<SquareTile> GetReachableTiles(Vector2Int center, int movement, System.Type friendlyDataType = null)
    {
        var reachable = new HashSet<SquareTile>();
        var visited = new Dictionary<Vector2Int, int>(); // pos → 남은 이동력
        var queue = new Queue<(Vector2Int pos, int remaining)>();

        visited[center] = movement;
        queue.Enqueue((center, movement));

        while (queue.Count > 0)
        {
            var (pos, remaining) = queue.Dequeue();

            foreach (var dir in SquareDirections)
            {
                Vector2Int next = pos + dir;
                if (!_tilesByPosition.TryGetValue(next, out var tile)) continue;
                if (!tile.IsWalkable) continue;
                if (!IsEdgePassable(pos, next)) continue;

                int nextRemaining = remaining - 1;
                if (visited.TryGetValue(next, out int prev) && prev >= nextRemaining) continue;
                visited[next] = nextRemaining;

                TileEntity occupant = tile.Slot != null ? tile.Slot.SlotEntity : null;
                if (occupant != null)
                {
                    // 같은 종류 Actor → 통과만 허용 (정지 불가)
                    bool isFriendly = friendlyDataType != null
                        && occupant is Actor a
                        && a.Data != null
                        && a.Data.GetType() == friendlyDataType;

                    if (!isFriendly) continue; // 다른 종류 → 차단
                    // 우군 통과: reachable에 추가하지 않고 BFS만 계속
                }
                else
                {
                    reachable.Add(tile);
                }

                if (nextRemaining > 0)
                    queue.Enqueue((next, nextRemaining));
            }
        }

        return reachable;
    }

    /// <summary>
    /// from → to 까지의 A* 최단 경로를 반환한다 (movement 범위 내).
    /// Manhattan distance 휴리스틱으로 목적지 방향을 선호하는 경로를 탐색한다.
    /// 우군 통과 규칙은 GetReachableTiles와 동일.
    /// 도달 불가 시 null 반환. 반환 리스트는 from 제외, to 포함.
    /// </summary>
    public List<SquareTile> GetPath(
        Vector2Int from, Vector2Int to,
        int movement, System.Type friendlyDataType = null)
    {
        var predecessors = new Dictionary<Vector2Int, Vector2Int>();
        var bestG = new Dictionary<Vector2Int, int>(); // pos → 최소 이동 횟수(g-score)
        // A* 우선순위 큐: f=g+h 기준 정렬, 같은 f 내에서는 FIFO
        var open = new SortedDictionary<int, Queue<(Vector2Int pos, int g)>>();

        int Heuristic(Vector2Int p) => Mathf.Abs(p.x - to.x) + Mathf.Abs(p.y - to.y);

        void EnqueueNode(Vector2Int p, int g)
        {
            int f = g + Heuristic(p);
            if (!open.TryGetValue(f, out var q))
                open[f] = q = new Queue<(Vector2Int, int)>();
            q.Enqueue((p, g));
        }

        bestG[from] = 0;
        EnqueueNode(from, 0);

        while (open.Count > 0)
        {
            var bucket = open.First();
            var (pos, g) = bucket.Value.Dequeue();
            if (bucket.Value.Count == 0) open.Remove(bucket.Key);

            if (pos == to) break;

            // 이미 더 좋은 경로로 처리된 노드는 건너뜀
            if (bestG.TryGetValue(pos, out int prevG) && prevG < g) continue;

            foreach (var dir in SquareDirections)
            {
                Vector2Int next = pos + dir;
                if (!_tilesByPosition.TryGetValue(next, out var tile)) continue;
                if (!tile.IsWalkable) continue;
                if (!IsEdgePassable(pos, next)) continue;

                int newG = g + 1;
                if (newG > movement) continue;
                if (bestG.TryGetValue(next, out int prevBestG) && prevBestG <= newG) continue;

                TileEntity occupant = tile.Slot?.SlotEntity;
                if (occupant != null)
                {
                    bool isFriendly = friendlyDataType != null
                        && occupant is Actor a
                        && a.Data != null
                        && a.Data.GetType() == friendlyDataType;
                    if (!isFriendly) continue;
                }

                bestG[next] = newG;
                predecessors[next] = pos;
                EnqueueNode(next, newG);
            }
        }

        if (!predecessors.ContainsKey(to)) return null;

        // 경로 역추적
        var path = new List<Vector2Int>();
        var current = to;
        while (current != from)
        {
            path.Add(current);
            current = predecessors[current];
        }
        path.Reverse();

        var result = new List<SquareTile>(path.Count);
        foreach (var p in path)
            result.Add(_tilesByPosition[p]);
        return result;
    }

    /// <summary>
    /// BFS 경로를 스무딩해 불필요한 중간 웨이포인트를 제거한다.
    /// anchor에서 직선으로 도달 가능한 가장 먼 타일을 greedy하게 선택해
    /// 대각선 직선 이동이 가능하도록 웨이포인트를 줄인다.
    /// </summary>
    public List<SquareTile> SmoothPath(List<SquareTile> path, Vector2Int fromPos, System.Type friendlyDataType = null)
    {
        if (path == null || path.Count <= 1) return path;

        var result = new List<SquareTile>();
        Vector2Int anchor = fromPos;
        int i = 0;

        while (i < path.Count)
        {
            int furthest = i;
            for (int j = path.Count - 1; j > i; j--)
            {
                if (HasLineOfSight(anchor, path[j].GridPosition, friendlyDataType))
                {
                    furthest = j;
                    break;
                }
            }
            result.Add(path[furthest]);
            anchor = path[furthest].GridPosition;
            i = furthest + 1;
        }

        return result;
    }

    /// <summary>
    /// Bresenham's line 알고리즘으로 두 그리드 좌표 간 직선상의 모든 타일이
    /// 통과 가능한지 검사한다. 시작 좌표는 검사하지 않는다.
    /// - 카디널 이동: EdgeTile 벽 통과 가능 여부 검사 포함
    /// - 대각선 이동: 코너 두 타일 및 EdgeTile 경로 검사로 코너 커팅 방지
    /// </summary>
    private bool HasLineOfSight(Vector2Int from, Vector2Int to, System.Type friendlyDataType = null)
    {
        int x = from.x, y = from.y;
        int dx = Mathf.Abs(to.x - from.x);
        int dy = Mathf.Abs(to.y - from.y);
        int sx = to.x > from.x ? 1 : -1;
        int sy = to.y > from.y ? 1 : -1;
        int err = dx - dy;

        while (x != to.x || y != to.y)
        {
            int prevX = x, prevY = y;
            int e2 = 2 * err;
            bool moveX = e2 > -dy;
            bool moveY = e2 < dx;

            if (moveX) { err -= dy; x += sx; }
            if (moveY) { err += dx; y += sy; }

            var pos  = new Vector2Int(x, y);
            var prev = new Vector2Int(prevX, prevY);

            if (moveX && moveY)
            {
                // 대각선 스텝: 두 코너 경유 경로 중 하나 이상 통과 가능해야 함
                var cornerA = new Vector2Int(x, prevY);   // X방향 코너
                var cornerB = new Vector2Int(prevX, y);   // Y방향 코너

                bool cornerAWalkable = _tilesByPosition.TryGetValue(cornerA, out var tileA) && tileA.IsWalkable;
                bool cornerBWalkable = _tilesByPosition.TryGetValue(cornerB, out var tileB) && tileB.IsWalkable;

                // 코너 커팅 방지: 두 코너 모두 walkable이어야 함
                if (!cornerAWalkable || !cornerBWalkable) return false;

                // EdgeTile 벽 검사: 두 경로 중 하나 이상 통과 가능해야 함
                bool routeA = IsEdgePassable(prev, cornerA) && IsEdgePassable(cornerA, pos);
                bool routeB = IsEdgePassable(prev, cornerB) && IsEdgePassable(cornerB, pos);

                if (!routeA && !routeB) return false;
            }
            else if (moveX)
            {
                if (!IsEdgePassable(prev, pos)) return false;
            }
            else
            {
                if (!IsEdgePassable(prev, pos)) return false;
            }

            if (!_tilesByPosition.TryGetValue(pos, out var tile)) return false;
            if (!tile.IsWalkable) return false;

            TileEntity occupant = tile.Slot?.SlotEntity;
            if (occupant != null)
            {
                bool isFriendly = friendlyDataType != null
                    && occupant is Actor a && a.Data != null
                    && a.Data.GetType() == friendlyDataType;
                if (!isFriendly) return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 그리드 좌표를 월드 좌표로 변환 (Vector2Int.y → 월드 Z축)
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPosition)
    {
        return new Vector3(
            gridPosition.x * _tileSize,
            0f,
            gridPosition.y * _tileSize
        );
    }

    /// <summary>
    /// 월드 좌표를 그리드 좌표로 변환 (월드 Z축 → Vector2Int.y)
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPosition.x / _tileSize),
            Mathf.RoundToInt(worldPosition.z / _tileSize)
        );
    }

    /// <summary>
    /// 현재 타일맵 전체의 월드 좌표 중앙을 반환한다.
    /// 타일이 없으면 Vector3.zero 반환.
    /// </summary>
    public Vector3 GetMapCenter()
    {
        if (_tilesByPosition.Count == 0) return Vector3.zero;

        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var pos in _tilesByPosition.Keys)
        {
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y < minY) minY = pos.y;
            if (pos.y > maxY) maxY = pos.y;
        }

        return new Vector3(
            (minX + maxX) * 0.5f * _tileSize,
            0f,
            (minY + maxY) * 0.5f * _tileSize
        );
    }

    /// <summary>
    /// 모든 타일 선택 해제
    /// </summary>
    public void DeselectAllTiles()
    {
        foreach (var tile in _tilesByPosition.Values)
        {
            tile.SetState(TileState.Normal);
        }
    }

    public string GetTilePath(TileType type)
    {
        return type switch
        {
            TileType.Empty => string.Empty,
            TileType.Ground => "Assets/Resources/Tile/Prefabs/TileGround_01.prefab",
            TileType.Dirt =>  "Assets/Resources/Tile/Prefabs/TileGround_02.prefab",
            TileType.Water => "Assets/Resources/Tile/Prefabs/TileWater_03.prefab",
            _ => string.Empty
        };
    }
    
    #endregion

    #region Private Methods

    private IEntityCatalog GetCatalog()
    {
        _entityCatalog ??= Resources.Load<EntityCatalog>(CATALOG_PATH);
        return _entityCatalog;
    }

    /// <summary>
    /// entityId로 프리팹을 조회합니다.
    /// catalog entry의 prefab → resourcePath 순으로 폴백.
    /// </summary>
    private GameObject ResolvePrefab(int entityId)
    {
        if (entityId <= 0) return null;
        var catalog = GetCatalog();
        if (catalog == null) return null;

        var entry = catalog.GetEntry(entityId);
        if (entry == null) return null;

        if (entry.prefab != null) return entry.prefab;
        if (!string.IsNullOrEmpty(entry.resourcePath))
            return Resources.Load<GameObject>(entry.resourcePath);
        return null;
    }

    private bool LoadTilePrefab()
    {
        if (_tilePrefab != null)
            return true;

        _tilePrefab = Resources.Load<GameObject>(TILE_PREFAB_PATH);
        
        if (_tilePrefab == null)
        {
            Debug.LogError($"[TileManager] Failed to load tile prefab: Resources/{TILE_PREFAB_PATH}");
            return false;
        }

        return true;
    }

    private bool LoadEdgeTilePrefab()
    {
        if (_edgeTilePrefab != null)
            return true;

        _edgeTilePrefab = Resources.Load<GameObject>(EDGE_TILE_PREFAB_PATH);
        
        if (_edgeTilePrefab == null)
        {
            Debug.LogError($"[TileManager] Failed to load edge tile prefab: Resources/{EDGE_TILE_PREFAB_PATH}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 두 인접 타일 사이 EdgeTile에 isPassable=false인 TileWall이 있으면 false 반환.
    /// </summary>
    private bool IsEdgePassable(Vector2Int a, Vector2Int b)
    {
        var key = new EdgeTileKey(a, b);
        if (!_edgeTiles.TryGetValue(key, out var edge)) return true;
        if (edge.Slot == null) return true;
        if (edge.Slot.SlotEntity == null) return true;
        if (edge.Slot.SlotEntity is TileWall wall && !wall.isPassable) return false;
        return true;
    }

    private void CreateTile(Vector2Int gridPosition)
    {
        TileType type = Random.Range(0, 3) switch
        {
            0 => TileType.Ground,
            1 => TileType.Dirt,
            2 => TileType.Water,
            _ => TileType.Ground
        };

        CreateTile(gridPosition, type);
    }

    private void CreateTile(Vector2Int gridPosition, TileType type)
    {
        if (_tilesByPosition.ContainsKey(gridPosition))
            return;

        Vector3 worldPos = GridToWorld(gridPosition);
        GameObject tileObj = Instantiate(_tilePrefab, worldPos, Quaternion.identity, transform);
        tileObj.name = $"Tile_{gridPosition.x}_{gridPosition.y}";

        SquareTile tile = tileObj.GetComponent<SquareTile>();
        if (tile != null)
        {
            tile.Init(_nextTileId, gridPosition, type);
            _tilesByPosition.Add(gridPosition, tile);
            _tilesById.Add(_nextTileId, tile);
            _nextTileId++;
        }
        else
        {
            Debug.LogWarning($"[TileManager] Tile prefab missing SquareTile component");
            Destroy(tileObj);
        }
    }

    /// <summary>
    /// 모든 타일에 대해 인접 방향을 순회하며 EdgeTile 생성 (중복 방지)
    /// </summary>
    private void GenerateEdgeTiles()
    {
        foreach (var kvp in _tilesByPosition)
        {
            Vector2Int pos = kvp.Key;

            foreach (var dir in SquareDirections)
            {
                Vector2Int neighborPos = pos + dir;
                var key = new EdgeTileKey(pos, neighborPos);

                // 이미 생성된 Edge는 건너뜀
                if (_edgeTiles.ContainsKey(key))
                    continue;

                // 인접 타일이 존재하는 경우에만 생성
                if (!_tilesByPosition.ContainsKey(neighborPos))
                    continue;

                CreateEdgeTile(key, pos, dir);
            }
        }
    }

    private void CreateEdgeTile(EdgeTileKey key, Vector2Int fromPos, Vector2Int dirOffset)
    {
        // 두 타일의 중간 월드 좌표
        Vector3 worldA = GridToWorld(fromPos);
        Vector3 worldB = GridToWorld(fromPos + dirOffset);
        Vector3 edgeWorldPos = (worldA + worldB) * 0.5f;

        // 방향에 따른 회전
        TileDirection direction = OffsetToDirection[dirOffset];
        Quaternion rotation = direction switch
        {
            TileDirection.East  => Quaternion.Euler(0, 90, 0),
            TileDirection.West  => Quaternion.Euler(0, 90, 0),
            _ => Quaternion.identity
        };

        GameObject edgeObj = Instantiate(_edgeTilePrefab, edgeWorldPos, rotation, transform);
        edgeObj.name = $"Edge_{key}";

        EdgeTile edge = edgeObj.GetComponent<EdgeTile>();
        if (edge != null)
        {
            edge.Init(key, direction);
            _edgeTiles.Add(key, edge);
        }
        else
        {
            Debug.LogWarning($"[TileManager] Edge tile prefab missing EdgeTile component");
            Destroy(edgeObj);
        }
    }
    
    #endregion
}
