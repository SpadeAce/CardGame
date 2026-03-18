using SA.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[AssetPath("Prefabs/UI/Shop/ShopPage")]
public class ShopPage : PageView
{
    #region Linker
    [Linker("Root/CardGroup")]
    public GameObject _goCardGroup;
    [Linker("Root/Button_Close")]
    public Button _buttonClose;
    [Linker("Root/Button_LevelUp")]
    public Button _buttonLevelUp;
    [Linker("Root/Text_Goods")]
    public Text _textGoods;
    #endregion Linker

    private const int ItemsPerRow = 5;

    private readonly List<ShopItem> _activeItems = new();

    public override void PreOpen()
    {
        _buttonClose.onClick.RemoveAllListeners();
        _buttonClose.onClick.AddListener(OnClickClose);

        _buttonLevelUp.onClick.RemoveAllListeners();
        _buttonLevelUp.onClick.AddListener(OnClickLevelUp);
    }

    public override void OnOpened()
    {
        if (LobbyManager.Instance.ShopCards == null)
            LobbyManager.Instance.GenerateShopList();
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

        var prefab = PrefabLoader.Load<ShopItem>();
        var cards = LobbyManager.Instance.ShopCards;

        for (int i = 0; i < cards.Count; i++)
        {
            var item = Instantiate(prefab, _goCardGroup.transform);
            int row = i / ItemsPerRow;
            int col = i % ItemsPerRow;
            item.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(-440f + 250f * col, -row * 350f);
            item.SetData(cards[i]);
            item.OnBuyItem += OnItemBought;
            _activeItems.Add(item);
        }
    }

    private void OnItemBought(ShopItem shopItem, DCard card)
    {
        LobbyManager.Instance.RemoveShopCard(card.Data);
        _activeItems.Remove(shopItem);
        RefreshGoods();
    }

    /// <summary>
    /// Stage 진행 후 외부에서 목록을 갱신할 때 호출.
    /// </summary>
    public void Refresh()
    {
        LobbyManager.Instance.GenerateShopList();
        RefreshItems();
    }

    #region Events
    public void OnClickClose()
    {
        Close();
    }

    public void OnClickLevelUp()
    {
        if (!LobbyManager.Instance.LevelUpShop()) return;
        RefreshItems();
    }
    #endregion Events
}
