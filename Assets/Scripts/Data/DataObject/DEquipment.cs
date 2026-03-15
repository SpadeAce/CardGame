using GameData;
using SA;

public class DEquipment : DObject
{
    public int equipmentId {get; private set;}
    public EquipmentData Data {get; private set;}

    public DEquipment(int equipId)
    {
        this.equipmentId = equipId;
        this.Data = DataManager.Instance.Equipment.Get(equipId);
    }

    public DEquipment(EquipmentData data)
    {
        this.equipmentId = data.Id;
        this.Data = data;
    }

    public long EquippedPawnId { get; private set; } = 0;
    public bool IsEquipped => EquippedPawnId != 0;

    public void SetEquippedPawn(long pawnInstanceId) => EquippedPawnId = pawnInstanceId;
    public void ClearEquippedPawn() => EquippedPawnId = 0;
}