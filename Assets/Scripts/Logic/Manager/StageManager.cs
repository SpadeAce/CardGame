using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SA;
using GameData;

public class StageManager : MonoSingleton<StageManager>, IResettable
{
    Dictionary<int, Actor> _dicUserActor = new Dictionary<int, Actor>();
    Dictionary<int, Actor> _dicEnemyActor = new Dictionary<int, Actor>();
    HashSet<SquareTile> _reachableTiles = new HashSet<SquareTile>();
    HashSet<SquareTile> _cardRangeTiles  = new HashSet<SquareTile>();
    HashSet<SquareTile> _radiusPreviewTiles = new HashSet<SquareTile>();

    private bool _isBusy;
    public bool IsBusy => _isBusy;

    public SquareTile SelectedTile { get; private set; }
    public DObject SelectedObject { get; private set; }

    private int _stageGoldReward;
    public int StageGoldReward => _stageGoldReward;

    private int _stageExpReward;
    public int StageExpReward => _stageExpReward;

    public IReadOnlyDictionary<int, Actor> UserActors  => _dicUserActor;
    public IReadOnlyDictionary<int, Actor> EnemyActors => _dicEnemyActor;

    public SquareTile GetTileForActor(Actor actor)
        => actor?.Slot?.GetComponentInParent<SquareTile>();

    public void SelectTile(SquareTile tile)
    {
        // Case 1: 같은 타일 재클릭 → 선택 해제
        if (SelectedTile == tile)
        {
            Deselect();
            return;
        }

        // Case 2: Actor가 선택된 상태에서 다른 타일 클릭
        Actor selectedActor = GetActorOnTile(SelectedTile);
        if (selectedActor != null)
        {
            bool canMove = TurnManager.Instance.IsPawnTurn
                           && selectedActor.Data is DPawn
                           && GetEffectiveMovement(selectedActor) > 0;

            if (IsEmptyTile(tile) && _reachableTiles.Contains(tile) && canMove)
            {
                int effective = GetEffectiveMovement(selectedActor);
                var path = TileManager.Instance.GetPath(
                    SelectedTile.GridPosition,
                    tile.GridPosition,
                    effective,
                    selectedActor.Data?.GetType());
                if (path != null)
                {
                    int remaining = effective - path.Count;
                    var smoothed = TileManager.Instance.SmoothPath(
                        path, SelectedTile.GridPosition, selectedActor.Data?.GetType());
                    selectedActor.MoveTo(smoothed, () =>
                    {
                        TurnManager.Instance.OnActorMoved(selectedActor, remaining);
                        SquareTile arrivedTile = GetTileForActor(selectedActor);
                        if (arrivedTile != null)
                        {
                            SelectedTile = arrivedTile;
                            SelectedObject = selectedActor.Data;
                            arrivedTile.SetSelected(true);
                            onSelectedChanged?.Invoke();
                            if (remaining > 0)
                                ShowMovementRange(arrivedTile.GridPosition, remaining, selectedActor.Data?.GetType());
                        }
                    });
                }
                Deselect();
                return;
            }

            Deselect();
            if (GetActorOnTile(tile) == null) return;
            // 액터가 있는 타일이면 Case 3으로 fall-through하여 즉시 선택 전환
        }

        // Case 3: 새 타일 선택
        if (SelectedTile != null)
            SelectedTile.SetSelected(false);

        SelectedTile = tile;
        SelectedObject = (tile.Slot != null && tile.Slot.SlotEntity != null)
            ? tile.Slot.SlotEntity.Data
            : null;
        tile.SetSelected(true);
        onSelectedChanged?.Invoke();

        Actor newActor = GetActorOnTile(tile);
        if (newActor != null)
        {
            int effectiveMovement = GetEffectiveMovement(newActor);
            bool canShowRange = TurnManager.Instance.IsPawnTurn
                                && newActor.Data is DPawn
                                && effectiveMovement > 0;
            if (canShowRange)
                ShowMovementRange(tile.GridPosition, effectiveMovement, newActor.Data?.GetType());
        }
    }

    public void Deselect()
    {
        ClearMovementHighlights();

        if (SelectedTile != null)
            SelectedTile.SetSelected(false);
        SelectedTile = null;
        SelectedObject = null;
        onSelectedChanged?.Invoke();
    }

    private void ShowMovementRange(Vector2Int pos, int movement, System.Type friendlyDataType = null)
    {
        _reachableTiles = TileManager.Instance.GetReachableTiles(pos, movement, friendlyDataType);
        foreach (var t in _reachableTiles)
            t.SetHighlighted(true);
    }

