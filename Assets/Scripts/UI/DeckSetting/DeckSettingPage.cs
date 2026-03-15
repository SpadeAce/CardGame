using SA.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;

[AssetPath("Prefabs/UI/DeckSetting/DeckSettingPage")]
public class DeckSettingPage : PageView
{
    enum DECK_SETTING_PAGE {PAWN, CARD}

    #region Linker
    [Linker("Root/Button_Close")]
    public Button _buttonClose;

    [Linker("Root/Button_PawnTab")]
    public Button _buttonPawnTab;
    [Linker("Root/Button_CardTab")]
    public Button _buttonCardTab;

    [Linker("Root/PawnPage")]
    public DeckSetting_Pawn _pawn;
    [Linker("Root/CardPage")]
    public DeckSetting_Card _card;
    #endregion Linker

    DECK_SETTING_PAGE _currentPage = DECK_SETTING_PAGE.PAWN;

    public override void OnOpened()
    {
        _pawn.OnOpened();
    }

    public override void PreOpen()
    {
        _buttonClose.onClick.RemoveAllListeners();
        _buttonClose.onClick.AddListener(OnClickClose);

        _buttonPawnTab.onClick.RemoveAllListeners();
        _buttonPawnTab.onClick.AddListener(OnClickPawnTab);

        _buttonCardTab.onClick.RemoveAllListeners();
        _buttonCardTab.onClick.AddListener(OnClickCardTab);

        RefreshTab();
    }

    private void RefreshTab()
    {
        _pawn.gameObject.SetActive(_currentPage == DECK_SETTING_PAGE.PAWN);
        _card.gameObject.SetActive(_currentPage == DECK_SETTING_PAGE.CARD);
    }

    #region Events
    public void OnClickClose()
    {
        Close();
    }

    public void OnClickPawnTab()
    {
        _currentPage = DECK_SETTING_PAGE.PAWN;
        RefreshTab();
    }

    public void OnClickCardTab()
    {
        _currentPage = DECK_SETTING_PAGE.CARD;
        RefreshTab();
    }
    #endregion Events
}
