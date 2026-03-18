using UnityEngine;
using UnityEngine.UI;

public class PawnIcon : IconBase
{  
    #region Link
    [Linker("Root/Text_Name")]
    public Text _textName;
    [Linker("Root/Text_Class")]
    public Text _textClass;
    #endregion Link

    public void SetData(DPawn pawn)
    {
        _textName.text = pawn.CodeName;
        _textClass.text = pawn.Data.ClassType.ToString();
        _rawIcon.texture = Resources.Load<Texture>(pawn.IconPath);
    }
}
