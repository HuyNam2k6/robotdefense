using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CenterWarningBanner - Thông Báo Cảnh Báo To Rõ Ràng Nằm Chính Giữa Màn Hình:
/// - Khi người chơi nhấn nâng cấp vũ khí/nhà máy mà cấp độ Nhà Chính (Lò Năng Lượng) chưa đạt yêu cầu:
///   Hiển thị thông báo to rõ ràng giữa màn: "⚠️ Vui lòng nâng cấp Lò Năng Lượng (Nhà Chính) lên Cấp X!"
/// - Thiết kế chuẩn Mobile dọc, chữ to rõ ràng, viền phát sáng cam/vàng neon,
///   hiệu ứng phóng to đàn hồi (Punch scale) và tự động ẩn sau 3 giây.
/// </summary>
public class CenterWarningBanner : MonoBehaviour
{
    public static CenterWarningBanner Instance { get; private set; }

    private GameObject bannerObj;
    private RectTransform bannerRect;
    private CanvasGroup canvasGroup;
    private Text mainTitleText;
    private Text subtitleText;
    private Coroutine activeBannerCoroutine;

    private static Font cachedFont;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        if (Instance == null)
        {
            Canvas canvas = WallSelectionManager.GetMainScreenCanvas();
            if (canvas != null && canvas.GetComponentInChildren<CenterWarningBanner>() == null)
            {
                GameObject holder = new GameObject("[CenterWarningBanner_Manager]");
                holder.transform.SetParent(canvas.transform, false);
                holder.AddComponent<CenterWarningBanner>();
            }
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnsureUIElements();
    }

    private void EnsureUIElements()
    {
        if (bannerObj != null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = WallSelectionManager.GetMainScreenCanvas();
        if (canvas == null) return;

        cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 1. Khung chứa Banner to ở giữa màn hình
        bannerObj = new GameObject("Center_Warning_Banner", typeof(RectTransform), typeof(CanvasGroup));
        bannerObj.layer = LayerMask.NameToLayer("UI");
        bannerObj.transform.SetParent(canvas.transform, false);

        canvasGroup = bannerObj.GetComponent<CanvasGroup>();

        bannerRect = bannerObj.GetComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
        bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
        bannerRect.pivot = new Vector2(0.5f, 0.5f);
        bannerRect.anchoredPosition = new Vector2(0f, 60f); // Nằm chính diện tâm màn hình
        bannerRect.sizeDelta = new Vector2(860f, 240f);

        // Nền tối bán trong suốt cao cấp
        Image bgImg = bannerObj.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.08f, 0.14f, 0.96f);

        // Viền phát sáng vàng cam cảnh báo rực rỡ
        Outline outline = bannerObj.AddComponent<Outline>();
        outline.effectColor = new Color(1.0f, 0.45f, 0.1f, 0.95f);
        outline.effectDistance = new Vector2(3f, -3f);

        // Dải màu trang trí phía trên (Top Accent Line)
        GameObject accentObj = new GameObject("AccentLine", typeof(RectTransform), typeof(Image));
        accentObj.transform.SetParent(bannerObj.transform, false);
        RectTransform aRt = accentObj.GetComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0f, 1f);
        aRt.anchorMax = new Vector2(1f, 1f);
        aRt.pivot = new Vector2(0.5f, 1f);
        aRt.sizeDelta = new Vector2(0f, 6f);
        accentObj.GetComponent<Image>().color = new Color(1f, 0.75f, 0.1f, 1f);

