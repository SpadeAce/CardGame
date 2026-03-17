using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class DeckSetting_Pawn : MonoBehaviourEx
{
    #region Links
    [Linker("ReadyGroup/PawnSlot_1",
        "ReadyGroup/PawnSlot_2",
        "ReadyGroup/PawnSlot_3",
        "ReadyGroup/PawnSlot_4",
        "ReadyGroup/PawnSlot_5")]
    public List<DeckPawnSlot> _readySlotList = new List<DeckPawnSlot>();
    [Linker("OwnPawnList")]
    public GameObject _ownPawnListRoot;
    #endregion Links

    private readonly List<DeckPawnItem> _activeItems = new();

    public void OnOpened()
    {
        InitSlots();
        RefreshOwnPawns();
    }

    private void InitSlots()
    {
        int openCount = DeckManager.Instance.OpenSlotCount;
        for (int i = 0; i < _readySlotList.Count; i++)
        {
            var slot = _readySlotList[i];
            bool locked = i >= openCount;
            slot.Init(i, locked);
            slot.OnRightClick -= OnSlotRightClick;
            slot.OnRightClick += OnSlotRightClick;
            slot.OnLockedClick -= OnLockedSlotClick;
            slot.OnLockedClick += OnLockedSlotClick;
            if (!locked)
                slot.SetData(DeckManager.Instance.GetDeckPawn(i));
        }
    }

    private void RefreshOwnPawns()
    {
        foreach (var item in _activeItems)
            if (item != null) Destroy(item.gameObject);
        _activeItems.Clear();

        var prefab = PrefabLoader.Load<DeckPawnItem>();

        var assignedPawns = new HashSet<DPawn>(DeckManager.Instance.DeckPawns);

        const float itemSpacing = 105f;
        int index = 0;
        foreach (var pawn in PawnManager.Instance.Pawns.Where(p => !assignedPawns.Contains(p)))
        {
            var item = Instantiate(prefab, _ownPawnListRoot.transform);
            item.GetComponent<RectTransform>().anchoredPosition = new Vector2(itemSpacing * index, 0f);
            item.SetData(pawn);
            item.OnDragBegin += OnItemDragBegin;
            item.OnDragEnd   += OnItemDragEnd;
            item.OnRightClick += OnItemRightClick;
            _activeItems.Add(item);
            index++;
        }
    }

    private void OnItemDragBegin(DeckPawnItem item, PointerEventData e)
    {
        item.transform.SetAsLastSibling();
    }

    private void OnItemDragEnd(DeckPawnItem item, PointerEventData e)
    {
        DeckPawnSlot target = GetSlotAtPosition(e.position);
        if (target != null && !target.IsLocked)
        {
            DeckManager.Instance.AssignPawnToDeck(item.Pawn, target.SlotIndex);
            target.SetData(item.Pawn);
            RefreshOwnPawns();
        }
    }

    private void OnItemRightClick(DeckPawnItem item)
    {
        int emptySlot = DeckManager.Instance.FindEmptySlotIndex();
        if (emptySlot < 0) return;
        DeckManager.Instance.AssignPawnToDeck(item.Pawn, emptySlot);
        _readySlotList[emptySlot].SetData(item.Pawn);
        RefreshOwnPawns();
    }

    private void OnLockedSlotClick(DeckPawnSlot slot)
    {
        const int upgradeCost = 100;
        var popup = UIManager.Instance.OpenView<NoticePopup>();
        popup.Init("슬롯 확장", $"{upgradeCost} 골드를 소모하여\n슬롯을 확장하시겠습니까?", () =>
        {
            if (!PlayerManager.Instance.SpendGold(upgradeCost)) return;
            DeckManager.Instance.ExpandSlot();
            InitSlots();
            RefreshOwnPawns();
        });
    }

    private void OnSlotRightClick(DeckPawnSlot slot)
    {
        DeckManager.Instance.UnassignPawnFromDeck(slot.SlotIndex);
        slot.Clear();
        RefreshOwnPawns();
    }

    private DeckPawnSlot GetSlotAtPosition(Vector2 screenPos)
    {
        foreach (var slot in _readySlotList)
        {
            var rt = slot.GetComponent<RectTransform>();
            if (RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, null))
                return slot;
        }
        return null;
    }
}
