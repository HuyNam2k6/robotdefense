using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IslandSettler - Dân Cư Căn Cứ / Cư Dân Robot Sinh Sống Trên Đảo
/// - 100% Raycast & Trọng lực bằng code -9.18f (TUYỆT ĐỐI KHÔNG DÙNG RIGIDBODY).
/// - ĐỔI MÀU RIÊNG BIỆT (Xanh Cyan, Xanh Ngọc, Tím Hồng, Cam...) TUYỆT ĐỐI KHÔNG TRÙNG MÀU VÀNG VỚI PLAYER.
/// - ĐI QUANH MAP NHẶT VÀNG: Tự động phát hiện và chạy đi nhặt các đồng xu rơi (DroppedGoldCoin).
/// - TRÚ ẨN VÀO NHÀ KHI LÀNG BỊ TẤN CÔNG: Chạy vào ngôi nhà gần nhất (CivilianHouse) và biến mất khi vào nhà.
/// - RA NGOÀI KHI AN TOÀN: Khi hết quái/trời sáng, tự động bước ra khỏi nhà ăn mừng và tiếp tục nhặt vàng.
/// - KHI NHẤN VÀO: Hiển thị lời chào thân mật "Xin chào! Bạn khỏe không?" kèm animation vẫy tay chào (Wave).
/// </summary>
public class IslandSettler : MonoBehaviour
{
    [Header("=== THÔNG TIN DÂN CƯ ===")]
    public string citizenName = "Bobi";
    public Color themeColor = new Color(0.1f, 0.75f, 1f); // Mặc định Cyan nổi bật

    [Header("=== CÀI ĐẶT DI CHUYỂN ===")]
    public float walkSpeed = 2.8f;
    public float runSpeed = 4.5f;
    public float rotateSpeed = 460.0f;
    public float wanderRadius = 85.0f;
    public float gravity = -9.18f;

    [Header("=== TƯƠNG TÁC PLAYER ===")]
    public float playerDetectRadius = 3.8f;
    public float greetCooldown = 6.0f;

    [Header("=== TRẠNG THÁI HIỆN TẠI ===")]
    public bool isInsideHouse = false;
    public int goldCollectedCount = 0;

    private Animator anim;
    private Transform playerTransform;
    private Vector3 homePosition;
    private Vector3 targetMovePoint;
    private float verticalVelocity = 0f;
    private float stateTimer = 0f;
    private float lastGreetTime = -999f;
    private bool isGreeting = false;

    // Biến phát hiện kẹt vật cản
    private Vector3 lastStuckCheckPos;
    private float stuckCheckTimer = 0f;

    // Mục tiêu nhặt vàng & nhà trú ẩn
    private DroppedGoldCoin targetCoin;
    private CivilianHouse targetHouse;

    // Trạng thái AI
    private enum SettlerState
    {
        Idle,
        Wandering,
        CollectingGold,
        FleeingToHouse,
        Greeting,
        Celebrating
    }
    private SettlerState currentState = SettlerState.Idle;

    // Overhead UI
    private GameObject overheadCanvasObj;
    private GameObject chatBubblePanel;
    private UnityEngine.UI.Text overheadText;
    private UnityEngine.UI.Text emoteText;

    private void OnEnable()
    {
        DroppedGoldCoin.OnCoinDropped += HandleCoinDroppedNotification;
    }

    private void OnDisable()
    {
        DroppedGoldCoin.OnCoinDropped -= HandleCoinDroppedNotification;
    }

    private void HandleCoinDroppedNotification(Vector3 coinPos)
    {
        if (isInsideHouse || currentState == SettlerState.FleeingToHouse) return;

        // Dân làng phát hiện vàng rơi -> Lập tức chạy đến nhặt vàng
        CheckForGoldCoinsToCollect();
    }

    private static readonly string[] NamesPool = new string[]
    {
        "Bobi", "Sparky", "Tiki", "Nomi", "Gizmo", "Clank", "Rusty", "Pip", "Chippy", "Bolt"
    };

