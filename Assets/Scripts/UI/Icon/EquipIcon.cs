using UnityEngine;

public class EquipIcon : IconBase
{  
    public void SetData(DEquipment equip)
    {
        _rawIcon.texture = Resources.Load<Texture>(equip.Data.IconPath);
    }
}
