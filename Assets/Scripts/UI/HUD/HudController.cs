using System.Collections.Generic;
using UnityEngine;

public class HudController : MonoBehaviour
{
    private const int PoolInitSize = 10;
    private const float StackOffset = 0.4f;

    private FloatingText _prefab;
    private readonly Queue<FloatingText> _pool = new();
    private readonly Dictionary<Transform, int> _activeCount = new();

    public void Init()
    {
        _prefab = PrefabLoader.Load<FloatingText>();
        for (int i = 0; i < PoolInitSize; i++)
            _pool.Enqueue(CreateInstance());
    }

    public void ShowFloatingText(FloatingTextType type, string text,
                                  Vector3 worldPos, Transform actor = null)
    {
        float yOffset = 0.5f;
        if (actor != null)
        {
            _activeCount.TryGetValue(actor, out int count);
            yOffset += count * StackOffset;
            _activeCount[actor] = count + 1;
        }

        var ft = _pool.Count > 0 ? _pool.Dequeue() : CreateInstance();
        ft.Show(type, text, worldPos + Vector3.up * yOffset, f =>
        {
            if (actor != null && _activeCount.ContainsKey(actor))
            {
                _activeCount[actor]--;
                if (_activeCount[actor] <= 0)
                    _activeCount.Remove(actor);
            }
            _pool.Enqueue(f);
        });
    }

    public void ClearAll()
    {
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf)
                child.gameObject.SetActive(false);
        }
        _activeCount.Clear();
    }

    private FloatingText CreateInstance()
    {
        var ft = Instantiate(_prefab, transform);
        ft.gameObject.SetActive(false);
        return ft;
    }
}
