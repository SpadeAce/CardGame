using UnityEngine;
using SA.UI;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using GameData;

[AssetPath("Prefabs/UI/Stage/StagePage")]
public class StagePage : PageView
{
    #region Links
    [Linker("Root/Button_Pause")]
    public Button _buttonPause;

    [Linker("Root/Button_EndTurn")]
    public Button _buttonEndTurn;

    [Linker("Root/CardGroup")]
    public GameObject _goCardGroup;

    [Linker("Root/PawnCardGroup")]
    public GameObject _goPawnCardGroup;

    [Linker("Root/Text_Turn")]
    public Text _textTurn;
    
    [Linker("Root/Image_Energy/Text_Energy")]
    public Text _textEnergy;

    [Linker("Root/Image_EnemyTarget")]
    public GameObject _goEnemyTarget;
    [Linker("Root/Image_AllyTarget")]
    public GameObject _goAllyTarget;
    #endregion Links

    List<BattleCard> cardIconList = new List<BattleCard>();
    private List<BattleCard> _pawnCardIconList = new List<BattleCard>();
    private DPawn _subscribedPawn;

    // --- 카드 드래그 상태 ---
    private bool _isCardDragging;
    private bool _pendingClearDragFlag;
    private Actor _casterActor;
    private Vector3 _dragIconOriginalPos;
    private DPawn _dragCardOwner; // null=글로벌 핸드, non-null=Pawn 핸드

    private const float CancelRadius = 100f;
    private const float SelfConfirmDelta = 150f;

    public bool IsCardDragging => _isCardDragging;

    public override void PreOpen()
    {
        _buttonPause.onClick.RemoveAllListeners();
        _buttonPause.onClick.AddListener(OnClickPause);

        _buttonEndTurn.onClick.RemoveAllListeners();
        _buttonEndTurn.onClick.AddListener(OnClickEndTurn);

        DeckManager.Instance.onHandChanged               += SetCardList;
        StageManager.Instance.onBattleEnd                += OnBattleEnd;
        StageManager.Instance.onSelectedChanged          += SetCardList;
        StageManager.Instance.onSelectedChanged          += SetPawnCardList;
        TurnManager.Instance.onTurnChanged               += OnTurnChanged;
        TurnManager.Instance.onSharedActingPowerChanged  += SetCardList;
        TurnManager.Instance.onSharedActingPowerChanged  += SetPawnCardList;
        TurnManager.Instance.onSharedActingPowerChanged  += RefreshEnergy;
        
        _goEnemyTarget.SetActive(false);
        _goAllyTarget.SetActive(false);
    }

    public override void OnOpened()
    {
        SetCardList();
        SetPawnCardList();
        RefreshEnergy();
    }

    public override void PreClose()
    {
        DeckManager.Instance.onHandChanged               -= SetCardList;
        StageManager.Instance.onBattleEnd                -= OnBattleEnd;
        StageManager.Instance.onSelectedChanged          -= SetCardList;
        StageManager.Instance.onSelectedChanged          -= SetPawnCardList;
        TurnManager.Instance.onTurnChanged               -= OnTurnChanged;
        TurnManager.Instance.onSharedActingPowerChanged  -= SetCardList;
        TurnManager.Instance.onSharedActingPowerChanged  -= SetPawnCardList;
        TurnManager.Instance.onSharedActingPowerChanged  -= RefreshEnergy;

        if (_subscribedPawn != null)
        {
            _subscribedPawn.OnPawnHandChanged -= SetPawnCardList;
            _subscribedPawn.onStatsChanged    -= SetCardList;
            _subscribedPawn.onStatsChanged    -= SetPawnCardList;
            _subscribedPawn = null;
        }

        foreach (var icon in cardIconList)
        {
            icon.OnDragBegin -= OnCardDragBegin;
            icon.OnDragging  -= OnCardDragging;
            icon.OnDragEnd   -= OnCardDragEnd;
        }
        foreach (var icon in _pawnCardIconList)
        {
            icon.OnDragBegin -= OnPawnCardDragBegin;
            icon.OnDragging  -= OnCardDragging;
            icon.OnDragEnd   -= OnCardDragEnd;
        }
    }

    private void LateUpdate()
    {
        if (_pendingClearDragFlag)
        {
            _isCardDragging = false;
            _pendingClearDragFlag = false;
        }
    }

