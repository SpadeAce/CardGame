using UnityEngine;

[AssetPath("Prefabs/UI/DeckSetting/DeckCardItem")]
public class DeckCardItem : MonoBehaviourEx
{
    #region Links
    [Linker("Spawn_Icon")]
    public Spawn _spawnIcon;
    #endregion Links

    private DCard _card;
    public DCard Card => _card;

    public void SetData(DCard card)
    {
        _card = card;
        var icon = _spawnIcon.Get<CardIcon>();
        if (icon != null)
            icon.SetData(card.Data);
    }
}
