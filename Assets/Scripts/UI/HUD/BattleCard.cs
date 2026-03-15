using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BattleCard : MonoBehaviourEx, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    #region Link
    [Linker("Root")]
    public GameObject _root;

    [Linker("Root/Image_EnergyCost/Text_Cost")]
    public Text _textEnergyCost;
    [Linker("Root/Image_AmmoCost/Text_Cost")]
    public Text _textAmmoCost;

    [Linker("Root/Text_Name")]
    public Text _textName;
    [Linker("Root/Text_Desc")]
    public Text _textDesc;
    [Linker("Root/RawImage_Icon")]
    public RawImage _rawIcon;
    [Linker("Root/Image_Dimmed")]
    public Image _imageDimmed;
    #endregion Link

    private DCard _card;
    private Vector2 _dragStartPosition;
    private bool _isDragging;
    private bool _isDisabled;

    public DCard Card => _card;
    public Vector2 DragStartPosition => _dragStartPosition;

    public event System.Action<BattleCard, PointerEventData> OnDragBegin;
    public event System.Action<BattleCard, PointerEventData> OnDragging;
    public event System.Action<BattleCard, PointerEventData> OnDragEnd;

    public void SetData(DCard card)
    {
        _card = card;

        _textEnergyCost.text = card.Data.EnergyCost.ToString();
        _textAmmoCost.text = card.Data.AmmoCost.ToString();

        _textName.text = TextManager.Instance.Get(card.Data.Name);
        _textDesc.text = TextManager.Instance.Get(card.Data.Desc);

        _rawIcon.texture = Resources.Load<Texture>(card.Data.IconPath);
    }

    public void SetVisible(bool visible) => _root.SetActive(visible);

    public void SetDisabled(bool disabled)
    {
        _isDisabled = disabled;
        _imageDimmed.gameObject.SetActive(disabled);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_isDisabled) return;
        _dragStartPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isDisabled) return;
        if (!_isDragging)
        {
            _isDragging = true;
            OnDragBegin?.Invoke(this, eventData);
        }
        OnDragging?.Invoke(this, eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_isDisabled) return;
        _isDragging = false;
        OnDragEnd?.Invoke(this, eventData);
    }
}
