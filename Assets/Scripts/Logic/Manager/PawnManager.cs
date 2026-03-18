using System.Collections.Generic;

public class PawnManager : MonoSingleton<PawnManager>, IResettable
{
    private readonly List<DPawn> _pawnList = new();
    public IReadOnlyList<DPawn> Pawns => _pawnList;

    public void AddPawn(DPawn pawn)
        => _pawnList.Add(pawn);

    public void ResetAll()
    {
        _pawnList.Clear();
    }
}
