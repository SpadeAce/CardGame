using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager : MonoSingleton<TurnManager>
{
    public enum TurnPhase { Pawn, Monster }

    public TurnPhase CurrentPhase { get; private set; }
    public bool IsPawnTurn    => CurrentPhase == TurnPhase.Pawn;
    public bool IsMonsterTurn => CurrentPhase == TurnPhase.Monster;

    public int CurrentTurn { get; private set; } = 0;
    public int TurnLimit   { get; private set; } = 0;

    public event Action<int> onTurnChanged;

    public const int SharedActingPowerPerTurn = 5;
    public int SharedActingPower { get; private set; }
    public event Action onSharedActingPowerChanged;

    public bool HasEnoughSharedActingPower(int cost) => SharedActingPower >= cost;

    public void ConsumeSharedActingPower(int amount)
    {
        SharedActingPower = Mathf.Max(0, SharedActingPower - amount);
        onSharedActingPowerChanged?.Invoke();
    }

    public void AddSharedActingPower(int amount)
    {
        SharedActingPower += amount;
        onSharedActingPowerChanged?.Invoke();
    }

    public void SetTurnLimit(int limit) => TurnLimit = limit;

    private readonly Dictionary<Actor, int> _remainingMovement = new Dictionary<Actor, int>();

    /// <summary>
    /// 게임 시작 — Pawn 턴으로 시작한다. 초기 핸드는 InitStage에서 세팅되므로 드로우 생략.
    /// </summary>
    public void StartGame()
    {
        CurrentTurn = 0;
        StartPawnTurn(drawCards: false);
    }

    /// <summary>
    /// Pawn 턴 시작: 이동 기록 초기화 후 Pawn 페이즈로 전환. 기본적으로 카드 1장 드로우.
    /// </summary>
    public void StartPawnTurn(bool drawCards = true)
    {
        CurrentTurn++;
        onTurnChanged?.Invoke(CurrentTurn);
        _remainingMovement.Clear();
        CurrentPhase = TurnPhase.Pawn;

        SharedActingPower = SharedActingPowerPerTurn;
        onSharedActingPowerChanged?.Invoke();

        foreach (var actor in StageManager.Instance.UserActors.Values)
        {
            if (actor != null && actor.Data is DPawn pawn)
                pawn.TickBuffs();
        }

        foreach (var actor in StageManager.Instance.EnemyActors.Values)
        {
            if (actor != null && actor.Data is DMonster mon)
                mon.TickBuffs();
        }

        if (drawCards)
        {
            DeckManager.Instance.StartTurnDraw();
            foreach (var actor in StageManager.Instance.UserActors.Values)
                if (actor?.Data is DPawn drawPawn)
                    drawPawn.DrawCards(DPawn.DrawPerTurn);
        }
    }

    /// <summary>
    /// Pawn 턴 종료 (UI 버튼 또는 외부 호출).
    /// Monster 턴 자동 실행 후 다음 Pawn 턴으로 전환된다.
    /// </summary>
    public void EndPawnTurn()
    {
        if (!IsPawnTurn) return;
        StageManager.Instance.Deselect();
        CurrentPhase = TurnPhase.Monster;
        StartCoroutine(MonsterTurnCoroutine());
    }

    /// <summary>
    /// 이동 후 잔여 이동력을 기록한다.
    /// </summary>
    public void OnActorMoved(Actor actor, int remaining) => _remainingMovement[actor] = remaining;

    /// <summary>
    /// 이미 이동한 Actor의 잔여 이동력을 버프/디버프 만큼 보정한다.
    /// 아직 이동하지 않은 Actor는 무시한다(GetEffectiveMovement가 pawn.Movement를 직접 읽음).
    /// </summary>
    public void AdjustRemainingMovement(Actor actor, int delta)
    {
        if (_remainingMovement.TryGetValue(actor, out int current))
            _remainingMovement[actor] = UnityEngine.Mathf.Max(0, current + delta);
    }

    /// <summary>
    /// 이번 턴에 이동한 적 없으면 -1, 이동했으면 잔여 이동력을 반환한다.
    /// </summary>
    public int GetRemainingMovement(Actor actor)
        => _remainingMovement.TryGetValue(actor, out int r) ? r : -1;

    /// <summary>
    /// 잔여 이동력이 0 이하(완전 소진)이면 true.
    /// </summary>
    public bool HasActorMoved(Actor actor)
        => _remainingMovement.TryGetValue(actor, out int r) && r <= 0;

    // ─────────────────────────────────────────────────────────────────────────
    // Monster Turn
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Monster 턴: Alert 상태 판정 후 각 몬스터가 순서대로 행동한다.
    /// 모든 행동 완료 후 Pawn 턴으로 전환된다.
    /// </summary>
    private IEnumerator MonsterTurnCoroutine()
    {
        var enemies = StageManager.Instance.EnemyActors.Values
            .Where(e => e != null && e.gameObject.activeInHierarchy).ToList();
        var users = StageManager.Instance.UserActors.Values
            .Where(u => u != null && u.gameObject.activeInHierarchy).ToList();

        bool isAlert = CheckAlertState(enemies, users);

        foreach (var monster in enemies)
            yield return StartCoroutine(MonsterActCoroutine(monster, users, isAlert));

        if (TurnLimit > 0 && CurrentTurn >= TurnLimit)
        {
            StageManager.Instance.OnTurnLimitExceeded();
            yield break;
        }

        StartPawnTurn();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Alert 판정
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 어느 한 몬스터의 Sight 범위 안에 Pawn이 1명이라도 있으면 전체 Alert.
    /// </summary>
    private bool CheckAlertState(List<Actor> enemies, List<Actor> users)
    {
        foreach (var enemy in enemies)
        {
            if (!(enemy.Data is DMonster mon)) continue;
            var tile = StageManager.Instance.GetTileForActor(enemy);
            if (tile == null) continue;
            foreach (var user in users)
            {
                var userTile = StageManager.Instance.GetTileForActor(user);
                if (userTile == null) continue;
                if (TileManager.Instance.GetDistance(tile.GridPosition, userTile.GridPosition) <= mon.Sight)
                    return true;
            }
        }
        return false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 몬스터 1개 행동
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator MonsterActCoroutine(Actor monster, List<Actor> users, bool isAlert)
    {
        if (monster == null || !monster.gameObject.activeInHierarchy) yield break;
        if (!(monster.Data is DMonster mon)) yield break;

        var currentTile = StageManager.Instance.GetTileForActor(monster);
        if (currentTile == null) yield break;

        if (isAlert && users.Count > 0)
        {
            Actor target = GetNearestActor(currentTile, users);
            if (target == null) yield break;
            var targetTile = StageManager.Instance.GetTileForActor(target);
            if (targetTile == null) yield break;

            // 이동 전 공격 시도
            if (mon.Range > 0)
            {
                int dist = TileManager.Instance.GetDistance(currentTile.GridPosition, targetTile.GridPosition);
                if (dist <= mon.Range)
                {
                    yield return StartCoroutine(ExecuteAttack(monster, target, mon.Attack));
                    yield break; // 공격 후 이동 불가
                }
            }

            // 목표 방향으로 이동
            if (mon.Movement > 0)
            {
                var bestTile = GetBestTileToward(currentTile, targetTile, mon.Movement);
                if (bestTile != null)
                {
                    var path = TileManager.Instance.GetPath(
                        currentTile.GridPosition, bestTile.GridPosition, mon.Movement, typeof(DMonster));
                    if (path != null)
                    {
                        var smoothed = TileManager.Instance.SmoothPath(
                            path, currentTile.GridPosition, typeof(DMonster));
                        bool done = false;
                        monster.MoveTo(smoothed, () => done = true);
                        yield return new WaitUntil(() => done);
                    }
                }

                // 이동 후 공격 시도
                if (mon.Range > 0)
                {
                    currentTile = StageManager.Instance.GetTileForActor(monster);
                    targetTile  = StageManager.Instance.GetTileForActor(target);
                    if (currentTile != null && targetTile != null)
                    {
                        int newDist = TileManager.Instance.GetDistance(
                            currentTile.GridPosition, targetTile.GridPosition);
                        if (newDist <= mon.Range)
                            yield return StartCoroutine(ExecuteAttack(monster, target, mon.Attack));
                    }
                }
            }
        }
        else
        {
            // 배회: 최대 2칸 랜덤 이동
            if (mon.Movement <= 0) yield break;
            int wanderRange = Mathf.Min(2, mon.Movement);
            var reachable = TileManager.Instance.GetReachableTiles(
                currentTile.GridPosition, wanderRange, typeof(DMonster));
            var candidates = reachable.Where(t => t != currentTile).ToList();
            if (candidates.Count == 0) yield break;

            var wanderTarget = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            var path = TileManager.Instance.GetPath(
                currentTile.GridPosition, wanderTarget.GridPosition, wanderRange, typeof(DMonster));
            if (path != null)
            {
                var smoothed = TileManager.Instance.SmoothPath(
                    path, currentTile.GridPosition, typeof(DMonster));
                bool done = false;
                monster.MoveTo(smoothed, () => done = true);
                yield return new WaitUntil(() => done);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 공격 실행
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 공격 애니메이션 재생 후 피해를 적용한다.
    /// </summary>
    private IEnumerator ExecuteAttack(Actor attacker, Actor target, int attackPower)
    {
        // 1. 공격 모션
        bool done = false;
        attacker.PerformAttack(target, () => done = true);
        yield return new WaitUntil(() => done);

        // 2. 피해 적용
        if (target.Data is DPawn pawn)
            pawn.TakeDamage(attackPower);

        // 3. GetHit 모션
        done = false;
        target.ReceiveHit(attacker, () => done = true);
        yield return new WaitUntil(() => done);

        // 4. 사망 처리
        bool isDead = (target.Data is DPawn p && p.IsDead)
                   || (target.Data is DMonster m && m.IsDead);
        if (isDead)
        {
            done = false;
            target.Die(() => done = true);
            yield return new WaitUntil(() => done);
            StageManager.Instance.CheckBattleResult();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 헬퍼
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// targets 중 from 타일에서 맨해튼 거리가 가장 가까운 Actor를 반환한다.
    /// </summary>
    private Actor GetNearestActor(SquareTile from, List<Actor> targets)
    {
        Actor nearest = null;
        int minDist = int.MaxValue;
        foreach (var t in targets)
        {
            if (t == null || !t.gameObject.activeInHierarchy) continue;
            var tile = StageManager.Instance.GetTileForActor(t);
            if (tile == null) continue;
            int dist = TileManager.Instance.GetDistance(from.GridPosition, tile.GridPosition);
            if (dist < minDist) { minDist = dist; nearest = t; }
        }
        return nearest;
    }

    /// <summary>
    /// movement 범위 내 도달 가능한 타일 중 target에 가장 가까운 타일을 반환한다.
    /// GetReachableTiles는 빈 타일만 반환하므로 별도 필터 불필요.
    /// </summary>
    private SquareTile GetBestTileToward(SquareTile from, SquareTile target, int movement)
    {
        var reachable = TileManager.Instance.GetReachableTiles(
            from.GridPosition, movement, typeof(DMonster));
        SquareTile best = null;
        int bestDist = int.MaxValue;
        foreach (var tile in reachable)
        {
            int dist = TileManager.Instance.GetDistance(tile.GridPosition, target.GridPosition);
            if (dist < bestDist) { bestDist = dist; best = tile; }
        }
        return best;
    }
}