    public void ClearMovementHighlights()
    {
        foreach (var t in _reachableTiles)
            t.SetHighlighted(false);
        _reachableTiles.Clear();
    }

    public void RestoreMovementRange()
    {
        if (SelectedTile == null) return;
        Actor actor = GetActorOnTile(SelectedTile);
        if (actor == null) return;
        int effectiveMovement = GetEffectiveMovement(actor);
        bool canShowRange = TurnManager.Instance.IsPawnTurn
                            && actor.Data is DPawn
                            && effectiveMovement > 0;
        if (canShowRange)
            ShowMovementRange(SelectedTile.GridPosition, effectiveMovement, actor.Data?.GetType());
    }

    public void RefreshMovementRange()
    {
        ClearMovementHighlights();
        RestoreMovementRange();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 카드 사용
    // ─────────────────────────────────────────────────────────────────────────

    public int GetCardUseRange(DCard card, Actor caster)
    {
        int pawnRange = caster.Data is DPawn pawn ? pawn.Data.Range : 0;
        return card.Data.Range + pawnRange;
    }

    public void ShowCardRange(SquareTile casterTile, DCard card, Actor caster)
    {
        ClearCardRange();
        int range = GetCardUseRange(card, caster);
        Vector2Int origin = casterTile.GridPosition;
        for (int dx = -range; dx <= range; dx++)
        {
            int remainY = range - Mathf.Abs(dx);
            for (int dy = -remainY; dy <= remainY; dy++)
            {
                SquareTile tile = TileManager.Instance.GetTile(new Vector2Int(origin.x + dx, origin.y + dy));
                if (tile == null || !tile.IsWalkable) continue;
                tile.SetTarget(true);
                _cardRangeTiles.Add(tile);
            }
        }
    }

    public void ClearCardRange()
    {
        foreach (var tile in _cardRangeTiles)
            tile.SetTarget(false);
        _cardRangeTiles.Clear();
    }

    public bool IsInCardRange(SquareTile tile) => _cardRangeTiles.Contains(tile);

    public void ShowRadiusPreview(SquareTile centerTile, DCard card)
    {
        ClearRadiusPreview();
        int radius = card.Data.Radius;
        Vector2Int center = centerTile.GridPosition;
        for (int dx = -radius; dx <= radius; dx++)
        {
            int remainY = radius - Mathf.Abs(dx);
            for (int dy = -remainY; dy <= remainY; dy++)
            {
                SquareTile tile = TileManager.Instance.GetTile(new Vector2Int(center.x + dx, center.y + dy));
                if (tile == null) continue;
                tile.SetRadius(true);
                _radiusPreviewTiles.Add(tile);
            }
        }
    }

    public void ClearRadiusPreview()
    {
        foreach (var t in _radiusPreviewTiles) t.SetRadius(false);
        _radiusPreviewTiles.Clear();
    }

    private bool CanAffordCard(DCard card, DPawn pawn)
        => TurnManager.Instance.HasEnoughSharedActingPower(card.Data.EnergyCost)
        && pawn.HasEnoughAmmo(card.Data.AmmoCost);

    private void SpendCardCost(DCard card, DPawn pawn)
    {
        TurnManager.Instance.ConsumeSharedActingPower(card.Data.EnergyCost);
        pawn.ConsumeAmmo(card.Data.AmmoCost);
    }

    public void UseCardSelf(DCard card, Actor caster, DPawn cardOwner = null)
    {
        if (caster.Data is DPawn selfPawn && !CanAffordCard(card, selfPawn))
            return;

        if (caster.Data is DPawn p) SpendCardCost(card, p);
        if (cardOwner != null) cardOwner.UsePawnCard(card);
        else DeckManager.Instance.UseCard(card);
        _isBusy = true;
        StartCoroutine(ApplyAndUnlock(card, caster, caster));
    }

    public bool TryUseCard(DCard card, Actor caster, SquareTile targetTile, DPawn cardOwner = null)
    {
        SquareTile casterTile = GetTileForActor(caster);
        if (casterTile == null) return false;

        Vector2Int casterPos = casterTile.GridPosition;
        Vector2Int targetPos = targetTile.GridPosition;
        int dist = Mathf.Abs(casterPos.x - targetPos.x) + Mathf.Abs(casterPos.y - targetPos.y);
        if (dist > GetCardUseRange(card, caster)) return false;

        if (caster.Data is DPawn costPawn && !CanAffordCard(card, costPawn))
            return false;

        if (card.Data.Target == TargetType.Enemy)
        {
            if (targetTile.Slot?.SlotEntity == null) return false;
            Actor targetActor = targetTile.Slot.SlotEntity as Actor;
            if (targetActor == null || !(targetActor.Data is DMonster)) return false;
            if (caster.Data is DPawn ep) SpendCardCost(card, ep);
            if (cardOwner != null) cardOwner.UsePawnCard(card);
            else DeckManager.Instance.UseCard(card);
            _isBusy = true;
            StartCoroutine(ApplyWithRadiusAndUnlock(card, caster, targetTile));
            return true;
        }
        else if (card.Data.Target == TargetType.Ground)
        {
            if (!targetTile.IsWalkable) return false;
            if (caster.Data is DPawn gp) SpendCardCost(card, gp);
            if (cardOwner != null) cardOwner.UsePawnCard(card);
            else DeckManager.Instance.UseCard(card);
            _isBusy = true;
            StartCoroutine(ApplyWithRadiusAndUnlock(card, caster, targetTile));
            return true;
        }
        else if (card.Data.Target == TargetType.Ally)
        {
            if (targetTile.Slot?.SlotEntity == null) return false;
            Actor targetActor = targetTile.Slot.SlotEntity as Actor;
            if (targetActor == null || !(targetActor.Data is DPawn)) return false;
            if (caster.Data is DPawn ap) SpendCardCost(card, ap);
            if (cardOwner != null) cardOwner.UsePawnCard(card);
            else DeckManager.Instance.UseCard(card);
            _isBusy = true;
            StartCoroutine(ApplyWithRadiusAndUnlock(card, caster, targetTile));
            return true;
        }
        return false;
    }

    private IEnumerator ApplyAndUnlock(DCard card, Actor caster, Actor targetActor)
    {
        yield return ApplyCardEffectsCoroutine(card, caster, targetActor);
        _isBusy = false;
    }

    private IEnumerator ApplyWithRadiusAndUnlock(DCard card, Actor caster, SquareTile centerTile)
    {
        var targets = GetRadiusTargets(centerTile, card);
        foreach (var target in targets)
            yield return ApplyCardEffectsCoroutine(card, caster, target);
        _isBusy = false;
    }

    private List<Actor> GetRadiusTargets(SquareTile centerTile, DCard card)
    {
        var results = new List<Actor>();
        int radius = card.Data.Radius;
        Vector2Int center = centerTile.GridPosition;

        for (int dx = -radius; dx <= radius; dx++)
        {
            int remainY = radius - Mathf.Abs(dx);
            for (int dy = -remainY; dy <= remainY; dy++)
            {
                SquareTile tile = TileManager.Instance.GetTile(new Vector2Int(center.x + dx, center.y + dy));
                if (tile == null) continue;

                Actor actor = tile.Slot?.SlotEntity as Actor;

                switch (card.Data.Target)
                {
                    case TargetType.Enemy:
                        if (actor?.Data is DMonster) results.Add(actor);
                        break;
                    case TargetType.Ally:
                        if (actor?.Data is DPawn) results.Add(actor);
                        break;
                    case TargetType.Ground:
                        if (tile.IsWalkable) results.Add(actor); // actor가 null이어도 포함 (빈 땅 효과)
                        break;
                }
            }
        }
        return results;
    }

    private IEnumerator ApplyCardEffectsCoroutine(DCard card, Actor caster, Actor targetActor)
    {
        DObject targetData = targetActor?.Data;
        for (int i = 0; i < card.Data.EffectId.Count; i++)
        {
            var effect = DataManager.Instance.CardEffect.Get(card.Data.EffectId[i]);
            if (effect == null) continue;
            int value = i < card.Data.EffectValue.Count ? card.Data.EffectValue[i] : 0;
            switch (effect.EffectType)
            {
                case CardEffectType.Damage:
                    if (targetActor != null && targetData is DMonster monster)
                    {
                        bool attackDone = false;
                        caster.PerformAttack(targetActor, () => attackDone = true);
                        yield return new WaitForSeconds(0.3f);

                        monster.TakeDamage(value);

                        bool hitDone = false;
                        targetActor.ReceiveHit(caster, () => hitDone = true);

                        yield return new WaitUntil(() => attackDone);
                        yield return new WaitUntil(() => hitDone);

                        if (monster.IsDead)
                        {
                            _stageGoldReward += monster.Data.Gold;
                            _stageExpReward  += monster.Data.Exp;
                            bool dieDone = false;
                            targetActor.Die(() => dieDone = true);
                            yield return new WaitUntil(() => dieDone);
                            CheckBattleResult();
                            RefreshMovementRange();
                        }
                    }
                    else if (targetData is DPawn pawn) pawn.TakeDamage(value);
                    break;
                case CardEffectType.Heal:
                    if      (targetData is DPawn p)   p.Heal(value);
                    else if (targetData is DMonster m) m.Heal(value);
                    break;
                case CardEffectType.Shield:
                    if      (targetData is DPawn ps)   ps.AddShield(value);
                    else if (targetData is DMonster ms) ms.AddShield(value);
                    break;
                case CardEffectType.DrawCard:
                    DeckManager.Instance.DrawCards(value);
                    break;
                case CardEffectType.RestoreAction:
                    TurnManager.Instance.AddSharedActingPower(value);
                    break;
                case CardEffectType.Reload:
                    if (targetData is DPawn reloadPawn)
                        reloadPawn.RestoreAmmo();
                    break;
                case CardEffectType.BuffAttack:
                case CardEffectType.BuffArmor:
                case CardEffectType.BuffMovement:
                    if (targetData is DPawn buffP)
                    {
                        buffP.ApplyBuff(effect.EffectType, value, effect.Duration);
                        if (effect.EffectType == CardEffectType.BuffMovement)
                        {
                            TurnManager.Instance.AdjustRemainingMovement(targetActor, value);
                            RefreshMovementRange();
                        }
                    }
                    else if (targetData is DMonster buffM) buffM.ApplyBuff(effect.EffectType, value, effect.Duration);
                    break;
                case CardEffectType.DebuffAttack:
                case CardEffectType.DebuffArmor:
                case CardEffectType.DebuffMovement:
                    if (targetData is DPawn debuffP)
                    {
                        debuffP.ApplyBuff(effect.EffectType, -value, effect.Duration);
                        if (effect.EffectType == CardEffectType.DebuffMovement)
                        {
                            TurnManager.Instance.AdjustRemainingMovement(targetActor, -value);
                            RefreshMovementRange();
                        }
                    }
                    else if (targetData is DMonster debuffM) debuffM.ApplyBuff(effect.EffectType, -value, effect.Duration);
                    break;
            }
        }
    }

    private int GetActorMovement(Actor actor)
    {
        if (actor.Data is DPawn pawn)   return pawn.Movement;
        if (actor.Data is DMonster mon) return mon.Movement;
        return 0;
    }

    /// <summary>
    /// 이번 턴 잔여 이동력을 반환한다. 이동한 적 없으면 전체 이동력을 반환한다.
    /// </summary>
    private int GetEffectiveMovement(Actor actor)
    {
        int remaining = TurnManager.Instance.GetRemainingMovement(actor);
        return remaining >= 0 ? remaining : GetActorMovement(actor);
    }

    private Actor GetActorOnTile(SquareTile tile)
    {
        if (tile == null || tile.Slot == null) return null;
        return tile.Slot.SlotEntity as Actor;
    }

    private bool IsEmptyTile(SquareTile tile)
    {
        return tile.Slot != null && tile.Slot.SlotEntity == null;
    }

    /// <summary>
    /// SelectedObject가 변경될 때 발생. (Pawn 선택 전환, 선택 해제 포함)
    /// </summary>
    public event Action onSelectedChanged;

    /// <summary>
    /// 전투 종료 시 발생. bool: true = 승리, false = 패배
    /// </summary>
    public event Action<bool> onBattleEnd;

    public void OnTurnLimitExceeded() => onBattleEnd?.Invoke(false);

    public void CheckBattleResult()
    {
        bool allEnemiesDead = _dicEnemyActor.Values.All(e => e == null || !e.gameObject.activeInHierarchy);
        bool allUsersDead   = _dicUserActor.Values.All(u => u == null || !u.gameObject.activeInHierarchy);

        if (!allEnemiesDead && !allUsersDead) return;

        if (allEnemiesDead) _stageGoldReward += 100;
        onBattleEnd?.Invoke(allEnemiesDead);
    }

    public void LoadStage(TileMapPreset preset)
    {
        if (preset != null)
        {
            LoadStageFromPreset(preset);
        }
        else
        {
            LoadStageDefault();
        }
    }

    public void ResetAll()
    {
        foreach (var actor in _dicUserActor.Values)
            if (actor != null) Destroy(actor.gameObject);
        foreach (var actor in _dicEnemyActor.Values)
            if (actor != null) Destroy(actor.gameObject);

        _dicUserActor.Clear();
        _dicEnemyActor.Clear();
        _stageGoldReward = 0;
        _stageExpReward = 0;
        _isBusy = false;
        _reachableTiles.Clear();
        _cardRangeTiles.Clear();
        _radiusPreviewTiles.Clear();
        SelectedTile = null;
        SelectedObject = null;

        onSelectedChanged = null;
        onBattleEnd = null;
    }

    private void ResetStageState()
    {
        _dicUserActor.Clear();
        _dicEnemyActor.Clear();
        _stageGoldReward = 0;
        _stageExpReward  = 0;
        _reachableTiles.Clear();
        _cardRangeTiles.Clear();
        _radiusPreviewTiles.Clear();
        Deselect();
    }

    private void LoadStageFromPreset(TileMapPreset preset)
    {
        ResetStageState();
        TurnManager.Instance.SetTurnLimit(preset.turnLimit);
        TileManager.Instance.GenerateTileMapFromPreset(preset);

        // 스폰 포인트 기반 액터 배치 (DeckManager의 Pawn 사용)
        var deckPawns = DeckManager.Instance.DeckPawns.ToList();
        var playerSpawns = TileManager.Instance.GetSpawnPoints(preset, SpawnPointType.Player);
        int pawnLimit = preset.maxPawnCount > 0 ? preset.maxPawnCount : playerSpawns.Count;
        int pawnCount = Mathf.Min(pawnLimit, playerSpawns.Count, deckPawns.Count);

        int actorIndex = 0;
        for (int i = 0; i < pawnCount; i++)
        {
            var tile = TileManager.Instance.GetTile(playerSpawns[i].position);
            if (tile == null) continue;

            DPawn pawn = deckPawns[i];
            GameObject pawnPrefab = Resources.Load<GameObject>(pawn.Data.PrefabPath);
            GameObject pawnObj = Instantiate(pawnPrefab, Vector3.zero, Quaternion.identity, transform);
            pawnObj.name = $"Pawn_{actorIndex}";
            Actor actor = pawnObj.GetComponent<Actor>();
            actor.Init(pawn);
            tile.Slot?.SetEntity(actor);
            _dicUserActor.Add(actorIndex, actor);
            actorIndex++;
        }

        var enemySpawns = TileManager.Instance.GetSpawnPoints(preset, SpawnPointType.Enemy);
        for (int i = 0; i < enemySpawns.Count; i++)
        {
            var tile = TileManager.Instance.GetTile(enemySpawns[i].position);
            if (tile == null) continue;

            int monsterId = enemySpawns[i].spawnId > 0 ? enemySpawns[i].spawnId : 1;
            DMonster monster = new DMonster(monsterId);
            GameObject monsterPrefab = Resources.Load<GameObject>(monster.Data.PrefabPath);
            GameObject monsterObj = Instantiate(monsterPrefab, Vector3.zero, Quaternion.identity, transform);
            monsterObj.name = $"Monster_{i}";
            Actor actor = monsterObj.GetComponent<Actor>();
            actor.Init(monster);
            tile.Slot?.SetEntity(actor);
            _dicEnemyActor.Add(i, actor);
        }
    }

    private void LoadStageDefault()
    {
        ResetStageState();
        TileManager.Instance.GenerateTileMap(20, 20, tileSize: 1.05f);

        for (int i = 0; i < 10; i++)
        {
            var tile = TileManager.Instance.GetTile(new Vector2Int(i, 5));
            if (tile == null) continue;

            DPawn pawn = new DPawn(1);

            GameObject monsterPrefab = Resources.Load<GameObject>("Prefabs/Actor/Monster/Monster_Slime");
            GameObject monsterObj = Instantiate(monsterPrefab, Vector3.zero, Quaternion.identity, transform);
            monsterObj.name = $"Monster_{i}";
            Actor actor = monsterObj.GetComponent<Actor>();
            actor.Init(pawn);
            tile.Slot?.SetEntity(actor);
            _dicEnemyActor.Add(i, actor);
        }

        for (int i = 0; i < 5; i++)
        {
            var tile = TileManager.Instance.GetTile(new Vector2Int(i, 0));
            if (tile == null) continue;

            DPawn pawn = new DPawn(1);

            GameObject pawnPrefab = Resources.Load<GameObject>("Prefabs/Actor/Pawn/Pawn_Soldier");
            GameObject pawnObj = Instantiate(pawnPrefab, Vector3.zero, Quaternion.identity, transform);
            pawnObj.name = $"Pawn_{i}";
            Actor actor = pawnObj.GetComponent<Actor>();
            actor.Init(pawn);
            tile.Slot?.SetEntity(actor);
            _dicUserActor.Add(i, actor);
        }
    }
}
