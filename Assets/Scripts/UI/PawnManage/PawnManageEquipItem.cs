using System;
using UnityEngine;
using UnityEngine.EventSystems;

[AssetPath("Prefabs/UI/PawnManage/PawnManageEquipItem")]
public class PawnManageEquipItem : MonoBehaviourEx,
    IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerClickHandler
{
    #region Linker
    [Linker("Spawn_Icon")]
    public Spawn _spawnIcon;
    #endregion Linker

    private DEquipment _equip;
    private bool _isDragging;
    private Vector3 _originalPos;
    private Transform _originalParent;
    private int _originalSiblingIndex;
    private Canvas _canvas;

    public DEquipment Equip => _equip;

    public event Action<PawnManageEquipItem, PointerEventData> OnDragBegin;
    public event Action<PawnManageEquipItem, PointerEventData> OnDragging;
    public event Action<PawnManageEquipItem, PointerEventData> OnDragEnd;
    public event Action<PawnManageEquipItem> OnRightClick;

    public void SetData(DEquipment equip)
    {
        _equip = equip;
        _spawnIcon.Get<EquipIcon>().SetData(equip);
    }

    public void OnPointerDown(PointerEventData e)
    {
        _originalPos = transform.position;
        _originalParent = transform.parent;
        _originalSiblingIndex = transform.GetSiblingIndex();
        _canvas = GetComponentInParent<Canvas>();
    }

    public void OnDrag(PointerEventData e)
    {
        if (!_isDragging)
        {
            _isDragging = true;
            transform.SetParent(_canvas.transform, true);
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
            transform.SetParent(_originalParent, true);
            transform.SetSiblingIndex(_originalSiblingIndex);
        }
        transform.position = _originalPos;
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Right)
            OnRightClick?.Invoke(this);
    }
}
