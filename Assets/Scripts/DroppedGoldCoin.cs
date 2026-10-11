using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DroppedGoldCoin - Đồng Xu Vàng Rơi Trên Chiến Trường:
/// - Khi Enemy/Boss chết, vàng sẽ văng ra và RƠI CHÍNH XÁC XUỐNG MẶT ĐẤT (Arc/Gravity).
/// - Dù quái vật bay trên trời (Mecha Dragon) hay chết trên cao, toàn bộ tiền đều rơi từ trên không xuống mặt sàn.
/// - Đồng xu nằm sát đất, tự xoay 360 độ và nhấp nhô nhẹ nhàng.
/// - Khi Player hoặc Dân Làng đến gần, từ trường sẽ hút đồng xu gom vào người chơi.
/// - Tuyệt đối không bao giờ bị lơ lửng trên không trung.
/// </summary>
public class DroppedGoldCoin : MonoBehaviour
{
    [Header("=== GIÁ TRỊ ĐỒNG XU ===")]
    public int coinValue = 5;

    [Header("=== CÀI ĐẶT HÚT & NHẶT ===")]
    public float magnetDistance = 5.0f;
    public float collectDistance = 1.0f;
    public float moveSpeed = 0f;
    public float maxSpeed = 18f;
    public float acceleration = 28f;

    [Header("=== HIỆU ỨNG RƠI XUỐNG ĐẤT (FALLING ARC) ===")]
    public bool isLanded = false;
    private Vector3 currentVelocity;
    private Vector3 targetGroundPos;
    private float targetGroundY;
    private int bounceCount = 0;
    private const int MAX_BOUNCES = 2;

    [Header("=== ANIMATION XOAY VÀ NHẤP NHÔ NHẸ TRÊN ĐẤT ===")]
    public float rotateSpeed = 180f;
    public float bobSpeed = 3.5f;
    public float bobHeight = 0.04f; // Chỉ nhấp nhô nhẹ 4cm sát mặt đất

    private Transform playerTransform;
    private bool isBeingAttracted = false;
    private float spawnTime;

    private static Material sharedGoldCoinMaterial;

    public static readonly List<DroppedGoldCoin> ActiveCoins = new List<DroppedGoldCoin>();
    public static event System.Action<Vector3> OnCoinDropped;

    private void Awake()
    {
        spawnTime = Time.time;
    }

    private void OnEnable()
    {
        if (!ActiveCoins.Contains(this)) ActiveCoins.Add(this);
    }

    private void OnDisable()
    {
        ActiveCoins.Remove(this);
    }

    private void Start()
    {
        FindPlayer();

        // Đảm bảo tính toán chính xác cao độ mặt đất hòn đảo
        if (targetGroundY <= 0.01f)
        {
            targetGroundY = GetTrueGroundHeight(transform.position);
            targetGroundPos = new Vector3(transform.position.x, targetGroundY + 0.12f, transform.position.z);
        }
    }

    /// <summary>
    /// Kích hoạt quỹ đạo văng và rơi xuống đất từ vị trí quái chết
    /// </summary>
    public void LaunchFall(Vector3 startPos, Vector3 targetLandingGround)
    {
        transform.position = startPos;
        targetGroundPos = targetLandingGround;
        targetGroundY = targetLandingGround.y;
        isLanded = false;
        bounceCount = 0;

        // Vận tốc văng ban đầu: Bung nhẹ lên trên và tản ra ngoài
        Vector3 horizontalDir = (targetLandingGround - startPos);
        horizontalDir.y = 0f;
        float hDist = horizontalDir.magnitude;

        // Nếu quái chết ở rất cao trên không (rồng bay): rơi nhanh xuống
        float dropHeight = Mathf.Max(0.5f, startPos.y - targetLandingGround.y);
        float fallTime = Mathf.Clamp(Mathf.Sqrt((2f * dropHeight) / 22f), 0.35f, 1.2f);

        Vector3 hVelocity = fallTime > 0.01f ? (horizontalDir / fallTime) : Vector3.zero;
        float vVelocity = dropHeight > 2.0f ? 2.5f : 4.5f; // Nảy nhẹ lên trước khi rơi xuống

        currentVelocity = new Vector3(hVelocity.x, vVelocity, hVelocity.z);
    }

    private void Update()
    {
        // 1. Luôn tự xoay tròn 360 độ quanh trục thẳng đứng
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

        // 2. Nếu đang trong quá trình rơi từ trên không xuống mặt đất:
        if (!isLanded && !isBeingAttracted)
        {
            ApplyFallPhysics();
            return;
        }

        // 3. Khi đã nằm trên mặt đất: Giữ cố định trên sàn + nhấp nhô nhẹ 4cm
        if (isLanded && !isBeingAttracted)
        {
            float bob = Mathf.Sin((Time.time - spawnTime) * bobSpeed) * bobHeight;
            transform.position = new Vector3(targetGroundPos.x, targetGroundY + 0.12f + bob, targetGroundPos.z);
        }

        // 4. Tìm Player nếu chưa có tham chiếu
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        // 5. Kiểm tra khoảng cách tới Player (tính khoảng cách 2D mặt phẳng + độ chênh lệch cao độ)
        Vector3 playerPos = playerTransform.position + Vector3.up * 0.8f;
        float distToPlayer = Vector3.Distance(transform.position, playerPos);
        float dist2D = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(playerPos.x, playerPos.z));

