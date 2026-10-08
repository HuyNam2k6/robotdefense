using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EnemySpawner - Hệ Thống Sinh Quái Vật Thường & Đại Boss Mecha Dựa Trên Điểm Spawn.
/// - Quái vật chỉ xuất hiện tại các ĐIỂM SPAWN do bạn chỉ định hoặc tạo trên Scene.
/// - Quái vật thường (Alien Bug, Scrap Spider): Sinh 5 giây / 1 con (5s 1 con).
/// - Đại Boss Mecha (Mecha Cyber Dragon): Sinh 4 phút / 1 con (240s 1 con).
/// - Điểm Spawn: Kéo vào mảng spawnPoints hoặc tạo GameObject chứa chữ "EnemySpawnPoint" trên Scene.
/// - Hiển thị Gizmos trực quan trong Unity Scene View để dễ dàng di chuyển và thêm bớt điểm spawn.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("=== CÀI ĐẶT THỜI GIAN SPAWN (TIMERS) ===")]
    [Tooltip("Thời gian sinh quái vật thường (mặc định 5s 1 con theo yêu cầu)")]
    public float normalEnemyInterval = 5.0f;

    [Tooltip("Thời gian sinh Đại Boss Mecha (mặc định 4 phút = 240s theo yêu cầu)")]
    public float bossEnemyInterval = 240.0f;

    [Header("=== CÁC ĐIỂM SPAWN (DO BẠN TẠO TRÊN SCENE) ===")]
    [Tooltip("Kéo các GameObject Điểm Spawn vào đây. Nếu để trống, Spawner sẽ tự quét các GameObject có tên chứa 'EnemySpawnPoint' trên Scene")]
    public Transform[] spawnPoints;

    [Header("=== PREFABS QUÁI VẬT & BOSS ===")]
    [Tooltip("Danh sách prefab quái vật thông thường (Alien Bug, Spider)")]
    public GameObject[] normalEnemyPrefabs;

    [Tooltip("Prefab Đại Boss Mecha (Mecha Cyber Dragon)")]
    public GameObject mechaBossPrefab;

    [Header("=== GIAO DIỆN ĐẾM GIỜ BOSS (HUD TIMER) ===")]
    [Tooltip("Hiển thị đồng hồ đếm ngược thời gian xuất hiện của Boss trên màn hình")]
    public bool showBossTimerHUD = true;

    // Biến đếm thời gian
    private float normalTimer = 0f;
    private float bossTimer = 0f;
    private int normalEnemyIndex = 0;
    private int spawnPointIndex = 0;
    private Text bossCountdownText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        if (Instance == null && FindAnyObjectByType<EnemySpawner>() == null)
        {
            GameObject spawnerObj = new GameObject("[Enemy_Wave_Spawner]");
            spawnerObj.AddComponent<EnemySpawner>();
            Debug.Log("<color=#00FF88>[EnemySpawner]</color> Đã tự động kích hoạt Spawner quái thường (5s) và Mecha Boss (4')!");
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
        EnsureSpawnPointsExist();
    }

    private void Start()
    {
        normalTimer = 0f;
        bossTimer = 0f;

        EnsurePrefabsLoaded();
        EnsureSpawnPointsExist();

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
    /// Tìm hoặc lấy điểm Spawn hợp lệ từ danh sách do bạn tạo
    /// </summary>
    public Transform GetNextSpawnPoint()
    {
        EnsureSpawnPointsExist();

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return null;
        }

        // Lọc các Transform còn tồn tại
        List<Transform> valid = new List<Transform>();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null) valid.Add(spawnPoints[i]);
        }

        if (valid.Count == 0) return null;

        // Lấy luân phiên các điểm spawn
        Transform chosen = valid[spawnPointIndex % valid.Count];
        spawnPointIndex++;
        return chosen;
    }

    /// <summary>
    /// Đảm bảo luôn có điểm Spawn trong game (nếu bạn chưa tạo, tự động tạo cụm mẫu trên Scene để bạn tùy ý kéo thả)
    /// </summary>
    public void EnsureSpawnPointsExist()
    {
        // 1. Nếu đã gán trong Inspector và các điểm còn tồn tại -> sử dụng ngay
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            bool hasValid = false;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null) { hasValid = true; break; }
            }
            if (hasValid) return;
        }

        // 2. Tự động tìm cụm GameObject [Enemy_Spawn_Points] trên Scene
        GameObject rootGroup = GameObject.Find("[Enemy_Spawn_Points]");
        if (rootGroup != null && rootGroup.transform.childCount > 0)
        {
            List<Transform> list = new List<Transform>();
            for (int i = 0; i < rootGroup.transform.childCount; i++)
            {
                list.Add(rootGroup.transform.GetChild(i));
            }
            spawnPoints = list.ToArray();
            return;
        }

        // 3. Quét các GameObject có tên chứa "EnemySpawnPoint" do bạn tạo thủ công trên Scene
        GameObject[] allGos = FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
        List<Transform> foundPoints = new List<Transform>();
        for (int i = 0; i < allGos.Length; i++)
        {
            string n = allGos[i].name.ToLower();
            if (n.Contains("enemyspawn") || n.Contains("spawnpoint_enemy") || n == "enemyspawnpoint")
            {
                foundPoints.Add(allGos[i].transform);
            }
        }

        if (foundPoints.Count > 0)
        {
            spawnPoints = foundPoints.ToArray();
            return;
        }

        // 4. Nếu bạn chưa tạo điểm spawn nào trên Scene: Tự động khởi tạo cụm [Enemy_Spawn_Points] 
        // để bạn có thể nhìn thấy ngay trong Hierarchy và kéo đổi vị trí tùy thích!
        CreateDefaultSpawnPointsGroup();
    }

    private void CreateDefaultSpawnPointsGroup()
    {
        GameObject root = new GameObject("[Enemy_Spawn_Points]");
        root.transform.position = Vector3.zero;

        Transform[] newPoints = new Transform[3];

        // Điểm spawn 1: Bên trái phía Bắc
        GameObject p1 = new GameObject("EnemySpawnPoint_Trai");
        p1.transform.SetParent(root.transform, false);
        p1.transform.position = new Vector3(-12f, GetTrueGroundHeight(new Vector3(-12f, 15f, 28f)) + 0.15f, 28f);
        p1.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        newPoints[0] = p1.transform;

        // Điểm spawn 2: Chính giữa phía Bắc
        GameObject p2 = new GameObject("EnemySpawnPoint_Giua");
        p2.transform.SetParent(root.transform, false);
        p2.transform.position = new Vector3(0f, GetTrueGroundHeight(new Vector3(0f, 15f, 30f)) + 0.15f, 30f);
        p2.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        newPoints[1] = p2.transform;

        // Điểm spawn 3: Bên phải phía Bắc
        GameObject p3 = new GameObject("EnemySpawnPoint_Phai");
        p3.transform.SetParent(root.transform, false);
        p3.transform.position = new Vector3(12f, GetTrueGroundHeight(new Vector3(12f, 15f, 28f)) + 0.15f, 28f);
        p3.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        newPoints[2] = p3.transform;

        spawnPoints = newPoints;
        Debug.Log("<color=#00FF88>[EnemySpawner]</color> 📍 Đã tạo cụm [Enemy_Spawn_Points] gồm 3 điểm spawn (Trái, Giữa, Phải) trên Scene để bạn tùy ý di chuyển!");
    }

    /// <summary>
    /// Sinh 1 quái vật bình thường (Alien Bug hoặc Spider) mỗi 5 giây tại điểm spawn do bạn tạo
    /// </summary>
    public GameObject SpawnNormalEnemy()
    {
        EnsurePrefabsLoaded();
        if (normalEnemyPrefabs == null || normalEnemyPrefabs.Length == 0)
        {
            Debug.LogWarning("[EnemySpawner] Chưa tìm thấy Prefab quái vật thường!");
            return null;
        }

        Transform spawnPoint = GetNextSpawnPoint();
        if (spawnPoint == null)
        {
            Debug.LogWarning("<color=yellow>[EnemySpawner]</color> ⚠️ Không tìm thấy Điểm Spawn nào! Hãy tạo ít nhất 1 GameObject có tên 'EnemySpawnPoint' hoặc kéo vào mảng spawnPoints!");
            return null;
        }

        // Chọn luân phiên quái thường
        GameObject prefab = normalEnemyPrefabs[normalEnemyIndex % normalEnemyPrefabs.Length];
        normalEnemyIndex++;

        if (prefab == null) return null;

        Vector3 spawnPos = spawnPoint.position;
        Quaternion spawnRot = spawnPoint.rotation;

        GameObject enemyGo = Instantiate(prefab, spawnPos, spawnRot);
        enemyGo.name = $"{prefab.name}_{Time.frameCount}";
        enemyGo.transform.localScale = Vector3.one; // RULE 2: Luôn chuẩn Vector3.one

        // RULE 8: Xóa Rigidbody nếu có (quái dùng trọng lực code)
        Rigidbody rb = enemyGo.GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        if (!enemyGo.CompareTag("Enemy"))
        {
            try { enemyGo.tag = "Enemy"; } catch { }
        }

        Debug.Log($"<color=#FF7777>[Spawner]</color> 👾 Đã sinh quái thường: <b>{enemyGo.name}</b> tại điểm <b>{spawnPoint.name}</b> ({spawnPos}) (Chu kỳ 5s)");
        return enemyGo;
    }

    /// <summary>
    /// Sinh Đại Boss Mecha Cyber Dragon mỗi 4 phút (240 giây) tại điểm spawn do bạn tạo
    /// </summary>
    public GameObject SpawnMechaBoss()
    {
        EnsurePrefabsLoaded();
        if (mechaBossPrefab == null)
        {
            Debug.LogWarning("[EnemySpawner] Chưa tìm thấy Prefab Mecha Boss Dragon!");
            return null;
        }

        Transform spawnPoint = GetNextSpawnPoint();
        if (spawnPoint == null)
        {
            Debug.LogWarning("<color=yellow>[EnemySpawner]</color> ⚠️ Không tìm thấy Điểm Spawn nào cho Boss!");
            return null;
        }

        // Boss là quái bay (Flying Unit), xuất hiện trên không trung cao hơn điểm spawn 7.0m
        Vector3 spawnPos = spawnPoint.position + Vector3.up * 7.0f;
        Quaternion spawnRot = spawnPoint.rotation;

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

        Debug.Log($"<color=red><b>[Spawner] 🚨🚨 CẢNH BÁO: ĐẠI BOSS MECHA CYBER DRAGON ĐÃ XUẤT HIỆN TẠI {spawnPoint.name}! (Chu kỳ 4 phút)</b></color>");
        return bossGo;
    }

    private float GetTrueGroundHeight(Vector3 pos)
    {
        float terrainGround = 9.5f;
        if (Terrain.activeTerrain != null)
        {
            terrainGround = Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.transform.position.y;
        }

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

    private void EnsurePrefabsLoaded()
    {
        if (normalEnemyPrefabs == null || normalEnemyPrefabs.Length == 0 || normalEnemyPrefabs[0] == null)
        {
            List<GameObject> list = new List<GameObject>();
#if UNITY_EDITOR
            // Chỉ lấy duy nhất quái vật từ thư mục Assets/prefabsEnemy theo đúng yêu cầu
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/prefabsEnemy" });
            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Enemy_MechaDragon"))
                {
                    if (mechaBossPrefab == null)
                        mechaBossPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
                else if (path.Contains("Enemy_") || path.Contains("Bug") || path.Contains("Spider"))
                {
                    GameObject p = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (p != null && !list.Contains(p)) list.Add(p);
                }
            }
#endif
            if (list.Count > 0)
            {
                normalEnemyPrefabs = list.ToArray();
            }
        }

        if (mechaBossPrefab == null)
        {
#if UNITY_EDITOR
            // Chỉ lấy duy nhất Boss Mecha từ thư mục Assets/prefabsEnemy theo đúng yêu cầu
            mechaBossPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabsEnemy/Enemy_MechaDragon.prefab");
#endif
        }
    }

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

    // Hiển thị trực quan các điểm Spawn trên Scene View để bạn dễ dàng quan sát và kéo thả
    private void OnDrawGizmos()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.85f);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform p = spawnPoints[i];
            if (p != null)
            {
                Gizmos.DrawWireSphere(p.position, 1.2f);
                Gizmos.DrawLine(p.position, p.position + p.forward * 2.5f);
            }
        }
    }
}
