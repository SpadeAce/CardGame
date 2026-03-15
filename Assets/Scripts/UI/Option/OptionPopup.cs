using UnityEngine;
using SA.UI;
using UnityEngine.UI;

[AssetPath("Prefabs/UI/Option/OptionPopup")]
public class OptionPopup : PopupView
{
    #region Linker
    [Linker("Root/Button_Confirm")]
    public Button _buttonConfirm;

    [Linker("Root/Button_Cancel")]
    public Button _buttonCancel;
    #endregion Linker

    public override void PreOpen()
    {
        _buttonConfirm.onClick.RemoveAllListeners();
        _buttonConfirm.onClick.AddListener(OnClickConfirm);

        _buttonCancel.onClick.RemoveAllListeners();
        _buttonCancel.onClick.AddListener(OnClickCancel);
    }

    #region Events
    public void OnClickConfirm()
    {
        Close();
    }

    public void OnClickCancel()
    {
        Close();
    }
    #endregion Events
}
