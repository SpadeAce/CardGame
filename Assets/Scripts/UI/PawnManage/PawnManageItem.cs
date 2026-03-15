using System;
using UnityEngine;
using UnityEngine.EventSystems;

[AssetPath("Prefabs/UI/PawnManage/PawnManageItem")]
public class PawnManageItem : MonoBehaviourEx, IPointerClickHandler
{
    #region Linker
    [Linker("Spawn_Icon")]
    public Spawn _spawnIcon;
    [Linker("Image_Selected")]
    public GameObject _goSelected;
    #endregion Linker

    private DPawn _pawn;
    public DPawn Pawn => _pawn;

    public event Action<PawnManageItem> OnSelected;

    public void SetData(DPawn pawn)
    {
        _pawn = pawn;
        _spawnIcon.Get<PawnIcon>().SetData(pawn);
        _goSelected.SetActive(false);
    }

    public void SetSelected(bool selected) => _goSelected.SetActive(selected);

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Left)
            OnSelected?.Invoke(this);
    }
}
