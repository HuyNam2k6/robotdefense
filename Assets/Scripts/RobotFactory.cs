using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Nhà máy sản xuất Quân đội Robot (Robot Factory).
/// - Được người chơi xây dựng từ Cửa hàng (Shop UI).
/// - Khi người chơi nhấn vào Nhà Máy trên đảo: Mở Bảng Chế Tạo Robot Chiến Đấu to nửa màn hình ở giữa màn hình!
/// - Chế tạo robot chiến đấu theo yêu cầu: Mỗi con mất đúng 15 giây (15s).
/// - Hoạt cảnh cơ khí chạy nhịp nhàng suốt 15s: Cánh tay robot hàn xì, bánh răng cưa quay,
///   thân máy rung nảy dập khuôn, đèn tia lửa hàn điện chớp tắt, thanh tiến độ 3D đếm ngược.
/// - Xuất xưởng đúng con robot mà người chơi đã chọn: Stan, Mike, George, hoặc Leela.
/// - Tối ưu 100% Snapdragon 810 (Zero GC, không Rigidbody theo Rule 8).
/// </summary>
public class RobotFactory : MonoBehaviour
{
    public static RobotFactory Instance { get; private set; }

    [Header("--- Cấu Hình Sản Xuất Quân Đội (15s / Con) ---")]
    [Tooltip("Thời gian để đúc và lắp ráp xong 1 Robot: Đúng 15 giây theo yêu cầu")]
    public float productionTime = 15.0f;

    [Tooltip("Mặc định false: Người chơi phải nhấn vào Nhà Máy/Cửa hàng để ra lệnh chế tạo")]
    public bool autoProduce = false;

    [Tooltip("Giới hạn số lượng quân đội tối đa trên chiến trường (Tối ưu cho Snapdragon 810)")]
    public int maxArmySize = 8;

    [Tooltip("4 Prefab Robot chiến đấu: 0=Stan, 1=Mike, 2=George, 3=Leela")]
    public GameObject[] combatRobotPrefabs;

    [Header("--- Vị Trí Xuất Xưởng ---")]
    public Transform spawnPoint;
    public Transform rallyPoint; // Điểm tập kết quân sau khi bước ra khỏi băng chuyền

    [Header("--- Chi Tiết Cơ Khí Nhà Máy (Để Chạy Animation) ---")]
    public Transform[] cogs;            // Các bánh răng quay
    public Transform[] robotArms;       // Cánh tay robot hàn xì
    public Transform factoryBody;       // Khối thân máy chính (rung lắc theo nhịp)
    public Light weldingLight;          // Đèn chớp tia lửa hàn

    [Header("--- Chi Phí Chế Tạo (Mặc định 0🪙 - Có thể cấu hình) ---")]
    public int craftRobotCost = 0;

    // Biến trạng thái
    private bool isProducing = false;
    private float productionTimer = 0f;
    private Queue<int> productionQueue = new Queue<int>();
    private int currentCraftingRobotIndex = 0;
    private int fallbackRobotIndex = 0;
    private List<GameObject> activeArmy = new List<GameObject>();

    private Vector3 originalBodyScale = Vector3.one;
    private float armAngle = 0f;

    // Thanh tiến độ 3D trên nóc máy
    private Transform progressBarRoot;
    private Transform progressBarFill;
    private Camera mainCam;

    void Awake()
    {
        Instance = this;
        if (factoryBody != null) originalBodyScale = factoryBody.localScale;
        if (spawnPoint == null) spawnPoint = transform;

        // Loại bỏ Rigidbody nếu có (Quy tắc Rule 8: Tính trọng lực bằng code)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        // Đảm bảo có Collider để nhận click chuột / chạm tay
        if (GetComponent<Collider>() == null)
        {
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1.2f, 0.5f);
            col.size = new Vector3(3.2f, 2.4f, 3.8f);
        }

