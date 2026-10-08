using UnityEngine;

/// <summary>
/// mineBotFarm - Robot Đào Mỏ Tự Động Hóa Dùng Hoàn Toàn Bằng Raycast & Trọng Lực Bằng Code.
/// - Không sử dụng Rigidbody (tiết kiệm CPU tối đa).
/// - Không sử dụng Collider trên robot (sử dụng Raycast để dò mặt đất, né vật cản và nhận diện tảng đá).
/// - Trọng lực tính bằng code với gia tốc -9.18 m/s^2.
/// - Kế thừa WorkerBot để tích hợp 100% với hệ thống kinh tế GameEconomy và chống đạn thân thiện.
/// </summary>
public class mineBotFarm : WorkerBot
{
    [Header("=== CÀI ĐẶT DI CHUYỂN ===")]
    [Tooltip("Tốc độ di chuyển")]
    public float moveSpeed = 3.0f;

    [Tooltip("Tốc độ xoay hướng (độ/giây)")]
    public float rotateSpeed = 540.0f;

    [Tooltip("Khoảng cách dừng trước tảng đá để bắt đầu đào (tiếp cận sát vỏ đá)")]
    public float stopDistance = 0.85f;

    [Header("=== TÌM MỤC TIÊU THEO TAG HOẶC TÊN ===")]
    [Tooltip("Tag của vật thể tài nguyên (mặc định 'Rock')")]
    public string targetTag = "Rock";

    [Header("=== TRỌNG LỰC CODE (KHÔNG DÙNG RIGIDBODY) ===")]
    [Tooltip("Gia tốc trọng lực mô phỏng bằng code, mặc định -9.18")]
    public float gravity = -9.18f;

    [Tooltip("Vận tốc rơi hiện tại (m/s)")]
    private float verticalVelocity = 0f;

    [Header("=== RAYCAST NÉ VẬT CẢN (OBSTACLE AVOIDANCE) ===")]
    [Tooltip("Khoảng cách tia Raycast phát hiện vật cản")]
    public float obstacleDetectDistance = 2.2f;

    [Tooltip("Độ cao tia Raycast tính từ chân robot")]
    public float raycastHeightOffset = 1.2f;

    [Tooltip("Góc quét tia Raycast né vật cản 2 bên")]
    public float whiskerAngle = 35.0f;

    [Tooltip("Lực né tránh khi gặp chướng ngại vật")]
    public float avoidanceMultiplier = 1.8f;

