using UnityEngine;

/// <summary>
/// 타일 슬롯 — SquareTile, EdgeTile 모두에서 사용하는 엔티티 배치 슬롯
/// </summary>
public class TileSlot : MonoBehaviourEx
{
    public TileEntity SlotEntity { get; private set; }

    public void SetEntity(TileEntity entity)
    {
        SlotEntity = entity;
        entity.SetSlot(this);
    }

    public void ClearEntity()
    {
        SlotEntity = null;
    }
}
