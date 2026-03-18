using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DeckManager : MonoSingleton<DeckManager>, IResettable
{
    // ── 덱 편성 슬롯 (null = 빈 슬롯) ────────────────────────
    private const int DeckSlotCount = 5;
    private DPawn[] _deckPawnSlots = new DPawn[DeckSlotCount];

    private int _openSlotCount = 2;
    public int OpenSlotCount => _openSlotCount;

    public bool ExpandSlot()
    {
        if (_openSlotCount >= DeckSlotCount) return false;
        _openSlotCount++;
        return true;
    }

    // Stage용 — null 제외 편성 목록
    public IEnumerable<DPawn> DeckPawns => _deckPawnSlots.Where(p => p != null);

    public bool AssignPawnToDeck(DPawn pawn, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= DeckSlotCount) return false;
        if (slotIndex >= _openSlotCount) return false;
        _deckPawnSlots[slotIndex] = pawn;
        return true;
    }

    public void UnassignPawnFromDeck(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < DeckSlotCount)
            _deckPawnSlots[slotIndex] = null;
    }

    public int FindEmptySlotIndex()
    {
        for (int i = 0; i < _openSlotCount; i++)
            if (_deckPawnSlots[i] == null) return i;
        return -1;
    }

    public DPawn GetDeckPawn(int slotIndex)
        => (slotIndex >= 0 && slotIndex < DeckSlotCount) ? _deckPawnSlots[slotIndex] : null;

    // ── 카드 덱 ───────────────────────────────────────────────
    private readonly List<DCard> _deckCardList = new();
    private readonly List<DCard> _cardPool = new();
    private readonly List<DCard> _discardPool = new();

    public void AddCard(DCard card)
    {
        _deckCardList.Add(card);
    }

    public List<DCard> handCardList = new List<DCard>();

    public event System.Action onHandChanged;

    private const int InitialHandSize = 5;
    private const int MaxHandSize = 10;
    private const int DrawPerTurn = 1;

    public void InitTestData()
    {
        AddCard(new DCard(10001));
        AddCard(new DCard(10001));
        AddCard(new DCard(10001));
        AddCard(new DCard(10004));
        AddCard(new DCard(10004));
        AddCard(new DCard(10004));

        PawnManager.Instance.AddPawn(new DPawn(1));
        PawnManager.Instance.AddPawn(new DPawn(2));

        ItemManager.Instance.AddEquip(new DEquipment(10001));
        ItemManager.Instance.AddEquip(new DEquipment(10002));
        ItemManager.Instance.AddEquip(new DEquipment(10003));
        ItemManager.Instance.AddEquip(new DEquipment(10004));
        ItemManager.Instance.AddEquip(new DEquipment(30001));
        ItemManager.Instance.AddEquip(new DEquipment(30002));
        ItemManager.Instance.AddEquip(new DEquipment(30003));
        ItemManager.Instance.AddEquip(new DEquipment(50101));
        ItemManager.Instance.AddEquip(new DEquipment(50201));
        ItemManager.Instance.AddEquip(new DEquipment(50301));
        ItemManager.Instance.AddEquip(new DEquipment(60101));
    }

    /// <summary>
    /// Stage 종료 후 호출. 덱의 모든 Pawn 스탯을 기본값으로 초기화.
    /// </summary>
    public void ResetPawnStats()
    {
        foreach (var pawn in DeckPawns)
            pawn.ResetStats();
    }

    /// <summary>
    /// Stage 진입 시 호출. _deckCardList를 셔플하여 드로우 풀 생성 후 초기 핸드 세팅.
    /// </summary>
    public void InitStage()
    {
        _cardPool.Clear();
        _discardPool.Clear();
        handCardList.Clear();

        _cardPool.AddRange(_deckCardList);
        ShufflePool(_cardPool);

        DrawCards(InitialHandSize);
    }

    /// <summary>
    /// 매 Pawn 턴 시작 시 호출. 드로우 풀에서 카드를 1장 드로우.
    /// </summary>
    public void StartTurnDraw()
    {
        DrawCards(DrawPerTurn);
    }

    /// <summary>
    /// 카드 사용. Supply/Consume 타입은 소멸, 그 외는 소모풀로 반환.
    /// </summary>
    public void UseCard(DCard card)
    {
        handCardList.Remove(card);

        var type = card.Data.Type;
        if (type == GameData.CardType.Supply || type == GameData.CardType.Consume)
        {
            onHandChanged?.Invoke();
            return;
        }

        _discardPool.Add(card);
        onHandChanged?.Invoke();
    }

    public void DrawCards(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (handCardList.Count >= MaxHandSize) break;

            if (_cardPool.Count == 0)
            {
                if (_discardPool.Count == 0) break;
                RefillFromDiscard();
            }

            var card = _cardPool[_cardPool.Count - 1];
            _cardPool.RemoveAt(_cardPool.Count - 1);
            handCardList.Add(card);
        }
        onHandChanged?.Invoke();
    }

    private void RefillFromDiscard()
    {
        _cardPool.AddRange(_discardPool);
        _discardPool.Clear();
        ShufflePool(_cardPool);
    }

    public void ResetAll()
    {
        for (int i = 0; i < _deckPawnSlots.Length; i++)
            _deckPawnSlots[i] = null;
        _openSlotCount = 2;

        _deckCardList.Clear();
        _cardPool.Clear();
        _discardPool.Clear();
        handCardList.Clear();

        onHandChanged = null;
    }

    private void ShufflePool(List<DCard> pool)
    {
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = pool[i];
            pool[i] = pool[j];
            pool[j] = temp;
        }
    }
}
