using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    [Header("--- Thông Số Quái Vật ---")]
    public string enemyName = "Scrap Bug";
    public float maxHealth = 50f;
    public float currentHealth;
    public float moveSpeed = 3f;
    public int goldReward = 15;
    public float attackRange = 1.8f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.2f;

    [Header("--- Thanh Máu Nổi (World-Space HealthBar) ---")]
    public bool showHealthBar = true;
    public float healthBarHeight = 1.6f;
    private GameObject healthBarObj;
    private RectTransform healthBarFill;
    private Image healthBarFillImage;
    private Text healthBarText;
    private Camera mainCamera;

    [Header("--- Hiệu Chỉnh Góc Xoay Model ---")]
    [Tooltip("Góc bù xoay nếu model gốc bị quay ngược (ví dụ 180 độ đối với Spider)")]
    public float modelRotationOffset = 0f;

    [Header("--- Mục Tiêu Tấn Công ---")]
    public Transform target;

    [Header("--- Chế Độ Bay (Flying / Aerial Enemy) ---")]
    public bool isFlying = false;
    public float flightAltitude = 3.2f;
    public float hoverBobSpeed = 2.2f;
    public float hoverBobAmount = 0.35f;

    [Header("--- Hiệu Ứng & Animation ---")]
    public Animator animator;
    public GameObject deathVFX;

    [Header("--- Hiệu Ứng Đạn / Đòn Đánh (Projectile VFX) ---")]
    [Tooltip("Prefab quả cầu đạn năng lượng (Hiển thị trong Thư viện Project: Dragon_PlasmaBall.prefab)")]
    public GameObject projectilePrefab;
    [Tooltip("Màu sắc của quả cầu đạn nếu tự sinh bằng code")]
    public Color projectileColor = new Color(0f, 1f, 1f, 1f);
    [Tooltip("Vị trí bắn đạn ra từ miệng rồng (tự động tìm xương Head nếu để trống)")]
    public Transform shootPoint;
    [Tooltip("Độ trễ (giây) từ lúc bắt đầu animation đến lúc đầu chúi xuống phun đạn")]
    public float attackDelay = 0.5f;

    private float lastAttackTime = 0f;
    private float lastDamageTime = -999f;
    public bool isDead = false;
    private Collider myCollider;
    private float retargetTimer = 0f;

    public static readonly System.Collections.Generic.List<Enemy> AllEnemies = new System.Collections.Generic.List<Enemy>();

    void OnEnable()
    {
        if (!AllEnemies.Contains(this)) AllEnemies.Add(this);
    }

    void OnDisable()
    {
        AllEnemies.Remove(this);
    }

    [Header("--- Bán Kính Va Chạm Raycast Không Dùng Collider ---")]
    public float hitRadius = 1.2f;

    /// <summary>
    /// Tính toán va chạm trừ HP của quái vật bằng Raycast toán học (Ray-Sphere Intersection),
    /// Hoạt động 100% chuẩn xác ngay cả khi đã xóa toàn bộ Collider trên Enemy!
    /// </summary>
    public static bool RaycastHitEnemy(Ray ray, float maxDistance, out Enemy hitEnemy, out Vector3 hitPoint)
    {
        hitEnemy = null;
        hitPoint = Vector3.zero;
        float closestDist = float.MaxValue;

        for (int i = 0; i < AllEnemies.Count; i++)
        {
            Enemy e = AllEnemies[i];
            if (e == null || e.isDead || !e.gameObject.activeInHierarchy) continue;

            float r = e.hitRadius;
            Vector3 center = e.transform.position + Vector3.up * (e.isFlying ? 1.0f : 0.8f);

            // Thuật toán Ray-Sphere Intersection
            Vector3 m = ray.origin - center;
            float b = Vector3.Dot(m, ray.direction);
            float c = Vector3.Dot(m, m) - r * r;

            if (c > 0.0f && b > 0.0f) continue;

            float discr = b * b - c;
            if (discr < 0.0f) continue;

            float t = -b - Mathf.Sqrt(discr);
            if (t < 0.0f) t = 0.0f;

            if (t <= maxDistance && t < closestDist)
            {
                closestDist = t;
                hitEnemy = e;
                hitPoint = ray.origin + ray.direction * t;
            }
        }

        return hitEnemy != null;
    }

    void Awake()
    {
        currentHealth = maxHealth;
        myCollider = GetComponent<Collider>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Tối ưu CPU & Khử lỗi bay lên trời: Xóa bỏ Rigidbody và chuyển toàn bộ Collider thành Trigger
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        Collider[] allCols = GetComponentsInChildren<Collider>();
        foreach (var c in allCols)
        {
            if (c != null) c.isTrigger = true; // Tuyệt đối không dùng collider vật lý cản trở hay đẩy quái
        }

        // Tự động chuẩn hóa kích thước theo yêu cầu thiết kế mới nhất:
        // 1. Boss Rồng (MechaDragon): Giảm độ cao bay, scale 4.0
        // 2. Bọ ngoài hành tinh (AlienBug): Scale 3.0, bò đất
        // 3. Nhện (Spider): Scale 0.5, bò đất
        string lowerCheck = (name + " " + enemyName).ToLower();
        bool isBoss = isFlying || lowerCheck.Contains("dragon") || GetComponent<MechaDragonBoss>() != null;

        if (isBoss)
        {
            transform.localScale = Vector3.one * 4.0f;
            isFlying = true;
            flightAltitude = 1.4f; // Bay là là mặt đất 1.4m, vừa tầm mắt để người chơi nhìn rõ và tấn công thoải mái
            hitRadius = 3.2f;
        }
        else if (lowerCheck.Contains("alienbug") || lowerCheck.Contains("bug"))
        {
            transform.localScale = Vector3.one * 3.0f;
            isFlying = false;
            flightAltitude = 0f;
            hitRadius = 1.6f;
        }
        else if (lowerCheck.Contains("spider"))
        {
            transform.localScale = Vector3.one * 0.5f;
            isFlying = false;
            flightAltitude = 0f;
            hitRadius = 0.9f;
        }

        // Lượng máu (HP): Giữ nguyên đúng chỉ số thiết kế theo từng loại quái từ Prefab (không đồng bộ số máu)
        baseMaxHealth = maxHealth;
        baseMoveSpeed = moveSpeed;
        baseAttackDamage = attackDamage;
        baseAttackCooldown = attackCooldown;
        baseGoldReward = goldReward;
        currentHealth = maxHealth;

        // Tự động bù góc 180 độ cho Nhện nếu chưa đặt (vì model gốc hướng đầu về -Z)
        if (modelRotationOffset == 0f && lowerCheck.Contains("spider"))
        {
            modelRotationOffset = 180f;
        }

        // Tự động gắn chặt mắt rồng vào xương đầu (Head) để mắt không bao giờ bay lạc hướng khi tấn công
        if (isBoss)
        {
            EnsureDragonEyesAttachedToHead();
        }
    }

    private void EnsureDragonEyesAttachedToHead()
    {
        Transform head = null;
        Transform eyeArm = null;
        Transform eyesMesh = null;

        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
        foreach (var t in allTransforms)
        {
            if (t.name.Equals("Head", System.StringComparison.OrdinalIgnoreCase)) head = t;
            else if (t.name.Equals("EyeArmature", System.StringComparison.OrdinalIgnoreCase)) eyeArm = t;
            else if (t.name.Equals("Eyes", System.StringComparison.OrdinalIgnoreCase)) eyesMesh = t;
        }

        if (head != null)
        {
            if (eyeArm != null && eyeArm.parent != head)
            {
                eyeArm.SetParent(head, true);
            }
            if (eyesMesh != null)
            {
                if (eyesMesh.parent != head)
                {
                    eyesMesh.SetParent(head, true);
                }
                var smr = eyesMesh.GetComponent<SkinnedMeshRenderer>();
                if (smr != null)
                {
                    smr.updateWhenOffscreen = true;
                }
            }
        }
    }

    public float GetHeadTopHeight()
    {
        Renderer[] rends = GetComponentsInChildren<Renderer>(true);
        if (rends != null && rends.Length > 0)
        {
            float maxTop = float.MinValue;
            foreach (var r in rends)
            {
                if (r == null || r is ParticleSystemRenderer || r is LineRenderer) continue;
                string rn = r.name;
                if (rn.Contains("HealthBar") || rn.Contains("Fill") || rn.Contains("Background") || rn.Contains("HP_Text")) continue;
                if (r.bounds.max.y > maxTop) maxTop = r.bounds.max.y;
            }
            if (maxTop > float.MinValue + 5f)
            {
                float h = (maxTop - transform.position.y) + 0.55f;
                if (h > 1.2f) return h;
            }
        }

        if (isFlying || name.ToLower().Contains("dragon")) return 6.0f; // Rồng giảm 20% độ to (scale 4.0) -> thanh máu cao 6.0m
        if (name.ToLower().Contains("spider") || enemyName.ToLower().Contains("spider")) return 1.1f; // Nhện scale 0.5 -> thanh máu cao 1.1m
        return 3.2f; // AlienBug scale 3.0 -> thanh máu cao 3.2m
    }

    private float baseMaxHealth;
    private float baseMoveSpeed;
    private float baseAttackDamage;
    private float baseAttackCooldown;
    private int baseGoldReward;

    void Start()
    {
        mainCamera = Camera.main;
        healthBarHeight = GetHeadTopHeight();

        SnapToGroundImmediately();
        FindTarget();
        CreateFloatingHealthBar();
        ApplyDayNightModifiers();
    }

    /// <summary>
    /// Điều chỉnh chỉ số Quái vật theo Ngày / Đêm & Tăng dần độ khó theo từng Đêm:
    /// - Ban ngày: Tốc độ tấn công chậm hơn 40% (cooldown x1.40), sát thương giảm 40% (-40% DMG) để người chơi dễ farm vàng chuẩn bị phòng thủ.
    /// - Ban đêm: Càng sang các đêm sau (Đêm 2, Đêm 3...) quái vật càng mạnh hơn (+20% HP & DMG mỗi đêm), rơi nhiều vàng hơn!
    /// </summary>
    public void ApplyDayNightModifiers()
    {
        if (DayNightSystem.Instance == null) return;
        if (baseMaxHealth <= 0f) baseMaxHealth = maxHealth;
        if (baseMoveSpeed <= 0f) baseMoveSpeed = moveSpeed;
        if (baseAttackDamage <= 0f) baseAttackDamage = attackDamage;
        if (baseAttackCooldown <= 0f) baseAttackCooldown = attackCooldown;
        if (baseGoldReward <= 0) baseGoldReward = goldReward;

        bool isNight = DayNightSystem.Instance.isNight;
        int currentDay = DayNightSystem.Instance.currentDay;

        if (isNight)
        {
            // Ban đêm: Càng sang đêm sau quái càng mạnh hơn một chút (+20% HP & DMG mỗi đêm)
            float nightProgress = 1.0f + Mathf.Max(0, currentDay - 1) * 0.20f;

            attackDamage = baseAttackDamage * 1.35f * nightProgress;
            attackCooldown = Mathf.Max(0.55f, baseAttackCooldown * 0.85f); // Tấn công nhanh dồn dập
            maxHealth = baseMaxHealth * 1.20f * nightProgress;
            moveSpeed = baseMoveSpeed * 1.20f;
            goldReward = Mathf.CeilToInt(baseGoldReward * nightProgress);
        }
        else
        {
            // Ban ngày: Tốc độ tấn công yếu hơn / chậm hơn 40%, sát thương giảm 40% để người chơi farm vàng
            attackDamage = baseAttackDamage * 0.60f;
            attackCooldown = baseAttackCooldown * 1.40f; // Chậm hơn 40%
            maxHealth = baseMaxHealth * 0.90f;
            moveSpeed = baseMoveSpeed * 1.15f;
            goldReward = Mathf.Max(12, baseGoldReward);
        }

        currentHealth = Mathf.Min(currentHealth, maxHealth);
        UpdateHealthBar();
    }

    public void FindTarget()
    {
        // ================= CHỌN MỤC TIÊU LINH HOẠT THEO KHOẢNG CÁCH =================
        // Không fix cứng thứ tự ưu tiên, mà trong 3 loại:
        // 1. Tường (WallSegment)
        // 2. Hệ thống phòng thủ / Robot đồng minh (Turret & FriendlyCombatRobot)
        // 3. Người chơi (Player)
        // Cái nào ở gần Quái vật nhất thì sẽ tự động tấn công cái đấy!

        // 1. Quét tìm BỨC TƯỜNG (WallSegment) gần nhất
        float closestWallDist = float.MaxValue;
        Transform closestWall = null;

        // Ưu tiên dùng danh sách tĩnh AllWalls để tối ưu hiệu năng CPU
        for (int i = 0; i < WallSegment.AllWalls.Count; i++)
        {
            WallSegment w = WallSegment.AllWalls[i];
            if (w != null && w.gameObject.activeInHierarchy && !w.isDestroyed && w.currentHealth > 0f)
            {
                float d = Vector3.Distance(transform.position, w.transform.position);
                if (d < closestWallDist)
                {
                    closestWallDist = d;
                    closestWall = w.transform;
                }
            }
        }
        if (closestWall == null)
        {
            WallSegment[] allWalls = FindObjectsByType<WallSegment>();
            for (int i = 0; i < allWalls.Length; i++)
            {
                WallSegment w = allWalls[i];
                if (w != null && w.gameObject.activeInHierarchy && !w.isDestroyed && w.currentHealth > 0f)
                {
                    float d = Vector3.Distance(transform.position, w.transform.position);
                    if (d < closestWallDist)
                    {
                        closestWallDist = d;
                        closestWall = w.transform;
                    }
                }
            }
        }

        // 2. Quét tìm HỆ THỐNG PHÒNG THỦ / ROBOT ĐỒNG MINH gần nhất
        float closestDefenseDist = float.MaxValue;
        Transform closestDefense = null;

        // 2a. Trụ nâng cấp UpgradableTurret
        for (int i = 0; i < UpgradableTurret.AllTurrets.Count; i++)
        {
            UpgradableTurret t = UpgradableTurret.AllTurrets[i];
            if (t != null && t.gameObject.activeInHierarchy && !t.isDestroyed && t.currentHealth > 0f)
            {
                float d = Vector3.Distance(transform.position, t.transform.position);
                if (d < closestDefenseDist)
                {
                    closestDefenseDist = d;
                    closestDefense = t.transform;
                }
            }
        }

        // 2b. Robot chiến đấu đồng minh FriendlyCombatRobot
        FriendlyCombatRobot[] nearbyRobots = FindObjectsByType<FriendlyCombatRobot>();
        for (int i = 0; i < nearbyRobots.Length; i++)
        {
            FriendlyCombatRobot r = nearbyRobots[i];
            if (r != null && r.gameObject.activeInHierarchy && !r.isDead && r.currentHealth > 0f)
            {
                float d = Vector3.Distance(transform.position, r.transform.position);
                if (d < closestDefenseDist)
                {
                    closestDefenseDist = d;
                    closestDefense = r.transform;
                }
            }
        }

        // 2c. Trụ súng thường TurretController (nếu có)
        TurretController[] simpleTurrets = FindObjectsByType<TurretController>();
        for (int i = 0; i < simpleTurrets.Length; i++)
        {
            TurretController st = simpleTurrets[i];
            if (st != null && st.gameObject.activeInHierarchy && st.GetComponent<UpgradableTurret>() == null)
            {
                float d = Vector3.Distance(transform.position, st.transform.position);
                if (d < closestDefenseDist)
                {
                    closestDefenseDist = d;
                    closestDefense = st.transform;
                }
            }
        }

        // 3. Quét tìm NGƯỜI CHƠI (Player)
        float closestPlayerDist = float.MaxValue;
        Transform closestPlayer = null;
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null && player.gameObject.activeInHierarchy && !player.isDead && player.currentHealth > 0f)
        {
            closestPlayerDist = Vector3.Distance(transform.position, player.transform.position);
            closestPlayer = player.transform;
        }

        // 4. Quét tìm NHÀ CHÍNH / CĂN CỨ (BaseHQ)
        float baseHQDist = float.MaxValue;
        Transform baseHQTarget = null;
        if (BaseHQ.Instance != null && BaseHQ.Instance.gameObject.activeInHierarchy && !BaseHQ.Instance.isDestroyed && BaseHQ.Instance.currentHealth > 0f)
        {
            baseHQDist = Vector3.Distance(transform.position, BaseHQ.Instance.transform.position);
            baseHQTarget = BaseHQ.Instance.transform;
        }

        // ================= SO SÁNH & CHỌN MỤC TIÊU =================
        // Thứ tự ưu tiên phòng tuyến:
        // - Nếu có Tường cản đường ở gần (< 10m): Quái cắn phá tường để mở đường
        // - Nếu có Trụ súng hoặc Lính ở gần (< 8m): Quái tiêu diệt hỏa lực cản đường
        // - Nếu có Player áp sát (< 6m): Quái tấn công Player
        // - Mục tiêu tối thượng cốt lõi: Hành quân thẳng về NHÀ CHÍNH (BaseHQ)!
        float minDistance = float.MaxValue;
        Transform chosenTarget = null;

        if (closestWall != null && closestWallDist < 9.5f)
        {
            minDistance = closestWallDist;
            chosenTarget = closestWall;
        }

        if (closestDefense != null && closestDefenseDist < 8.0f && closestDefenseDist < minDistance)
        {
            minDistance = closestDefenseDist;
            chosenTarget = closestDefense;
        }

        if (closestPlayer != null && closestPlayerDist < 6.0f && closestPlayerDist < minDistance)
        {
            minDistance = closestPlayerDist;
            chosenTarget = closestPlayer;
        }

        // Nếu không có vật cản gần níu chân -> Mục tiêu số 1 là NHÀ CHÍNH!
        if (chosenTarget == null && baseHQTarget != null)
        {
            chosenTarget = baseHQTarget;
        }
        else if (chosenTarget == null)
        {
            // Dự phòng nếu chưa có Nhà Chính
            if (closestWall != null && closestWallDist < minDistance) { minDistance = closestWallDist; chosenTarget = closestWall; }
            if (closestDefense != null && closestDefenseDist < minDistance) { minDistance = closestDefenseDist; chosenTarget = closestDefense; }
            if (closestPlayer != null && closestPlayerDist < minDistance) { minDistance = closestPlayerDist; chosenTarget = closestPlayer; }
        }

        target = chosenTarget;
    }

    /// <summary>
    /// Kiểm tra an toàn xem mục tiêu (nhà chính, tường, robot, trụ súng, người chơi) còn sống và hoạt động hay không
    /// </summary>
    private bool IsTargetAlive(Transform t)
    {
        if (t == null) return false;
        if (!t.gameObject.activeInHierarchy) return false;

        var hq = t.GetComponent<BaseHQ>();
        if (hq != null) return !hq.isDestroyed && hq.currentHealth > 0f;

        var wall = t.GetComponent<WallSegment>();
        if (wall != null) return !wall.isDestroyed && wall.currentHealth > 0f;

        var robot = t.GetComponent<FriendlyCombatRobot>();
        if (robot != null) return !robot.isDead && robot.currentHealth > 0f;

        var turret = t.GetComponent<UpgradableTurret>();
        if (turret != null) return !turret.isDestroyed && turret.currentHealth > 0f;

        var player = t.GetComponent<PlayerController>();
        if (player != null) return !player.isDead && player.currentHealth > 0f;

        return true;
    }

    void Update()
    {
        if (isDead) return;

        // Kiểm tra xem mục tiêu hiện tại còn sống không
        if (target != null && !IsTargetAlive(target))
        {
            target = null;
        }

        // Định kỳ kiểm tra lại mục tiêu nếu đang di chuyển để luôn hướng về mục tiêu gần nhất
        if (target != null)
        {
            float curDist = isFlying 
                ? Vector3.Distance(transform.position, new Vector3(target.position.x, transform.position.y, target.position.z)) 
                : Vector3.Distance(transform.position, target.position);
            if (curDist > attackRange)
            {
                retargetTimer += Time.deltaTime;
                if (retargetTimer >= 0.75f)
                {
                    retargetTimer = 0f;
                    FindTarget();
                }
            }
        }

        if (target == null || !target.gameObject.activeInHierarchy)
        {
            FindTarget();
            if (target == null)
            {
                // Nếu chưa có mục tiêu, tự động bò xuôi theo trục Z hướng về phía dưới căn cứ
                Vector3 fallbackDir = Vector3.back;
                Quaternion targetRot = Quaternion.LookRotation(fallbackDir) * Quaternion.Euler(0f, modelRotationOffset, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
                transform.position += fallbackDir * (moveSpeed * Time.deltaTime);
                if (animator != null) animator.SetBool("isMoving", true);
                ApplyGroundGravity();
                return;
            }
        }

        // Tính khoảng cách tới mục tiêu (Đối với quái bay isFlying: đo theo mặt phẳng ngang XZ để không bị lỗi khoảng cách trục Y)
        Vector3 targetFlatPos = new Vector3(target.position.x, transform.position.y, target.position.z);
        float distance = isFlying ? Vector3.Distance(transform.position, targetFlatPos) : Vector3.Distance(transform.position, target.position);

        if (distance > attackRange)
        {
            // Di chuyển về phía mục tiêu
            Vector3 dir = (targetFlatPos - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                dir.Normalize();
                Quaternion targetRot = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, modelRotationOffset, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
                transform.position += dir * (moveSpeed * Time.deltaTime);
            }

            if (animator != null) animator.SetBool("isMoving", true);
        }
        else
        {
            // Đã áp sát mục tiêu -> dừng bước và xoay đầu hướng về phía mục tiêu để tấn công
            Vector3 aimDir = (target.position - transform.position);
            if (!isFlying) aimDir.y = 0f;
            if (aimDir.sqrMagnitude > 0.01f)
            {
                aimDir.Normalize();
                Quaternion targetRot = Quaternion.LookRotation(aimDir) * Quaternion.Euler(0f, modelRotationOffset, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }

            if (animator != null) animator.SetBool("isMoving", false);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                Attack();
            }
        }

        ApplyGroundGravity();
    }

    void LateUpdate()
    {
        if (healthBarObj != null)
        {
            // ================= QUY TẮC HIỂN THỊ THANH MÁU THÔNG MINH =================
            // 1. Khi bị tấn công (TakeDamage): Hiện ngay lập tức!
            // 2. Nếu không bị tấn công trong 5s: Tự động ẩn đi!
            // 3. Nếu máu dưới 50%: LUÔN LUÔN HIỆN!
            bool isLowHp = (maxHealth > 0f && (currentHealth / maxHealth) <= 0.5f);
            bool isRecentlyAttacked = (Time.time - lastDamageTime <= 5.0f);
            bool shouldShowBar = showHealthBar && (isLowHp || isRecentlyAttacked) && !isDead;

            if (healthBarObj.activeSelf != shouldShowBar)
            {
                healthBarObj.SetActive(shouldShowBar);
            }

            if (shouldShowBar)
            {
                if (mainCamera == null) mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    healthBarObj.transform.rotation = mainCamera.transform.rotation;
                    healthBarObj.transform.position = transform.position + Vector3.up * healthBarHeight;

                    // Đồng bộ kích thước hiển thị của thanh HP to rõ nét (scale 0.020f world space)
                    Vector3 pLossy = transform.lossyScale;
                    float sx = pLossy.x > 0.001f ? (1f / pLossy.x) : 1f;
                    float sy = pLossy.y > 0.001f ? (1f / pLossy.y) : 1f;
                    float sz = pLossy.z > 0.001f ? (1f / pLossy.z) : 1f;
                    healthBarObj.transform.localScale = new Vector3(sx * 0.020f, sy * 0.020f, sz * 0.020f);
                }
            }
        }
    }

    // ================= TRỌNG LỰC BẰNG CODE CHO GAME 3D (RULE 8) =================
    private float GetTrueGroundY(Vector3 pos)
    {
        // 1. ƯU TIÊN TUYỆT ĐỐI 100%: Lấy độ cao chuẩn từ Terrain của Unity
        if (Terrain.activeTerrain != null)
        {
            return Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.transform.position.y;
        }

        // 2. Dự phòng nếu không có Terrain: Raycast chỉ lấy bề mặt đảo dưới chân, loại trừ cây cối, đá, tường, quái
        if (Physics.Raycast(new Vector3(pos.x, 35f, pos.z), Vector3.down, out RaycastHit hit, 50f))
        {
            string hitName = hit.collider.name.ToLower();
            if (!hitName.Contains("bullet") && !hitName.Contains("enemy") && !hitName.Contains("wall") && !hitName.Contains("tree") && !hitName.Contains("rock"))
            {
                return hit.point.y;
            }
        }

        return 9.5f; // Độ cao mặc định mặt đất căn cứ
    }

    private void SnapToGroundImmediately()
    {
        Vector3 currentPos = transform.position;
        float groundY = GetTrueGroundY(currentPos);
        currentPos.y = isFlying ? (groundY + flightAltitude) : groundY;
        transform.position = currentPos;
    }

    private void ApplyGroundGravity()
    {
        Vector3 currentPos = transform.position;
        float groundY = GetTrueGroundY(currentPos);

        if (isFlying)
        {
            // Quái bay (Rồng): Bay lượn vừa tầm mắt người chơi ở độ cao 1.4m để dễ nhìn và dễ tấn công
            float targetY = groundY + flightAltitude + Mathf.Sin(Time.time * hoverBobSpeed) * 0.2f;
            currentPos.y = Mathf.MoveTowards(currentPos.y, targetY, 8f * Time.deltaTime);
        }
        else
        {
            // Quái bò đất: Khóa chặt vào mặt đất, TUYỆT ĐỐI KHÔNG THỂ BAY LÊN TRỜI
            currentPos.y = groundY;
        }

        transform.position = currentPos;
    }

    void Attack()
    {
        if (animator != null)
        {
            animator.SetTrigger("attack");
        }

        if (target != null)
        {
            StartCoroutine(ExecuteAttackWithDelay(target));
        }
    }

    private IEnumerator ExecuteAttackWithDelay(Transform currentTarget)
    {
        if (attackDelay > 0f)
        {
            yield return new WaitForSeconds(attackDelay);
        }

        if (isDead) yield break;

        if (currentTarget != null && currentTarget.gameObject.activeInHierarchy)
        {
            currentTarget.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);

            if (isFlying)
            {
                StartCoroutine(SpawnFlyingAttackVFX(currentTarget.position));
            }
        }
    }

    private IEnumerator SpawnFlyingAttackVFX(Vector3 targetPos)
    {
        Vector3 spawnPos;
        if (shootPoint != null)
        {
            spawnPos = shootPoint.position;
        }
        else
        {
            Transform head = null;
            Transform[] allT = GetComponentsInChildren<Transform>(true);
            foreach (var t in allT)
            {
                if (t.name.Equals("Head", System.StringComparison.OrdinalIgnoreCase))
                {
                    head = t;
                    break;
                }
            }
            spawnPos = head != null ? head.position + head.forward * 0.8f : (transform.position + transform.forward * 1.5f + Vector3.down * 0.3f);
        }

        GameObject projectile;
        if (projectilePrefab != null)
        {
            projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "[Dragon_PlasmaBall]";
            projectile.transform.position = spawnPos;
            projectile.transform.localScale = Vector3.one * 0.65f;

            Collider col = projectile.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer r = projectile.GetComponent<Renderer>();
            if (r != null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                Material mat = new Material(sh);
                mat.color = projectileColor;
                r.material = mat;
            }
        }

        Vector3 startPos = projectile.transform.position;
        Vector3 endPos = targetPos + Vector3.up * 0.5f;
        float elapsed = 0f;
        float duration = 0.35f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (projectile != null)
            {
                projectile.transform.position = Vector3.Lerp(startPos, endPos, t);
            }
            yield return null;
        }

        if (projectile != null)
        {
            Destroy(projectile);
        }
    }

    private bool HasAnimatorParameter(string paramName)
    {
        if (animator == null) return false;
        foreach (var p in animator.parameters)
        {
            if (p.name == paramName) return true;
        }
        return false;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        lastDamageTime = Time.time; // Cập nhật thời điểm bị tấn công để hiện thanh máu trong 5s

        var boss = GetComponent<MechaDragonBoss>();
        if (boss != null && boss.isShieldActive)
        {
            amount *= 0.5f;
            FloatingDamageText.Spawn(transform.position + Vector3.up * 2f, 0, Color.cyan);
        }

        currentHealth -= amount;
        currentHealth = Mathf.Max(0f, currentHealth);
        UpdateHealthBar();

        // Hiển thị số nảy sát thương trực quan (Floating Damage Popup)
        Vector3 hitPos = transform.position + Vector3.up * (healthBarHeight * 0.8f);
        Color dmgColor = isFlying ? new Color(1f, 0.45f, 0.1f) : new Color(1f, 0.9f, 0.15f);
        FloatingDamageText.Spawn(hitPos, amount, dmgColor);

        Debug.Log($"<color=orange>[Enemy]</color> {enemyName} bị bắn trúng! -{amount:F0} DMG | HP còn: {currentHealth:F0}/{maxHealth}");

        if (animator != null && currentHealth > 0f && HasAnimatorParameter("hit"))
        {
            animator.SetTrigger("hit");
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        AllEnemies.Remove(this);

        if (healthBarObj != null)
        {
            Destroy(healthBarObj);
        }

        Debug.Log($"<color=red>[Enemy]</color> {name} đã bị tiêu diệt! +{goldReward}🪙 Vàng");

        // Rơi vàng ra mặt đất thực tế để người chơi đi nhặt gom lại
        if (goldReward > 0)
        {
            DroppedGoldCoin.SpawnCoins(transform.position, goldReward);
        }

        // Kích nổ hiệu ứng chết
        if (deathVFX != null)
        {
            Instantiate(deathVFX, transform.position, Quaternion.identity);
        }

        if (animator != null && HasAnimatorParameter("die"))
        {
            animator.SetTrigger("die");
        }

        if (myCollider != null)
        {
            myCollider.enabled = false;
        }

        Destroy(gameObject, 0.9f);
    }

    // ================= THANH MÁU NỔI CHO QUÁI VẬT & BOSS =================
    private void CreateFloatingHealthBar()
    {
        if (!showHealthBar) return;

        // Xóa bất kỳ thanh máu cũ nào còn sót lại để đảm bảo luôn chỉ có DUY NHẤT 1 thanh máu
        Transform existing = transform.Find("[Enemy_HealthBar]");
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }
        if (healthBarObj != null)
        {
            Destroy(healthBarObj);
            healthBarObj = null;
        }

        healthBarHeight = GetHeadTopHeight();
        healthBarObj = new GameObject("[Enemy_HealthBar]");
        healthBarObj.transform.SetParent(transform, false);
        healthBarObj.transform.position = transform.position + Vector3.up * healthBarHeight;

        Canvas canvas = healthBarObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        CanvasScaler cs = healthBarObj.AddComponent<CanvasScaler>();
        cs.dynamicPixelsPerUnit = 10;

        RectTransform rt = healthBarObj.GetComponent<RectTransform>();
        bool isBoss = isFlying || maxHealth >= 500f;
        float barWidth = isBoss ? 280f : 180f;
        float barHeight = isBoss ? 32f : 24f;
        rt.sizeDelta = new Vector2(barWidth, barHeight);

        // Khử scale của model cha để thanh máu có kích thước to rõ nét (scale 0.020f)
        Vector3 parentLossy = transform.lossyScale;
        float sx = parentLossy.x > 0.001f ? (1f / parentLossy.x) : 1f;
        float sy = parentLossy.y > 0.001f ? (1f / parentLossy.y) : 1f;
        float sz = parentLossy.z > 0.001f ? (1f / parentLossy.z) : 1f;
        rt.localScale = new Vector3(sx * 0.020f, sy * 0.020f, sz * 0.020f);

        // Khung nền đen đậm viền sắc nét
        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(healthBarObj.transform, false);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        Image bgImg = bg.GetComponent<Image>();
        bgImg.color = isBoss ? new Color(0.08f, 0.02f, 0.03f, 0.95f) : new Color(0.05f, 0.05f, 0.07f, 0.92f);
        
        var bgOutline = bg.AddComponent<Outline>();
        bgOutline.effectColor = isBoss ? new Color(1f, 0.3f, 0.1f, 0.8f) : new Color(0.1f, 0.4f, 0.7f, 0.8f);
        bgOutline.effectDistance = new Vector2(1f, -1f);

        // Thanh fill máu sinh động
        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(healthBarObj.transform, false);
        healthBarFill = fill.GetComponent<RectTransform>();
        healthBarFill.anchorMin = new Vector2(0.01f, 0.12f);
        healthBarFill.anchorMax = new Vector2(0.99f, 0.88f);
        healthBarFill.sizeDelta = Vector2.zero;
        healthBarFill.pivot = new Vector2(0f, 0.5f);
        healthBarFill.anchoredPosition = Vector2.zero;
        healthBarFillImage = fill.GetComponent<Image>();
        healthBarFillImage.color = isBoss ? new Color(1f, 0.18f, 0.25f, 1f) : new Color(0.2f, 0.95f, 0.45f, 1f);

        // Text hiển thị số HP to, rõ nét, có viền đen tương phản
        GameObject textObj = new GameObject("HP_Text", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(healthBarObj.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
        healthBarText = textObj.GetComponent<Text>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        healthBarText.font = font;
        healthBarText.fontSize = isBoss ? 17 : 14;
        healthBarText.fontStyle = FontStyle.Bold;
        healthBarText.alignment = TextAnchor.MiddleCenter;
        healthBarText.color = Color.white;
        
        var txtOutline = textObj.AddComponent<Outline>();
        txtOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        txtOutline.effectDistance = new Vector2(1.2f, -1.2f);

        UpdateHealthBar();

        // Mặc định ban đầu (chưa bị tấn công và HP >= 50%): ẩn thanh máu
        healthBarObj.SetActive(false);
    }

    public void UpdateHealthBar()
    {
        if (healthBarFill == null) return;

        float pct = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        healthBarFill.localScale = new Vector3(pct, 1f, 1f);

        if (healthBarFillImage != null)
        {
            bool isBoss = isFlying || maxHealth >= 500f;
            if (isBoss)
            {
                healthBarFillImage.color = Color.Lerp(new Color(0.85f, 0.1f, 0.1f), new Color(1f, 0.35f, 0.45f), pct);
            }
            else
            {
                if (pct > 0.5f)
                    healthBarFillImage.color = Color.Lerp(new Color(1f, 0.85f, 0.15f), new Color(0.2f, 0.95f, 0.45f), (pct - 0.5f) * 2f);
                else
                    healthBarFillImage.color = Color.Lerp(new Color(0.95f, 0.15f, 0.15f), new Color(1f, 0.85f, 0.15f), pct * 2f);
            }
        }

        if (healthBarText != null)
        {
            bool isBoss = isFlying || maxHealth >= 500f;
            if (isBoss)
                healthBarText.text = $"<b>👑 BOSS: {Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}</b>";
            else
                healthBarText.text = $"<b>{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}</b>";
        }
    }
}

