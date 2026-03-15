using System.Collections.Generic;
using GameData;
using SA;

public class DCard : DItem
{
    public int cardId {get; private set;}
    public CardData Data { get; private set; }

    public DCard(int cardId)
    {
        this.cardId = cardId;
        this.Data = DataManager.Instance.Card.Get(cardId);
    }

    public DCard(CardData data)
    {
        this.cardId = data.Id;
        this.Data = data;
    }
}
