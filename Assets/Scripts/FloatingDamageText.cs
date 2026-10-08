using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý hiển thị số nảy sát thương (Floating Damage Text) dạng World-Space.
/// - Bay lên và mờ dần khi Enemy hoặc Player nhận sát thương.
/// - Màu sắc: Vàng/Cam khi quái vật bị bắn trúng, Đỏ khi Player bị đánh trúng.
/// - Luôn xoay mặt về phía Camera chính (Billboard).
/// - Tối ưu hiệu năng cho chip Snapdragon 810.
/// </summary>
public class FloatingDamageText : MonoBehaviour
{
    private static Font cachedFont;

    public static void Spawn(Vector3 worldPosition, float damageAmount, Color textColor, string prefix = "-")
    {
        if (damageAmount <= 0f) return;

        GameObject go = new GameObject("[Damage_Popup]");
        go.transform.position = worldPosition + Vector3.up * 0.5f + Random.insideUnitSphere * 0.25f;

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        CanvasScaler cs = go.AddComponent<CanvasScaler>();
        cs.dynamicPixelsPerUnit = 25;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(2f, 1f);
        rt.localScale = Vector3.one * 0.035f;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(go.transform, false);

        Text text = textObj.AddComponent<Text>();
        if (cachedFont == null)
        {
            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        text.font = cachedFont;
        text.fontSize = 28;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = textColor;
        text.text = $"{prefix}{Mathf.RoundToInt(damageAmount)}";

        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        RectTransform tRt = textObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;

        var anim = go.AddComponent<FloatingDamageText>();
        anim.StartAnimation(text);
    }

    public static void SpawnResource(Vector3 worldPosition, string displayText, Color textColor, float customScale = 0.07f)
    {
        GameObject go = new GameObject("[Resource_Popup]");
        go.transform.position = worldPosition + Vector3.up * 0.5f + Random.insideUnitSphere * 0.2f;

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        CanvasScaler cs = go.AddComponent<CanvasScaler>();
        cs.dynamicPixelsPerUnit = 25;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(4f, 1.5f);
        rt.localScale = Vector3.one * customScale;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(go.transform, false);

        Text text = textObj.AddComponent<Text>();
        if (cachedFont == null)
        {
            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        text.font = cachedFont;
        text.fontSize = 32;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = textColor;
        text.text = displayText;

        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        outline.effectDistance = new Vector2(2f, -2f);

        RectTransform tRt = textObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;

        var anim = go.AddComponent<FloatingDamageText>();
        anim.StartAnimation(text);
    }

    private void StartAnimation(Text textComp)
    {
        StartCoroutine(AnimateRoutine(textComp));
    }

    private IEnumerator AnimateRoutine(Text textComp)
    {
        Camera cam = Camera.main;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * 1.4f + transform.right * Random.Range(-0.4f, 0.4f);

        float duration = 0.75f;
        float elapsed = 0f;
        Color initialColor = textComp != null ? textComp.color : Color.yellow;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Di chuyển nảy lên mượt mà (Ease Out)
            float moveT = Mathf.Sin(t * Mathf.PI * 0.5f);
            transform.position = Vector3.Lerp(startPos, endPos, moveT);

            // Scale đàn hồi nảy nhẹ
            float scaleMul = (t < 0.25f) ? Mathf.Lerp(0.5f, 1.3f, t / 0.25f) : Mathf.Lerp(1.3f, 1.0f, (t - 0.25f) / 0.75f);
            transform.localScale = Vector3.one * (0.035f * scaleMul);

            // Xoay mặt về camera
            if (cam != null)
            {
                transform.rotation = cam.transform.rotation;
            }

            // Mờ dần về cuối
            if (textComp != null && t > 0.4f)
            {
                float alpha = Mathf.Lerp(1f, 0f, (t - 0.4f) / 0.6f);
                Color c = initialColor;
                c.a = alpha;
                textComp.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
