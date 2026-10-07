using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Đại diện cho Robot chiến đấu phe mình (Quân đội đồng minh bảo vệ căn cứ).
/// 4 Chủng loại Robot đặc trưng:
/// - Stan: Pháo Nhện 4 chân (Bắn pháo tầm trung & Dậm chân đẩy lùi)
/// - Mike: Đấu sĩ hộ pháp (Tanker cận chiến, Cú đấm móc & Nhát chém diện rộng)
/// - George: Sát thủ chân kiếm (Cực cơ động, Chém song kiếm lướt & Cú đá xoay)
/// - Leela: Xạ thủ bắn tỉa (Tầm bắn cực xa, Bắn laser & Cú đá tự vệ cận chiến)
/// Tuân thủ quy tắc dự án:
/// - Trọng lực 100% bằng code (Rule 8), KHÔNG dùng Rigidbody.
/// - Tối ưu Snapdragon 810: Zero GC per frame, SRP Batcher friendly, chia sẻ Shader.
/// </summary>
public class FriendlyCombatRobot : MonoBehaviour
{
    public enum RobotClass
    {
        Stan_Artillery,  // Ụ nhện 4 chân - Hỏa lực tầm xa & Dẫm đạp
        Mike_Brawler,    // Đấu sĩ hộ pháp - Cú đấm móc & Chém càn quét
        George_Assassin, // Sát thủ chân kiếm - Chém lướt tốc độ cao & Đá liên hoàn
        Leela_Sniper     // Xạ thủ trinh sát - Bắn tỉa tầm xa & Đá tự vệ
    }

    [Header("--- Định Danh & Chủng Loại ---")]
    public RobotClass robotClass = RobotClass.Stan_Artillery;
    public string robotName = "Stan Pháo Nhện";

    [Header("--- Chỉ Số Chiến Đấu ---")]
    public float maxHealth = 200f;
    public float currentHealth;
    public float moveSpeed = 3.5f;
    public float attackRange = 10f;
    public float attackDamage = 30f;
    public float attackCooldown = 1.4f;

    [Header("--- Hiệu Chỉnh Góc Model ---")]
    public float modelRotationOffset = 0f;

    [Header("--- Trọng Lực Bằng Code (Rule 8) ---")]
    public float gravitySnapSpeed = 20f;
    public float groundOffset = 0f;

    [Header("--- Phạm Vi Tuần Tra / Bảo Vệ ---")]
    public Transform guardOriginPoint;
    public float patrolRadius = 25f;

    [Header("--- Hiệu Ứng & Animation ---")]
    public Animator animator;
    public GameObject attackVFX;
    public GameObject deathVFX;

    // Biến trạng thái
    private Transform currentTarget;
    private float lastAttackTime = 0f;
    private bool isDead = false;
    private Vector3 guardPos;
    private Vector3 patrolDestination;
    private float nextPatrolTime = 0f;
    private bool isCelebrating = false;

    // Thanh máu mini World-Space
    private Transform hpBarForeground;
    private Transform hpBarRoot;
    private Camera mainCam;

    void Awake()
    {
        currentHealth = maxHealth;
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Loại bỏ Rigidbody nếu có (Quy tắc Rule 8: Game 3D tính trọng lực bằng code)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        // Khởi tạo vị trí bảo vệ
        guardPos = transform.position;
        patrolDestination = guardPos;

        // Tạo thanh máu 3D đơn giản nhẹ cho Snapdragon 810
        CreateWorldSpaceHealthBar();
    }

    void Start()
    {
        mainCam = Camera.main;
        SnapToGroundImmediately();
        if (guardOriginPoint != null) guardPos = guardOriginPoint.position;
    }

