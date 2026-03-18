using UnityEngine;
using SA.UI;
using UnityEngine.UI;

[AssetPath("Prefabs/UI/Pause/PausePopup")]
public class PausePopup : PopupView
{
    #region Linker
    [Linker("Root/Button_Continue")]
    public Button _buttonContinue;
    [Linker("Root/Button_Option")]
    public Button _buttonOption;
    [Linker("Root/Button_Exit")]
    public Button _buttonExit;
    #endregion Linker

    public override void PreOpen()
    {
        _buttonContinue.onClick.RemoveAllListeners();
        _buttonContinue.onClick.AddListener(OnClickContinue);

        _buttonOption.onClick.RemoveAllListeners();
        _buttonOption.onClick.AddListener(OnClickOption);

        _buttonExit.onClick.RemoveAllListeners();
        _buttonExit.onClick.AddListener(OnClickExit);
    }

    #region Events
    public void OnClickContinue()
    {
        Close();
    }

    public void OnClickOption()
    {        
        UIManager.Instance.OpenView<OptionPopup>();
    }

    public void OnClickExit()
    {
        SceneController.Instance.ReturnToTitle();

//#if UNITY_EDITOR
//        UnityEditor.EditorApplication.isPlaying = false;
//#else
//        Application.Quit();
//#endif
    }
    #endregion Events
}
