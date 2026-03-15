using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 사각형 타일 게임 로직 클래스
/// Unity Grid의 좌표 시스템과 함께 사용
/// </summary>
public class SquareTile : MonoBehaviourEx
{
    #region Events
    
    /// <summary>
    /// 타일 상태 변경 시 발생
    /// </summary>
    public event Action<SquareTile, TileState> OnStateChanged;
    
    /// <summary>
    /// 타일 클릭 시 발생
    /// </summary>
    public event Action<SquareTile> OnClicked;
    
    #endregion

    #region Serialized Fields
    
    [Header("Visual")]
    [SerializeField] private Spawn _spawnModel;
    [SerializeField] private GameObject _goSelected;
    [SerializeField] private GameObject _goHighlighted;
    [SerializeField] private GameObject _goTarget;
    [SerializeField] private GameObject _goRadius;
    [SerializeField] private TileSlot _slot;
    
    #endregion

    #region Private Fields
    
    private Vector2Int _gridPosition;
    private int _tileId;
    private TileType _tileType = TileType.Empty;
    private TileState _tileState = TileState.Normal;
    
    #endregion

    #region Properties
    
    public Vector2Int GridPosition => _gridPosition;
    public int TileId => _tileId;
    public TileType TileType => _tileType;
    public TileState State => _tileState;
    
    /// <summary>
    /// 자체 엔티티 배치용 슬롯
    /// </summary>
    public TileSlot Slot => _slot;
    
    /// <summary>
    /// 이동 가능 여부
    /// </summary>
    public bool IsWalkable => _tileType != TileType.Blocked && _tileType != TileType.Water;
    
    /// <summary>
    /// 선택 상태 여부
    /// </summary>
    public bool IsSelected => _tileState == TileState.Selected;
    
    #endregion

    #region Public Methods
    
    /// <summary>
    /// 타일 초기화
    /// </summary>
    public void Init(int tileId, Vector2Int gridPosition, TileType type = TileType.Empty)
    {
        _tileId = tileId;
        _gridPosition = gridPosition;
        _tileType = type;
        SetState(TileState.Normal);
        _spawnModel.GetFromPath(TileManager.Instance.GetTilePath(type));
    }

    /// <summary>
    /// 타일 타입 설정
    /// </summary>
    public void SetTileType(TileType type)
    {
        _tileType = type;
    }

    /// <summary>
    /// 타일 상태 설정
    /// </summary>
    public void SetState(TileState newState)
    {
        if (_tileState == newState)
            return;

        TileState oldState = _tileState;
        _tileState = newState;
        
        UpdateVisuals();
        OnStateChanged?.Invoke(this, newState);
    }

    /// <summary>
    /// 타일 선택 상태 토글
    /// </summary>
    public void ToggleSelection()
    {
        SetState(_tileState == TileState.Selected ? TileState.Normal : TileState.Selected);
        OnClicked?.Invoke(this);
    }

    /// <summary>
    /// 타일 선택 상태 설정
    /// </summary>
    public void SetSelected(bool selected)
    {
        SetState(selected ? TileState.Selected : TileState.Normal);
    }

    /// <summary>
    /// 타일 하이라이트 상태 설정
    /// </summary>
    public void SetHighlighted(bool highlighted)
    {
        if (_tileState == TileState.Selected)
            return; // 선택 상태 우선
            
        SetState(highlighted ? TileState.Highlighted : TileState.Normal);
    }

    /// <summary>
    /// 타일 타겟 표시 설정
    /// </summary>
    public void SetTarget(bool target)
    {
        if (_goTarget != null)
            _goTarget.SetActive(target);
    }

    /// <summary>
    /// 반경 미리보기 표시 설정
    /// </summary>
    public void SetRadius(bool active)
    {
        if (_goRadius != null)
            _goRadius.SetActive(active);
    }

    /// <summary>
    /// 타일 정리
    /// </summary>
    public void Clear()
    {
        SetState(TileState.Normal);
        SetRadius(false);
        OnStateChanged = null;
        OnClicked = null;
    }

    #endregion

    #region Private Methods
    
    /// <summary>
    /// 시각적 요소 업데이트
    /// </summary>
    private void UpdateVisuals()
    {
        if (_goSelected != null)
            _goSelected.SetActive(_tileState == TileState.Selected);

        if (_goHighlighted != null)
            _goHighlighted.SetActive(_tileState == TileState.Highlighted);

    }
    
    #endregion

    #region Debug
    
    private void OnDrawGizmos()
    {
        if(!Application.isPlaying)
            return;

        // 상태에 따른 색상
        Color gizmoColor = _tileState switch
        {
            TileState.Selected => Color.green,
            TileState.Highlighted => Color.yellow,
            TileState.Disabled => Color.red,
            _ => Color.gray
        };
        
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, 0.1f);

#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(
            transform.position,
            $"\nID: {_tileId}\nPos: {_gridPosition}\nType: {_tileType}"
        );
#endif

        // 인접 타일 연결선 (빨간색) — Handles 이후에 그려야 색상 간섭 방지
        if (TileManager.Instance != null)
        {
            Gizmos.color = Color.red;
            var neighbors = TileManager.Instance.GetAdjacentTiles(_gridPosition);
            foreach (var neighbor in neighbors)
            {
                Gizmos.DrawLine(transform.position, neighbor.transform.position);
            }
        }
    }
    
    #endregion
}
