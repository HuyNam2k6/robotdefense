using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// OfflineAFKRewardSystem - Hệ Thống Phần Thưởng Thu Hoạch Ngoại Tuyến (AFK / Idle Rewards):
/// - Khi người chơi thoát game hoặc tắt app, hệ thống tự động ghi nhớ thời gian.
/// - Khi quay lại game, căn cứ tự động tính toán tài nguyên đã sản xuất được trong thời gian vắng mặt.
/// - Hiển thị Popup chào đón sang trọng phong cách Sci-Fi để người chơi nhận thưởng!
/// </summary>
public class OfflineAFKRewardSystem : MonoBehaviour
{
    public static OfflineAFKRewardSystem Instance { get; private set; }

    private const string PREF_KEY_LOGOUT_TIME = "AFK_LastLogoutTime";
    private const int MAX_OFFLINE_SECONDS = 28800; // Giới hạn tối đa 8 tiếng tích lũy khi offline
    private const int MIN_OFFLINE_SECONDS = 15;    // Tối thiểu 15 giây để kích hoạt popup

    private GameObject rewardModalObj;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInit()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("[System_OfflineAFKReward]");
            go.AddComponent<OfflineAFKRewardSystem>();
            DontDestroyOnLoad(go);
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
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(CheckOfflineRewardsDelayed());
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveLogoutTime();
        }
        else
        {
            // Trở lại game sau khi minimize
            StartCoroutine(CheckOfflineRewardsDelayed());
        }
    }

    private void OnApplicationQuit()
    {
        SaveLogoutTime();
    }

    private void SaveLogoutTime()
    {
        PlayerPrefs.SetString(PREF_KEY_LOGOUT_TIME, DateTime.UtcNow.ToBinary().ToString());
        PlayerPrefs.Save();
    }

    private IEnumerator CheckOfflineRewardsDelayed()
    {
        // Chờ các hệ thống GameEconomy & BaseHQ khởi tạo hoàn tất
        yield return new WaitForSeconds(0.8f);

        if (!PlayerPrefs.HasKey(PREF_KEY_LOGOUT_TIME))
        {
            SaveLogoutTime();
            yield break;
        }

        string rawTime = PlayerPrefs.GetString(PREF_KEY_LOGOUT_TIME);
        if (!long.TryParse(rawTime, out long binaryTime))
        {
            SaveLogoutTime();
            yield break;
        }

        DateTime logoutTime = DateTime.FromBinary(binaryTime);
        TimeSpan elapsed = DateTime.UtcNow - logoutTime;
        int totalSeconds = (int)elapsed.TotalSeconds;

        // Cập nhật lại thời gian hiện tại
        SaveLogoutTime();

        if (totalSeconds < MIN_OFFLINE_SECONDS)
        {
            yield break;
        }

        // Giới hạn tối đa 8 tiếng
        int activeSeconds = Mathf.Min(totalSeconds, MAX_OFFLINE_SECONDS);

        // Tính sản lượng tài nguyên
        float goldRate = (GameEconomy.Instance != null) ? GameEconomy.Instance.GetCurrentCoinsPerSecond() : 12f;
        float stoneRate = (GameEconomy.Instance != null) ? GameEconomy.Instance.GetCurrentStonePerSecond() : 2f;
        float woodRate = (GameEconomy.Instance != null) ? GameEconomy.Instance.GetCurrentWoodPerSecond() : 2f;

        int rewardGold = Mathf.Max(20, Mathf.RoundToInt(activeSeconds * goldRate * 0.85f));
        int rewardStone = Mathf.Max(5, Mathf.RoundToInt(activeSeconds * stoneRate * 0.85f));
        int rewardWood = Mathf.Max(5, Mathf.RoundToInt(activeSeconds * woodRate * 0.85f));

        ShowRewardModal(activeSeconds, rewardGold, rewardStone, rewardWood);
    }

    private void ShowRewardModal(int seconds, int gold, int stone, int wood)
    {
        if (rewardModalObj != null) Destroy(rewardModalObj);

        Canvas canvas = WallSelectionManager.GetMainScreenCanvas();
        if (canvas == null) return;

        Font safeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 1. Overlay tối màn hình
        rewardModalObj = new GameObject("Modal_AFK_OfflineRewards", typeof(RectTransform));
        rewardModalObj.layer = LayerMask.NameToLayer("UI");
        rewardModalObj.transform.SetParent(canvas.transform, false);

        RectTransform overlayRt = rewardModalObj.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        Image overlayImg = rewardModalObj.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.75f);

        // 2. Panel nội dung chính
        GameObject boxObj = new GameObject("ContentBox", typeof(RectTransform));
        boxObj.transform.SetParent(rewardModalObj.transform, false);

        RectTransform boxRt = boxObj.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.anchoredPosition = Vector2.zero;
        boxRt.sizeDelta = new Vector2(650f, 440f);

        Image boxBg = boxObj.AddComponent<Image>();
        boxBg.color = new Color(0.08f, 0.11f, 0.18f, 0.98f);

        Outline boxOutline = boxObj.AddComponent<Outline>();
        boxOutline.effectColor = new Color(0.2f, 0.85f, 1f, 0.8f);
        boxOutline.effectDistance = new Vector2(2f, -2f);

        // 3. Tiêu đề
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(boxObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -20f);
        titleRt.sizeDelta = new Vector2(-40f, 44f);

        Text titleText = titleObj.GetComponent<Text>();
        titleText.font = safeFont;
        titleText.fontSize = 24;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.85f, 0.2f, 1f);
        titleText.text = "🎁 BÁO CÁO THU HOẠCH TỰ ĐỘNG (AFK)";

        // 4. Phụ đề thời gian
        string timeStr = FormatTimeString(seconds);
        GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(Text));
        subObj.transform.SetParent(boxObj.transform, false);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0f, 1f);
        subRt.anchorMax = new Vector2(1f, 1f);
        subRt.pivot = new Vector2(0.5f, 1f);
        subRt.anchoredPosition = new Vector2(0f, -68f);
        subRt.sizeDelta = new Vector2(-40f, 32f);

        Text subText = subObj.GetComponent<Text>();
        subText.font = safeFont;
        subText.fontSize = 16;
        subText.alignment = TextAnchor.MiddleCenter;
        subText.color = new Color(0.85f, 0.95f, 1f, 0.9f);
        subText.text = $"Trong thời gian bạn vắng mặt (<b><color=#00FF88>{timeStr}</color></b>), căn cứ đã tích lũy:";

        // 5. Thẻ hiển thị 3 tài nguyên
        GameObject cardRow = new GameObject("ResourceRow", typeof(RectTransform));
        cardRow.transform.SetParent(boxObj.transform, false);
        RectTransform rowRt = cardRow.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0.5f, 0.5f);
        rowRt.anchorMax = new Vector2(0.5f, 0.5f);
        rowRt.pivot = new Vector2(0.5f, 0.5f);
        rowRt.anchoredPosition = new Vector2(0f, 10f);
        rowRt.sizeDelta = new Vector2(580f, 140f);

        HorizontalLayoutGroup hlg = cardRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        CreateResourceCard(cardRow.transform, "🪙 VÀNG", $"+{gold:#,##0}", new Color(1f, 0.85f, 0.2f), new Color(0.25f, 0.20f, 0.05f, 0.9f), safeFont);
        CreateResourceCard(cardRow.transform, "🪨 ĐÁ", $"+{stone:#,##0}", new Color(0.7f, 0.85f, 1f), new Color(0.1f, 0.18f, 0.28f, 0.9f), safeFont);
        CreateResourceCard(cardRow.transform, "🪵 GỖ", $"+{wood:#,##0}", new Color(0.9f, 0.7f, 0.5f), new Color(0.24f, 0.15f, 0.08f, 0.9f), safeFont);

        // 6. Nút "NHẬN TẤT CẢ" to rõ ràng
        GameObject btnObj = new GameObject("Btn_ClaimAll", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(boxObj.transform, false);
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0f);
        btnRt.anchorMax = new Vector2(0.5f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.anchoredPosition = new Vector2(0f, 30f);
        btnRt.sizeDelta = new Vector2(380f, 64f);

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = new Color(0.12f, 0.75f, 0.35f, 1f);

        Outline btnOutline = btnObj.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0f, 0.35f, 0.15f, 0.9f);
        btnOutline.effectDistance = new Vector2(2f, -2f);

        GameObject btnTxtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        RectTransform btnTxtRt = btnTxtObj.GetComponent<RectTransform>();
        btnTxtRt.anchorMin = Vector2.zero;
        btnTxtRt.anchorMax = Vector2.one;
        btnTxtRt.offsetMin = Vector2.zero;
        btnTxtRt.offsetMax = Vector2.zero;

        Text btnText = btnTxtObj.GetComponent<Text>();
        btnText.font = safeFont;
        btnText.fontSize = 20;
        btnText.fontStyle = FontStyle.Bold;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;
        btnText.text = "💎 NHẬN TẤT CẢ PHẦN THƯỞNG";

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            if (GameEconomy.Instance != null)
            {
                GameEconomy.Instance.AddCoins(gold);
                GameEconomy.Instance.AddStone(stone);
                GameEconomy.Instance.AddWood(wood);
            }

            if (PlayerController.Instance != null)
            {
                FloatingDamageText.SpawnResource(PlayerController.Instance.transform.position + Vector3.up * 2f, $"+{gold}🪙", new Color(1f, 0.85f, 0.2f), 0.15f);
            }

            Destroy(rewardModalObj);
            Debug.Log($"<color=#00FF88>[Offline AFK Reward]</color> Đã nhận: +{gold} Vàng, +{stone} Đá, +{wood} Gỗ!");
        });
    }

    private void CreateResourceCard(Transform parent, string title, string amount, Color textColor, Color bgColor, Font font)
    {
        GameObject card = new GameObject("Card_" + title, typeof(RectTransform), typeof(Image));
        card.transform.SetParent(parent, false);
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(175f, 110f);

        Image img = card.GetComponent<Image>();
        img.color = bgColor;

        Outline ol = card.AddComponent<Outline>();
        ol.effectColor = textColor * 0.7f;
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        // Title
        GameObject tObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
        tObj.transform.SetParent(card.transform, false);
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -12f);
        tRt.sizeDelta = new Vector2(-10f, 26f);

        Text t = tObj.GetComponent<Text>();
        t.font = font;
        t.fontSize = 15;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(0.85f, 0.9f, 1f, 0.85f);
        t.text = title;

        // Amount
        GameObject aObj = new GameObject("Amount", typeof(RectTransform), typeof(Text));
        aObj.transform.SetParent(card.transform, false);
        RectTransform aRt = aObj.GetComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0f, 0f);
        aRt.anchorMax = new Vector2(1f, 0f);
        aRt.pivot = new Vector2(0.5f, 0f);
        aRt.anchoredPosition = new Vector2(0f, 18f);
        aRt.sizeDelta = new Vector2(-10f, 36f);

        Text a = aObj.GetComponent<Text>();
        a.font = font;
        a.fontSize = 20;
        a.fontStyle = FontStyle.Bold;
        a.alignment = TextAnchor.MiddleCenter;
        a.color = textColor;
        a.text = amount;
    }

    private string FormatTimeString(int seconds)
    {
        if (seconds < 60) return $"{seconds} giây";
        int minutes = seconds / 60;
        int remainingSec = seconds % 60;
        if (minutes < 60) return $"{minutes} phút {remainingSec} giây";
        int hours = minutes / 60;
        int remainingMin = minutes % 60;
        return $"{hours} giờ {remainingMin} phút";
    }
}
