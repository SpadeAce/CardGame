using GameData;

/// <summary>
/// 테이블 데이터 매니저.
/// LoadAll()을 게임 진입 시점에 한 번 호출한 뒤
/// DataManager.Instance.{Table}.Get(id) 형태로 데이터를 조회한다.
/// </summary>
public class DataManager : Singleton<DataManager>
{
    public PawnTable Pawn { get; private set; }
    public MonsterTable Monster { get; private set; }
    public CardTable Card { get; private set; }
    public CardEffectTable CardEffect { get; private set; }
    public TileEntityTable TileEntity { get; private set; }
    public NamePresetTable NamePreset { get; private set; }
    public ShopTable Shop { get; private set; }
    public EquipmentDataTable Equipment { get; private set; }


    public void LoadAll()
    {
        Pawn = new PawnTable();
        Pawn.Load();

        Monster = new MonsterTable();
        Monster.Load();

        Card = new CardTable();
        Card.Load();

        CardEffect = new CardEffectTable();
        CardEffect.Load();

        TileEntity = new TileEntityTable();
        TileEntity.Load();

        NamePreset = new NamePresetTable();
        NamePreset.Load();

        Shop = new ShopTable();
        Shop.Load();

        Equipment = new EquipmentDataTable();
        Equipment.Load();
    }
}