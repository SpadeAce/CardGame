using System.Collections.Generic;
using GameData;

/// <summary>
/// Pawn의 기저 스탯을 합산하는 컴포넌트.
/// PawnData(기본값) + 장비 보너스를 합산하여 최종 기저 스탯을 제공한다.
/// 런타임 변동(피해, 버프 등)은 DPawn이 직접 관리한다.
/// </summary>
public class PawnStats
{
    public int Hp       { get; private set; }
    public int Attack   { get; private set; }
    public int Armor    { get; private set; }
    public int Shield   { get; private set; }
    public int Movement { get; private set; }
    public int Range    { get; private set; }
    public int Sight    { get; private set; }
    public int Ammo     { get; private set; }
    public int CardCap  { get; private set; }
    public int Accuracy { get; private set; }

    public void Recalculate(PawnData data, IReadOnlyList<DEquipment> equips,
        IReadOnlyList<(GameData.StatusType type, int value)> growthBonuses = null)
    {
        Hp       = data.Hp;
        Attack   = data.Attack;
        Armor    = data.Armor;
        Shield   = data.Shield;
        Movement = data.Movement;
        Range    = data.Range;
        Sight    = data.Sight;
        Ammo     = data.Ammo;
        CardCap  = 0;
        Accuracy = data.Accuracy;

        foreach (var equip in equips)
        {
            var types  = equip.Data.StatusType;
            var values = equip.Data.StatusValue;
            for (int i = 0; i < types.Count; i++)
            {
                switch (types[i])
                {
                    case StatusType.StatAtk:      Attack   += values[i]; break;
                    case StatusType.StatDef:      Armor    += values[i]; break;
                    case StatusType.StatHp:       Hp       += values[i]; break;
                    case StatusType.StatShield:   Shield   += values[i]; break;
                    case StatusType.StatMovement: Movement += values[i]; break;
                    case StatusType.StatRange:    Range    += values[i]; break;
                    case StatusType.StatCardCap:  CardCap  += values[i]; break;
                    case StatusType.StatAccuracy: Accuracy += values[i]; break;
                    case StatusType.StatAmmoCap:  Ammo     += values[i]; break;
                }
            }
        }

        if (growthBonuses != null)
        {
            foreach (var (type, value) in growthBonuses)
            {
                switch (type)
                {
                    case StatusType.StatAtk:      Attack   += value; break;
                    case StatusType.StatDef:      Armor    += value; break;
                    case StatusType.StatHp:       Hp       += value; break;
                    case StatusType.StatShield:   Shield   += value; break;
                    case StatusType.StatMovement: Movement += value; break;
                    case StatusType.StatRange:    Range    += value; break;
                    case StatusType.StatCardCap:  CardCap  += value; break;
                    case StatusType.StatAccuracy: Accuracy += value; break;
                    case StatusType.StatAmmoCap:  Ammo     += value; break;
                }
            }
        }
    }
}
