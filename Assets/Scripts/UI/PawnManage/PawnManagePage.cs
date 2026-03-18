using GameData;
using SA.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AssetPath("Prefabs/UI/PawnManage/PawnManagePage")]
public class PawnManagePage : PageView
{
    #region Linker
    [Linker("Root/Scroll_PawnList")]
    public ScrollRect _scrollPawnList;
    [Linker("Root/Scroll_EquipList")]
    public ScrollRect _scrollEquipList;

    [Linker("Root/Button_Close")]
    public Button _buttonClose;

    [Linker("Root/PawnStat")]
    public GameObject _goPawnStat;
    [Linker("Root/PawnStat/RawImage_Pawn")]
    public RawImage _rawPawn;
    [Linker("Root/PawnStat/Text_Name")]
    public Text _textName;
    [Linker("Root/PawnStat/Text_Class")]
    public Text _textClass;
    [Linker("Root/PawnStat/Text_HP")]
    public Text _textStatHP;
    [Linker("Root/PawnStat/Text_ATK")]
    public Text _textStatAtk;
    [Linker("Root/PawnStat/Text_DEF")]
    public Text _textStatDef;
    [Linker("Root/PawnStat/Text_SHIELD")]
    public Text _textStatShield;
    [Linker("Root/PawnStat/Text_MOVEMENT")]
    public Text _textStatMovement;
    
    [Linker("Root/EquipSlot")]
    public GameObject _goEquipSlot;
    [Linker("Root/EquipSlot/Slot_Weapon",
    "Root/EquipSlot/Slot_Armor",
    "Root/EquipSlot/Slot_Tool_1",
    "Root/EquipSlot/Slot_Tool_2")]
    public List<PawnManageEquipSlot> _equipSlotList = new List<PawnManageEquipSlot>();
    #endregion Linker

    private readonly List<PawnManageItem> _activePawnItems = new();
    private readonly List<PawnManageEquipItem> _activeEquipItems = new();

    private PawnManageItem _selectedPawnItem;

    public override void PreOpen()
    {
        _buttonClose.onClick.RemoveAllListeners();
        _buttonClose.onClick.AddListener(Close);

        foreach (var slot in _equipSlotList)
        {
            slot.OnRightClick -= OnSlotRightClick;
            slot.OnRightClick += OnSlotRightClick;
        }
    }

    public override void OnOpened()
    {
        _selectedPawnItem = null;
        _goPawnStat.SetActive(false);
        _goEquipSlot.SetActive(false);
        RefreshPawnList();
        RefreshEquipList();
        RefreshEquipSlots();
    }

    private void RefreshPawnList()
    {
        foreach (var item in _activePawnItems)
            if (item != null) Destroy(item.gameObject);
        _activePawnItems.Clear();

        var prefab = PrefabLoader.Load<PawnManageItem>();
        var pawns = PawnManager.Instance.Pawns;
        const float itemW = 130f;
        const float itemH = 200f;

        for (int i = 0; i < pawns.Count; i++)
        {
            var item = Instantiate(prefab, _scrollPawnList.content);
            var rt = item.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(itemW * i + itemW / 2f, -itemH / 2f);
            item.SetData(pawns[i]);
            item.OnSelected += OnPawnItemSelected;
            _activePawnItems.Add(item);
        }

        float viewW = _scrollPawnList.viewport.rect.width;
        _scrollPawnList.content.sizeDelta = new Vector2(
            Mathf.Max(0f, itemW * pawns.Count - viewW),
            _scrollPawnList.content.sizeDelta.y);
    }

    private void RefreshEquipList()
    {
        foreach (var item in _activeEquipItems)
            if (item != null) Destroy(item.gameObject);
        _activeEquipItems.Clear();

        var prefab = PrefabLoader.Load<PawnManageEquipItem>();
        var equips = ItemManager.Instance.Equips.Where(e => !e.IsEquipped).ToList();
        const int itemsPerRow = 4;
        const float itemSize = 100f;

        float slotW = _scrollEquipList.viewport.rect.width / itemsPerRow;

        for (int i = 0; i < equips.Count; i++)
        {
            int row = i / itemsPerRow;
            int col = i % itemsPerRow;
            var item = Instantiate(prefab, _scrollEquipList.content);
            var rt = item.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(
                col * slotW + slotW / 2f,
                -(row * itemSize + itemSize / 2f));
            item.SetData(equips[i]);
            item.OnDragEnd += OnEquipItemDragEnd;
            item.OnRightClick += OnEquipItemRightClick;
            _activeEquipItems.Add(item);
        }

        int rowCount = equips.Count > 0 ? Mathf.CeilToInt((float)equips.Count / itemsPerRow) : 0;
        _scrollEquipList.content.sizeDelta = new Vector2(
            _scrollEquipList.content.sizeDelta.x,
            itemSize * rowCount);
    }

