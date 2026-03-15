using UnityEngine;
using SA.UI;
using UnityEngine.UI;

[AssetPath("Prefabs/UI/BattleResult/BattleResultPopup")]
public class BattleResultPopup : PopupView
{
    public class BattleResultParam : ViewParam
    {
        public bool isWin;    
    }

    #region Linker
    [Linker("Root/Text_Title")]
    public Text _textTitle;
    [Linker("Root/Button_Exit")]
    public Button _buttonExit;
    [Linker("Root/Button_Continue")]
    public Button _buttonContinue;
    #endregion Linker

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
    }

    #region Events
    public void OnClickContinue()
    {
        SceneController.Instance.ChangeScene("LobbyScene");
    }

    public void OnClickExit()
    {
        SceneController.Instance.ChangeScene("TitleScene");
    }
    #endregion Events
}
