using System;
using GameData;
using UnityEngine;
using UnityEngine.UI;

public class BattleResultItem : MonoBehaviourEx
{
    #region Linker
    [Linker("Spawn_Icon"), SerializeField]
    private Spawn _spawnIcon;
    [Linker("Image_Selected"), SerializeField]
    private GameObject _goSelected;
    [Linker(""), SerializeField]
    private Button _button;
    #endregion Linker

    public void SetCard(CardData cardData)
    {
        var icon = _spawnIcon.Get<CardIcon>();
        icon.SetData(cardData);
    }

    public void SetSelected(bool selected)
    {
        _goSelected.SetActive(selected);
    }

    public void SetClickListener(Action onClick)
    {
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => onClick?.Invoke());
    }
}