    private void SetCardList()
    {
        for (int i = 0; i < cardIconList.Count; i++)
            cardIconList[i].gameObject.SetActive(false);

        List<DCard> cardList = new List<DCard>();
        cardList.AddRange(DeckManager.Instance.handCardList);

        DPawn selectedPawn = StageManager.Instance.SelectedObject as DPawn;

        GameObject cardIconPrefab = Resources.Load<GameObject>("Prefabs/UI/HUD/BattleCard");

        for (int i = 0; i < cardList.Count; i++)
        {
            if (cardIconList.Count > i)
            {
                BattleCard existing = cardIconList[i];
                existing.OnDragBegin -= OnCardDragBegin;
                existing.OnDragging  -= OnCardDragging;
                existing.OnDragEnd   -= OnCardDragEnd;
                existing.OnDragBegin += OnCardDragBegin;
                existing.OnDragging  += OnCardDragging;
                existing.OnDragEnd   += OnCardDragEnd;

                existing.gameObject.SetActive(true);
                existing.transform.localPosition = Vector3.right * (100 * i);
                existing.SetData(cardList[i]);
                existing.SetDisabled(selectedPawn != null && (!TurnManager.Instance.HasEnoughSharedActingPower(cardList[i].Data.EnergyCost) || !selectedPawn.HasEnoughAmmo(cardList[i].Data.AmmoCost)));
            }
            else
            {
                GameObject cardObject = Instantiate<GameObject>(cardIconPrefab);
                BattleCard card = cardObject.GetComponent<BattleCard>();
                card.transform.SetParent(_goCardGroup.transform, false);
                card.transform.localScale = Vector3.one;
                card.transform.localPosition = Vector3.right * (100 * i);
                card.SetData(cardList[i]);
                card.SetDisabled(selectedPawn != null && (!TurnManager.Instance.HasEnoughSharedActingPower(cardList[i].Data.EnergyCost) || !selectedPawn.HasEnoughAmmo(cardList[i].Data.AmmoCost)));

                card.OnDragBegin += OnCardDragBegin;
                card.OnDragging  += OnCardDragging;
                card.OnDragEnd   += OnCardDragEnd;

                cardIconList.Add(card);
            }
        }
    }

    // ── Pawn 카드 리스트 표시 ─────────────────────────────────────────────

    private void SetPawnCardList()
    {
        if (_subscribedPawn != null)
        {
            _subscribedPawn.OnPawnHandChanged -= SetPawnCardList;
            _subscribedPawn.onStatsChanged    -= SetCardList;
            _subscribedPawn.onStatsChanged    -= SetPawnCardList;
        }

        DPawn newPawn = StageManager.Instance.SelectedObject as DPawn;
        _subscribedPawn = newPawn;

        if (_subscribedPawn != null)
        {
            _subscribedPawn.OnPawnHandChanged += SetPawnCardList;
            _subscribedPawn.onStatsChanged    += SetCardList;
            _subscribedPawn.onStatsChanged    += SetPawnCardList;
        }

        for (int i = 0; i < _pawnCardIconList.Count; i++)
            _pawnCardIconList[i].gameObject.SetActive(false);

        if (newPawn == null) return;

        List<DCard> pawnCards = newPawn.PawnHandCards;
        GameObject prefab = Resources.Load<GameObject>("Prefabs/UI/HUD/BattleCard");
        for (int i = 0; i < pawnCards.Count; i++)
        {
            BattleCard icon;
            if (_pawnCardIconList.Count > i)
            {
                icon = _pawnCardIconList[i];
                icon.OnDragBegin -= OnPawnCardDragBegin;
                icon.OnDragging  -= OnCardDragging;
                icon.OnDragEnd   -= OnCardDragEnd;
                icon.OnDragBegin += OnPawnCardDragBegin;
                icon.OnDragging  += OnCardDragging;
                icon.OnDragEnd   += OnCardDragEnd;
                icon.gameObject.SetActive(true);
            }
            else
            {
                var go = Instantiate(prefab);
                icon = go.GetComponent<BattleCard>();
                icon.transform.SetParent(_goPawnCardGroup.transform, false);
                icon.transform.localScale = Vector3.one;
                icon.OnDragBegin += OnPawnCardDragBegin;
                icon.OnDragging  += OnCardDragging;
                icon.OnDragEnd   += OnCardDragEnd;
                _pawnCardIconList.Add(icon);
            }
            icon.transform.localPosition = Vector3.right * (100 * i);
            icon.SetData(pawnCards[i]);
            icon.SetDisabled(!TurnManager.Instance.HasEnoughSharedActingPower(pawnCards[i].Data.EnergyCost) || !newPawn.HasEnoughAmmo(pawnCards[i].Data.AmmoCost));
        }
    }

    // ── BattleCard 드래그 이벤트 핸들러 ──────────────────────────────────────

    private void StartCardDrag(BattleCard icon, PointerEventData eventData, DPawn owner)
    {
        if (StageManager.Instance.IsBusy) return;
        SquareTile selectedTile = StageManager.Instance.SelectedTile;
        if (selectedTile == null || selectedTile.Slot == null) return;
        Actor caster = selectedTile.Slot.SlotEntity as Actor;
        if (caster == null || !(caster.Data is DPawn)) return;

        DCard card = icon.Card;
        if (card.Data.Target == TargetType.None) return;

        _isCardDragging = true;
        _goEnemyTarget.SetActive(false);
        _goAllyTarget.SetActive(false);
        _casterActor = caster;
        _dragCardOwner = owner;
        _dragIconOriginalPos = icon.transform.localPosition;

        StageManager.Instance.ClearMovementHighlights();
        icon.transform.position = new Vector3(eventData.position.x, eventData.position.y, 0f);

        if (card.Data.Target == TargetType.Enemy
            || card.Data.Target == TargetType.Ground
            || card.Data.Target == TargetType.Ally)
            StageManager.Instance.ShowCardRange(selectedTile, card, caster);
    }

