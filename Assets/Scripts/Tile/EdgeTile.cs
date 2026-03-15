using UnityEngine;

/// <summary>
/// 타일과 타일 사이의 경계(Edge) 타일
/// 벽, 장애물 등의 TileEntity를 배치하기 위한 슬롯을 보유
/// TileManager가 생성·관리하며, SquareTile + TileDirection으로 조회
/// </summary>
public class EdgeTile : MonoBehaviourEx
{
    #region Serialized Fields
    
    [Header("Visual")]
    [SerializeField] private Spawn _spawnModel;
    [SerializeField] private TileSlot _slot;
    
    #endregion

    #region Private Fields

    private EdgeTileKey _key;
    private TileDirection _direction;

    #endregion

    #region Properties

    /// <summary>
    /// 내부 식별 키 (좌표 쌍)
    /// </summary>
    public EdgeTileKey Key => _key;

    /// <summary>
    /// 기준 타일 기준 방향
    /// </summary>
    public TileDirection Direction => _direction;

    /// <summary>
    /// 엔티티 배치용 슬롯
    /// </summary>
    public TileSlot Slot => _slot;

    #endregion

    #region Public Methods

    /// <summary>
    /// 초기화
    /// </summary>
    public void Init(EdgeTileKey key, TileDirection direction)
    {
        _key = key;
        _direction = direction;
    }

    /// <summary>
    /// 정리
    /// </summary>
    public void Clear()
    {
        if (_slot != null)
            _slot.ClearEntity();
    }

    #endregion
}
