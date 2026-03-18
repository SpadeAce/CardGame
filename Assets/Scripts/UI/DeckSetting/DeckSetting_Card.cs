using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeckSetting_Card : MonoBehaviourEx
{
    #region Links
    [Linker("Scroll_CardList")]
    public ScrollRect _scrollCardList;
    #endregion Links

    private readonly List<DeckCardItem> _activeItems = new();

    public void OnOpened()
    {
        RefreshCardList();
    }

    private void RefreshCardList()
    {
        foreach (var item in _activeItems)
            if (item != null) Destroy(item.gameObject);
        _activeItems.Clear();

        var cards = DeckManager.Instance.DeckCards;
        if (cards.Count == 0) return;

        var prefab = PrefabLoader.Load<DeckCardItem>();

        const int itemsPerRow = 5;
        const float itemH = 350f;

        float slotW = _scrollCardList.viewport.rect.width / itemsPerRow;

        for (int i = 0; i < cards.Count; i++)
        {
            int row = i / itemsPerRow;
            int col = i % itemsPerRow;

            var item = Instantiate(prefab, _scrollCardList.content);
            var rt = item.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(
                col * slotW + slotW / 2f,
                -(row * itemH + itemH / 2f));

            item.SetData(cards[i]);
            _activeItems.Add(item);
        }

        int rowCount = Mathf.CeilToInt((float)cards.Count / itemsPerRow);
        _scrollCardList.content.sizeDelta = new Vector2(
            _scrollCardList.content.sizeDelta.x,
            itemH * rowCount);
    }
}