    private void OnCardDragBegin(BattleCard icon, PointerEventData eventData)
        => StartCardDrag(icon, eventData, null);

    private void OnPawnCardDragBegin(BattleCard icon, PointerEventData eventData)
        => StartCardDrag(icon, eventData, StageManager.Instance.SelectedObject as DPawn);

    private void OnCardDragging(BattleCard icon, PointerEventData eventData)
    {
        if (!_isCardDragging) return;

        DCard card = icon.Card;
        Vector2 currentPos = eventData.position;
        bool isOutside = Vector2.Distance(currentPos, icon.DragStartPosition) >= CancelRadius;

        bool useEnemy = isOutside && (card.Data.Target == TargetType.Enemy || card.Data.Target == TargetType.Ground);
        bool useAlly  = isOutside && card.Data.Target == TargetType.Ally;

        icon.SetVisible(!useEnemy && !useAlly);
        _goEnemyTarget.SetActive(useEnemy);
        _goAllyTarget.SetActive(useAlly);

        if (useEnemy)
            _goEnemyTarget.transform.position = new Vector3(currentPos.x, currentPos.y, 0f);
        else if (useAlly)
            _goAllyTarget.transform.position = new Vector3(currentPos.x, currentPos.y, 0f);
        else
            icon.transform.position = new Vector3(currentPos.x, currentPos.y, 0f);

        if (card.Data.Target == TargetType.Enemy
            || card.Data.Target == TargetType.Ground
            || card.Data.Target == TargetType.Ally)
        {
            Ray ray = Camera.main.ScreenPointToRay(eventData.position);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                SquareTile hovered = hit.collider.GetComponentInParent<SquareTile>();
                if (hovered != null && StageManager.Instance.IsInCardRange(hovered))
                    StageManager.Instance.ShowRadiusPreview(hovered, card);
                else
                    StageManager.Instance.ClearRadiusPreview();
            }
            else
                StageManager.Instance.ClearRadiusPreview();
        }
    }

    private void OnCardDragEnd(BattleCard icon, PointerEventData eventData)
    {
        if (!_isCardDragging) return;

        DCard card = icon.Card;
        Vector2 startPos = icon.DragStartPosition;
        Vector2 endPos = eventData.position;

        icon.transform.localPosition = _dragIconOriginalPos;
        icon.SetVisible(true);
        _goEnemyTarget.SetActive(false);
        _goAllyTarget.SetActive(false);
        StageManager.Instance.ClearCardRange();
        StageManager.Instance.ClearRadiusPreview();
        StageManager.Instance.RestoreMovementRange();

        // LateUpdate에서 플래그 해제 (StageScript.Update 가드 유지용)
        _pendingClearDragFlag = true;

        Actor caster = _casterActor;
        DPawn cardOwner = _dragCardOwner;
        _casterActor = null;
        _dragCardOwner = null;

        // 취소: 드래그 시작 위치 근처에서 해제
        if (Vector2.Distance(endPos, startPos) < CancelRadius) return;

        if (card.Data.Target == TargetType.Self)
        {
            float deltaY = endPos.y - startPos.y;
            if (deltaY >= SelfConfirmDelta)
                StageManager.Instance.UseCardSelf(card, caster, cardOwner);
        }
        else if (card.Data.Target == TargetType.Enemy
                 || card.Data.Target == TargetType.Ground
                 || card.Data.Target == TargetType.Ally)
        {
            Ray ray = Camera.main.ScreenPointToRay(endPos);
            if (!Physics.Raycast(ray, out RaycastHit hit)) return;

            SquareTile targetTile = hit.collider.GetComponentInParent<SquareTile>();
            if (targetTile == null) return;

            StageManager.Instance.TryUseCard(card, caster, targetTile, cardOwner);
        }
    }

    // ── 턴 갱신 ──────────────────────────────────────────────────────────

    private void OnTurnChanged(int turn)
    {
        _textTurn.text = string.Format("Turn {0}", turn);
    }

    private void RefreshEnergy()
    {
        if (_textEnergy != null)
            _textEnergy.text = TurnManager.Instance.SharedActingPower.ToString();
    }

    // ── 전투 결과 ─────────────────────────────────────────────────────────

    private void OnBattleEnd(bool isWin)
    {
        UIManager.Instance.OpenView<BattleResultPopup>(new BattleResultPopup.BattleResultParam
        {
            isWin      = isWin,
            goldReward = isWin ? StageManager.Instance.StageGoldReward : 0,
            expReward  = isWin ? StageManager.Instance.StageExpReward  : 0,
        });
    }

    // ── 버튼 핸들러 ───────────────────────────────────────────────────────

    private void OnClickPause()
    {
        UIManager.Instance.OpenView<PausePopup>();
    }

    private void OnClickEndTurn()
    {
        TurnManager.Instance.EndPawnTurn();
    }
}
