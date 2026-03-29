using System;
using System.Collections.Generic;
using SA;
using SA.UI;
using UnityEngine;

[AssetPath("Prefabs/UI/Info/InfoPopup")]
public class InfoPopup : PopupView
{
    public enum ObjectType {NONE, ENEMY, PAWN, CARD, ITEM, EQUIPMENT}
    #region Inner Class
    [Serializable]
    public class InfoPreset
    {
        public ObjectType type;
        public GameObject prefab;
    }
    #endregion Inner Class

    #region Links
    public List<InfoPreset> _infoPresetList = new List<InfoPreset>();

    [Linker("Root")]
    public GameObject _root;
    #endregion Links

    private ObjectType currentType = ObjectType.NONE;
    private BaseInfoPreset currentPreset;

    public void SetData(DObject obj)
    {
        ObjectType targetType = ObjectType.NONE;
        

    }
}
