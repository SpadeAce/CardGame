using System.Collections.Generic;
using GameData;
using SA;

public class DMonster : DObject
{
    public int monsterId { get; private set; }
    public MonsterData Data { get; private set; }

    public int HP { get; private set; }
    public int Shield { get; private set; }
    public int Armor { get; private set; }
    public int Movement { get; private set; }

    public int Attack { get; private set; }
    public int Range { get; private set; }
    public int Sight { get; private set; }

    private readonly List<BuffEntry> _buffs = new List<BuffEntry>();
    public IReadOnlyList<BuffEntry> Buffs => _buffs;

    public DMonster(int monsterId)
    {
        this.monsterId = monsterId;
        this.Data = DataManager.Instance.Monster.Get(monsterId);
        UpdateStatus();
    }

    public DMonster(MonsterData data)
    {
        this.monsterId = data.Id;
        this.Data = data;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        HP = Data.Hp;
        Shield = Data.Shield;
        Armor = Data.Armor;
        Movement = Data.Movement;
        Attack = Data.Attack;
        Range = Data.Range;
        Sight = Data.Sight;
    }

    /// <summary>
    /// 피해 적용: attackPower - Armor = 실피해 → Shield 우선 차감 → HP 감소
    /// </summary>
    public void TakeDamage(int attackPower)
    {
        int net = UnityEngine.Mathf.Max(0, attackPower - Armor);
        if (net <= 0)
        {
            onFloatingText?.Invoke(FloatingTextType.Block, 0);
            return;
        }

        int absorbed = UnityEngine.Mathf.Min(Shield, net);
        Shield -= absorbed;
        net    -= absorbed;
        HP      = UnityEngine.Mathf.Max(0, HP - net);
        onFloatingText?.Invoke(FloatingTextType.Damage, net + absorbed);
        onStatsChanged?.Invoke();
    }

    public void Heal(int amount)
    {
        int before = HP;
        HP = UnityEngine.Mathf.Min(HP + amount, Data.Hp);
        int actual = HP - before;
        if (actual > 0)
            onFloatingText?.Invoke(FloatingTextType.Heal, actual);
        onStatsChanged?.Invoke();
    }

    public void AddShield(int amount)
    {
        Shield += amount;
        onFloatingText?.Invoke(FloatingTextType.Shield, amount);
        onStatsChanged?.Invoke();
    }

    public void ApplyBuff(CardEffectType type, int value, int duration)
    {
        _buffs.Add(new BuffEntry(type, value, duration));
        ApplyStatDelta(type, value);

        bool isDebuff = type == CardEffectType.DebuffAttack
                     || type == CardEffectType.DebuffArmor
                     || type == CardEffectType.DebuffMovement;
        onFloatingText?.Invoke(
            isDebuff ? FloatingTextType.Debuff : FloatingTextType.Buff,
            UnityEngine.Mathf.Abs(value));

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
                Attack = UnityEngine.Mathf.Max(0, Attack + delta); break;
            case CardEffectType.BuffArmor:
            case CardEffectType.DebuffArmor:
                Armor = UnityEngine.Mathf.Max(0, Armor + delta); break;
            case CardEffectType.BuffMovement:
            case CardEffectType.DebuffMovement:
                Movement = UnityEngine.Mathf.Max(0, Movement + delta); break;
        }
    }

    public bool IsDead => HP <= 0;

    public event System.Action onStatsChanged;
    public event System.Action<FloatingTextType, int> onFloatingText;
}