    [Header("=== HIỂN THỊ & ANIMATION ===")]
    private Animator anim;
    private bool isMining = false;
    private GameObject overheadBadgeObj;
    private UnityEngine.UI.Text overheadText;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.applyRootMotion = false; // Tắt root motion để code kiểm soát tọa độ chính xác
        }
    }

    private void Start()
    {
        stoneMinedCount = 0;
        isMining = false;

        // Tự động tìm mục tiêu nếu chưa được gán trước
        if (targetRock == null)
        {
            FindBestMiningTarget();
        }
    }

    private void Update()
    {
        // 1. Nếu chưa có mục tiêu hoặc mục tiêu bị hủy, tự động tìm kiếm lại
        if (targetRock == null)
        {
            FindBestMiningTarget();
            if (targetRock == null)
            {
                if (anim != null) anim.SetBool("isMoving", false);
                ApplyGravityAndGroundClamp();
                return;
            }
        }

        // 2. Tính toán khoảng cách và hướng tới tảng đá mục tiêu
        Vector3 myPos = transform.position;
        Vector3 targetPos = targetRock.position;
        Vector3 toTarget = targetPos - myPos;
        toTarget.y = 0f; // Chỉ xét trên mặt phẳng ngang
        float distToTarget = toTarget.magnitude;

        // 3. Sử dụng Raycast phía trước để kiểm tra xem đã chạm vào vỏ ngoài tảng đá chưa
        bool reachedRockByRaycast = CheckIfFacingRock(toTarget.normalized);

        if (distToTarget <= stopDistance || reachedRockByRaycast)
        {
            // === ĐÃ ĐẾN VỊ TRÍ KHAI THÁC ===
            ExecuteMiningProcess(toTarget);
        }
        else
        {
            // === DI CHUYỂN TỚI MỤC TIÊU & NÉ VẬT CẢN BẰNG RAYCAST ===
            ExecuteMovementWithAvoidance(toTarget.normalized);
        }

        // 4. Áp dụng trọng lực code -9.18 và khóa cao độ mặt đất
        ApplyGravityAndGroundClamp();

        // 5. Cập nhật bảng đếm số đá hiển thị trực tiếp trên đầu Robot
        UpdateOverheadBadge();
    }

    /// <summary>
    /// Di chuyển về phía mục tiêu và sử dụng Raycast hình cánh quạt để né chướng ngại vật
    /// </summary>
    private void ExecuteMovementWithAvoidance(Vector3 dirToTarget)
    {
        isMining = false;
        Vector3 rayOrigin = transform.position + Vector3.up * raycastHeightOffset;
        Vector3 avoidanceVector = Vector3.zero;

        // 3 tia Raycast quét chướng ngại vật: Chính giữa, Lệch Trái, Lệch Phải
        Vector3 leftRayDir = Quaternion.Euler(0, -whiskerAngle, 0) * transform.forward;
        Vector3 rightRayDir = Quaternion.Euler(0, whiskerAngle, 0) * transform.forward;
        Vector3 forwardRayDir = transform.forward;

        // Quét tia giữa
        if (Physics.Raycast(rayOrigin, forwardRayDir, out RaycastHit centerHit, obstacleDetectDistance))
        {
            if (!IsTargetRock(centerHit.transform))
            {
                // Thêm lực đẩy lệch sang hướng pháp tuyến của vật cản
                avoidanceVector += centerHit.normal * (1f - centerHit.distance / obstacleDetectDistance);
            }
        }

        // Quét tia bên trái
        if (Physics.Raycast(rayOrigin, leftRayDir, out RaycastHit leftHit, obstacleDetectDistance * 0.85f))
        {
            if (!IsTargetRock(leftHit.transform))
            {
                avoidanceVector += transform.right * (1f - leftHit.distance / obstacleDetectDistance);
            }
        }

        // Quét tia bên phải
        if (Physics.Raycast(rayOrigin, rightRayDir, out RaycastHit rightHit, obstacleDetectDistance * 0.85f))
        {
            if (!IsTargetRock(rightHit.transform))
            {
                avoidanceVector -= transform.right * (1f - rightHit.distance / obstacleDetectDistance);
            }
        }

        // Tổng hợp hướng di chuyển cuối cùng: Hướng mục tiêu + Lực bẻ lái né vật cản
        Vector3 finalMoveDir = (dirToTarget + avoidanceVector * avoidanceMultiplier).normalized;
        finalMoveDir.y = 0f;

        if (finalMoveDir.sqrMagnitude > 0.001f)
        {
            // Dịch chuyển vị trí bằng Code
            transform.position += finalMoveDir * moveSpeed * Time.deltaTime;

            // Xoay mặt mượt mà về hướng di chuyển
            Quaternion targetRotation = Quaternion.LookRotation(finalMoveDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);

            if (anim != null) anim.SetBool("isMoving", true);
        }
    }

    /// <summary>
    /// Kiểm tra xem tia Raycast phía trước có chạm thẳng vào tảng đá mục tiêu không
    /// </summary>
    private bool CheckIfFacingRock(Vector3 dirToTarget)
    {
        Vector3 rayOrigin = transform.position + Vector3.up * raycastHeightOffset;
        if (Physics.Raycast(rayOrigin, transform.forward, out RaycastHit hit, stopDistance))
        {
            if (IsTargetRock(hit.transform)) return true;
        }
        return false;
    }

    private bool IsTargetRock(Transform hitTrans)
    {
        if (hitTrans == null) return false;
        if (targetRock != null && (hitTrans == targetRock || hitTrans.IsChildOf(targetRock))) return true;
        if (!string.IsNullOrEmpty(targetTag) && hitTrans.CompareTag(targetTag)) return true;
        return false;
    }

    /// <summary>
    /// Xử lý quá trình đào đá khi đã tiếp cận tảng đá mục tiêu.
    /// Kích hoạt Animation Attack và để 100% Animation Event OnMiningHit điều khiển!
    /// </summary>
    private void ExecuteMiningProcess(Vector3 toTarget)
    {
        isMining = true;

        // Luôn xoay mặt nhìn thẳng vào tảng đá khi đứng đào
        if (toTarget.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }

        // Dừng di chuyển và kích hoạt animation Attack (để lặp vô tận theo Clip)
        if (anim != null)
        {
            anim.SetBool("isMoving", false);
            var state = anim.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName("Attack") && !anim.IsInTransition(0))
            {
                anim.SetTrigger("mine");
            }
        }
    }

    /// <summary>
    /// HÀM GỌI TRỰC TIẾP TỪ ANIMATION EVENT:
    /// Kích hoạt chính xác tại frame mũi cúp bổ trúng tảng đá!
    /// 100% KHÔNG DÙNG BỘ ĐẾM THỜI GIAN NỮA!
    /// </summary>
    public void OnMiningHit()
    {
        ProcessMiningHit();
    }

    /// <summary>
    /// Tương thích nếu bạn chọn AwardStoneAtImpact trong Animation Event
    /// </summary>
    public void AwardStoneAtImpact()
    {
        ProcessMiningHit();
    }

    public void AwardStoneAtImpact(float dummy)
    {
        ProcessMiningHit();
    }

    /// <summary>
    /// Xử lý tăng đá, cộng kinh tế và nảy số khi Animation Event bắn tín hiệu
    /// </summary>
    private void ProcessMiningHit()
    {
        if (!isMining || targetRock == null) return;

        // 1. Tăng số đá khai thác
        stoneMinedCount++;

        // 2. Cộng tài nguyên vào hệ thống kinh tế
        if (GameEconomy.Instance != null)
        {
            GameEconomy.Instance.AddStone(1);
        }

        // 3. Hiển thị chữ số nảy tài nguyên "+1 ĐÁ 🪨" màu xanh ngọc sáng to rõ ràng trên đỉnh tảng đá
        Vector3 popupPos = (targetRock != null) ? (targetRock.position + Vector3.up * 2.5f) : (transform.position + Vector3.up * 4.2f);
        FloatingDamageText.SpawnResource(popupPos, "+1 ĐÁ 🪨", new Color(0.2f, 0.95f, 1f, 1f), 0.08f);

        Debug.Log($"<color=#00FF88>⚡ [Animation Event - OnMiningHit] Cúp nện trúng đá! Đã khai thác 1 Đá từ {targetRock.name}! Tổng đá: {stoneMinedCount}</color>");
    }

    private void EnsureOverheadBadge()
    {
        if (overheadBadgeObj != null) return;
        overheadBadgeObj = new GameObject("[Overhead_Badge]");
        overheadBadgeObj.transform.SetParent(transform, false);
        overheadBadgeObj.transform.localPosition = new Vector3(0f, 4.2f, 0f);

        Canvas canvas = overheadBadgeObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform rt = overheadBadgeObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300f, 60f);
        rt.localScale = Vector3.one * 0.015f;

        GameObject txtGo = new GameObject("Text");
        txtGo.transform.SetParent(overheadBadgeObj.transform, false);
        overheadText = txtGo.AddComponent<UnityEngine.UI.Text>();
        overheadText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        overheadText.fontSize = 28;
        overheadText.fontStyle = FontStyle.Bold;
        overheadText.alignment = TextAnchor.MiddleCenter;
        overheadText.color = new Color(0.2f, 0.95f, 1f, 1f);
        overheadText.text = "⛏️ Robot Đào Mỏ";

        UnityEngine.UI.Outline ol = txtGo.AddComponent<UnityEngine.UI.Outline>();
        ol.effectColor = Color.black;
        ol.effectDistance = new Vector2(2f, -2f);

        RectTransform tRt = txtGo.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;
    }

    private void UpdateOverheadBadge()
    {
        EnsureOverheadBadge();
        if (Camera.main != null && overheadBadgeObj != null)
        {
            overheadBadgeObj.transform.rotation = Camera.main.transform.rotation;
        }
        if (overheadText != null)
        {
            if (isMining)
                overheadText.text = $"⛏️ Đang Đào: <color=#FFE600>{stoneMinedCount}</color> Đá";
            else
                overheadText.text = $"🚶 Đi tới mỏ đá...";
        }
    }

    /// <summary>
    /// Trọng lực code -9.18 m/s^2 và khóa độ cao mặt đất bằng Raycast & Terrain (Zero Rigidbody)
    /// </summary>
    private void ApplyGravityAndGroundClamp()
    {
        Vector3 curPos = transform.position;
        float groundY = GetTrueGroundHeight(curPos);

        // Áp dụng gia tốc trọng lực: v = v0 + g * dt (g = -9.18)
        verticalVelocity += gravity * Time.deltaTime;

        // Vị trí Y mới
        float newY = curPos.y + verticalVelocity * Time.deltaTime;

        // Nếu chạm hoặc lún xuống dưới mặt đất -> khóa cố định vào mặt đất và triệt tiêu vận tốc rơi
        if (newY <= groundY)
        {
            newY = groundY;
            verticalVelocity = 0f;
        }

        transform.position = new Vector3(curPos.x, newY, curPos.z);
    }

    /// <summary>
    /// Dò tìm độ cao mặt đất chính xác bằng Raycast từ trên xuống, loại trừ mọi vật thể trên cao
    /// </summary>
    private float GetTrueGroundHeight(Vector3 pos)
    {
        float terrainGround = -999f;
        if (Terrain.activeTerrain != null)
        {
            terrainGround = Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.transform.position.y;
        }

        // Bắn Raycast từ ngang thắt lưng/ngực xuống dưới để tìm mặt phẳng sàn
        Vector3 rayStart = new Vector3(pos.x, pos.y + 1.2f, pos.z);
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 15f);

        float bestHitY = -999f;
        foreach (var h in hits)
        {
            // Bỏ qua chính robot hoặc các vật thể di động
            if (h.transform == transform || h.transform.IsChildOf(transform)) continue;

            // Bỏ qua vật thể tảng đá, quái vật, đạn, người chơi (chỉ lấy sàn đảo/đất)
            string hitName = h.collider.name.ToLower();
            if (hitName.Contains("rock") || hitName.Contains("bullet") || hitName.Contains("enemy")) continue;

            if (h.point.y > bestHitY && h.point.y <= pos.y + 0.35f)
            {
                bestHitY = h.point.y;
            }
        }

        float finalGround = Mathf.Max(terrainGround, bestHitY);
        // Fallback an toàn: Nếu không tìm thấy, giữ tối thiểu ở độ cao mặt đất đảo (~10.5m)
        if (finalGround < 5f) finalGround = 10.5f;

        return finalGround;
    }

    /// <summary>
    /// Tự động quét tìm mục tiêu khai thác tối ưu nhất trong Scene theo Tag hoặc Tên
    /// </summary>
    public void FindBestMiningTarget()
    {
        // 1. Tìm theo Tag chỉ định (mặc định 'Rock')
        if (!string.IsNullOrEmpty(targetTag))
        {
            try
            {
                GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(targetTag);
                if (taggedObjects != null && taggedObjects.Length > 0)
                {
                    float minDist = float.MaxValue;
                    Transform nearest = null;
                    foreach (var obj in taggedObjects)
                    {
                        if (obj == null) continue;
                        float d = Vector3.Distance(transform.position, obj.transform.position);
                        if (d < minDist)
                        {
                            minDist = d;
                            nearest = obj.transform;
                        }
                    }
                    if (nearest != null)
                    {
                        targetRock = nearest;
                        return;
                    }
                }
            }
            catch
            {
                // Nếu tag chưa đăng ký trong TagManager, chuyển sang tìm theo tên
            }
        }

        // 2. Tìm theo tên các tảng đá thực tế trong Scene
        string[] commonRockNames = new string[] { "rocks (1)", "Rock_D", "rocks", "Rock_06_A_LOD0", "Rock" };
        foreach (var rName in commonRockNames)
        {
            GameObject found = GameObject.Find(rName);
            if (found != null)
            {
                targetRock = found.transform;
                return;
            }
        }

        // 3. Quét toàn bộ Scene tìm object có tên chứa "rock"
        var allCols = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
        float closestDist = float.MaxValue;
        Transform bestRock = null;
        foreach (var col in allCols)
        {
            if (col.name.ToLower().Contains("rock"))
            {
                float d = Vector3.Distance(transform.position, col.transform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    bestRock = col.transform;
                }
            }
        }
        if (bestRock != null)
        {
            targetRock = bestRock;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Vẽ tia Raycast trong Scene để quan sát trực quan
        Vector3 rayOrigin = transform.position + Vector3.up * raycastHeightOffset;

        // Tia giữa
        Gizmos.color = Color.green;
        Gizmos.DrawRay(rayOrigin, transform.forward * obstacleDetectDistance);

        // Tia cánh quạt né 2 bên
        Vector3 leftRayDir = Quaternion.Euler(0, -whiskerAngle, 0) * transform.forward;
        Vector3 rightRayDir = Quaternion.Euler(0, whiskerAngle, 0) * transform.forward;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(rayOrigin, leftRayDir * (obstacleDetectDistance * 0.85f));
        Gizmos.DrawRay(rayOrigin, rightRayDir * (obstacleDetectDistance * 0.85f));

        // Đường nối tới tảng đá mục tiêu
        if (targetRock != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(rayOrigin, targetRock.position);
        }
    }
}
