using UnityEngine;
using SA.UI;
using UnityEngine.UI;
using System;


[AssetPath("Prefabs/UI/Notice/NoticePopup")]
public class NoticePopup : PopupView
{
    #region Linker
    [Linker("Root/Text_Title")]
    public Text _textTitle;
    [Linker("Root/Text_Body")]
    public Text _textBody;

    [Linker("Root/Button_Confirm")]
    public Button _buttonConfirm;
    [Linker("Root/Button_Cancel")]
    public Button _buttonCancel;
    #endregion Linker

    Action _onClickConfirm;
    Action _onClickCancel;

    public void Init(string title, string body, Action onClickConfirm, Action onClickCancel = null)
    {
        _textTitle.text = title;
        _textBody.text = body;
        _buttonConfirm.onClick.RemoveAllListeners();
        _buttonConfirm.onClick.AddListener(OnClickConfirm);
        _buttonCancel.onClick.RemoveAllListeners();
        _buttonCancel.onClick.AddListener(OnClickCancel);

        _onClickConfirm = onClickConfirm;
        _onClickCancel = onClickCancel;
    }

    void OnClickConfirm()
    {
        _onClickConfirm?.Invoke();
        Close();   
    }

    void OnClickCancel()
    {
        _onClickCancel?.Invoke();
        Close();
    }
}