        // Ban ngày từ trường hút mạnh hơn (lên tới 8m)
        float currentMagnetDist = (DayNightSystem.Instance != null && !DayNightSystem.Instance.isNight) 
            ? magnetDistance * 1.6f 
            : magnetDistance;

        if (distToPlayer <= currentMagnetDist || dist2D <= currentMagnetDist)
        {
            isBeingAttracted = true;
        }

        // 6. Nếu bị hút về phía Player: Trượt mượt mà theo đường thẳng vào túi đồ Player
        if (isBeingAttracted)
        {
            moveSpeed = Mathf.MoveTowards(moveSpeed, maxSpeed, acceleration * Time.deltaTime);
            transform.position = Vector3.MoveTowards(transform.position, playerPos, moveSpeed * Time.deltaTime);

            // Khi chạm vào Player -> Nhặt thành công!
            if (distToPlayer <= collectDistance || dist2D <= collectDistance || Vector3.Distance(transform.position, playerPos) <= collectDistance)
            {
                CollectCoin();
            }
        }
    }

    /// <summary>
    /// Mô phỏng vật lý rơi tự do chuẩn xác bằng code (Không dùng Rigidbody)
    /// </summary>
    private void ApplyFallPhysics()
    {
        float dt = Time.deltaTime;

        // Trọng lực kéo xuống đất: g = -24 m/s²
        currentVelocity.y -= 24f * dt;

        Vector3 newPos = transform.position + currentVelocity * dt;

        // Kiểm tra chạm sàn đất
        if (newPos.y <= targetGroundY + 0.12f)
        {
            newPos.y = targetGroundY + 0.12f;
            bounceCount++;

            if (bounceCount < MAX_BOUNCES && Mathf.Abs(currentVelocity.y) > 3.0f)
            {
                // Nảy lên nhẹ (đàn hồi 30%)
                currentVelocity.y = Mathf.Abs(currentVelocity.y) * 0.32f;
                currentVelocity.x *= 0.5f;
                currentVelocity.z *= 0.5f;
            }
            else
            {
                // Tiếp đất hoàn toàn! Nằm cố định trên thảm cỏ
                isLanded = true;
                currentVelocity = Vector3.zero;
                targetGroundPos = newPos;
            }
        }

        transform.position = newPos;
    }

    private void CollectCoin()
    {
        if (GameEconomy.Instance != null && coinValue > 0)
        {
            GameEconomy.Instance.AddCoins(coinValue);
        }

        Vector3 popupPos = (playerTransform != null ? playerTransform.position : transform.position) + Vector3.up * 1.6f;
        FloatingDamageText.SpawnResource(popupPos, $"+{coinValue} 🪙", new Color(1f, 0.88f, 0.1f, 1f), 0.08f);

        Destroy(gameObject);
    }

    /// <summary>
    /// Cho phép Dân Làng (IslandSettler) chạy đến nhặt đồng xu vàng giúp Player
    /// </summary>
    public void CollectBySettler(Transform settler)
    {
        if (GameEconomy.Instance != null && coinValue > 0)
        {
            GameEconomy.Instance.AddCoins(coinValue);
        }

        Vector3 popupPos = (settler != null ? settler.position : transform.position) + Vector3.up * 1.5f;
        FloatingDamageText.SpawnResource(popupPos, $"+{coinValue} 🪙", new Color(1f, 0.88f, 0.1f, 1f), 0.08f);

        Destroy(gameObject);
    }

    private void FindPlayer()
    {
        if (PlayerController.Instance != null)
        {
            playerTransform = PlayerController.Instance.transform;
            return;
        }

        GameObject pGo = GameObject.FindWithTag("Player");
        if (pGo != null)
        {
            playerTransform = pGo.transform;
            return;
        }

        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            playerTransform = pc.transform;
        }
    }

    /// <summary>
    /// Dò tìm cao độ mặt đất chuẩn xác nhất của hòn đảo:
    /// - Ưu tiên Terrain.SampleHeight
    /// - Lọc sạch toàn bộ cây cối, quái vật, boss, đạn, tường, trụ, coin để tuyệt đối không đặt coin trên không
    /// </summary>
    public static float GetTrueGroundHeight(Vector3 pos)
    {
        float terrainY = 9.5f;
        if (Terrain.activeTerrain != null)
        {
            terrainY = Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.transform.position.y;
        }

        // Bắn Raycast quét các mặt sàn hợp lệ bên dưới
        Vector3 rayStart = new Vector3(pos.x, 35f, pos.z);
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 50f);
        float bestFloorY = -999f;

        foreach (var h in hits)
        {
            if (h.collider == null) continue;
            string n = h.collider.name.ToLower();

            // LOẠI TRỪ TRIỆT ĐỂ: Bất kỳ vật thể nào không phải mặt sàn đi bộ
            if (n.Contains("player") || n.Contains("enemy") || n.Contains("boss") || 
                n.Contains("bullet") || n.Contains("wall") || n.Contains("turret") || 
                n.Contains("tree") || n.Contains("coin") || n.Contains("leaf") || 
                n.Contains("pine") || n.Contains("rock") || n.Contains("spider") || 
                n.Contains("dragon") || n.Contains("building") || n.Contains("mesh"))
            {
                continue;
            }

            if (h.collider is TerrainCollider || n.Contains("ground") || n.Contains("terrain") || n.Contains("floor"))
            {
                if (h.point.y > bestFloorY && h.point.y < 30f)
                {
                    bestFloorY = h.point.y;
                }
            }
        }

        return Mathf.Max(terrainY, bestFloorY > -900f ? bestFloorY : terrainY);
    }

    /// <summary>
    /// Rơi các đồng xu vàng từ quái vật/boss: 
    /// Toàn bộ tiền sẽ bung ra và RƠI CHÍNH XÁC XUỐNG MẶT ĐẤT!
    /// </summary>
    public static void SpawnCoins(Vector3 centerPos, int totalGold)
    {
        if (totalGold <= 0) return;

        // Số lượng đồng xu: Quái thường rơi 2 - 3 đồng, Boss rơi 6 - 8 đồng
        int coinCount = totalGold >= 100 ? Mathf.Clamp(totalGold / 40, 5, 8) : Mathf.Clamp(totalGold / 5, 2, 4);
        int valuePerCoin = Mathf.Max(1, totalGold / coinCount);
        int remainder = totalGold - (valuePerCoin * coinCount);

        Vector3 averageGroundLandingPos = centerPos;

        for (int i = 0; i < coinCount; i++)
        {
            int thisCoinVal = valuePerCoin + (i == 0 ? remainder : 0);

            // Bán kính tản ra xung quanh điểm rơi
            Vector2 randCircle = Random.insideUnitCircle * Random.Range(0.8f, 2.2f);
            Vector3 landingGroundPos = new Vector3(centerPos.x + randCircle.x, 0f, centerPos.z + randCircle.y);

            // Tính toán mặt sàn đất thực tế dưới chân vị trí rơi
            float groundFloorY = GetTrueGroundHeight(landingGroundPos);
            landingGroundPos.y = groundFloorY + 0.12f;

            if (i == 0) averageGroundLandingPos = landingGroundPos;

            // Tạo GameObject đồng xu
            GameObject coinObj = CreateCoinVisualObject(thisCoinVal);

            DroppedGoldCoin coinComp = coinObj.GetComponent<DroppedGoldCoin>();
            if (coinComp != null)
            {
                coinComp.coinValue = thisCoinVal;
                // Kích hoạt quỹ đạo rơi từ vị trí quái chết xuống mặt đất!
                coinComp.LaunchFall(centerPos, landingGroundPos);
            }
        }

        // Báo cho toàn bộ Dân Làng trên đảo biết vị trí MẶT ĐẤT nơi tiền rơi xuống để chạy đến nhặt!
        OnCoinDropped?.Invoke(averageGroundLandingPos);
    }

    /// <summary>
    /// Khởi tạo GameObject đồng xu 3D mỏng đẹp chuẩn URP Lit
    /// </summary>
    private static GameObject CreateCoinVisualObject(int value)
    {
        GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coin.name = $"Dropped_Gold_Coin_{value}";

        // Tỷ lệ đồng xu dẹt mỏng: đường kính 0.42m, độ dày 0.035m
        coin.transform.localScale = new Vector3(0.42f, 0.025f, 0.42f);

        // Khử collider vật lý cứng (chỉ dùng Trigger để tối ưu CPU)
        Collider col = coin.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        // Gán Material kim loại vàng hoàng gia phát sáng URP Lit
        Renderer rend = coin.GetComponent<Renderer>();
        if (rend != null)
        {
            if (sharedGoldCoinMaterial == null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
                sharedGoldCoinMaterial = new Material(sh);
                sharedGoldCoinMaterial.name = "GoldCoin_Shared_Mat";
                sharedGoldCoinMaterial.color = new Color(1f, 0.84f, 0.12f, 1f); // Vàng kim rực rỡ

                if (sharedGoldCoinMaterial.HasProperty("_Metallic")) sharedGoldCoinMaterial.SetFloat("_Metallic", 0.95f);
                if (sharedGoldCoinMaterial.HasProperty("_Smoothness")) sharedGoldCoinMaterial.SetFloat("_Smoothness", 0.88f);
                if (sharedGoldCoinMaterial.HasProperty("_EmissionColor"))
                {
                    sharedGoldCoinMaterial.EnableKeyword("_EMISSION");
                    sharedGoldCoinMaterial.SetColor("_EmissionColor", new Color(1f, 0.55f, 0.05f) * 0.75f);
                }
            }
            rend.sharedMaterial = sharedGoldCoinMaterial;
        }

        // Gắn script DroppedGoldCoin
        DroppedGoldCoin comp = coin.AddComponent<DroppedGoldCoin>();
        comp.coinValue = value;

        return coin;
    }
}
