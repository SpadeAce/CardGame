using UnityEngine;
using System.Collections.Generic;

public class ItemManager : MonoSingleton<ItemManager>
{
    private readonly List<DItem> _itemList = new();
    private readonly List<DEquipment> _equipList = new();

    public IReadOnlyList<DEquipment> Equips => _equipList;

    public void AddEquip(DEquipment equip)
        => _equipList.Add(equip);
}
