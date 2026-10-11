using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CivilianHouse - Ngôi Nhà Của Dân Cư / Nơi Trú Ẩn An Toàn:
/// - Khi làng bị tấn công (ban đêm hoặc có quái vật), dân cư sẽ chạy vào ngôi nhà gần nhất.
/// - Khi chạm vào cửa nhà, dân cư sẽ biến mất (trú ẩn an toàn bên trong nhà).
/// - Khi căn cứ an toàn trở lại (trời sáng, không còn quái vật đe dọa), dân cư sẽ tự động bước ra ngoài và tiếp tục đi nhặt vàng!
/// </summary>
public class CivilianHouse : MonoBehaviour
{
    public static readonly List<CivilianHouse> AllHouses = new List<CivilianHouse>();

    [Header("=== CÀI ĐẶT NGÔI NHÀ ===")]
    public string houseName = "Nhà Dân Cư #1";
    public Transform doorPoint;
    public float enterDistance = 1.6f;

    [Header("=== DANH SÁCH DÂN CƯ TRÚ ẨN ===")]
    private readonly List<IslandSettler> shelteredSettlers = new List<IslandSettler>();

    [Header("=== OVERHEAD UI ===")]
    private GameObject overheadCanvas;
    private UnityEngine.UI.Text statusText;

    private float checkAttackTimer = 0f;
    private bool wasUnderAttack = false;

    private void Awake()
    {
        if (doorPoint == null) doorPoint = transform;
    }

    private void OnEnable()
    {
        if (!AllHouses.Contains(this)) AllHouses.Add(this);
    }

    private void OnDisable()
    {
        AllHouses.Remove(this);
    }

    private void Start()
    {
        CreateOverheadUI();
    }

    private void Update()
    {
        checkAttackTimer -= Time.deltaTime;
        if (checkAttackTimer <= 0f)
        {
            checkAttackTimer = 1.0f;
            CheckSafetyAndRelease();
        }

        UpdateOverheadFacing();
    }

    /// <summary>
    /// Kiểm tra trạng thái toàn căn cứ có đang bị quái vật tấn công hay không
    /// </summary>
    public static bool IsVillageUnderAttack()
    {
        // 1. Kiểm tra ban đêm
        if (DayNightSystem.Instance != null && DayNightSystem.Instance.isNight)
            return true;

        // 2. Kiểm tra quái vật còn sống trong phạm vi đảo
        var enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && enemies[i].gameObject.activeInHierarchy && enemies[i].currentHealth > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Cho dân cư vào nhà trú ẩn
    /// </summary>
    public void EnterHouse(IslandSettler settler)
    {
        if (settler == null || shelteredSettlers.Contains(settler)) return;

        shelteredSettlers.Add(settler);
        settler.EnterHouseInternal(this);

        UpdateStatusText();
        FloatingDamageText.SpawnResource(transform.position + Vector3.up * 2.8f, $"🛡️ {settler.citizenName} đã vào nhà an toàn!", new Color(0.3f, 1f, 0.6f), 0.08f);
    }

    /// <summary>
    /// Kiểm tra khi an toàn thì cho toàn bộ dân cư trong nhà bước ra ngoài
    /// </summary>
    private void CheckSafetyAndRelease()
    {
        bool underAttack = IsVillageUnderAttack();

        if (wasUnderAttack && !underAttack)
        {
            // Vừa hết nguy hiểm -> Thả dân cư ra ngoài
            ReleaseAllSettlers();
        }

        wasUnderAttack = underAttack;
    }

    public void ReleaseAllSettlers()
    {
        if (shelteredSettlers.Count == 0) return;

        StartCoroutine(ReleaseRoutine());
    }

    private IEnumerator ReleaseRoutine()
    {
        List<IslandSettler> releasing = new List<IslandSettler>(shelteredSettlers);
        shelteredSettlers.Clear();
        UpdateStatusText();

        FloatingDamageText.SpawnResource(transform.position + Vector3.up * 3.0f, "☀️ An toàn rồi! Dân cư ra ngoài nhặt vàng!", Color.yellow, 0.09f);

        for (int i = 0; i < releasing.Count; i++)
        {
            IslandSettler s = releasing[i];
            if (s != null)
            {
                Vector3 exitPos = doorPoint.position + (doorPoint.forward * 1.2f) + (Random.insideUnitSphere * 0.8f);
                exitPos.y = doorPoint.position.y;
                s.ExitHouseInternal(exitPos);
            }
            yield return new WaitForSeconds(0.35f);
        }
    }

    private void CreateOverheadUI()
    {
        if (overheadCanvas != null) return;

        overheadCanvas = new GameObject("Overhead_HouseUI");
        overheadCanvas.transform.SetParent(transform, false);
        overheadCanvas.transform.localPosition = new Vector3(0, 3.8f, 0);

        Canvas canvas = overheadCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        overheadCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();

        RectTransform rt = overheadCanvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(240, 50);
        overheadCanvas.transform.localScale = Vector3.one * 0.013f;

        GameObject textObj = new GameObject("StatusText");
        textObj.transform.SetParent(overheadCanvas.transform, false);
        statusText = textObj.AddComponent<UnityEngine.UI.Text>();
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (statusText.font == null) statusText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        statusText.fontSize = 20;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = new Color(0.9f, 0.95f, 1f);

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.sizeDelta = new Vector2(240, 45);
        textRt.anchoredPosition = Vector2.zero;

        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        if (statusText != null)
        {
            if (shelteredSettlers.Count > 0)
            {
                statusText.text = $"🏠 <b>{houseName}</b>\n<color=#00FF88>🛡️ Trú ẩn: {shelteredSettlers.Count} người</color>";
            }
            else
            {
                statusText.text = $"🏠 <b>{houseName}</b>\n<color=#A0D8EF>Bình yên</color>";
            }
        }
    }

    private void UpdateOverheadFacing()
    {
        if (overheadCanvas == null) return;
        Camera cam = Camera.main;
        if (cam != null)
        {
            overheadCanvas.transform.rotation = Quaternion.LookRotation(overheadCanvas.transform.position - cam.transform.position);
        }
    }
}
