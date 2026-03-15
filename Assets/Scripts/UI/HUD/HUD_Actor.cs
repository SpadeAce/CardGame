using UnityEngine;
using UnityEngine.UI;

public class HUD_Actor : MonoBehaviour
{
    [SerializeField] private Image      _imageHP;
    [SerializeField] private Image      _imageShield;
    [SerializeField] private Text       _textArmor;
    [SerializeField] private Text       _textActingPower;
    [SerializeField] private GameObject _goActingPower;
    [SerializeField] private Text _textName;

    private DPawn    _pawn;
    private DMonster _monster;

    public void Init(DPawn pawn)
    {
        if (_pawn != null)    _pawn.onStatsChanged    -= Refresh;
        if (_monster != null) _monster.onStatsChanged -= Refresh;

        _pawn    = pawn;
        _monster = null;

        if (_goActingPower != null) _goActingPower.SetActive(true);

        _pawn.onStatsChanged += Refresh;
        Refresh();
    }

    public void Init(DMonster monster)
    {
        if (_pawn != null)    _pawn.onStatsChanged    -= Refresh;
        if (_monster != null) _monster.onStatsChanged -= Refresh;

        _monster = monster;
        _pawn    = null;

        if (_goActingPower != null) _goActingPower.SetActive(false);

        _monster.onStatsChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (_pawn != null)    _pawn.onStatsChanged    -= Refresh;
        if (_monster != null) _monster.onStatsChanged -= Refresh;
    }

    private void Refresh()
    {
        if (_pawn != null)
        {
            int maxHp = _pawn.Data.Hp;
            if (_imageHP != null) _imageHP.fillAmount = maxHp > 0 ? (float)_pawn.HP / maxHp : 0f;
            if (_imageShield != null)
            {
                bool hasShield = _pawn.Shield > 0;
                _imageShield.gameObject.SetActive(hasShield);
                if (hasShield) _imageShield.fillAmount = maxHp > 0 ? Mathf.Min(1f, (float)_pawn.Shield / maxHp) : 0f;
            }
            if (_textArmor       != null) _textArmor.text       = _pawn.Armor.ToString();
            if (_textActingPower != null) _textActingPower.text = _pawn.Ammo.ToString();
            if(_textName != null) _textName.text = _pawn.CodeName;
        }
        else if (_monster != null)
        {
            int maxHp = _monster.Data.Hp;
            if (_imageHP != null) _imageHP.fillAmount = maxHp > 0 ? (float)_monster.HP / maxHp : 0f;
            if (_imageShield != null)
            {
                bool hasShield = _monster.Shield > 0;
                _imageShield.gameObject.SetActive(hasShield);
                if (hasShield) _imageShield.fillAmount = maxHp > 0 ? Mathf.Min(1f, (float)_monster.Shield / maxHp) : 0f;
            }
            if (_textArmor != null) _textArmor.text = _monster.Armor.ToString();
        }
    }

    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        transform.rotation = cam.transform.rotation;
    }
}