    private static readonly Color[] ColorPalette = new Color[]
    {
        new Color(0.08f, 0.85f, 1.0f),  // Xanh Cyan Neon
        new Color(0.15f, 0.95f, 0.45f), // Xanh Ngọc Lục Bảo
        new Color(1.0f, 0.25f, 0.65f),  // Hồng Tím Magenta
        new Color(1.0f, 0.55f, 0.15f),  // Cam Rực Rỡ
        new Color(0.65f, 0.35f, 1.0f),  // Tím Hoàng Gia
        new Color(0.85f, 0.92f, 1.0f)   // Bạc Ánh Kim
    };

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.applyRootMotion = false;
        }

        if (string.IsNullOrEmpty(citizenName) || citizenName == "Bobi")
        {
            citizenName = NamesPool[Random.Range(0, NamesPool.Length)];
        }

        // Chọn màu sắc ngẫu nhiên không trùng với Player (vàng)
        themeColor = ColorPalette[Random.Range(0, ColorPalette.Length)];
        ApplyCustomColors();

        // Collider Trigger nhẹ nhàng để nhận bắt click chuột và chạm cảm ứng trên mobile
        EnsureClickCollider();
    }

    private void Start()
    {
        homePosition = transform.position;
        targetMovePoint = homePosition;
        FindPlayer();
        CreateOverheadUI();
        stateTimer = Random.Range(1.0f, 3.0f);
        currentState = SettlerState.Idle;

        EnsureClickCollider();
    }

    private void EnsureClickCollider()
    {
        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col == null) col = gameObject.AddComponent<CapsuleCollider>();
        col.isTrigger = true;
        col.center = new Vector3(0f, 0.75f, 0f);
        col.radius = 0.55f;
        col.height = 1.6f;
    }

    private void Update()
    {
        if (isInsideHouse) return; // Đang ở trong nhà -> Tạm dừng cập nhật

        if (playerTransform == null) FindPlayer();

        ApplyGravityAndGrounding();
        CheckDangerAndThreats();
        UpdateStateMachine();
        UpdateOverheadPositionAndFacing();
        CheckPlayerClickRaycast();
    }

    /// <summary>
    /// Thay đổi vật liệu của Dân Cư để KHÔNG TRÙNG MÀU VỚI PLAYER (Vàng)
    /// </summary>
    public void ApplyCustomColors()
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor", themeColor);
        mpb.SetColor("_Color", themeColor);
        mpb.SetColor("_EmissionColor", themeColor * 0.45f);

        foreach (var r in renderers)
        {
            if (r == null || r.gameObject.name.Contains("Overhead")) continue;

            bool isHead = r.gameObject.name.ToLower().Contains("head");

            // Chỉ dùng MaterialPropertyBlock cho các bộ phận không phải đầu để tránh đè màu mắt
            if (!isHead)
            {
                r.SetPropertyBlock(mpb);
            }

            // TUYỆT ĐỐI CHỈ gọi r.materials khi đang chơi (Play Mode) để tránh leak material trong Edit Mode
            if (Application.isPlaying)
            {
                Material[] mats = r.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material m = mats[i];
                    if (m == null) continue;

                    // Mắt của dân làng: luôn là màu đen theo yêu cầu
                    if (isHead && i == 2)
                    {
                        m.color = Color.black;
                        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.black);
                        if (m.HasProperty("_Color")) m.SetColor("_Color", Color.black);
                        if (m.HasProperty("_EmissionColor"))
                        {
                            m.DisableKeyword("_EMISSION");
                            m.SetColor("_EmissionColor", Color.black);
                        }
                        continue;
                    }

                    m.color = themeColor;
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", themeColor);
                    if (m.HasProperty("_Color")) m.SetColor("_Color", themeColor);
                    if (m.HasProperty("_EmissionColor"))
                    {
                        m.EnableKeyword("_EMISSION");
                        m.SetColor("_EmissionColor", themeColor * 0.45f);
                    }
                }
                r.materials = mats;
            }
        }
    }

    /// <summary>
    /// TỐI ƯU HÓA CPU: Gỡ bỏ 100% Collider trên dân cư để giải phóng PhysX broadphase & narrowphase
    /// </summary>
    private void StripAllColliders()
    {
        var cols = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] == null) continue;
            if (Application.isPlaying)
            {
                Destroy(cols[i]);
            }
            else
            {
                DestroyImmediate(cols[i]);
            }
        }
    }

    private void FindPlayer()
    {
        var player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null) playerTransform = player.transform;
    }

    /// <summary>
    /// Kiểm tra xem làng có bị quái vật tấn công không để chạy vào nhà
    /// </summary>
    private void CheckDangerAndThreats()
    {
        if (isInsideHouse) return;

        bool isUnderAttack = CivilianHouse.IsVillageUnderAttack();

        if (isUnderAttack && currentState != SettlerState.FleeingToHouse)
        {
            // Làng bị tấn công! Tìm ngôi nhà gần nhất để chạy vào
            FindNearestHouse();
            if (targetHouse != null)
            {
                currentState = SettlerState.FleeingToHouse;
                ShowEmote("😱 Quái vật! Chạy vào nhà mau!");
                SetMovingAnim(true);
            }
        }
    }

    private void FindNearestHouse()
    {
        targetHouse = null;
        float closestDist = float.MaxValue;
        var houses = CivilianHouse.AllHouses;

        for (int i = 0; i < houses.Count; i++)
        {
            CivilianHouse h = houses[i];
            if (h == null) continue;

            float d = Vector3.Distance(transform.position, h.transform.position);
            if (d < closestDist)
            {
                closestDist = d;
                targetHouse = h;
            }
        }

        // Nếu chưa có nhà nào được đặt trên Scene, chạy về gần BaseHQ
        if (targetHouse == null)
        {
            var baseHQ = Object.FindFirstObjectByType<BaseHQ>();
            if (baseHQ != null)
            {
                targetMovePoint = baseHQ.transform.position + (Vector3)Random.insideUnitCircle * 3.5f;
            }
        }
    }

    private void UpdateStateMachine()
    {
        stateTimer -= Time.deltaTime;

        // Nếu làng đang bị tấn công -> Ưu tiên chạy vào nhà
        if (currentState == SettlerState.FleeingToHouse)
        {
            HandleFleeingToHouse();
            return;
        }

        // Nếu bình thường: Quét tìm đồng xu vàng (DroppedGoldCoin) rơi trên map
        if (currentState != SettlerState.Greeting && currentState != SettlerState.Celebrating)
        {
            CheckForGoldCoinsToCollect();
        }

        switch (currentState)
        {
            case SettlerState.CollectingGold:
                HandleCollectingGold();
                break;

            case SettlerState.Idle:
                SetMovingAnim(false);
                if (stateTimer <= 0f)
                {
                    // Chọn điểm dạo chơi mới rộng khắp bản đồ
                    targetMovePoint = PickMapWanderDestination();
                    currentState = SettlerState.Wandering;
                    stateTimer = Random.Range(12.0f, 25.0f);
                    lastStuckCheckPos = transform.position;
                    stuckCheckTimer = 0f;
                }
                break;

            case SettlerState.Wandering:
                MoveTowards(targetMovePoint, walkSpeed);

                // Phát hiện kẹt vật cản: Nếu sau 1.8s không tiến được > 0.25m thì đổi hướng khác
                stuckCheckTimer += Time.deltaTime;
                if (stuckCheckTimer >= 1.8f)
                {
                    stuckCheckTimer = 0f;
                    if (Vector3.Distance(transform.position, lastStuckCheckPos) < 0.25f)
                    {
                        currentState = SettlerState.Idle;
                        stateTimer = Random.Range(1.0f, 2.5f);
                        SetMovingAnim(false);
                        break;
                    }
                    lastStuckCheckPos = transform.position;
                }

                float distToTarget = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                                                      new Vector3(targetMovePoint.x, 0, targetMovePoint.z));
                if (distToTarget < 1.2f || stateTimer <= 0f)
                {
                    currentState = SettlerState.Idle;
                    stateTimer = Random.Range(2.0f, 4.5f);
                    SetMovingAnim(false);
                }
                break;

            case SettlerState.Celebrating:
                SetMovingAnim(false);
                if (stateTimer <= 0f)
                {
                    currentState = SettlerState.Idle;
                    stateTimer = 2.0f;
                }
                break;

            case SettlerState.Greeting:
                SetMovingAnim(false);
                if (playerTransform != null)
                {
                    Vector3 lookDir = playerTransform.position - transform.position;
                    lookDir.y = 0f;
                    if (lookDir.sqrMagnitude > 0.01f)
                    {
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(lookDir), rotateSpeed * Time.deltaTime);
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Lấy điểm dạo chơi ngẫu nhiên trên toàn bản đồ đảo (chỉ chọn điểm cao nguyên khô ráo, không rơi xuống biển)
    /// </summary>
    private Vector3 PickMapWanderDestination()
    {
        Terrain t = Terrain.activeTerrain;
        float maxExtentX = (t != null) ? t.terrainData.size.x * 0.35f : 60f;
        float maxExtentZ = (t != null) ? t.terrainData.size.z * 0.42f : 120f;

        for (int attempt = 0; attempt < 10; attempt++)
        {
            float rx, rz;
            // 70% đi xa khám phá toàn đảo, 30% dạo quanh vị trí hiện tại
            if (Random.value < 0.7f)
            {
                rx = Random.Range(-maxExtentX, maxExtentX);
                rz = Random.Range(-maxExtentZ, maxExtentZ);
            }
            else
            {
                rx = Mathf.Clamp(transform.position.x + Random.Range(-30f, 30f), -maxExtentX, maxExtentX);
                rz = Mathf.Clamp(transform.position.z + Random.Range(-30f, 30f), -maxExtentZ, maxExtentZ);
            }

            Vector3 candidate = new Vector3(rx, transform.position.y, rz);
            if (t != null)
            {
                float groundY = t.SampleHeight(candidate) + t.transform.position.y;
                // Chỉ chọn điểm nằm trên mặt đảo khô ráo (>= 7.5m), tuyệt đối không đi xuống biển
                if (groundY >= 7.5f)
                {
                    candidate.y = groundY;
                    return candidate;
                }
            }
            else
            {
                return candidate;
            }
        }

        return homePosition;
    }

    /// <summary>
    /// Tìm đồng xu vàng rơi trên bản đồ để chạy đến nhặt
    /// </summary>
    private void CheckForGoldCoinsToCollect()
    {
        if (targetCoin != null && targetCoin.gameObject.activeInHierarchy) return;

        var coins = DroppedGoldCoin.ActiveCoins;
        if (coins.Count == 0) return;

        DroppedGoldCoin bestCoin = null;
        float bestDist = 200.0f; // Quét toàn bộ hòn đảo để nhặt vàng ngay khi rơi

        for (int i = 0; i < coins.Count; i++)
        {
            DroppedGoldCoin c = coins[i];
            if (c == null) continue;

            float d = Vector3.Distance(transform.position, c.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                bestCoin = c;
            }
        }

        if (bestCoin != null)
        {
            targetCoin = bestCoin;
            currentState = SettlerState.CollectingGold;
            ShowEmote("🪙 Có vàng rơi! Đi nhặt nhanh!");
        }
    }

    private void HandleCollectingGold()
    {
        if (targetCoin == null || !targetCoin.gameObject.activeInHierarchy)
        {
            targetCoin = null;
            currentState = SettlerState.Idle;
            stateTimer = 1.0f;
            return;
        }

        Vector3 coinPos = targetCoin.transform.position;
        MoveTowards(coinPos, runSpeed);

        float dist = Vector3.Distance(transform.position, coinPos);
        float dist2D = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(coinPos.x, coinPos.z));
        if (dist <= 1.6f || dist2D <= 1.5f)
        {
            // Nhặt thành công đồng xu
            goldCollectedCount += targetCoin.coinValue;
            targetCoin.CollectBySettler(transform);
            targetCoin = null;

            ShowEmote("✨ Đã nhặt vàng cho làng!");
            if (anim != null) anim.SetTrigger("jump");

            currentState = SettlerState.Idle;
            stateTimer = 1.5f;
            SetMovingAnim(false);
        }
    }

    private void HandleFleeingToHouse()
    {
        if (targetHouse == null)
        {
            FindNearestHouse();
            if (targetHouse == null) return;
        }

        Vector3 doorPos = targetHouse.doorPoint != null ? targetHouse.doorPoint.position : targetHouse.transform.position;
        MoveTowards(doorPos, runSpeed);

        float distToDoor = Vector3.Distance(transform.position, doorPos);
        if (distToDoor <= targetHouse.enterDistance)
        {
            // Chạm vào cửa nhà -> Biến mất vào trong nhà an toàn!
            targetHouse.EnterHouse(this);
        }
    }

    /// <summary>
    /// Gọi khi chạm vào nhà và biến mất vào trong
    /// </summary>
    public void EnterHouseInternal(CivilianHouse house)
    {
        isInsideHouse = true;
        SetMovingAnim(false);

        // Ẩn toàn bộ renderers và UI
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            r.enabled = false;
        }
        if (overheadCanvasObj != null) overheadCanvasObj.SetActive(false);
    }

    /// <summary>
    /// Gọi khi an toàn và bước ra khỏi nhà
    /// </summary>
    public void ExitHouseInternal(Vector3 exitPos)
    {
        isInsideHouse = false;
        transform.position = exitPos;

        // Hiện lại toàn bộ renderers và UI
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            r.enabled = true;
        }
        if (overheadCanvasObj != null) overheadCanvasObj.SetActive(true);

        currentState = SettlerState.Celebrating;
        stateTimer = 3.5f;
        if (anim != null) anim.SetTrigger("dance");

        ShowEmote("🎉 An toàn rồi! Ra ngoài nhặt vàng!");
    }

    /// <summary>
    /// Khi click chuột hoặc nhấn vào con dân làng: Hiện lời chào "Xin chào! Bạn khỏe không?"
    /// </summary>
    public void OnPlayerClicked()
    {
        if (isInsideHouse || isGreeting) return;

        StartCoroutine(GreetPlayerRoutine());
    }

    /// <summary>
    /// Nhận diện tương tác click chuột / chạm màn hình:
    /// - Kết hợp Physics.Raycast qua Trigger Collider
    /// - Và tính khoảng cách Screen-Space / Ray-to-Point để chạm trên Mobile cực nhạy
    /// </summary>
    private void CheckPlayerClickRaycast()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (WallSelectionManager.IsPointerOverUI()) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            // 1. Kiểm tra va chạm Raycast qua Collider Trigger của Dân Cư
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    OnPlayerClicked();
                    return;
                }
            }

            Vector3 center = transform.position + Vector3.up * 0.75f;

            // 2. Khoảng cách hình học từ tia Ray tới tâm dân cư
            Vector3 toCenter = center - ray.origin;
            float projection = Vector3.Dot(toCenter, ray.direction);
            if (projection > 0f)
            {
                Vector3 closestPoint = ray.origin + ray.direction * projection;
                if ((center - closestPoint).sqrMagnitude <= 1.8f) // Bán kính nhận diện mở rộng ~1.35m
                {
                    OnPlayerClicked();
                    return;
                }
            }

            // 3. Khoảng cách Screen-Space (Tối ưu cho cả Mobile Touch và PC)
            Vector3 screenPos = cam.WorldToScreenPoint(center);
            if (screenPos.z > 0f)
            {
                Vector2 screenDelta = (Vector2)screenPos - (Vector2)Input.mousePosition;
                if (screenDelta.sqrMagnitude <= 85f * 85f) // Bán kính 85 pixel
                {
                    OnPlayerClicked();
                }
            }
        }
    }

    private IEnumerator GreetPlayerRoutine()
    {
        isGreeting = true;
        currentState = SettlerState.Greeting;
        lastGreetTime = Time.time;
        SetMovingAnim(false);

        // 1. Quay mặt ngay lập tức về phía Player
        if (playerTransform == null) FindPlayer();
        if (playerTransform != null)
        {
            Vector3 lookDir = playerTransform.position - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }

        // 2. Kích hoạt animation vẫy tay chào thân thiện (Wave)
        if (anim != null)
        {
            anim.ResetTrigger("jump");
            anim.ResetTrigger("dance");
            anim.SetTrigger("wave");
        }

        // 3. Hiển thị chữ trên đầu dân làng
        if (chatBubblePanel != null)
        {
            chatBubblePanel.SetActive(true);
        }
        if (emoteText != null)
        {
            emoteText.text = "👋 Xin chào! Bạn khỏe không?";
            emoteText.color = new Color(1f, 0.92f, 0.2f, 1f);
        }

        // 4. Duy trì hiển thị và vẫy tay trong 2.8 giây
        yield return new WaitForSeconds(2.8f);

        // 5. Sau đó tự động ẩn bong bóng chữ trên đầu đi
        if (chatBubblePanel != null)
        {
            chatBubblePanel.SetActive(false);
        }

        isGreeting = false;
        currentState = SettlerState.Idle;
        stateTimer = Random.Range(1.5f, 3.0f);
    }

    private void MoveTowards(Vector3 target, float speed)
    {
        Vector3 moveDir = target - transform.position;
        moveDir.y = 0f;

        if (moveDir.sqrMagnitude > 0.05f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);

            Vector3 step = transform.forward * speed * Time.deltaTime;
            transform.position += step;
            SetMovingAnim(true);
        }
        else
        {
            SetMovingAnim(false);
        }
    }

    private void SetMovingAnim(bool moving)
    {
        if (anim != null)
        {
            anim.SetBool("isMoving", moving);
        }
    }

    private void ApplyGravityAndGrounding()
    {
        Vector3 rayStart = transform.position + Vector3.up * 1.5f;
        RaycastHit hit;

        if (Physics.Raycast(rayStart, Vector3.down, out hit, 8f, ~0, QueryTriggerInteraction.Ignore))
        {
            float groundY = hit.point.y;
            if (transform.position.y > groundY + 0.05f)
            {
                verticalVelocity += gravity * Time.deltaTime;
                float newY = transform.position.y + verticalVelocity * Time.deltaTime;
                if (newY < groundY)
                {
                    newY = groundY;
                    verticalVelocity = 0f;
                }
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }
            else
            {
                transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
                verticalVelocity = 0f;
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
            transform.position += Vector3.up * verticalVelocity * Time.deltaTime;
        }
    }

    private void CreateOverheadUI()
    {
        if (overheadCanvasObj != null) return;

        overheadCanvasObj = new GameObject("OverheadCanvas_Settler");
        overheadCanvasObj.transform.SetParent(transform, false);
        // Đưa thanh hiển thị lên cao hẳn trên đỉnh đầu dân làng (2.45m) để người chơi nhìn rõ từ xa
        overheadCanvasObj.transform.localPosition = new Vector3(0, 2.45f, 0);

        Canvas canvas = overheadCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 500; // Ưu tiên hiển thị trên cùng không bị che khuất
        overheadCanvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();

        RectTransform rt = overheadCanvasObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(360, 110);
        overheadCanvasObj.transform.localScale = Vector3.one * 0.0135f;

        // Khung nền Bong Bóng Thoại (Chat Balloon Box) với viền Cyber nổi bật
        chatBubblePanel = new GameObject("ChatBubble_Panel", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Outline));
        chatBubblePanel.transform.SetParent(overheadCanvasObj.transform, false);
        RectTransform bRt = chatBubblePanel.GetComponent<RectTransform>();
        bRt.sizeDelta = new Vector2(350, 52);
        bRt.anchoredPosition = new Vector2(0, 28);

        UnityEngine.UI.Image bImg = chatBubblePanel.GetComponent<UnityEngine.UI.Image>();
        bImg.color = new Color(0.04f, 0.10f, 0.22f, 0.96f); // Nền xanh đen công nghệ tương phản rất cao

        UnityEngine.UI.Outline bOutline = chatBubblePanel.GetComponent<UnityEngine.UI.Outline>();
        bOutline.effectColor = new Color(0f, 0.95f, 1f, 1f); // Viền Cyan sắc nét
        bOutline.effectDistance = new Vector2(2f, -2f);

        // Text Lời thoại: "👋 Xin chào! Bạn khỏe không?" to rõ, dễ đọc
        GameObject emoteObj = new GameObject("EmoteText");
        emoteObj.transform.SetParent(chatBubblePanel.transform, false);
        emoteText = emoteObj.AddComponent<UnityEngine.UI.Text>();
        emoteText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        emoteText.fontSize = 20;
        emoteText.fontStyle = FontStyle.Bold;
        emoteText.alignment = TextAnchor.MiddleCenter;
        emoteText.color = new Color(1f, 0.92f, 0.2f, 1f); // Màu vàng rực rỡ nổi bật
        emoteText.text = "👋 Xin chào! Bạn khỏe không?";

        RectTransform emoteRt = emoteObj.GetComponent<RectTransform>();
        emoteRt.anchorMin = Vector2.zero;
        emoteRt.anchorMax = Vector2.one;
        emoteRt.sizeDelta = Vector2.zero;

        // Text tên dân cư phía dưới
        GameObject textObj = new GameObject("NameText");
        textObj.transform.SetParent(overheadCanvasObj.transform, false);
        overheadText = textObj.AddComponent<UnityEngine.UI.Text>();
        overheadText.font = emoteText.font;
        overheadText.fontSize = 17;
        overheadText.fontStyle = FontStyle.Bold;
        overheadText.alignment = TextAnchor.MiddleCenter;
        overheadText.color = themeColor;
        overheadText.text = $"🤖 <b>{citizenName}</b>";

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.sizeDelta = new Vector2(300, 26);
        textRt.anchoredPosition = new Vector2(0, -20);

        // MẶC ĐỊNH ẨN BÓNG THOẠI, CHỈ XUẤT HIỆN KHI NGƯỜI CHƠI NHẤN VÀO DÂN LÀNG
        chatBubblePanel.SetActive(false);
    }

    private Coroutine emoteCoroutine;
    public void ShowEmote(string text)
    {
        if (emoteText == null) return;
        if (emoteCoroutine != null) StopCoroutine(emoteCoroutine);
        emoteCoroutine = StartCoroutine(ShowEmoteRoutine(text));
    }

    private IEnumerator ShowEmoteRoutine(string text)
    {
        if (chatBubblePanel != null) chatBubblePanel.SetActive(true);
        if (emoteText != null)
        {
            emoteText.text = text;
            emoteText.color = new Color(0.2f, 1f, 0.4f, 1f); // Nháy xanh neon khi có lời thoại mới
        }

        // Duy trì lời thoại đặc biệt ít nhất 3.0s
        yield return new WaitForSeconds(3.0f);

        // Sau khi hết thời gian, tự động ẩn bong bóng thoại đi theo đúng yêu cầu
        if (chatBubblePanel != null)
        {
            chatBubblePanel.SetActive(false);
        }
    }

    private float GetHeadTopY()
    {
        Renderer[] rends = GetComponentsInChildren<Renderer>(true);
        float highest = float.MinValue;
        foreach (var r in rends)
        {
            if (r == null || r is ParticleSystemRenderer || r is LineRenderer) continue;
            string n = r.name;
            if (n.Contains("Canvas") || n.Contains("Text") || n.Contains("Panel") || n.Contains("Bubble")) continue;
            if (r.bounds.max.y > highest) highest = r.bounds.max.y;
        }
        if (highest > float.MinValue + 1f)
        {
            return highest + 0.55f; // Cao hơn đỉnh đầu thực tế của dân làng 0.55m
        }
        return transform.position.y + 2.75f; // Mặc định cao hơn chân 2.75m
    }

    private void LateUpdate()
    {
        UpdateOverheadPositionAndFacing();
    }

    /// <summary>
    /// Giữ thanh chữ VÀ BONG BÓNG THOẠI LUÔN Ở TRÊN ĐỈNH ĐẦU DÂN LÀNG và hướng thẳng về Camera
    /// </summary>
    private void UpdateOverheadPositionAndFacing()
    {
        if (overheadCanvasObj == null) return;

        // 1. Luôn khóa vị trí ở trên đỉnh đầu dân làng trong World Space (không bao giờ tụt xuống giữa bụng)
        overheadCanvasObj.transform.position = new Vector3(transform.position.x, GetHeadTopY(), transform.position.z);

        // 2. Khử ảnh hưởng của scale thân dân làng để kích thước chữ luôn chuẩn xác
        Vector3 pLossy = transform.lossyScale;
        float sx = pLossy.x > 0.001f ? (0.0135f / pLossy.x) : 0.0135f;
        float sy = pLossy.y > 0.001f ? (0.0135f / pLossy.y) : 0.0135f;
        float sz = pLossy.z > 0.001f ? (0.0135f / pLossy.z) : 0.0135f;
        overheadCanvasObj.transform.localScale = new Vector3(sx, sy, sz);

        // 3. Xoay thẳng về Camera chính để người chơi luôn nhìn rõ nhất từ mọi góc độ
        Camera cam = Camera.main;
        if (cam != null)
        {
            overheadCanvasObj.transform.rotation = cam.transform.rotation;
        }
    }
}
