using GameData;
using UnityEngine;
using UnityEngine.UI;

public class CardIcon : IconBase
{
    #region Link
    [Linker("Root/Image_Cost/Text_Cost")]
    public Text _textCost;
    [Linker("Root/Image_Ammo/Text_Ammo")]
    public Text _textAmmo;
    [Linker("Root/Text_Name")]
    public Text _textName;
    [Linker("Root/Text_Desc")]
    public Text _textDesc;
    #endregion Link

    private CardData _card;
    public CardData Card => _card;

    public void SetData(CardData card)
    {
        _card = card;

        _textCost.text = card.EnergyCost.ToString();
        _textAmmo.text = card.AmmoCost.ToString();
        _textName.text = TextManager.Instance.Get(card.Name);
        _textDesc.text = TextManager.Instance.Get(card.Desc);

        _rawIcon.texture = Resources.Load<Texture>(card.IconPath);
    }
}