    private void RefreshEquipSlots()
    {
        if (_selectedPawnItem == null)
        {
            foreach (var slot in _equipSlotList)
                slot.Clear();
            return;
        }

        var pawn = _selectedPawnItem.Pawn;
        var typeCounters = new Dictionary<EquipSlotType, int>();
        foreach (var slot in _equipSlotList)
        {
            var type = slot.SlotType;
            if (!typeCounters.TryGetValue(type, out int idx)) idx = 0;
            var match = pawn.Equips.Where(e => e.Data.Slot == type).ElementAtOrDefault(idx);
            if (match != null) slot.SetEquip(match);
            else slot.Clear();
            typeCounters[type] = idx + 1;
        }
    }

    private void OnPawnItemSelected(PawnManageItem item)
    {
        if (_selectedPawnItem == item) return;
        if (_selectedPawnItem != null) _selectedPawnItem.SetSelected(false);
        _selectedPawnItem = item;
        _selectedPawnItem.SetSelected(true);
        _goPawnStat.SetActive(true);
        _goEquipSlot.SetActive(true);
        RefreshEquipSlots();
        RefreshPawnStat();
    }

    private void RefreshPawnStat()
    {
        var pawn = _selectedPawnItem.Pawn;
        _rawPawn.texture = Resources.Load<Texture>(pawn.IconPath);
        _textName.text = pawn.CodeName;
        _textClass.text = TextManager.Instance.Get(pawn.Data.Name);
        _textStatHP.text = $"HP: {pawn.HP}";
        _textStatAtk.text = $"ATK: {pawn.Attack}";
        _textStatDef.text = $"DEF: {pawn.Armor}";
        _textStatShield.text = $"SHIELD: {pawn.Shield}";
        _textStatMovement.text = $"MOVEMENT: {pawn.Movement}";
    }

    private void OnEquipItemDragEnd(PawnManageEquipItem item, PointerEventData e)
    {
        if (_selectedPawnItem == null) return;
        foreach (var slot in _equipSlotList)
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(
                slot.GetComponent<RectTransform>(), e.position, null)) continue;
            if (slot.SlotType != item.Equip.Data.Slot) break;
            EquipToSlot(slot, item.Equip);
            break;
        }
    }

    private void OnEquipItemRightClick(PawnManageEquipItem item)
    {
        if (_selectedPawnItem == null) return;
        var matchingSlots = _equipSlotList.Where(s => s.SlotType == item.Equip.Data.Slot).ToList();
        if (matchingSlots.Count == 0) return;
        var emptySlot = matchingSlots.FirstOrDefault(s => s.IsEmpty);
        var target = emptySlot != null ? emptySlot : matchingSlots[0];
        EquipToSlot(target, item.Equip);
    }

    private void OnSlotRightClick(PawnManageEquipSlot slot)
    {
        if (_selectedPawnItem == null) return;
        slot.CurrentEquip.ClearEquippedPawn();
        _selectedPawnItem.Pawn.Unequip(slot.CurrentEquip);
        slot.Clear();
        RefreshEquipList();
        RefreshPawnStat();
    }

    private void EquipToSlot(PawnManageEquipSlot slot, DEquipment equip)
    {
        var pawn = _selectedPawnItem.Pawn;
        if (!slot.IsEmpty)
        {
            slot.CurrentEquip.ClearEquippedPawn();
            pawn.Unequip(slot.CurrentEquip);
        }
        pawn.Equip(equip);
        equip.SetEquippedPawn(pawn.InstanceId);
        slot.SetEquip(equip);
        RefreshEquipList();
        RefreshPawnStat();
    }
}