    public void SetupRobotClass(RobotClass rClass)
    {
        robotClass = rClass;
        switch (robotClass)
        {
            case RobotClass.Stan_Artillery:
                robotName = "💥 Stan Pháo Nhện";
                maxHealth = 220f;
                moveSpeed = 3.4f;
                attackRange = 12f;
                attackDamage = 32f;
                attackCooldown = 1.5f;
                break;

            case RobotClass.Mike_Brawler:
                robotName = "🛡️ Mike Đấu Sĩ";
                maxHealth = 320f; // Tanker trâu máu
                moveSpeed = 3.6f;
                attackRange = 2.6f;
                attackDamage = 45f;
                attackCooldown = 1.2f;
                break;

            case RobotClass.George_Assassin:
                robotName = "⚡ George Sát Thủ";
                maxHealth = 160f;
                moveSpeed = 5.4f; // Chạy nhanh
                attackRange = 3.0f;
                attackDamage = 35f;
                attackCooldown = 0.85f; // Chém liên hoàn
                break;

            case RobotClass.Leela_Sniper:
                robotName = "🎯 Leela Bắn Tỉa";
                maxHealth = 140f;
                moveSpeed = 4.0f;
                attackRange = 18f; // Tầm bắn siêu xa
                attackDamage = 50f;
                attackCooldown = 1.8f;
                break;
        }
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (isDead) return;

        // 1. Áp dụng trọng lực code bám sát địa hình (Rule 8)
        ApplyGroundGravity();

        // 2. Cập nhật góc nhìn thanh máu về phía Camera
        UpdateHealthBarVisual();

        // 3. Tìm mục tiêu quái vật gần nhất
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy || IsTargetDead(currentTarget))
        {
            FindClosestEnemy();
        }

