using System;
using GameData;
using UnityEngine;
using UnityEngine.EventSystems;

public class PawnManageEquipSlot : MonoBehaviourEx, IPointerClickHandler
{
    #region Links
    [Linker("Image_Empty")]
    public GameObject _goEmpty;
    [Linker("Image_Lock")]
    public GameObject _goLock;
    [Linker("Spawn_Icon")]
    public Spawn _spawnIcon;
    #endregion Links

    public EquipSlotType _slotType;

    private DEquipment _equip;

    public EquipSlotType SlotType => _slotType;
    public bool IsEmpty => _equip == null;
    public DEquipment CurrentEquip => _equip;

    public event Action<PawnManageEquipSlot> OnRightClick;

    public void SetEquip(DEquipment equip)
    {
        _equip = equip;
        _goEmpty.SetActive(false);
        _spawnIcon.gameObject.SetActive(true);
        _spawnIcon.Get<EquipIcon>().SetData(equip);
    }

    public void Clear()
    {
        _equip = null;
        _goEmpty.SetActive(true);
        _spawnIcon.gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Right && !IsEmpty)
            OnRightClick?.Invoke(this);
    }
}
