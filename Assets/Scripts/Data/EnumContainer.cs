/// <summary>
/// 타일 타입 열거형
/// </summary>
public enum TileType
{
    Empty,
    Ground,
    Dirt,
    Water,
    Blocked
}

/// <summary>
/// 타일 상태 열거형
/// </summary>
public enum TileState
{
    Normal,
    Selected,
    Highlighted,
    Disabled
}

public enum TileDirection
{
    None,
    North,
    East,
    South,
    West
}

/// <summary>
/// 스폰 포인트 타입 열거형
/// </summary>
public enum SpawnPointType
{
    None,
    Player,
    Enemy
}

/// <summary>
/// EntityCatalog 카테고리 열거형
/// </summary>
public enum EntityCategory
{
    None,
    Monster,
    Pawn,
    Wall,
    Item
}