using System.Linq;
using UnityEngine;
using SA.UI;
using UnityEngine.UI;

[AssetPath("Prefabs/UI/Lobby/LobbyPage")]
public class LobbyPage : PageView
{
    #region Linker
    [Linker("Root/Button_StartBattle")]
    public Button _buttonStartBattle;
    [Linker("Root/Button_DeckSetting")]
    public Button _buttonDeckSetting;
    [Linker("Root/Button_PawnManage")]
    public Button _buttonPawnManage;
    [Linker("Root/Button_Shop")]
    public Button _buttonShop;
    [Linker("Root/Button_Recruit")]
    public Button _buttonRecruit;
    [Linker("Root/Button_Exit")]
    public Button _buttonExit;
    #endregion Linker

    public override void PreOpen()
    {
        _buttonStartBattle.onClick.RemoveAllListeners();
        _buttonStartBattle.onClick.AddListener(OnClickStartBattle);

        _buttonDeckSetting.onClick.RemoveAllListeners();
        _buttonDeckSetting.onClick.AddListener(OnClickDeckSetting);

        _buttonPawnManage.onClick.RemoveAllListeners();
        _buttonPawnManage.onClick.AddListener(OnClickPawnManage);

        _buttonShop.onClick.RemoveAllListeners();
        _buttonShop.onClick.AddListener(OnClickShop);

        _buttonRecruit.onClick.RemoveAllListeners();
        _buttonRecruit.onClick.AddListener(OnClickRecruit);

        _buttonExit.onClick.RemoveAllListeners();
        _buttonExit.onClick.AddListener(OnClickExit);
    }

    #region Events
    public void OnClickStartBattle()
    {
        if (!DeckManager.Instance.DeckPawns.Any()) return;
        SceneController.Instance.ChangeScene("StageScene");
    }

    public void OnClickDeckSetting()
    {
        UIManager.Instance.OpenView<DeckSettingPage>();
    }

    public void OnClickPawnManage()
    {
        UIManager.Instance.OpenView<PawnManagePage>();    
    }

    public void OnClickShop()
    {
        UIManager.Instance.OpenView<ShopPage>();
    }

    public void OnClickRecruit()
    {
        UIManager.Instance.OpenView<RecruitPage>();
    }

    public void OnClickExit()
    {
        UIManager.Instance.OpenView<PausePopup>();
    }
    #endregion Events
}
