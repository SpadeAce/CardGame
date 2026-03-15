using SA.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[AssetPath("Prefabs/UI/Recruit/RecruitPage")]
public class RecruitPage : PageView
{
    #region Linker
    [Linker("Root/Button_Close")]
    public Button _buttonClose;

    [Linker("Root/PawnGroup")]
    public GameObject _goPawnGroup;

    [Linker("Root/Button_Reroll")]
    public Button _buttonReroll;

    [Linker("Root/Text_Goods")]
    public Text _textGoods;
    #endregion Linker

    private readonly List<RecruitItem> _activeItems = new();

    public override void PreOpen()
    {
        _buttonClose.onClick.RemoveAllListeners();
        _buttonClose.onClick.AddListener(OnClickClose);

        _buttonReroll.onClick.RemoveAllListeners();
        _buttonReroll.onClick.AddListener(OnClickReroll);
    }

    public override void OnOpened()
    {
        if (LobbyManager.Instance.RecruitPawns == null)
            LobbyManager.Instance.GenerateRecruitList();
        RefreshItems();
        RefreshGoods();
    }

    private void RefreshGoods()
    {
        _textGoods.text = PlayerManager.Instance.Gold.ToString("N0");
    }

    private void RefreshItems()
    {
        foreach (var item in _activeItems)
            if (item != null) Destroy(item.gameObject);
        _activeItems.Clear();

        var prefab = PrefabLoader.Load<RecruitItem>();
        var pawns = LobbyManager.Instance.RecruitPawns;
        float startX = -360f * (pawns.Count - 1) / 2f;

        for (int i = 0; i < pawns.Count; i++)
        {
            var item = Instantiate(prefab, _goPawnGroup.transform);
            item.GetComponent<RectTransform>().anchoredPosition = new Vector2(startX + 360f * i, 0f);
            item.SetData(pawns[i]);
            item.OnRecruited += OnItemRecruited;
            _activeItems.Add(item);
        }
    }

    private void OnItemRecruited(RecruitItem recruitItem, DPawn pawn)
    {
        LobbyManager.Instance.RemoveRecruitPawn(pawn);
        _activeItems.Remove(recruitItem);
        Destroy(recruitItem.gameObject);
        RefreshGoods();
    }

    /// <summary>
    /// Stage 복귀 등 외부에서 목록을 갱신할 때 호출.
    /// </summary>
    public void Refresh()
    {
        LobbyManager.Instance.GenerateRecruitList();
        RefreshItems();
    }

    #region Events
    public void OnClickClose()
    {
        Close();
    }

    public void OnClickReroll()
    {
        LobbyManager.Instance.GenerateRecruitList();
        RefreshItems();
    }
    #endregion Events
}
