using System;
using UnityEngine;
using UnityEngine.UI;

[AssetPath("Prefabs/UI/Recruit/RecruitItem")]
public class RecruitItem : MonoBehaviourEx
{
    #region Links
    [Linker("Image_Name/Text_Name")]
    public Text _textName;
    [Linker("Text_Class")]
    public Text _textClass;
    [Linker("RawImage_Texture")]
    public RawImage _rawTexture;
    [Linker("Button_Recruit")]
    public Button _buttonRecruit;
    [Linker("Button_Recruit/Text_Price")]
    public Text _textPrice;
    #endregion Links

    private const int RecruitPrice = 100;

    DPawn _pawn;

    public event Action<RecruitItem, DPawn> OnRecruited;

    void Start()
    {
        _buttonRecruit.onClick.RemoveAllListeners();
        _buttonRecruit.onClick.AddListener(OnClickRecruit);
    }

    public void SetData(DPawn pawn)
    {
        _pawn = pawn;
        _textName.text = pawn.CodeName;
        _textClass.text = pawn.Data.ClassType.ToString();
        _textPrice.text = RecruitPrice.ToString();
    }

    public void OnClickRecruit()
    {
        if (!DeckManager.Instance.SpendGold(RecruitPrice)) return;
        PawnManager.Instance.AddPawn(_pawn);
        OnRecruited?.Invoke(this, _pawn);
    }
}
