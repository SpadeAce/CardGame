using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

public class LobbyManager : MonoSingleton<LobbyManager>, IResettable
{
    // ── Shop ──────────────────────────────────────────────
    private const int ShopMaxItemCount = 10;

    private int _shopLevel = 1;
    public int ShopLevel => _shopLevel;

    private List<CardData> _shopCards;
    public IReadOnlyList<CardData> ShopCards => _shopCards;

    public void GenerateShopList()
    {
        _shopCards = new List<CardData>();
        var shopData = DataManager.Instance.Shop.GetByLevel(_shopLevel);
        if (shopData == null) return;

        int count = Mathf.Min(shopData.CardId.Count, ShopMaxItemCount);
        for (int i = 0; i < count; i++)
        {
            var cardData = DataManager.Instance.Card.Get(shopData.CardId[i]);
            if (cardData != null) _shopCards.Add(cardData);
        }
    }

    public bool LevelUpShop()
    {
        int maxLevel = DataManager.Instance.Shop.GetMaxLevel();
        if (_shopLevel >= maxLevel) return false;
        _shopLevel++;
        GenerateShopList();
        return true;
    }

    public void RemoveShopCard(CardData card) => _shopCards?.Remove(card);

    // ── Recruit ───────────────────────────────────────────
    private const int RecruitSlotCount = 3;

    private List<DPawn> _recruitPawns;
    public IReadOnlyList<DPawn> RecruitPawns => _recruitPawns;

    public void GenerateRecruitList()
    {
        _recruitPawns = new List<DPawn>();
        var allPawns = DataManager.Instance.Pawn.GetAll().ToList();
        if (allPawns.Count == 0) return;

        for (int i = 0; i < RecruitSlotCount; i++)
        {
            var data = allPawns[Random.Range(0, allPawns.Count)];
            _recruitPawns.Add(new DPawn(data));
        }
    }

    public void RemoveRecruitPawn(DPawn pawn) => _recruitPawns?.Remove(pawn);

    // ── Stage 복귀 ────────────────────────────────────────────
    /// <summary>
    /// Stage 종료 후 호출. 다음 로비 진입 시 상점/영입 목록을 새로 생성하도록 초기화.
    /// </summary>
    public void ResetForNextStage()
    {
        _shopCards = null;
        _recruitPawns = null;
    }

    public void ResetAll()
    {
        _shopLevel = 1;
        _shopCards = null;
        _recruitPawns = null;
    }
}