        // 4. Xử lý hành vi chiến đấu hoặc tuần tra
        if (currentTarget != null)
        {
            if (isCelebrating)
            {
                isCelebrating = false;
            }
            CombatTarget(currentTarget);
        }
        else
        {
            CheckVictoryOrPatrol();
        }
    }

    private bool IsTargetDead(Transform enemyT)
    {
        if (enemyT == null) return true;
        Enemy enemyComp = enemyT.GetComponent<Enemy>();
        if (enemyComp != null && enemyComp.currentHealth <= 0f) return true;
        return false;
    }

    private bool IsValidEnemy(GameObject obj)
    {
        if (obj == null || !obj.activeInHierarchy) return false;

        // TUYỆT ĐỐI LOẠI TRỪ 100% NGƯỜI CHƠI (PLAYER)
        if (obj.CompareTag("Player") || obj.transform.root.CompareTag("Player")) return false;
        if (obj.GetComponentInParent<PlayerController>() != null || obj.transform.root.GetComponentInChildren<PlayerController>() != null) return false;
        string n = obj.name.ToLower();
        string rn = obj.transform.root.name.ToLower();
        if (n.Contains("player") || rn.Contains("player") || n.Contains("cuterobot") || rn.Contains("friendly")) return false;

        // Bỏ qua công trình và đồng đội phe mình
        if (obj.GetComponentInParent<FriendlyCombatRobot>() != null) return false;
        if (obj.GetComponentInParent<RobotFactory>() != null) return false;
        if (obj.GetComponentInParent<WorkerBot>() != null) return false;

        // Kiểm tra component Enemy
        Enemy enemyComp = obj.GetComponent<Enemy>() ?? obj.GetComponentInParent<Enemy>();
        if (enemyComp != null)
        {
            return enemyComp.currentHealth > 0f;
        }

        if (obj.CompareTag("Enemy")) return true;

        return false;
    }

    private void FindClosestEnemy()
    {
        float closestDist = float.MaxValue;
        Transform bestEnemy = null;
        Vector3 myPos = transform.position;

        // Quét 1: Quét toàn bộ component Enemy trong Scene
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < allEnemies.Length; i++)
        {
            if (allEnemies[i] == null || !allEnemies[i].gameObject.activeInHierarchy) continue;
            if (allEnemies[i].currentHealth <= 0f) continue;
            if (!IsValidEnemy(allEnemies[i].gameObject)) continue;

            float dist = Vector3.Distance(myPos, allEnemies[i].transform.position);
            if (dist < closestDist && dist <= 40f)
            {
                closestDist = dist;
                bestEnemy = allEnemies[i].transform;
            }
        }

        // Quét 2: Bổ sung các vật thể có Tag "Enemy" (nếu có)
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject e = enemies[i];
            if (e == null || !e.activeInHierarchy) continue;
            if (!IsValidEnemy(e)) continue;

            float dist = Vector3.Distance(myPos, e.transform.position);
            if (dist < closestDist && dist <= 40f)
            {
                closestDist = dist;
                bestEnemy = e.transform;
            }
        }

        currentTarget = bestEnemy;
    }

    private void CombatTarget(Transform target)
    {
        Vector3 targetPos = target.position;
        Vector3 flatTarget = new Vector3(targetPos.x, transform.position.y, targetPos.z);
        float distance = Vector3.Distance(transform.position, flatTarget);

        if (distance > attackRange)
        {
            // Di chuyển tiếp cận quái vật
            Vector3 dir = (flatTarget - transform.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, modelRotationOffset, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);

            // Tách các đồng đội ra tránh chồng lấn (Soft separation)
            Vector3 separation = CalculateSeparation();
            Vector3 finalMoveDir = (dir + separation).normalized;

            transform.position += finalMoveDir * (moveSpeed * Time.deltaTime);

            if (animator != null) animator.SetBool("isMoving", true);
        }
        else
        {
            // Đã vào tầm bắn / tầm chém -> Dừng bước và xoay mặt tấn công
            Vector3 aimDir = (flatTarget - transform.position).normalized;
            if (aimDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(aimDir) * Quaternion.Euler(0f, modelRotationOffset, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 12f);
            }

            if (animator != null) animator.SetBool("isMoving", false);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                ExecuteAttack(target);
            }
        }
    }

    private Vector3 CalculateSeparation()
    {
        Vector3 sep = Vector3.zero;
        FriendlyCombatRobot[] allBots = FindObjectsByType<FriendlyCombatRobot>(FindObjectsSortMode.None);
        for (int i = 0; i < allBots.Length; i++)
        {
            if (allBots[i] == this) continue;
            Vector3 diff = transform.position - allBots[i].transform.position;
            float sqrDist = diff.sqrMagnitude;
            if (sqrDist < 2.5f && sqrDist > 0.01f)
            {
                sep += diff.normalized * (1.5f / Mathf.Sqrt(sqrDist));
            }
        }
        sep.y = 0f;
        return sep;
    }

    private void ExecuteAttack(Transform target)
    {
        // Chọn loại đòn đánh phù hợp theo từng chủng robot:
        // 0 = Shoot, 1 = Punch, 2 = Slash, 3 = Kick
        int attackTypeIndex = 0;

        switch (robotClass)
        {
            case RobotClass.Stan_Artillery:
                attackTypeIndex = (Random.value > 0.3f) ? 0 : 3; // 70% Bắn đạn pháo, 30% dẫm đạp
                break;
            case RobotClass.Mike_Brawler:
                attackTypeIndex = (Random.value > 0.5f) ? 1 : 2;  // Cú đấm móc hoặc vung kiếm quét
                break;
            case RobotClass.George_Assassin:
                attackTypeIndex = (Random.value > 0.4f) ? 2 : 3;  // Kiếm chém lướt hoặc song phi quét chân
                break;
            case RobotClass.Leela_Sniper:
                attackTypeIndex = (Random.value > 0.2f) ? 0 : 3; // 80% Bắn tỉa chuẩn xác, 20% đá tự vệ
                break;
        }

        if (animator != null)
        {
            animator.SetInteger("attackType", attackTypeIndex);
            animator.SetTrigger("attack");
        }

        // Tùy theo loại đòn đánh: bắn đạn bay hoặc chém cận chiến
        if (attackTypeIndex == 0)
        {
            StartCoroutine(SpawnRangedProjectile(target, 0.25f));
        }
        else
        {
            StartCoroutine(DealDamageWithDelay(target, 0.35f, attackTypeIndex));
        }
    }

    private IEnumerator SpawnRangedProjectile(Transform target, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (isDead || target == null) yield break;

        Vector3 startPos = transform.position + Vector3.up * 1.4f + transform.forward * 0.8f;
        Vector3 endPos = target.position + Vector3.up * 1.0f;

        // Tạo quả cầu đạn laser / plasma nhẹ (URP unlit)
        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "[Robot_Bullet]";
        bullet.transform.position = startPos;
        bullet.transform.localScale = (robotClass == RobotClass.Stan_Artillery) ? Vector3.one * 0.45f : Vector3.one * 0.3f;

        Collider col = bullet.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer r = bullet.GetComponent<Renderer>();
        if (r != null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            Material mat = new Material(sh);
            mat.color = (robotClass == RobotClass.Stan_Artillery) ? new Color(1f, 0.45f, 0.1f) : new Color(0.1f, 0.9f, 1f);
            r.material = mat;
        }

        float travelTime = 0.2f;
        float elapsed = 0f;
        while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / travelTime;
            if (target != null) endPos = target.position + Vector3.up * 1.0f;
            if (bullet != null) bullet.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        if (bullet != null) Destroy(bullet);

        // Gây sát thương khi trúng đích
        if (target != null)
        {
            Enemy enemy = target.GetComponent<Enemy>();
            if (enemy != null && enemy.currentHealth > 0f)
            {
                enemy.TakeDamage(attackDamage);
            }
        }
    }

    private IEnumerator DealDamageWithDelay(Transform target, float delay, int atkType)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (isDead || target == null) yield break;

        Enemy enemy = target.GetComponent<Enemy>();
        if (enemy != null && enemy.currentHealth > 0f)
        {
            enemy.TakeDamage(attackDamage);

            // Tia lửa va chạm cận chiến
            CreateMeleeHitSpark(target.position + Vector3.up * 1.0f);
        }
    }

    private void CreateMeleeHitSpark(Vector3 pos)
    {
        // Tạo flash spark nhẹ không lag
        GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spark.name = "[Hit_Spark]";
        spark.transform.position = pos;
        spark.transform.localScale = Vector3.one * 0.4f;
        Collider col = spark.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer r = spark.GetComponent<Renderer>();
        if (r != null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            Material mat = new Material(sh);
            mat.color = new Color(1f, 0.9f, 0.2f);
            r.material = mat;
        }
        Destroy(spark, 0.08f);
    }

    private void CheckVictoryOrPatrol()
    {
        // Nếu không còn quái nào trên chiến trường -> Ăn mừng chiến thắng
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        bool hasActiveEnemy = false;
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && enemies[i].activeInHierarchy)
            {
                Enemy e = enemies[i].GetComponent<Enemy>();
                if (e != null && e.currentHealth > 0f)
                {
                    hasActiveEnemy = true;
                    break;
                }
            }
        }

        if (!hasActiveEnemy)
        {
            if (!isCelebrating && animator != null)
            {
                isCelebrating = true;
                animator.SetBool("isMoving", false);
                animator.SetTrigger("victory");
            }
        }
        else
        {
            isCelebrating = false;
            PatrolGuardArea();
        }
    }

    private void PatrolGuardArea()
    {
        float distToDest = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z),
                                            new Vector3(patrolDestination.x, 0f, patrolDestination.z));

        if (distToDest <= 1.5f || Time.time >= nextPatrolTime)
        {
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            patrolDestination = guardPos + new Vector3(randomCircle.x, 0f, randomCircle.y);
            nextPatrolTime = Time.time + Random.Range(3.5f, 7f);

            if (animator != null) animator.SetBool("isMoving", false);
        }
        else
        {
            Vector3 dir = (patrolDestination - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                dir.Normalize();
                Quaternion targetRot = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, modelRotationOffset, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 6f);
                transform.position += dir * ((moveSpeed * 0.65f) * Time.deltaTime);
                if (animator != null) animator.SetBool("isMoving", true);
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        UpdateHealthBarVisual();

        if (animator != null && currentHealth > 0f)
        {
            animator.SetTrigger("hit");
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (hpBarRoot != null) hpBarRoot.gameObject.SetActive(false);

        if (animator != null)
        {
            animator.SetBool("isMoving", false);
            animator.SetTrigger("die");
        }

        if (deathVFX != null)
        {
            Instantiate(deathVFX, transform.position + Vector3.up * 1f, Quaternion.identity);
        }

        Destroy(gameObject, 3.5f);
    }

    private void ApplyGroundGravity()
    {
        Vector3 pos = transform.position;
        float groundY = pos.y;
        bool foundGround = false;

        if (Terrain.activeTerrain != null)
        {
            groundY = Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.transform.position.y + groundOffset;
            foundGround = true;
        }

        if (foundGround)
        {
            pos.y = Mathf.MoveTowards(pos.y, groundY, gravitySnapSpeed * Time.deltaTime);
            transform.position = pos;
        }
    }

    private void SnapToGroundImmediately()
    {
        if (Terrain.activeTerrain != null)
        {
            Vector3 pos = transform.position;
            pos.y = Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.transform.position.y + groundOffset;
            transform.position = pos;
        }
    }

    // ================= THANH MÁU WORLD-SPACE MINI =================
    private void CreateWorldSpaceHealthBar()
    {
        GameObject barRoot = new GameObject("HealthBar_Root");
        barRoot.transform.SetParent(transform);
        barRoot.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        hpBarRoot = barRoot.transform;

        // Background xám đen
        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.name = "BG";
        bg.transform.SetParent(hpBarRoot);
        bg.transform.localPosition = Vector3.zero;
        bg.transform.localScale = new Vector3(1.0f, 0.12f, 1f);
        Collider colBg = bg.GetComponent<Collider>();
        if (colBg != null) Destroy(colBg);

        Renderer rBg = bg.GetComponent<Renderer>();
        if (rBg != null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            Material mBg = new Material(sh);
            mBg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
            rBg.material = mBg;
        }

        // Foreground xanh lục
        GameObject fg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fg.name = "FG";
        fg.transform.SetParent(hpBarRoot);
        fg.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        fg.transform.localScale = new Vector3(0.96f, 0.08f, 1f);
        Collider colFg = fg.GetComponent<Collider>();
        if (colFg != null) Destroy(colFg);

        Renderer rFg = fg.GetComponent<Renderer>();
        if (rFg != null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            Material mFg = new Material(sh);
            mFg.color = new Color(0f, 1f, 0.4f);
            rFg.material = mFg;
        }

        hpBarForeground = fg.transform;
    }

    private void UpdateHealthBarVisual()
    {
        if (hpBarRoot == null) return;

        // Xoay mặt thanh máu hướng về Camera
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam != null)
        {
            hpBarRoot.rotation = Quaternion.LookRotation(hpBarRoot.position - mainCam.transform.position);
        }

        // Cập nhật tỉ lệ độ dài thanh máu
        if (hpBarForeground != null)
        {
            float hpRatio = Mathf.Clamp01(currentHealth / maxHealth);
            hpBarForeground.localScale = new Vector3(0.96f * hpRatio, 0.08f, 1f);
            hpBarForeground.localPosition = new Vector3(-0.48f * (1f - hpRatio), 0f, -0.01f);

            // Đổi màu vàng / đỏ khi máu yếu
            Renderer r = hpBarForeground.GetComponent<Renderer>();
            if (r != null && r.material != null)
            {
                if (hpRatio > 0.5f) r.material.color = Color.Lerp(Color.yellow, new Color(0f, 1f, 0.4f), (hpRatio - 0.5f) * 2f);
                else r.material.color = Color.Lerp(Color.red, Color.yellow, hpRatio * 2f);
            }
        }
    }
}
