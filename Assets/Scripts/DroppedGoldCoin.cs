using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DroppedGoldCoin - Đồng Xu Vàng Rơi Trên Chiến Trường:
/// - Khi Enemy chết, vàng sẽ văng ra mặt đất thành các đồng xu 3D vàng kim phát sáng.
/// - Đồng xu nằm trên đất, tự xoay 360 độ và nhấp nhô bồng bềnh.
/// - Ban ngày (sáng ra) hoặc bất kỳ lúc nào Player đến gần (< 3.8m), đồng xu sẽ tự động bị từ trường hút về Player.
/// - Khi chạm Player: Cộng tiền vào GameEconomy và nảy số vàng trên màn hình.
/// </summary>
public class DroppedGoldCoin : MonoBehaviour
{
    [Header("=== GIÁ TRỊ ĐỒNG XU ===")]
    public int coinValue = 5;

    [Header("=== CÀI ĐẶT HÚT & NHẶT ===")]
    public float magnetDistance = 3.8f;
    public float collectDistance = 0.65f;
    public float moveSpeed = 0f;
    public float maxSpeed = 16f;
    public float acceleration = 24f;

    [Header("=== ANIMATION NHẤP NHÔ & XOAY ===")]
    public float rotateSpeed = 180f;
    public float bobSpeed = 3.5f;
    public float bobHeight = 0.12f;

    private Transform playerTransform;
    private Vector3 initialBasePos;
    private float groundY;
    private bool isBeingAttracted = false;
    private float spawnTime;

    private static Material sharedGoldCoinMaterial;

    private void Awake()
    {
        spawnTime = Time.time;
    }

    private void Start()
    {
        FindPlayer();

        // Dò độ cao mặt đất để cố định vị trí đồng xu không bị rơi xuyên sàn
        groundY = GetTrueGroundHeight(transform.position);
        initialBasePos = new Vector3(transform.position.x, groundY + 0.22f, transform.position.z);
    }

    private void Update()
    {
        // 1. Animation xoay tròn và nhấp nhô bồng bềnh
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

        if (!isBeingAttracted)
        {
            float bobOffset = Mathf.Sin((Time.time - spawnTime) * bobSpeed) * bobHeight;
            transform.position = initialBasePos + Vector3.up * bobOffset;
        }

        // 2. Tìm Player nếu chưa có tham chiếu
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        // 3. Kiểm tra khoảng cách tới Player
        Vector3 playerPos = playerTransform.position + Vector3.up * 0.8f;
        float distToPlayer = Vector3.Distance(transform.position, playerPos);

        if (distToPlayer <= magnetDistance)
        {
            isBeingAttracted = true;
        }

        // 4. Nếu bị hút về Player
        if (isBeingAttracted)
        {
            moveSpeed = Mathf.MoveTowards(moveSpeed, maxSpeed, acceleration * Time.deltaTime);
            transform.position = Vector3.MoveTowards(transform.position, playerPos, moveSpeed * Time.deltaTime);

            // Khi chạm vào Player -> Nhặt thành công!
            if (distToPlayer <= collectDistance || Vector3.Distance(transform.position, playerPos) <= collectDistance)
            {
                CollectCoin();
            }
        }
    }

    private void CollectCoin()
    {
        // 1. Cộng vàng vào kinh tế
        if (GameEconomy.Instance != null && coinValue > 0)
        {
            GameEconomy.Instance.AddCoins(coinValue);
        }

        // 2. Nảy số vàng nổi trên đầu Player
        Vector3 popupPos = (playerTransform != null ? playerTransform.position : transform.position) + Vector3.up * 1.6f;
        FloatingDamageText.SpawnResource(popupPos, $"+{coinValue} 🪙", new Color(1f, 0.88f, 0.1f, 1f), 0.08f);

        // 3. Tiêu hủy đồng xu
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

    private float GetTrueGroundHeight(Vector3 pos)
    {
        float tY = 9.5f;
        if (Terrain.activeTerrain != null)
        {
            tY = Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.transform.position.y;
        }

        if (Physics.Raycast(new Vector3(pos.x, 30f, pos.z), Vector3.down, out RaycastHit hit, 45f))
        {
            string n = hit.collider.name.ToLower();
            if (!n.Contains("player") && !n.Contains("enemy") && !n.Contains("bullet"))
            {
                return Mathf.Max(tY, hit.point.y);
            }
        }
        return tY;
    }

    /// <summary>
    /// Rơi các đồng xu vàng ra xung quanh vị trí quái chết
    /// </summary>
    public static void SpawnCoins(Vector3 centerPos, int totalGold)
    {
        if (totalGold <= 0) return;

        // Số lượng đồng xu: Quái thường rơi 2 - 3 đồng, Boss rơi 6 - 8 đồng
        int coinCount = totalGold >= 100 ? Mathf.Clamp(totalGold / 40, 5, 8) : Mathf.Clamp(totalGold / 5, 2, 4);
        int valuePerCoin = Mathf.Max(1, totalGold / coinCount);
        int remainder = totalGold - (valuePerCoin * coinCount);

        for (int i = 0; i < coinCount; i++)
        {
            int thisCoinVal = valuePerCoin + (i == 0 ? remainder : 0);

            // Văng ngẫu nhiên trong bán kính 1.2m
            Vector2 randCircle = Random.insideUnitCircle * 1.25f;
            Vector3 spawnPos = centerPos + new Vector3(randCircle.x, 0.5f, randCircle.y);

            GameObject coinObj = CreateCoinVisualObject(thisCoinVal);
            coinObj.transform.position = spawnPos;

            DroppedGoldCoin coinComp = coinObj.GetComponent<DroppedGoldCoin>();
            if (coinComp != null)
            {
                coinComp.coinValue = thisCoinVal;
            }
        }
    }

    /// <summary>
    /// Tự động sinh GameObject đồng xu 3D mỏng đẹp chuẩn URP Lit
    /// </summary>
    private static GameObject CreateCoinVisualObject(int value)
    {
        GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coin.name = $"Dropped_Gold_Coin_{value}";

        // Tỷ lệ đồng xu dẹt mỏng: đường kính 0.42m, độ dày 0.05m
        coin.transform.localScale = new Vector3(0.42f, 0.025f, 0.42f);

        // Khử collider vật lý đặc cứng (chỉ dùng trigger hoặc raycast code)
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
