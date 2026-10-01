using System;
using UnityEngine;

public class GameEconomy : MonoBehaviour
{
    public static GameEconomy Instance { get; private set; }

    [Header("--- Tài Nguyên & Tiền Tệ ---")]
    [Tooltip("Số vàng hiện có của người chơi")]
    public int coins = 0; // Để 0 theo yêu cầu, có thể chỉnh trong Inspector
    public int stoneCount = 0;

    [Header("--- Chi Phí Mua Sắm (Mặc Định 0🪙 - Tự Do Chỉnh Trong Inspector) ---")]
    [Tooltip("Giá mua Robot đào mỏ (Mặc định 0, bạn có thể chỉnh lại bất kỳ lúc nào)")]
    public int workerRobotCost = 0;

    [Tooltip("Giá mỗi đoạn tường khi kéo (Mặc định 0, bạn có thể chỉnh lại bất kỳ lúc nào)")]
    public int wallSegmentCost = 0;

    [Tooltip("Giá nâng cấp cúp sắt (Mặc định 0, bạn có thể chỉnh lại bất kỳ lúc nào)")]
    public int pickaxeUpgradeCost = 0;

    [Tooltip("Giá nâng cấp tường cho 5 lần nâng (Cấp 1->2, 2->3, 3->4, 4->5, 5->6)")]
    public int[] wallUpgradeCosts = new int[5] { 0, 0, 0, 0, 0 };

    [Header("--- Cấp Độ Cúp Sắt (Pickaxe) ---")]
    public int pickaxeLevel = 1;

    [Header("--- Cấp Độ Tường (Wall Level 1 - 6) ---")]
    [Range(1, 6)]
    public int wallLevel = 1;

    [Header("--- Prefab Để Mua Robot ---")]
    public GameObject workerRobotPrefab;
    public Transform robotSpawnPoint;

    // Sự kiện khi tài nguyên thay đổi
    public event Action OnEconomyChanged;
    public event Action<string> OnNotEnoughCoins;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (workerRobotPrefab == null)
        {
            workerRobotPrefab = Resources.Load<GameObject>("BipedRobot_Prefab");
            if (workerRobotPrefab == null)
            {
                var existing = GameObject.Find("BipedRobot_Prefab");
                if (existing != null) workerRobotPrefab = existing;
            }
        }
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        OnEconomyChanged?.Invoke();
    }

    public bool CanAfford(int amount)
    {
        if (amount <= 0) return true;
        return coins >= amount;
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0) return true;

        if (coins >= amount)
        {
            coins -= amount;
            OnEconomyChanged?.Invoke();
            return true;
        }
        TriggerNotEnoughCoins("Không đủ vàng! Cần " + amount + "🪙");
        return false;
    }

    public void AddStone(int amount)
    {
        stoneCount += amount;
        OnEconomyChanged?.Invoke();
    }

    public void TriggerNotEnoughCoins(string message = "Không đủ vàng!")
    {
        OnNotEnoughCoins?.Invoke(message);
    }

    // ================= 1. NÂNG CẤP CÚP SẮT =================
    public int GetPickaxeUpgradeCost()
    {
        return pickaxeUpgradeCost;
    }

    public bool UpgradePickaxe()
    {
        int cost = GetPickaxeUpgradeCost();
        if (SpendCoins(cost))
        {
            pickaxeLevel++;
            WorkerBot[] allBots = FindObjectsByType<WorkerBot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var bot in allBots)
            {
                bot.mineInterval = Mathf.Max(0.5f, 1.8f - pickaxeLevel * 0.15f);
            }
            OnEconomyChanged?.Invoke();
            Debug.Log($"<color=#00FF88>[Nâng Cấp Cúp]</color> Cúp sắt đã lên Cấp {pickaxeLevel}!");
            return true;
        }
        return false;
    }

    // ================= 2. MUA ROBOT ĐÀO MỎ =================
    public bool BuyWorkerRobot()
    {
        if (SpendCoins(workerRobotCost))
        {
            Vector3 spawnPos = Vector3.zero;
            if (robotSpawnPoint != null)
            {
                spawnPos = robotSpawnPoint.position + UnityEngine.Random.insideUnitSphere * 1.5f;
                spawnPos.y = robotSpawnPoint.position.y;
            }
            else
            {
                var sceneBot = GameObject.Find("BipedRobot_Prefab");
                if (sceneBot != null)
                {
                    spawnPos = sceneBot.transform.position + new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), 0, UnityEngine.Random.Range(-1.5f, 1.5f));
                }
            }

            if (workerRobotPrefab != null)
            {
                GameObject newBot = Instantiate(workerRobotPrefab, spawnPos, Quaternion.identity);
                newBot.name = $"BipedRobot_Worker_{FindObjectsByType<WorkerBot>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length}";
                WorkerBot wb = newBot.GetComponent<WorkerBot>();
                if (wb != null)
                {
                    wb.mineInterval = Mathf.Max(0.5f, 1.8f - pickaxeLevel * 0.15f);
                }
                Debug.Log("<color=#00FF88>[Mua Robot]</color> Đã mua thành công 1 Robot đào mỏ!");
            }
            OnEconomyChanged?.Invoke();
            return true;
        }
        return false;
    }

    // ================= 3. NÂNG CẤP BỨC TƯỜNG (CẤP 1 - 6) =================
    public static readonly string[] WallLevelNames = new string[]
    {
        "Cơ Bản (Đá/Sắt Thô)",
        "Bạc (Silver)",
        "Vàng (Gold)",
        "Bạch Kim (Platinum)",
        "Kim Cương (Diamond)",
        "Đen Titan (Titanium Black)"
    };

    public int GetWallUpgradeCost()
    {
        int index = wallLevel - 1;
        if (wallUpgradeCosts != null && index >= 0 && index < wallUpgradeCosts.Length)
        {
            return wallUpgradeCosts[index];
        }
        return 0;
    }

    public bool UpgradeGlobalWall()
    {
        if (wallLevel >= 6)
        {
            Debug.Log("<color=yellow>[Tường]</color> Tường đã đạt cấp tối đa (Cấp 6: Đen Titan)!");
            return false;
        }

        int cost = GetWallUpgradeCost();
        if (SpendCoins(cost))
        {
            wallLevel++;
            WallSegment.UpgradeAllWallsToLevel(wallLevel);
            OnEconomyChanged?.Invoke();
            Debug.Log($"<color=#00FF88>[Nâng Cấp Tường]</color> Tường đã nâng lên Cấp {wallLevel}: {WallLevelNames[wallLevel - 1]}!");
            return true;
        }
        return false;
    }
}