        // Tiêu đề to chính diện (Main Title)
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(bannerObj.transform, false);
        RectTransform tRt = titleObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 0.5f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -14f);
        tRt.sizeDelta = new Vector2(-40f, 0f);

        mainTitleText = titleObj.GetComponent<Text>();
        mainTitleText.font = cachedFont;
        mainTitleText.fontSize = 28;
        mainTitleText.fontStyle = FontStyle.Bold;
        mainTitleText.alignment = TextAnchor.MiddleCenter;
        mainTitleText.color = new Color(1f, 0.88f, 0.2f, 1f);
        mainTitleText.supportRichText = true;

        Outline titleOutline = titleObj.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        titleOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // Nội dung mô tả chi tiết (Subtitle / Detail)
        GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(Text));
        subObj.transform.SetParent(bannerObj.transform, false);
        RectTransform sRt = subObj.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 0f);
        sRt.anchorMax = new Vector2(1f, 0.5f);
        sRt.pivot = new Vector2(0.5f, 0f);
        sRt.anchoredPosition = new Vector2(0f, 16f);
        sRt.sizeDelta = new Vector2(-50f, 0f);

        subtitleText = subObj.GetComponent<Text>();
        subtitleText.font = cachedFont;
        subtitleText.fontSize = 22;
        subtitleText.alignment = TextAnchor.MiddleCenter;
        subtitleText.color = new Color(0.95f, 0.95f, 1f, 0.95f);
        subtitleText.supportRichText = true;

        bannerObj.SetActive(false);
    }

    /// <summary>
    /// Hiển thị thông báo to rõ ràng giữa màn hình theo đúng yêu cầu:
    /// "Vui lòng nâng cấp lò năng lượng (nhà chính) lên cấp {requiredLevel} để nâng cấp {buildingName}!"
    /// </summary>
    public static void ShowUpgradeBlocked(string buildingName, int currentBuildingLevel, int targetBuildingLevel)
    {
        int requiredHQLevel = Mathf.Min(targetBuildingLevel, 4);
        string title = $"⚠️ VUI LÒNG NÂNG CẤP LÒ NĂNG LƯỢNG LÊN CẤP {requiredHQLevel}!";
        string desc = (targetBuildingLevel >= 5)
            ? $"<b>{buildingName}</b> đang ở Cấp {currentBuildingLevel}. Hãy nâng Lò Năng Lượng lên <color=#00FFFF>Cấp 4 (MAX)</color> để mở khóa công trình lên Cấp {targetBuildingLevel}!"
            : $"<b>{buildingName}</b> đang ở Cấp {currentBuildingLevel}. Để nâng cấp lên <color=#00FFFF>Cấp {targetBuildingLevel}</color>, cần nâng cấp Lò Năng Lượng lên Cấp {requiredHQLevel} trước!";
        Show(title, desc, 3.2f);
    }

    /// <summary>
    /// Hiển thị thông báo to giữa màn hình với tiêu đề và mô tả tùy chỉnh
    /// </summary>
    public static void Show(string title, string detail = "", float duration = 3.0f)
    {
        if (Instance == null)
        {
            Canvas canvas = WallSelectionManager.GetMainScreenCanvas();
            if (canvas != null)
            {
                GameObject holder = new GameObject("[CenterWarningBanner_Manager]");
                holder.transform.SetParent(canvas.transform, false);
                Instance = holder.AddComponent<CenterWarningBanner>();
            }
        }

        if (Instance != null)
        {
            Instance.TriggerBanner(title, detail, duration);
        }
    }

    private void TriggerBanner(string title, string detail, float duration)
    {
        EnsureUIElements();
        if (bannerObj == null) return;

        if (activeBannerCoroutine != null)
        {
            StopCoroutine(activeBannerCoroutine);
        }

        activeBannerCoroutine = StartCoroutine(DoBannerAnimation(title, detail, duration));
    }

    private IEnumerator DoBannerAnimation(string title, string detail, float duration)
    {
        bannerObj.SetActive(true);
        bannerObj.transform.SetAsLastSibling();

        if (mainTitleText != null) mainTitleText.text = title;
        if (subtitleText != null)
        {
            subtitleText.text = detail;
            subtitleText.gameObject.SetActive(!string.IsNullOrEmpty(detail));
        }

        // Hiệu ứng Punch Scale & Fade In
        float elapsed = 0f;
        float popDuration = 0.25f;

        while (elapsed < popDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / popDuration;
            float scale = Mathf.Lerp(0.75f, 1.05f, Mathf.Sin(t * Mathf.PI * 0.5f));
            bannerRect.localScale = Vector3.one * scale;
            if (canvasGroup != null) canvasGroup.alpha = t;
            yield return null;
        }

        // Ổn định về 1.0f
        elapsed = 0f;
        float settleDuration = 0.12f;
        while (elapsed < settleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / settleDuration;
            bannerRect.localScale = Vector3.one * Mathf.Lerp(1.05f, 1.0f, t);
            yield return null;
        }
        bannerRect.localScale = Vector3.one;
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        // Giữ nguyên hiển thị
        yield return new WaitForSecondsRealtime(duration);

        // Mờ dần Fade Out
        elapsed = 0f;
        float fadeDuration = 0.35f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeDuration;
            if (canvasGroup != null) canvasGroup.alpha = 1f - t;
            bannerRect.localScale = Vector3.one * Mathf.Lerp(1.0f, 0.9f, t);
            yield return null;
        }

        bannerObj.SetActive(false);
        activeBannerCoroutine = null;
    }
}
