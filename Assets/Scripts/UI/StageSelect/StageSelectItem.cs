using UnityEngine;
using UnityEngine.UI;

public class StageSelectItem : MonoBehaviourEx
{
    #region Links
    [Linker("")]
    public Button _buttonBase;
    [Linker("Image_Difficulty/Text_Difficulty")]
    public Text _textDifficulty;
    [Linker("Text_Location")]
    public Text _textLocation;
    [Linker("Text_Enemy")]
    public Text _textEnemy;
    [Linker("Text_Description")]
    public Text _text_Description;
    #endregion Links   
}
