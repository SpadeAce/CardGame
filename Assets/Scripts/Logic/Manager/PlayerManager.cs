/// <summary>
/// 플레이어 상태(난이도 레벨, Gold)를 관리하는 매니저.
/// DontDestroyOnLoad 싱글톤으로 게임 전체에서 상태를 유지한다.
/// </summary>
public class PlayerManager : MonoSingleton<PlayerManager>, IResettable
{
    private int _difficultyLevel = 1;
    private int _gold = 1000;

    public int DifficultyLevel => _difficultyLevel;
    public int Gold => _gold;

    /// <summary>
    /// 스테이지 클리어 시 난이도 레벨을 1 증가시킨다.
    /// </summary>
    public void IncrementLevel()
    {
        _difficultyLevel++;
    }

    public bool SpendGold(int amount)
    {
        if (_gold < amount) return false;
        _gold -= amount;
        return true;
    }

    public void AddGold(int amount)
    {
        _gold += amount;
    }

    public void ResetAll()
    {
        _difficultyLevel = 1;
        _gold = 1000;
    }
}
