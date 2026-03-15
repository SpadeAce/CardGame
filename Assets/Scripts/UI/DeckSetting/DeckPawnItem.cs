using System;
using UnityEngine;
using UnityEngine.EventSystems;

[AssetPath("Prefabs/UI/DeckSetting/DeckPawnItem")]
public class DeckPawnItem : MonoBehaviourEx,
    IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerClickHandler
{
    #region Links
    [Linker("Spawn_Icon")]
    public Spawn _spawnIcon;
    #endregion Links

    private DPawn _pawn;
    private bool _isDragging;
    private Vector3 _originalPos;

    public DPawn Pawn => _pawn;

    public event Action<DeckPawnItem, PointerEventData> OnDragBegin;
    public event Action<DeckPawnItem, PointerEventData> OnDragging;
    public event Action<DeckPawnItem, PointerEventData> OnDragEnd;
    public event Action<DeckPawnItem> OnRightClick;

    public void SetData(DPawn pawn)
    {
        _pawn = pawn;
        _spawnIcon.Get<PawnIcon>().SetData(pawn);
    }

    public void OnPointerDown(PointerEventData e)
    {
        _originalPos = transform.position;
    }

    public void OnDrag(PointerEventData e)
    {
        if (!_isDragging)
        {
            _isDragging = true;
            OnDragBegin?.Invoke(this, e);
        }
        transform.position = new Vector3(e.position.x, e.position.y, 0f);
        OnDragging?.Invoke(this, e);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            OnDragEnd?.Invoke(this, e);
        }
        transform.position = _originalPos;
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Right)
            OnRightClick?.Invoke(this);
    }
}
