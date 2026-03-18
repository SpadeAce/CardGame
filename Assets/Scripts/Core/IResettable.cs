/// <summary>
/// 타이틀 복귀 시 런타임 데이터를 초기 상태로 리셋하는 공통 인터페이스.
/// MonoSingleton 매니저에 구현하면 ReturnToTitle() 호출 시 자동으로 ResetAll()이 실행된다.
/// </summary>
public interface IResettable
{
    void ResetAll();
}
