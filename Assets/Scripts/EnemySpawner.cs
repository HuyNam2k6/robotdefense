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
    [Tooltip("Thời gian sinh quái vật thường ban ngày (2.4s sinh 1 con để nhiều quái ban ngày cho người chơi cày vàng)")]
    public float normalEnemyDayInterval = 2.4f;

    [Tooltip("Thời gian sinh quái vật thường ban đêm")]
    public float normalEnemyNightInterval = 0.65f;

    [Header("=== LỊCH XUẤT HIỆN BOSS: MỖI ĐÊM ĐỀU CÓ 1 ĐẠI BOSS ===")]
    [Tooltip("Thời gian từ khi bắt đầu đêm đến khi Boss xuất hiện (giây)")]
    public float nightBossSpawnDelay = 4.0f;
    public float nightBossTimer = 0f;
    public bool isNightBossPending = false;
    public bool hasBossSpawnedTonight = false;
    public int currentNightIndex = 1;
    public GameObject activeBossObject = null;
    public string activeBossName = "";

    [Header("=== CÁC ĐIỂM SPAWN (DO BẠN TẠO TRÊN SCENE) ===")]
    [Tooltip("Kéo các GameObject Điểm Spawn vào đây. Nếu để trống, Spawner sẽ tự quét các GameObject có tên chứa 'EnemySpawnPoint' trên Scene")]
    public Transform[] spawnPoints;

    [Header("=== PREFABS QUÁI VẬT & 5 LOẠI BOSS KHÁC NHAU ===")]
    [Tooltip("Danh sách prefab quái vật thông thường (Alien Bug, Spider)")]
    public GameObject[] normalEnemyPrefabs;

    [Tooltip("Danh sách 5 loại Boss khác nhau (Mecha Dragon, Heavy Titan, Steampunk Warlord, Chúa Nhện, Bọ Gai Độc)")]
    public GameObject[] bossPrefabs;

    [Tooltip("Prefab Đại Boss Mecha dự phòng")]
    public GameObject mechaBossPrefab;

    [Header("=== GIAO DIỆN TRẠNG THÁI BOSS THƯỜNG TRỰC ===")]
    public bool showBossTimerHUD = true;

    // Biến đếm thời gian
    private float normalTimer = 0f;
    private int normalEnemyIndex = 0;
    private int spawnPointIndex = 0;
    private int lastBossTypeIndex = -1;
    private GameObject bossTimerObj;
    private Text bossCountdownText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        if (Instance == null && FindAnyObjectByType<EnemySpawner>() == null)
        {
            GameObject spawnerObj = new GameObject("[Enemy_Wave_Spawner]");
            spawnerObj.AddComponent<EnemySpawner>();
            Debug.Log("<color=#00FF88>[EnemySpawner]</color> Đã tự động kích hoạt Spawner quái thường và Đại Boss mỗi đêm!");
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
        EnsurePrefabsLoaded();
        EnsureSpawnPointsExist();

        if (showBossTimerHUD)
        {
            CreateBossTimerHUD();
        }
    }

    /// <summary>
    /// Được gọi mỗi khi bắt đầu ban đêm: Chuẩn bị sinh Đại Boss cho đêm này
    /// </summary>
    public void OnNightStarted(int nightNumber)
    {
        currentNightIndex = nightNumber;
        isNightBossPending = true;
        hasBossSpawnedTonight = false;
        nightBossTimer = 0f;
        activeBossObject = null;
        activeBossName = "";
        Debug.Log($"<color=#FF3333><b>[EnemySpawner]</b> 🌙 ĐÊM THỨ {nightNumber} BẮT ĐẦU! Đại Boss sẽ xuất hiện sau {nightBossSpawnDelay:F0}s!</color>");
    }

    /// <summary>
    /// Được gọi mỗi khi bình minh đến: Cập nhật trạng thái sang ban ngày
    /// </summary>
    public void OnDayStarted(int dayNumber)
    {
        isNightBossPending = false;
        hasBossSpawnedTonight = false;
        activeBossObject = null;
        activeBossName = "";
    }

    private void Update()
    {
        bool isNight = DayNightSystem.Instance == null || DayNightSystem.Instance.isNight;

        // 1. Sinh quái thường:
        // Ban ngày: giảm lực công, tăng tốc độ (chu kỳ 5s/con)
        // Ban đêm: cơ động mạnh mẽ, dồn dập (chu kỳ 0.7s/con)
        normalTimer += Time.deltaTime;
        float currentNormalInterval = isNight 
            ? (DayNightSystem.Instance != null ? DayNightSystem.Instance.nightEnemyInterval : normalEnemyNightInterval) 
            : (DayNightSystem.Instance != null ? DayNightSystem.Instance.dayEnemyInterval : normalEnemyDayInterval);

        if (normalTimer >= currentNormalInterval)
        {
            normalTimer = 0f;
            SpawnNormalEnemy();

            // Ban đêm: Thỉnh thoảng sinh thêm quái tạo bầy đàn
            if (isNight && Random.value < 0.35f)
            {
                SpawnNormalEnemy();
            }
        }

        // 2. Logic Boss MỖI ĐÊM ĐỀU XUẤT HIỆN:
        if (isNight)
        {
            // Tự động kích hoạt đếm giờ Boss đêm nếu chưa kích hoạt
            if (!hasBossSpawnedTonight && !isNightBossPending)
            {
                int curNight = DayNightSystem.Instance != null ? DayNightSystem.Instance.currentDay : 1;
                OnNightStarted(curNight);
            }

            if (isNightBossPending && !hasBossSpawnedTonight)
            {
                nightBossTimer += Time.deltaTime;

                // Khi hết thời gian chuẩn bị (4s) -> XUẤT HIỆN BOSS NGAY LẬP TỨC!
                if (nightBossTimer >= nightBossSpawnDelay)
                {
                    isNightBossPending = false;
                    hasBossSpawnedTonight = true;
                    SpawnMechaBoss();
                }
            }
        }

        // 3. Cập nhật HUD trạng thái Boss liên tục để người chơi luôn biết rõ Boss đang hoạt động ra sao!
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
            // Kiểm tra nếu các điểm spawn cũ bị xa tít ngoài màn hình (Z > 10f), kéo về gần căn cứ
            for (int i = 0; i < foundPoints.Count; i++)
            {
                if (foundPoints[i] != null && foundPoints[i].position.z > 10f)
                {
                    Vector3 pos = foundPoints[i].position;
                    pos.z = -11f;
                    pos.y = GetTrueGroundHeight(pos) + 0.15f;
                    foundPoints[i].position = pos;
                }
            }
            spawnPoints = foundPoints.ToArray();
            return;
        }

        // 4. Nếu bạn chưa tạo điểm spawn nào trên Scene: Tự động khởi tạo cụm [Enemy_Spawn_Points] 
        // để bạn có thể nhìn thấy ngay trong Hierarchy và kéo đổi vị trí tùy thích!
        CreateDefaultSpawnPointsGroup();
    }

    private void CreateDefaultSpawnPointsGroup()
    {
        GameObject root = GameObject.Find("[Enemy_Spawn_Points]");
        if (root == null)
        {
            root = new GameObject("[Enemy_Spawn_Points]");
            root.transform.position = Vector3.zero;
        }

        Transform[] newPoints = new Transform[3];

        // Điểm spawn 1: Bên trái phía trước căn cứ (Khoảng cách 20m)
        GameObject p1 = GameObject.Find("EnemySpawnPoint_Trai");
        if (p1 == null) { p1 = new GameObject("EnemySpawnPoint_Trai"); p1.transform.SetParent(root.transform, false); }
        p1.transform.position = new Vector3(-14f, GetTrueGroundHeight(new Vector3(-14f, 15f, -12f)) + 0.15f, -12f);
        p1.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        newPoints[0] = p1.transform;

        // Điểm spawn 2: Chính giữa phía trước căn cứ (Khoảng cách 21m)
        GameObject p2 = GameObject.Find("EnemySpawnPoint_Giua");
        if (p2 == null) { p2 = new GameObject("EnemySpawnPoint_Giua"); p2.transform.SetParent(root.transform, false); }
        p2.transform.position = new Vector3(0f, GetTrueGroundHeight(new Vector3(0f, 15f, -10f)) + 0.15f, -10f);
        p2.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        newPoints[1] = p2.transform;

        // Điểm spawn 3: Bên phải phía trước căn cứ (Khoảng cách 20m)
        GameObject p3 = GameObject.Find("EnemySpawnPoint_Phai");
        if (p3 == null) { p3 = new GameObject("EnemySpawnPoint_Phai"); p3.transform.SetParent(root.transform, false); }
        p3.transform.position = new Vector3(14f, GetTrueGroundHeight(new Vector3(14f, 15f, -12f)) + 0.15f, -12f);
        p3.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        newPoints[2] = p3.transform;

        spawnPoints = newPoints;
        Debug.Log("<color=#00FF88>[EnemySpawner]</color> 📍 Đã kích hoạt 3 điểm spawn gần căn cứ (-14, 0, +14 ở Z = -10 ~ -12m)!");
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

        // Tỷ lệ kích thước chuẩn theo yêu cầu: AlienBug tăng 3 lần (3.0), Nhện giảm 2 lần (0.5)
        string pName = prefab.name.ToLower();
        if (pName.Contains("alienbug") || pName.Contains("bug"))
        {
            enemyGo.transform.localScale = Vector3.one * 3.0f;
        }
        else if (pName.Contains("spider"))
        {
            enemyGo.transform.localScale = Vector3.one * 0.5f;
        }
        else
        {
            enemyGo.transform.localScale = prefab.transform.localScale;
        }

        // RULE 8: Xóa Rigidbody nếu có (quái dùng trọng lực code)
        Rigidbody rb = enemyGo.GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        if (!enemyGo.CompareTag("Enemy"))
        {
            try { enemyGo.tag = "Enemy"; } catch { }
        }

        // Cập nhật thanh HP nổi chuẩn đồng nhất và áp dụng chỉ số Ngày/Đêm ngay khi xuất hiện
        Enemy enemyComp = enemyGo.GetComponent<Enemy>();
        if (enemyComp != null)
        {
            enemyComp.ApplyDayNightModifiers();
            enemyComp.UpdateHealthBar();
        }

        Debug.Log($"<color=#FF7777>[Spawner]</color> 👾 Đã sinh quái thường: <b>{enemyGo.name}</b> (HP: {(enemyComp != null ? enemyComp.maxHealth : 0)}, Scale: {enemyGo.transform.localScale.x}) tại điểm <b>{spawnPoint.name}</b>");
        return enemyGo;
    }

    /// <summary>
    /// Sinh 1 loại Đại Boss ngẫu nhiên / luân phiên (Mecha Dragon, Heavy Titan, Steampunk Warlord, Chúa Nhện)
    /// </summary>
    public GameObject SpawnMechaBoss()
    {
        EnsurePrefabsLoaded();
        if (bossPrefabs == null || bossPrefabs.Length == 0)
        {
            Debug.LogWarning("[EnemySpawner] Chưa tìm thấy Prefab Boss nào trong Assets/prefabsEnemy!");
            return null;
        }

        Transform spawnPoint = GetNextSpawnPoint();
        if (spawnPoint == null)
        {
            Debug.LogWarning("<color=yellow>[EnemySpawner]</color> ⚠️ Không tìm thấy Điểm Spawn nào cho Boss!");
            return null;
        }

        // Mỗi lần Boss xuất hiện sẽ là một loại Boss khác nhau (không lặp lại)
        lastBossTypeIndex = (lastBossTypeIndex + 1) % bossPrefabs.Length;
        GameObject prefab = bossPrefabs[lastBossTypeIndex];
        if (prefab == null) prefab = mechaBossPrefab;
        if (prefab == null) return null;

        bool isDragon = prefab.name.ToLower().Contains("dragon");
        Vector3 spawnPos = spawnPoint.position + (isDragon ? Vector3.up * 1.2f : Vector3.up * 0.15f);
        Quaternion spawnRot = spawnPoint.rotation;

        GameObject bossGo = Instantiate(prefab, spawnPos, spawnRot);
        bossGo.name = $"{prefab.name}_{Time.frameCount}";

        // Tối ưu CPU: Tuyệt đối không dùng Rigidbody
        Rigidbody rb = bossGo.GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        if (!bossGo.CompareTag("Enemy"))
        {
            try { bossGo.tag = "Enemy"; } catch { }
        }

        if (isDragon)
        {
            MechaDragonBoss bossAI = bossGo.GetComponent<MechaDragonBoss>();
            if (bossAI == null) bossAI = bossGo.AddComponent<MechaDragonBoss>();
        }

        Enemy enemyComp = bossGo.GetComponent<Enemy>();
        if (enemyComp == null)
        {
            enemyComp = bossGo.AddComponent<Enemy>();
            string lowerName = bossGo.name.ToLower();
            if (lowerName.Contains("heavy"))
            {
                enemyComp.enemyName = "Heavy Titan Colossus";
                enemyComp.maxHealth = 450f;
                enemyComp.currentHealth = 450f;
                enemyComp.moveSpeed = 2.2f;
                enemyComp.attackDamage = 60f;
                enemyComp.attackRange = 3.5f;
                enemyComp.goldReward = 120;
                bossGo.transform.localScale = Vector3.one * 3.5f;
            }
            else if (lowerName.Contains("steampunk"))
            {
                enemyComp.enemyName = "Steampunk Warlord";
                enemyComp.maxHealth = 400f;
                enemyComp.currentHealth = 400f;
                enemyComp.moveSpeed = 2.8f;
                enemyComp.attackDamage = 50f;
                enemyComp.attackRange = 3.2f;
                enemyComp.goldReward = 100;
                bossGo.transform.localScale = Vector3.one * 3.2f;
            }
            else if (lowerName.Contains("spider"))
            {
                enemyComp.enemyName = "Chúa Nhện Broodmother";
                enemyComp.maxHealth = 380f;
                enemyComp.currentHealth = 380f;
                enemyComp.moveSpeed = 3.4f;
                enemyComp.attackDamage = 45f;
                enemyComp.attackRange = 2.8f;
                enemyComp.goldReward = 90;
                bossGo.transform.localScale = Vector3.one * 2.2f;
            }
            else if (lowerName.Contains("toxic") || lowerName.Contains("behemoth"))
            {
                enemyComp.enemyName = "Đại Bọ Gai Toxic Behemoth";
                enemyComp.maxHealth = 420f;
                enemyComp.currentHealth = 420f;
                enemyComp.moveSpeed = 3.0f;
                enemyComp.attackDamage = 55f;
                enemyComp.attackRange = 3.2f;
                enemyComp.goldReward = 110;
                bossGo.transform.localScale = Vector3.one * 2.8f;
            }
        }

        // Tăng thêm độ thử thách dần theo số đêm người chơi sống sót (mỗi đêm Boss mạnh hơn +22% và rơi nhiều vàng hơn)
        if (enemyComp != null)
        {
            float nightMultiplier = 1.0f + Mathf.Max(0, currentNightIndex - 1) * 0.22f;
            enemyComp.maxHealth = Mathf.Round(enemyComp.maxHealth * nightMultiplier);
            enemyComp.currentHealth = enemyComp.maxHealth;
            enemyComp.attackDamage = Mathf.Round(enemyComp.attackDamage * nightMultiplier);
            enemyComp.goldReward = Mathf.CeilToInt(enemyComp.goldReward * nightMultiplier);
            enemyComp.UpdateHealthBar();
        }

        // Cập nhật tham chiếu Đại Boss đang hoạt động để hiển thị thường trực trên UI
        string bDisplayName = enemyComp != null ? enemyComp.enemyName : bossGo.name;
        activeBossObject = bossGo;
        activeBossName = bDisplayName;

        // Thông báo toàn màn hình: Cảnh báo Boss xuất hiện!
        Vector3 noticePos = Camera.main != null ? (Camera.main.transform.position + Camera.main.transform.forward * 4.5f) : (spawnPos + Vector3.up * 4f);
        FloatingDamageText.SpawnResource(noticePos, $"🚨 ĐẠI BOSS {bDisplayName.ToUpper()} ĐÃ XUẤT HIỆN Ở ĐÊM THỨ {currentNightIndex}! 🚨", new Color(1f, 0.2f, 0.2f, 1f), 0.15f);

        Debug.Log($"<color=red><b>[Spawner] 🚨🚨 CẢNH BÁO: ĐẠI BOSS {bDisplayName} ĐÃ XUẤT HIỆN Ở ĐÊM THỨ {currentNightIndex} TẠI {spawnPoint.name}!</b></color>");
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
        // 1. Quái vật thường
        if (normalEnemyPrefabs == null || normalEnemyPrefabs.Length == 0 || normalEnemyPrefabs[0] == null)
        {
            List<GameObject> list = new List<GameObject>();
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/prefabsEnemy" });
            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Enemy_AlienBug") || path.Contains("Enemy_Spider"))
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

        // 2. Tải 5 loại Boss riêng biệt từ Assets/prefabsEnemy/ theo đúng quy tắc dự án
        if (bossPrefabs == null || bossPrefabs.Length == 0 || bossPrefabs[0] == null)
        {
            List<GameObject> bList = new List<GameObject>();
#if UNITY_EDITOR
            string[] bossPaths = new string[]
            {
                "Assets/prefabsEnemy/Enemy_MechaDragon.prefab",
                "Assets/prefabsEnemy/Boss_HeavyTitan.prefab",
                "Assets/prefabsEnemy/Boss_SteampunkWarlord.prefab",
                "Assets/prefabsEnemy/Boss_SpiderQueen.prefab",
                "Assets/prefabsEnemy/Boss_ToxicBehemoth.prefab"
            };
            foreach (var bPath in bossPaths)
            {
                GameObject bObj = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(bPath);
                if (bObj == null)
                {
                    if (bPath.Contains("HeavyTitan")) bObj = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/HeavyRobot/HeavyRobot_Prefab.prefab");
                    else if (bPath.Contains("SteampunkWarlord")) bObj = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/CombatSteampunkRobot/CombatSteampunkRobot_Prefabs.prefab");
                    else if (bPath.Contains("SpiderQueen")) bObj = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabsEnemy/Enemy_Spider.prefab");
                    else if (bPath.Contains("ToxicBehemoth")) bObj = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabsEnemy/Enemy_AlienBug.prefab");
                }
                if (bObj != null && !bList.Contains(bObj)) bList.Add(bObj);
            }
#endif
            if (bList.Count > 0)
            {
                bossPrefabs = bList.ToArray();
                mechaBossPrefab = bossPrefabs[0];
            }
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

        bossTimerObj = new GameObject("[HUD_Boss_Timer]", typeof(RectTransform));
        bossTimerObj.transform.SetParent(canvas.transform, false);
        RectTransform rt = bossTimerObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(16f, -125f); // Đặt ngay dưới Panel Ngày Đêm ở góc trên bên trái
        rt.sizeDelta = new Vector2(250f, 26f);

        Image bg = bossTimerObj.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.88f);

        Outline outline = bossTimerObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.6f, 1f, 0.75f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtGo.transform.SetParent(bossTimerObj.transform, false);
        RectTransform tRt = txtGo.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;

        bossCountdownText = txtGo.GetComponent<Text>();
        bossCountdownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        bossCountdownText.fontSize = 11;
        bossCountdownText.fontStyle = FontStyle.Bold;
        bossCountdownText.alignment = TextAnchor.MiddleCenter;
        bossCountdownText.color = new Color(0.2f, 0.95f, 0.6f);
        bossCountdownText.text = "☀️ <b>TRINH SÁT: ĐÊM SẼ CÓ ĐẠI BOSS!</b>";

        bossTimerObj.SetActive(true);
    }

    private void UpdateBossTimerHUD()
    {
        if (bossCountdownText == null || bossTimerObj == null) return;
        if (!showBossTimerHUD)
        {
            if (bossTimerObj.activeSelf) bossTimerObj.SetActive(false);
            return;
        }

        if (!bossTimerObj.activeSelf) bossTimerObj.SetActive(true);

        // 1. Nếu Boss đang xuất hiện và còn sống
        if (activeBossObject != null && activeBossObject.activeInHierarchy)
        {
            Enemy e = activeBossObject.GetComponent<Enemy>();
            if (e != null && !e.isDead && e.currentHealth > 0f)
            {
                bossCountdownText.color = Color.Lerp(Color.red, new Color(1f, 0.5f, 0.1f, 1f), Mathf.PingPong(Time.time * 5f, 1f));
                bossCountdownText.text = $"👹 <b>{activeBossName} (HP: {(int)e.currentHealth}/{(int)e.maxHealth})</b>";
                return;
            }
            else
            {
                activeBossObject = null;
            }
        }

        // 2. Nếu đang chuẩn bị vào đêm và đếm ngược Boss
        if (isNightBossPending && !hasBossSpawnedTonight)
        {
            float remaining = Mathf.Max(0f, nightBossSpawnDelay - nightBossTimer);
            int sec = Mathf.CeilToInt(remaining);
            bossCountdownText.color = Color.Lerp(Color.red, new Color(1f, 0.85f, 0.1f, 1f), Mathf.PingPong(Time.time * 6f, 1f));
            bossCountdownText.text = $"⚠️ <b>ĐẠI BOSS XUẤT HIỆN SAU: {sec}s!</b> ⏱️";
            return;
        }

        // 3. Trạng thái bình thường: thông báo rõ ràng cho người chơi
        bool isNight = DayNightSystem.Instance != null && DayNightSystem.Instance.isNight;
        if (isNight)
        {
            bossCountdownText.color = new Color(0.4f, 0.85f, 1f);
            bossCountdownText.text = $"🌙 <b>ĐÊM {currentNightIndex}: PHÒNG THỦ CĂN CỨ!</b>";
        }
        else
        {
            bossCountdownText.color = new Color(0.2f, 0.95f, 0.6f);
            bossCountdownText.text = $"☀️ <b>TRINH SÁT: ĐÊM SẼ CÓ ĐẠI BOSS!</b>";
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
