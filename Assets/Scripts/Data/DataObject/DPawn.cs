using System.Collections.Generic;
using System.Data;
using GameData;
using SA;
using UnityEngine;

public class DPawn : DObject
{
    public int pawnId {get;private set;}
    public PawnData Data {get;private set;}

    public int HP {get;private set;}
    public int Shield {get;private set;}
    public int Armor {get;private set;}
    public int Movement {get;private set;}
    
    public int Attack {get;private set;}
    public int Range {get;private set;}
    public int Sight {get;private set;}

    public int Ammo {get;private set;}

    private readonly List<BuffEntry> _buffs = new List<BuffEntry>();
    public IReadOnlyList<BuffEntry> Buffs => _buffs;

    public string CodeName {get; private set;}
    public string IconPath {get; private set;}

    public int Level { get; private set; } = 0;
    public int Exp   { get; private set; } = 0;

    private readonly List<(GameData.StatusType type, int value)> _growthBonuses = new();
    private readonly List<int> _growthCardIds = new();

    private readonly List<DEquipment> _equips = new List<DEquipment>();
    public IReadOnlyList<DEquipment> Equips => _equips;

    public PawnStats Stats { get; } = new PawnStats();

    public void Equip(DEquipment equip)
    {
        _equips.Add(equip);
        RecalculateStats();
    }

    public void Unequip(DEquipment equip)
    {
        _equips.Remove(equip);
        RecalculateStats();
    }

    private void RecalculateStats()
    {
        Stats.Recalculate(Data, _equips, _growthBonuses);
        UpdateStatus();
        onStatsChanged?.Invoke();
    }

    public const int InitialHandSize = 2;
    private const int BaseMaxHandSize = 3;
    public int MaxHandSize => BaseMaxHandSize + Stats.CardCap;
    public const int DrawPerTurn = 1;

    public List<DCard> PawnHandCards { get; } = new List<DCard>();
    public event System.Action OnPawnHandChanged;

    private readonly List<DCard> _pawnCardPool = new();
    private readonly List<DCard> _pawnDiscardPool = new();

    public void AddPawnHandCard(DCard card)
    {
        PawnHandCards.Add(card);
        OnPawnHandChanged?.Invoke();
    }

    public void RemovePawnHandCard(DCard card)
    {
        PawnHandCards.Remove(card);
        OnPawnHandChanged?.Invoke();
    }

    public void BuildCardPool()
    {
        _pawnCardPool.Clear();
        _pawnDiscardPool.Clear();
        PawnHandCards.Clear();
        foreach (var equip in _equips)
            foreach (var cardId in equip.Data.CardId)
                _pawnCardPool.Add(new DCard(cardId));
        foreach (var cardId in _growthCardIds)
            _pawnCardPool.Add(new DCard(cardId));
        ShufflePawnPool(_pawnCardPool);
    }

    public void InitHand() => DrawCards(InitialHandSize);

