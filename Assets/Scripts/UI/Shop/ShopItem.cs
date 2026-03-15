using System;
using GameData;
using UnityEngine;
using UnityEngine.UI;

[AssetPath("Prefabs/UI/Shop/ShopItem")]
public class ShopItem : MonoBehaviourEx
{
    #region Links
    [Linker("Button_Buy")]
    public Button _buttonBuy;
    [Linker("Spawn_Icon")]
    public Spawn _spawnIcon;
    [Linker("Button_Buy/Text_Price")]
    public Text _textPrice;
    #endregion Links

    public event Action<ShopItem, DCard> OnBuyItem;

    private CardData _cardData;

    void Start()
    {
        _buttonBuy.onClick.RemoveAllListeners();
        _buttonBuy.onClick.AddListener(OnClickBuy);
    }

    public void SetData(CardData data)
    {
        _cardData = data;
        _textPrice.text = data.Price > 0 ? data.Price.ToString() : "FREE";

        _spawnIcon.Get<CardIcon>().SetData(data);
    }

    public void OnClickBuy()
    {
        if (!DeckManager.Instance.SpendGold(_cardData.Price)) return;
        var card = new DCard(_cardData);
        DeckManager.Instance.AddCard(card);
        gameObject.SetActive(false);
        OnBuyItem?.Invoke(this, card);
    }
}
