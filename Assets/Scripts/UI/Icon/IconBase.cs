using UnityEngine;
using UnityEngine.UI;

public class IconBase : MonoBehaviourEx
{
    #region Link
    [Linker("Root/Image_BG")]
    public Image _imageBG;
    [Linker("Root/RawImage_Icon")]
    public RawImage _rawIcon;
    [Linker("Root/RawImage_SubIcon")]
    public RawImage _rawSubIcon;
    #endregion Link
}