    public void DrawCards(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (PawnHandCards.Count >= MaxHandSize) break;
            if (_pawnCardPool.Count == 0)
            {
                if (_pawnDiscardPool.Count == 0) break;
                RefillPawnFromDiscard();
            }
            var card = _pawnCardPool[_pawnCardPool.Count - 1];
            _pawnCardPool.RemoveAt(_pawnCardPool.Count - 1);
            AddPawnHandCard(card);
        }
    }

    public void UsePawnCard(DCard card)
    {
        RemovePawnHandCard(card);
        var type = card.Data.Type;
        if (type == GameData.CardType.Supply || type == GameData.CardType.Consume) return;
        _pawnDiscardPool.Add(card);
    }

    private void RefillPawnFromDiscard()
    {
        _pawnCardPool.AddRange(_pawnDiscardPool);
        _pawnDiscardPool.Clear();
        ShufflePawnPool(_pawnCardPool);
    }

    private void ShufflePawnPool(List<DCard> pool)
    {
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
    }

    public DPawn(int pawnId)
    {
        this.pawnId = pawnId;
        this.Data = DataManager.Instance.Pawn.Get(pawnId);
        string nameAlias = DataManager.Instance.NamePreset.GetRandomName(); 
        CodeName = TextManager.Instance.Get(nameAlias);
        IconPath = $"Textures/Icon/Pawn/Pawn_Female";
        Stats.Recalculate(Data, _equips, _growthBonuses);
        UpdateStatus();
    }

    public DPawn(PawnData data)
    {
        this.pawnId = data.Id;
        this.Data = data;
        string nameAlias = DataManager.Instance.NamePreset.GetRandomName(); 
        CodeName = TextManager.Instance.Get(nameAlias);
        IconPath = $"Textures/Icon/Pawn/Pawn_Female";
        Stats.Recalculate(Data, _equips, _growthBonuses);
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        HP       = Stats.Hp;
        Shield   = Stats.Shield;
        Armor    = Stats.Armor;
        Movement = Stats.Movement;
        Attack   = Stats.Attack;
        Range    = Stats.Range;
        Sight    = Stats.Sight;
        Ammo     = Stats.Ammo;
    }

    /// <summary>
    /// 피해 적용: attackPower - Armor = 실피해 → Shield 우선 차감 → HP 감소
    /// </summary>
    public void TakeDamage(int attackPower)
    {
        int net = Mathf.Max(0, attackPower - Armor);
        if (net <= 0) return;

        int absorbed = Mathf.Min(Shield, net);
        Shield -= absorbed;
        net    -= absorbed;
        HP      = Mathf.Max(0, HP - net);
        onStatsChanged?.Invoke();
    }

    public void Heal(int amount)
    {
        HP = Mathf.Min(HP + amount, Data.Hp);
        onStatsChanged?.Invoke();
    }

    public void AddShield(int amount)
    {
        Shield += amount;
        onStatsChanged?.Invoke();
    }

    public bool HasEnoughAmmo(int cost) => Ammo >= cost;

    public void ConsumeAmmo(int amount)
    {
        Ammo = Mathf.Max(0, Ammo - amount);
        onStatsChanged?.Invoke();
    }

    public void RestoreAmmo()
    {
        Ammo = Data.ActingPower;
        onStatsChanged?.Invoke();
    }

    public void AddAmmo(int amount)
    {
        Ammo += amount;
        onStatsChanged?.Invoke();
    }

    public void ApplyBuff(CardEffectType type, int value, int duration)
    {
        _buffs.Add(new BuffEntry(type, value, duration));
        ApplyStatDelta(type, value);
        onStatsChanged?.Invoke();
    }

    public void TickBuffs()
    {
        bool changed = false;
        for (int i = _buffs.Count - 1; i >= 0; i--)
        {
            var buff = _buffs[i];
            int newRemaining = buff.RemainingTurns - 1;
            if (newRemaining <= 0)
            {
                ApplyStatDelta(buff.Type, -buff.Value);
                _buffs.RemoveAt(i);
                changed = true;
            }
            else
            {
                _buffs[i] = new BuffEntry(buff.Type, buff.Value, newRemaining);
            }
        }
        if (changed) onStatsChanged?.Invoke();
    }

    private void ApplyStatDelta(CardEffectType type, int delta)
    {
        switch (type)
        {
            case CardEffectType.BuffAttack:
            case CardEffectType.DebuffAttack:
                Attack = Mathf.Max(0, Attack + delta); break;
            case CardEffectType.BuffArmor:
            case CardEffectType.DebuffArmor:
                Armor = Mathf.Max(0, Armor + delta); break;
            case CardEffectType.BuffMovement:
            case CardEffectType.DebuffMovement:
                Movement = Mathf.Max(0, Movement + delta); break;
        }
    }

    public void AddExp(int amount)
    {
        Exp += amount;
        TryLevelUp();
    }

    private void TryLevelUp()
    {
        while (true)
        {
            var nextData = DataManager.Instance.PawnGrowth
                .GetByClassLevel(Data.ClassType, Level + 1);
            if (nextData == null || Exp < nextData.RequireExp) break;

            Level++;
            var types  = nextData.StatusType;
            var values = nextData.StatusValue;
            for (int i = 0; i < types.Count; i++)
                _growthBonuses.Add((types[i], values[i]));
            foreach (var cardId in nextData.CardId)
                _growthCardIds.Add(cardId);
            RecalculateStats();
        }
    }

    public bool IsDead => HP <= 0;

    /// <summary>
    /// Stage 종료 후 호출. 버프/디버프를 모두 제거하고 기본 스탯으로 복원.
    /// </summary>
    public void ResetStats()
    {
        _buffs.Clear();
        UpdateStatus();
        onStatsChanged?.Invoke();
    }

    public event System.Action onStatsChanged;
}
