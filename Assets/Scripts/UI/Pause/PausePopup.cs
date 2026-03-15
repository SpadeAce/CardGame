using UnityEngine;
using SA.UI;
using UnityEngine.UI;

[AssetPath("Prefabs/UI/Pause/PausePopup")]
public class PausePopup : PopupView
{
    #region Linker
    [Linker("Root/Button_Continue")]
    public Button _buttonContinue;

    [Linker("Root/Button_Exit")]
    public Button _buttonExit;
    #endregion Linker

    public override void PreOpen()
    {
        _buttonContinue.onClick.RemoveAllListeners();
        _buttonContinue.onClick.AddListener(OnClickContinue);

        _buttonExit.onClick.RemoveAllListeners();
        _buttonExit.onClick.AddListener(OnClickExit);
    }

    #region Events
    public void OnClickContinue()
    {
        Close();
    }

    public void OnClickExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    #endregion Events
}
