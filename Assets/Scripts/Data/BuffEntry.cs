using GameData;

public struct BuffEntry
{
    public CardEffectType Type;
    public int Value;           // 적용된 델타 (+버프, -디버프)
    public int RemainingTurns;

    public BuffEntry(CardEffectType type, int value, int remainingTurns)
    {
        Type = type;
        Value = value;
        RemainingTurns = remainingTurns;
    }
}
