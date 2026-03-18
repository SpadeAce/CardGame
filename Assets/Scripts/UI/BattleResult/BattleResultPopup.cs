using UnityEngine;
using SA.UI;
using UnityEngine.UI;
using System.Collections.Generic;
using GameData;

[AssetPath("Prefabs/UI/BattleResult/BattleResultPopup")]
public class BattleResultPopup : PopupView
{
    public class BattleResultParam : ViewParam
    {
        public bool isWin;
        public int goldReward;
        public int expReward;
    }

    #region Linker
    [Linker("Root/Text_Title")]
    public Text _textTitle;
    [Linker("Root/Button_Exit")]
    public Button _buttonExit;
    [Linker("Root/Button_Continue")]
    public Button _buttonContinue;

    [Linker("Root/Text_Gold")]
    public Text _textRewardGold;
    [Linker("Root/Text_Exp")]
    public Text _textRewardExp;
    [Linker("Root/RewardCard")]
    public GameObject _goRewardCard;
    [Linker("Root/RewardCard/Spawn_Reward_1",
    "Root/RewardCard/Spawn_Reward_2",
    "Root/RewardCard/Spawn_Reward_3")]
    public List<Spawn> _spawnRewardList = new List<Spawn>();
    #endregion Linker

    private int _selectedIndex = -1;
    private List<int> _rewardCardIds = new List<int>();

    public override void PreOpen()
    {
        _buttonExit.onClick.RemoveAllListeners();
        _buttonExit.onClick.AddListener(OnClickExit);
        _buttonContinue.onClick.RemoveAllListeners();
        _buttonContinue.onClick.AddListener(OnClickContinue);

        BattleResultParam resultParam = param as BattleResultParam;
        _buttonExit.gameObject.SetActive(!resultParam.isWin);
        _buttonContinue.gameObject.SetActive(resultParam.isWin);

        _textTitle.text = resultParam.isWin ? "승리" : "패배";
        _textRewardGold.text = resultParam.goldReward.ToString();
        _textRewardExp.text = resultParam.expReward.ToString();

        _selectedIndex = -1;
        _rewardCardIds.Clear();

        if (resultParam.isWin)
        {
            _goRewardCard.SetActive(true);
            SetupRewardCards();
        }
        else
        {
            _goRewardCard.SetActive(false);
        }
    }

    private void SetupRewardCards()
    {
        int level = PlayerManager.Instance.DifficultyLevel;
        var rewardData = DataManager.Instance.StageReward.GetByLevel(level);

        if (rewardData == null || rewardData.CardId.Count == 0)
        {
            _goRewardCard.SetActive(false);
            return;
        }

        _rewardCardIds = PickWeightedCards(rewardData, 3);

        for (int i = 0; i < _spawnRewardList.Count; i++)
        {
            var item = _spawnRewardList[i].Get<BattleResultItem>();

            if (i < _rewardCardIds.Count)
            {
                item.gameObject.SetActive(true);
                var cardData = DataManager.Instance.Card.Get(_rewardCardIds[i]);
                item.SetCard(cardData);
                item.SetSelected(false);

                int index = i;
                item.SetClickListener(() => OnClickRewardCard(index));
            }
            else
            {
                item.gameObject.SetActive(false);
            }
        }
    }

    private List<int> PickWeightedCards(StageRewardData data, int count)
    {
        var result = new List<int>();
        var candidates = new List<int>();
        var weights = new List<int>();

        for (int i = 0; i < data.CardId.Count; i++)
        {
            candidates.Add(i);
            weights.Add(data.CardProb[i]);
        }

        int pick = Mathf.Min(count, candidates.Count);

        for (int n = 0; n < pick; n++)
        {
            int totalWeight = 0;
            for (int i = 0; i < weights.Count; i++)
                totalWeight += weights[i];

            int roll = Random.Range(0, totalWeight);
            int cumulative = 0;
            int selectedIdx = 0;

            for (int i = 0; i < weights.Count; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative)
                {
                    selectedIdx = i;
                    break;
                }
            }

            result.Add(data.CardId[candidates[selectedIdx]]);
            candidates.RemoveAt(selectedIdx);
            weights.RemoveAt(selectedIdx);
        }

        return result;
    }

    private void OnClickRewardCard(int index)
    {
        if (_selectedIndex == index)
        {
            _selectedIndex = -1;
            _spawnRewardList[index].Get<BattleResultItem>().SetSelected(false);
        }
        else
        {
            if (_selectedIndex >= 0)
                _spawnRewardList[_selectedIndex].Get<BattleResultItem>().SetSelected(false);

            _selectedIndex = index;
            _spawnRewardList[index].Get<BattleResultItem>().SetSelected(true);
        }
    }

    #region Events
    public void OnClickContinue()
    {
        BattleResultParam resultParam = param as BattleResultParam;
        PlayerManager.Instance.AddGold(resultParam.goldReward);
        PlayerManager.Instance.IncrementLevel();
        foreach (var pawn in DeckManager.Instance.DeckPawns)
            pawn.AddExp(resultParam.expReward);

        if (_selectedIndex >= 0 && _selectedIndex < _rewardCardIds.Count)
        {
            var card = new DCard(_rewardCardIds[_selectedIndex]);
            DeckManager.Instance.AddCard(card);
        }

        SceneController.Instance.ChangeScene("LobbyScene");
    }

    public void OnClickExit()
    {
        SceneController.Instance.ChangeScene("TitleScene");
    }
    #endregion Events
}
