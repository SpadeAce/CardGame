using SA;
using UnityEngine;

public abstract class TileEntity : MonoBehaviourEx
{
    public DObject Data {get;private set;}
    public TileSlot Slot { get; private set; }

    protected void SetData(DObject data) { Data = data; }

    public void SetSlot(TileSlot slot)
    {
        Slot = slot;
        transform.parent = slot.transform;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }
}
