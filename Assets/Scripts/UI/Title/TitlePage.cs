using UnityEngine;
using SA.UI;
using UnityEngine.UI;

[AssetPath("Prefabs/UI/Title/TitlePage")]
public class TitlePage : PageView
{
    #region Linker
    [Linker("Root/Button_NewGame")]
    public Button _buttonNewGame;
    [Linker("Root/Button_LoadGame")]
    public Button _buttonLoadGame;
    [Linker("Root/Button_Option")]
    public Button _buttonOption;
    [Linker("Root/Button_Exit")]
    public Button _buttonExit;
    #endregion Linker

    public override void PreOpen()
    {
        _buttonNewGame.onClick.RemoveAllListeners();
        _buttonNewGame.onClick.AddListener(OnClickNewGame);

        _buttonLoadGame.onClick.RemoveAllListeners();
        _buttonLoadGame.onClick.AddListener(OnClickLoadGame);

        _buttonOption.onClick.RemoveAllListeners();
        _buttonOption.onClick.AddListener(OnClickOption);

        _buttonExit.onClick.RemoveAllListeners();
        _buttonExit.onClick.AddListener(OnClickExit);
    }

    #region Events
    public void OnClickNewGame()
    {
        SceneController.Instance.ChangeScene("LobbyScene");
    }

    public void OnClickLoadGame()
    {
        SceneController.Instance.ChangeScene("LobbyScene");
    }

    public void OnClickOption()
    {
        UIManager.Instance.OpenView<OptionPopup>();
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
