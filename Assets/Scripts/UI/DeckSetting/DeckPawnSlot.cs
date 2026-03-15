using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class DeckPawnSlot : MonoBehaviourEx, IPointerClickHandler
{
    #region Links
    [Linker("Image_Empty")]
    public GameObject _goEmpty;
    [Linker("Image_Lock")]
    public GameObject _goLock;
    [Linker("Spawn_Icon")]
    public Spawn _spawnIcon;
    #endregion Links

    private DPawn _pawn;
    private bool _isLocked;
    private int _slotIndex;

    public DPawn Pawn => _pawn;
    public bool IsEmpty => _pawn == null;
    public bool IsLocked => _isLocked;
    public int SlotIndex => _slotIndex;

    public event Action<DeckPawnSlot> OnRightClick;

    public void Init(int slotIndex, bool isLocked)
    {
        _slotIndex = slotIndex;
        _isLocked = isLocked;
        _goLock.SetActive(isLocked);
        _goEmpty.SetActive(!isLocked);
        _pawn = null;
    }

    public void SetData(DPawn pawn)
    {
        _pawn = pawn;
        _goEmpty.SetActive(pawn == null);
        _spawnIcon.gameObject.SetActive(pawn != null);
        if (pawn != null)
            _spawnIcon.Get<PawnIcon>().SetData(pawn);
    }

    public void Clear()
    {
        _pawn = null;
        _goEmpty.SetActive(true);
        _spawnIcon.gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Right && !_isLocked && !IsEmpty)
            OnRightClick?.Invoke(this);
    }
}
