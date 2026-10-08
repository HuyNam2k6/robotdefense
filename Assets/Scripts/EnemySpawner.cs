using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EnemySpawner - Hệ Thống Sinh Quái Vật Thường & Đại Boss Mecha Tự Động.
/// - Quái vật thường (Alien Bug, Scrap Spider): Sinh 5 giây / 1 con (5s 1 con).
/// - Đại Boss Mecha (Mecha Cyber Dragon): Sinh 4 phút / 1 con (240s 1 con).
/// - Sử dụng Raycast để định vị độ cao mặt đất và bám sát địa hình (0 Rigidbody).
/// - Tự động tải Prefabs từ Assets/prefabs hoặc Assets/prefabsEnemy.
/// - Tối ưu 100% CPU di động Snapdragon 810.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("=== CÀI ĐẶT THỜI GIAN SPAWN (TIMERS) ===")]
    [Tooltip("Thời gian sinh quái vật thường (mặc định 5s 1 con theo yêu cầu)")]
    public float normalEnemyInterval = 5.0f;

    [Tooltip("Thời gian sinh Đại Boss Mecha (mặc định 4 phút = 240s theo yêu cầu)")]
    public float bossEnemyInterval = 240.0f;

    [Header("=== PREFABS QUÁI VẬT & BOSS ===")]
    [Tooltip("Danh sách prefab quái vật thông thường (Alien Bug, Spider)")]
    public GameObject[] normalEnemyPrefabs;

    [Tooltip("Prefab Đại Boss Mecha (Mecha Cyber Dragon)")]
    public GameObject mechaBossPrefab;

    [Header("=== TỌA ĐỘ SINH QUÁI TRÊN ĐẢO ===")]
    [Tooltip("Tọa độ Z đường biên quái xuất hiện (phía Bắc chiến trường)")]
    public float spawnZ = 28.0f;

    [Tooltip("Khoảng tọa độ X quái xuất hiện ngẫu nhiên")]
    public Vector2 spawnRangeX = new Vector2(-15.0f, 15.0f);

    [Header("=== GIAO DIỆN ĐẾM GIỜ BOSS (HUD TIMER) ===")]
    [Tooltip("Hiển thị đồng hồ đếm ngược thời gian xuất hiện của Boss trên màn hình")]
    public bool showBossTimerHUD = true;

    // Biến đếm thời gian
    private float normalTimer = 0f;
    private float bossTimer = 0f;
    private int normalEnemyIndex = 0;
    private Text bossCountdownText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        if (Instance == null && FindAnyObjectByType<EnemySpawner>() == null)
        {
            GameObject spawnerObj = new GameObject("[Enemy_Wave_Spawner]");
            spawnerObj.AddComponent<EnemySpawner>();
            Debug.Log("<color=#00FF88>[EnemySpawner]</color> Đã tự động khởi chạy Spawner quái thường (5s) và Mecha Boss (4')!");
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

        EnsurePrefabsLoaded();
    }

    private void Start()
    {
        normalTimer = 0f;
        bossTimer = 0f;

        EnsurePrefabsLoaded();
        if (showBossTimerHUD)
        {
            CreateBossTimerHUD();
        }
    }

    private void Update()
    {
        // 1. Đếm giờ và sinh quái vật thường (5s 1 con)
        normalTimer += Time.deltaTime;
        if (normalTimer >= normalEnemyInterval)
        {
            normalTimer = 0f;
            SpawnNormalEnemy();
        }

        // 2. Đếm giờ và sinh Đại Boss Mecha (4 phút = 240s 1 con)
        bossTimer += Time.deltaTime;
        if (bossTimer >= bossEnemyInterval)
        {
            bossTimer = 0f;
            SpawnMechaBoss();
        }

        // 3. Cập nhật HUD đếm ngược Boss nếu có
        UpdateBossTimerHUD();
    }

    /// <summary>
    /// Sinh 1 quái vật bình thường (Alien Bug hoặc Spider) mỗi 5 giây
    /// </summary>
    public GameObject SpawnNormalEnemy()
    {
        EnsurePrefabsLoaded();
        if (normalEnemyPrefabs == null || normalEnemyPrefabs.Length == 0)
        {
            Debug.LogWarning("[EnemySpawner] Chưa tìm thấy Prefab quái vật thường!");
            return null;
        }

        // Chọn luân phiên hoặc ngẫu nhiên các loại quái thường
        GameObject prefab = normalEnemyPrefabs[normalEnemyIndex % normalEnemyPrefabs.Length];
        normalEnemyIndex++;

        if (prefab == null) return null;

        Vector3 spawnPos = CalculateSpawnPosition(false);
        Quaternion spawnRot = Quaternion.Euler(0f, 180f, 0f); // Xoay mặt về hướng nam (về phía căn cứ)

        GameObject enemyGo = Instantiate(prefab, spawnPos, spawnRot);
        enemyGo.name = $"{prefab.name}_{Time.frameCount}";
        enemyGo.transform.localScale = Vector3.one; // RULE 2: Luôn chuẩn Vector3.one

        // RULE 8: Xóa Rigidbody nếu có (quái dùng trọng lực code)
        Rigidbody rb = enemyGo.GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        // Đảm bảo tag là Enemy
        if (!enemyGo.CompareTag("Enemy"))
        {
            try { enemyGo.tag = "Enemy"; } catch { }
        }

        Debug.Log($"<color=#FF7777>[Spawner]</color> 👾 Đã sinh quái thường: <b>{enemyGo.name}</b> tại {spawnPos} (Chu kỳ 5s)");
        return enemyGo;
    }

    /// <summary>
    /// Sinh Đại Boss Mecha Cyber Dragon mỗi 4 phút (240 giây)
    /// </summary>
    public GameObject SpawnMechaBoss()
    {
        EnsurePrefabsLoaded();
        if (mechaBossPrefab == null)
        {
            Debug.LogWarning("[EnemySpawner] Chưa tìm thấy Prefab Mecha Boss Dragon!");
            return null;
        }

        Vector3 spawnPos = CalculateSpawnPosition(true);
        Quaternion spawnRot = Quaternion.Euler(0f, 180f, 0f);

        GameObject bossGo = Instantiate(mechaBossPrefab, spawnPos, spawnRot);
        bossGo.name = "Mecha_Cyber_Dragon_BOSS";
        bossGo.transform.localScale = Vector3.one; // RULE 2: Luôn chuẩn Vector3.one

        Rigidbody rb = bossGo.GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        if (!bossGo.CompareTag("Enemy"))
        {
            try { bossGo.tag = "Enemy"; } catch { }
        }

        // Thông báo toàn màn hình: Cảnh báo Boss xuất hiện!
        Vector3 noticePos = Camera.main != null ? (Camera.main.transform.position + Camera.main.transform.forward * 4.5f) : (spawnPos + Vector3.up * 4f);
        FloatingDamageText.SpawnResource(noticePos, "⚠️ ĐẠI BOSS MECHA XUẤT HIỆN! ⚠️", new Color(1f, 0.15f, 0.15f, 1f), 0.12f);

        Debug.Log($"<color=red><b>[Spawner] 🚨🚨 CẢNH BÁO: ĐẠI BOSS MECHA CYBER DRAGON ĐÃ XUẤT HIỆN! (Chu kỳ 4 phút)</b></color> tại {spawnPos}");
        return bossGo;
    }

    /// <summary>
    /// Tính toán vị trí xuất hiện an toàn trên bề mặt bản đồ bằng Raycast & Terrain
    /// </summary>
    private Vector3 CalculateSpawnPosition(bool isBoss)
    {
        float rx = Random.Range(spawnRangeX.x, spawnRangeX.y);
        float rz = spawnZ + Random.Range(-2f, 3f);
        float groundY = GetTrueGroundHeight(new Vector3(rx, 15f, rz));

        if (isBoss)
        {
            // Boss Rồng cơ khí là đơn vị bay (Flying Unit), xuất hiện trên không trung cách đất 7.5m
            return new Vector3(rx, groundY + 7.5f, rz);
        }
        else
        {
            // Quái bò đất xuất hiện áp sát bề mặt đất
            return new Vector3(rx, groundY + 0.15f, rz);
        }
    }

    /// <summary>
    /// Dò tìm cao độ mặt đất bằng Raycast từ trên cao xuống và Terrain.SampleHeight
    /// </summary>
    private float GetTrueGroundHeight(Vector3 pos)
    {
        float terrainGround = 9.5f;
        if (Terrain.activeTerrain != null)
        {
            terrainGround = Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.transform.position.y;
        }

        // Bắn Raycast từ trên cao xuống để tìm mặt sàn
        if (Physics.Raycast(new Vector3(pos.x, 35f, pos.z), Vector3.down, out RaycastHit hit, 50f))
        {
            string hitName = hit.collider.name.ToLower();
            if (!hitName.Contains("bullet") && !hitName.Contains("enemy") && !hitName.Contains("wall"))
            {
                return Mathf.Max(terrainGround, hit.point.y);
            }
        }

        return terrainGround;
    }

    /// <summary>
    /// Tự động nạp Prefab từ thư mục dự án nếu chưa được kéo thả trong Inspector
    /// </summary>
    private void EnsurePrefabsLoaded()
    {
        if (normalEnemyPrefabs == null || normalEnemyPrefabs.Length == 0 || normalEnemyPrefabs[0] == null)
        {
            List<GameObject> list = new List<GameObject>();
#if UNITY_EDITOR
            GameObject bug = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Enemy_AlienBug.prefab")
                          ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabsEnemy/Enemy_AlienBug.prefab");
            GameObject spider = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Enemy_Spider.prefab")
                             ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabsEnemy/Enemy_Spider.prefab");
            if (bug != null) list.Add(bug);
            if (spider != null) list.Add(spider);
#endif
            if (list.Count > 0)
            {
                normalEnemyPrefabs = list.ToArray();
            }
        }

        if (mechaBossPrefab == null)
        {
#if UNITY_EDITOR
            mechaBossPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Enemy_MechaDragon.prefab")
                           ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabsEnemy/Enemy_MechaDragon.prefab");
#endif
        }
    }

    /// <summary>
    /// Tạo thanh đếm ngược thời gian xuất hiện Boss Mecha hiển thị gọn gàng trên HUD
    /// </summary>
    private void CreateBossTimerHUD()
    {
        Canvas canvas = null;
        Canvas[] allCanvases = FindObjectsByType<Canvas>();
        foreach (var c in allCanvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.name.Contains("Shop_Canvas") || c.name.Contains("Canvas"))
            {
                canvas = c;
                break;
            }
        }
        if (canvas == null && allCanvases.Length > 0) canvas = allCanvases[0];
        if (canvas == null) return;

        Transform existing = canvas.transform.Find("[HUD_Boss_Timer]");
        if (existing != null) Destroy(existing.gameObject);

        GameObject timerObj = new GameObject("[HUD_Boss_Timer]", typeof(RectTransform));
        timerObj.transform.SetParent(canvas.transform, false);
        RectTransform rt = timerObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(24f, -165f); // Ngay dưới thanh máu Player
        rt.sizeDelta = new Vector2(260f, 26f);

        Image bg = timerObj.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);

        Outline outline = timerObj.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.35f, 0.1f, 0.8f);
        outline.effectDistance = new Vector2(1f, -1f);

        GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtGo.transform.SetParent(timerObj.transform, false);
        RectTransform tRt = txtGo.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;

        bossCountdownText = txtGo.GetComponent<Text>();
        bossCountdownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        bossCountdownText.fontSize = 13;
        bossCountdownText.fontStyle = FontStyle.Bold;
        bossCountdownText.alignment = TextAnchor.MiddleCenter;
        bossCountdownText.color = new Color(1f, 0.85f, 0.3f, 1f);
        bossCountdownText.text = "⏱️ Boss Mecha: 04:00";
    }

    private void UpdateBossTimerHUD()
    {
        if (bossCountdownText == null) return;

        float remaining = Mathf.Max(0f, bossEnemyInterval - bossTimer);
        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);

        if (remaining <= 10f)
        {
            bossCountdownText.color = Color.Lerp(Color.red, Color.yellow, Mathf.PingPong(Time.time * 4f, 1f));
            bossCountdownText.text = $"⚠️ BOSS MECHA ĐẾN: {minutes:D2}:{seconds:D2}!";
        }
        else
        {
            bossCountdownText.color = new Color(1f, 0.85f, 0.3f, 1f);
            bossCountdownText.text = $"⏱️ Boss Mecha: {minutes:D2}:{seconds:D2}";
        }
    }
}
