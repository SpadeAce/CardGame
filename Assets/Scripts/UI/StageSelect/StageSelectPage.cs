using SA.UI;
using UnityEngine;
using UnityEngine.UI;

[AssetPath("Prefabs/UI/Shop/ShopPage")]
public class StageSelectPage : PageView
{
    #region Linker
    [Linker("Root/StageGroup")]
    public GameObject _goStageGroup;
    [Linker("Root/Button_Close")]
    public Button _buttonClose;
    #endregion Linker

    
}
