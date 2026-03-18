using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum FloatingTextType
{
    Damage,
    Heal,
    Shield,
    Buff,
    Debuff,
    Block
}

[AssetPath("Prefabs/UI/HUD/FloatingText")]
public class FloatingText : MonoBehaviourEx
{
    private const float BaseScale = 0.33f;

    [SerializeField] private Text _text;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Outline _outline;

    private System.Action<FloatingText> _onComplete;

    public void Show(FloatingTextType type, string text, Vector3 worldPos,
                     System.Action<FloatingText> onComplete)
    {
        _text.text = text;
        _text.color = GetColor(type);
        transform.position = worldPos;
        transform.localScale = Vector3.one * BaseScale;
        gameObject.SetActive(true);
        _onComplete = onComplete;

        StopAllCoroutines();
        StartCoroutine(AnimateByType(type, worldPos));
    }

    private IEnumerator AnimateByType(FloatingTextType type, Vector3 origin)
    {
        switch (type)
        {
            case FloatingTextType.Damage:
                yield return DamageBounce(origin);
                break;
            case FloatingTextType.Heal:
            case FloatingTextType.Shield:
                yield return VerticalRise(origin);
                break;
            default:
                yield return PopInPlace(origin);
                break;
        }
        gameObject.SetActive(false);
        _onComplete?.Invoke(this);
    }

    /// <summary>
    /// 대각 포물선 + 바운스 연출
    /// </summary>
    private IEnumerator DamageBounce(Vector3 origin)
    {
        float duration = 1.0f;
        float gravity = 6f;
        float horizontalSpeed = Random.Range(0.3f, 0.6f) * (Random.value > 0.5f ? 1 : -1);
        float verticalSpeed = 2.5f;

        Vector3 pos = origin;
        Vector3 velocity = new Vector3(horizontalSpeed, verticalSpeed, 0);
        float elapsed = 0f;
        float bounceDamping = 0.4f;
        float groundY = origin.y;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            velocity.y -= gravity * Time.deltaTime;
            pos += velocity * Time.deltaTime;

            if (pos.y < groundY && velocity.y < 0)
            {
                velocity.y = -velocity.y * bounceDamping;
                velocity.x *= bounceDamping;
                pos.y = groundY;
            }

            transform.position = pos;

            float alpha = elapsed < duration * 0.6f ? 1f : 1f - (elapsed - duration * 0.6f) / (duration * 0.4f);
            SetAlpha(alpha);
            Billboard();
            yield return null;
        }
    }

    /// <summary>
    /// 수직 상승 + 페이드아웃 연출
    /// </summary>
    private IEnumerator VerticalRise(Vector3 origin)
    {
        float duration = 0.8f;
        float riseHeight = 1.0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.position = origin + Vector3.up * (riseHeight * t);

            float alpha = t < 0.5f ? 1f : 1f - (t - 0.5f) * 2f;
            SetAlpha(alpha);
            Billboard();
            yield return null;
        }
    }

    /// <summary>
    /// 제자리 팝 (스케일 확대 → 축소 → 페이드아웃) 연출
    /// </summary>
    private IEnumerator PopInPlace(Vector3 origin)
    {
        float duration = 0.6f;
        float elapsed = 0f;
        transform.position = origin;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float scale;
            if (t < 0.15f)
                scale = Mathf.Lerp(0.5f, 1.3f, t / 0.15f);
            else if (t < 0.3f)
                scale = Mathf.Lerp(1.3f, 1.0f, (t - 0.15f) / 0.15f);
            else
                scale = 1.0f;
            transform.localScale = Vector3.one * (scale * BaseScale);

            float alpha = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            SetAlpha(alpha);
            Billboard();
            yield return null;
        }
        transform.localScale = Vector3.one * BaseScale;
    }

    private void SetAlpha(float alpha)
    {
        var c = _text.color;
        _text.color = new Color(c.r, c.g, c.b, alpha);
    }

    private void Billboard()
    {
        Camera cam = Camera.main;
        if (cam != null)
            transform.rotation = cam.transform.rotation;
    }

    private static Color GetColor(FloatingTextType type) => type switch
    {
        FloatingTextType.Damage => Color.red,
        FloatingTextType.Heal   => Color.green,
        FloatingTextType.Shield => Color.cyan,
        FloatingTextType.Buff   => new Color(1f, 0.8f, 0f),
        FloatingTextType.Debuff => new Color(0.7f, 0.3f, 0.9f),
        FloatingTextType.Block  => Color.gray,
        _ => Color.white,
    };
}