        CreateWorldSpaceProgressBar();
    }

    void Start()
    {
        mainCam = Camera.main;
        if (weldingLight != null) weldingLight.enabled = false;

        // Tự động load 4 prefab robot nếu inspector chưa gắn
        if (combatRobotPrefabs == null || combatRobotPrefabs.Length == 0)
        {
            List<GameObject> list = new List<GameObject>();
            string[] names = { "Stan", "Mike", "George", "Leela" };
            foreach (var n in names)
            {
                GameObject p = Resources.Load<GameObject>($"FriendlyRobot_{n}");
#if UNITY_EDITOR
                if (p == null) p = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/prefabs/FriendlyRobot_{n}.prefab");
#endif
                if (p != null) list.Add(p);
            }
            if (list.Count > 0) combatRobotPrefabs = list.ToArray();
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        // 1. Dọn dẹp danh sách quân đã tử trận
        CleanupDeadSoldiers();

        // 2. Cập nhật góc nhìn thanh tiến độ về phía Camera
        UpdateProgressBarVisual();

        // 3. Nếu đang có hàng đợi và chưa bắt đầu làm, kích hoạt chế tạo
        if (!isProducing && productionQueue.Count > 0)
        {
            if (activeArmy.Count < maxArmySize)
            {
                StartProductionCycle();
            }
            else
            {
                productionQueue.Clear(); // Đã max quân số
            }
        }

        // 4. Cập nhật chu kỳ chế tạo 15s & Animation chuyển động cơ khí
        if (isProducing)
        {
            productionTimer += Time.deltaTime;
            UpdateMechanicalAnimations();

            if (productionTimer >= productionTime)
            {
                FinishProductionAndSpawnRobot();
            }
        }
        else
        {
            // Trạng thái nghỉ (Idle)
            if (weldingLight != null && weldingLight.enabled) weldingLight.enabled = false;
        }

        // 5. Kiểm tra chạm tay trên màn hình cảm ứng di động (Mobile Touch)
        CheckMobileTouchRaycast();
    }

    /// <summary>
    /// Nhấp chuột hoặc chạm vào Nhà Máy trên không gian 3D -> Mở Bảng Chế Tạo Robot to nửa màn hình!
    /// </summary>
    void OnMouseDown()
    {
        TryOpenCraftingPanel();
    }

    private void CheckMobileTouchRaycast()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return;

                if (mainCam == null) mainCam = Camera.main;
                if (mainCam != null)
                {
                    Ray ray = mainCam.ScreenPointToRay(touch.position);
                    if (Physics.Raycast(ray, out RaycastHit hit, 500f))
                    {
                        if (hit.transform == transform || hit.transform.IsChildOf(transform))
                        {
                            TryOpenCraftingPanel();
                        }
                    }
                }
            }
        }
    }

    public void TryOpenCraftingPanel()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        BuildingSystem bs = FindAnyObjectByType<BuildingSystem>();
        if (bs != null && bs.IsPlacing) return; // Đang kéo đặt công trình thì bỏ qua

        if (RobotCraftingUI.Instance != null)
        {
            RobotCraftingUI.Instance.OpenPanel(this);
            Debug.Log("<color=#00FFFF>[Robot Factory]</color> Đã nhấn vào Nhà Máy! Mở Bảng Chế Tạo Robot Chiến Đấu.");
        }
        else
        {
            // Fallback mở Shop nếu chưa có UI chuyên dụng
            BuildingShopUI shop = FindAnyObjectByType<BuildingShopUI>();
            if (shop != null) shop.ToggleShopPanel();
        }
    }

    /// <summary>
    /// Người chơi chọn đích danh chủng robot muốn chế tạo (0=Stan, 1=Mike, 2=George, 3=Leela).
    /// </summary>
    public bool QueueCraftRobot(int robotIndex)
    {
        CleanupDeadSoldiers();

        int totalExpected = activeArmy.Count + (isProducing ? 1 : 0) + productionQueue.Count;
        if (totalExpected >= maxArmySize)
        {
            Debug.LogWarning($"<color=yellow>[Robot Factory]</color> Quân đội phòng thủ đã đạt tối đa ({maxArmySize}/{maxArmySize})!");
            return false;
        }

        // Kiểm tra chi phí nếu có
        if (craftRobotCost > 0 && GameEconomy.Instance != null)
        {
            if (!GameEconomy.Instance.CanAfford(craftRobotCost)) return false;
            GameEconomy.Instance.SpendCoins(craftRobotCost);
        }

        productionQueue.Enqueue(robotIndex);

        if (!isProducing)
        {
            StartProductionCycle();
        }

        Debug.Log($"<color=#00FF88><b>[Robot Factory]</b> Đã nhận lệnh chế tạo Robot [Index {robotIndex}]! Thời gian: {productionTime:F0}s (Đang chờ: {productionQueue.Count})</color>");
        return true;
    }

    /// <summary>
    /// Nhận lệnh chế tạo luân phiên (tương thích ngược)
    /// </summary>
    public bool QueueCraftRobot()
    {
        int nextIdx = fallbackRobotIndex % 4;
        fallbackRobotIndex++;
        return QueueCraftRobot(nextIdx);
    }

    private void StartProductionCycle()
    {
        if (productionQueue.Count > 0)
        {
            currentCraftingRobotIndex = productionQueue.Dequeue();
        }
        isProducing = true;
        productionTimer = 0f;
        if (weldingLight != null) weldingLight.enabled = true;
    }

    private void UpdateMechanicalAnimations()
    {
        float dt = Time.deltaTime;

        // A. Bánh răng quay tròn cơ khí liên tục
        if (cogs != null)
        {
            for (int i = 0; i < cogs.Length; i++)
            {
                if (cogs[i] != null)
                {
                    float dir = (i % 2 == 0) ? 1f : -1f;
                    cogs[i].Rotate(Vector3.forward * (220f * dir * dt), Space.Self);
                }
            }
        }

        // B. Cánh tay robot cử động hàn linh kiện nhịp nhàng
        armAngle += dt * 3.8f;
        if (robotArms != null)
        {
            for (int i = 0; i < robotArms.Length; i++)
            {
                if (robotArms[i] != null)
                {
                    float tilt = Mathf.Sin(armAngle + i * 1.5f) * 26f;
                    robotArms[i].localRotation = Quaternion.Euler(tilt, Mathf.Cos(armAngle) * 36f, 0f);
                }
            }
        }

        // C. Khối thân máy rung dập nhẹ (Squash & Stretch)
        if (factoryBody != null)
        {
            float bounce = Mathf.PingPong(productionTimer * 4.0f, 1f);
            float scaleY = originalBodyScale.y * (1f + bounce * 0.06f);
            float scaleXZ = originalBodyScale.x * (1f - bounce * 0.03f);
            factoryBody.localScale = new Vector3(scaleXZ, scaleY, scaleXZ);
        }

        // D. Tia lửa hàn điện chớp sáng liên tục
        if (weldingLight != null)
        {
            weldingLight.intensity = Mathf.PingPong(Time.time * 28f, 2.5f) + 0.5f;
        }
    }

    private void FinishProductionAndSpawnRobot()
    {
        isProducing = false;
        productionTimer = 0f;

        if (weldingLight != null) weldingLight.enabled = false;
        if (factoryBody != null) factoryBody.localScale = originalBodyScale;

        // Lấy đúng prefab robot mà người chơi đã chọn
        if (combatRobotPrefabs != null && combatRobotPrefabs.Length > 0)
        {
            int targetIdx = Mathf.Clamp(currentCraftingRobotIndex, 0, combatRobotPrefabs.Length - 1);
            GameObject prefabToSpawn = combatRobotPrefabs[targetIdx];

            if (prefabToSpawn != null)
            {
                Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
                Quaternion spawnRot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

                GameObject newBot = Instantiate(prefabToSpawn, spawnPos, spawnRot);
                newBot.name = $"{prefabToSpawn.name}_{activeArmy.Count + 1}";

                // Gán căn cứ tuần tra cho robot
                FriendlyCombatRobot combatScript = newBot.GetComponent<FriendlyCombatRobot>();
                if (combatScript != null)
                {
                    combatScript.guardOriginPoint = rallyPoint != null ? rallyPoint : transform;
                }

                // Hiệu ứng bước ra khỏi băng chuyền tới điểm tập kết
                StartCoroutine(WalkOutToRallyPoint(newBot, spawnPos, (rallyPoint != null) ? rallyPoint.position : spawnPos + transform.forward * 4.5f));

                activeArmy.Add(newBot);
                Debug.Log($"<color=#00FF88><b>[Robot Factory]</b> 🏭 CHẾ TẠO THÀNH CÔNG (15s): {newBot.name}! Ra trận bảo vệ căn cứ!</color>");
            }
        }

        // Nếu còn trong hàng đợi -> Tiếp tục làm con tiếp theo
        if (productionQueue.Count > 0 && activeArmy.Count < maxArmySize)
        {
            StartProductionCycle();
        }
    }

    private IEnumerator WalkOutToRallyPoint(GameObject bot, Vector3 fromPos, Vector3 toPos)
    {
        if (bot == null) yield break;

        Animator anim = bot.GetComponentInChildren<Animator>();
        if (anim != null) anim.SetBool("isMoving", true);

        float duration = 1.4f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (bot == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            Vector3 cur = Vector3.Lerp(fromPos, toPos, t);
            bot.transform.position = cur;
            bot.transform.LookAt(new Vector3(toPos.x, cur.y, toPos.z));
            yield return null;
        }

        if (bot != null && anim != null)
        {
            anim.SetBool("isMoving", false);
        }
    }

    private void CleanupDeadSoldiers()
    {
        for (int i = activeArmy.Count - 1; i >= 0; i--)
        {
            if (activeArmy[i] == null)
            {
                activeArmy.RemoveAt(i);
            }
        }
    }

    // ================= GETTER CHO CỬA HÀNG & UI =================
    public bool IsProducing => isProducing;
    public float RemainingProductionTime => Mathf.Max(0f, productionTime - productionTimer);
    public int QueuedCount => productionQueue.Count;
    public int CurrentCraftingIndex => currentCraftingRobotIndex;
    public int CurrentArmyCount => GetCurrentArmyCount();

    public int GetCurrentArmyCount()
    {
        CleanupDeadSoldiers();
        return activeArmy.Count;
    }

    public int GetMaxArmySize() => maxArmySize;

    public float GetProductionProgress()
    {
        if (!isProducing || productionTime <= 0f) return 0f;
        return Mathf.Clamp01(productionTimer / productionTime);
    }

    // ================= THANH TIẾN ĐỘ 3D TRÊN NÓC NHÀ MÁY =================
    private void CreateWorldSpaceProgressBar()
    {
        GameObject pRoot = new GameObject("FactoryProgress_Root");
        pRoot.transform.SetParent(transform);
        pRoot.transform.localPosition = new Vector3(0f, 3.2f, 0f);
        progressBarRoot = pRoot.transform;

        // BG đen viền
        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.name = "Progress_BG";
        bg.transform.SetParent(progressBarRoot);
        bg.transform.localPosition = Vector3.zero;
        bg.transform.localScale = new Vector3(2.4f, 0.28f, 1f);
        Collider cBg = bg.GetComponent<Collider>();
        if (cBg != null) Destroy(cBg);

        Renderer rBg = bg.GetComponent<Renderer>();
        if (rBg != null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            Material mBg = new Material(sh);
            mBg.color = new Color(0.1f, 0.12f, 0.15f, 0.9f);
            rBg.material = mBg;
        }

        // Fill vàng / cam công nghiệp
        GameObject fg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fg.name = "Progress_Fill";
        fg.transform.SetParent(progressBarRoot);
        fg.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        fg.transform.localScale = new Vector3(2.35f, 0.20f, 1f);
        Collider cFg = fg.GetComponent<Collider>();
        if (cFg != null) Destroy(cFg);

        Renderer rFg = fg.GetComponent<Renderer>();
        if (rFg != null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            Material mFg = new Material(sh);
            mFg.color = new Color(1f, 0.65f, 0f);
            rFg.material = mFg;
        }

        progressBarFill = fg.transform;
    }

    private void UpdateProgressBarVisual()
    {
        if (progressBarRoot == null) return;

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam != null)
        {
            progressBarRoot.rotation = Quaternion.LookRotation(progressBarRoot.position - mainCam.transform.position);
        }

        if (progressBarFill != null)
        {
            if (isProducing)
            {
                float ratio = GetProductionProgress();
                progressBarFill.localScale = new Vector3(2.35f * ratio, 0.20f, 1f);
                progressBarFill.localPosition = new Vector3(-1.175f * (1f - ratio), 0f, -0.01f);
                Renderer r = progressBarFill.GetComponent<Renderer>();
                if (r != null && r.material != null) r.material.color = new Color(1f, 0.65f, 0f);
            }
            else
            {
                int count = GetCurrentArmyCount();
                if (count >= maxArmySize)
                {
                    progressBarFill.localScale = new Vector3(2.35f, 0.20f, 1f);
                    progressBarFill.localPosition = new Vector3(0f, 0f, -0.01f);
                    Renderer r = progressBarFill.GetComponent<Renderer>();
                    if (r != null && r.material != null) r.material.color = new Color(0.2f, 0.8f, 0.2f);
                }
                else
                {
                    progressBarFill.localScale = new Vector3(0.01f, 0.20f, 1f);
                    progressBarFill.localPosition = new Vector3(-1.17f, 0f, -0.01f);
                }
            }
        }
    }
}
