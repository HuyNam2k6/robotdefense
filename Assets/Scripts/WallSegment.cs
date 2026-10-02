using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class WallSegment : MonoBehaviour
{
    public static readonly List<WallSegment> AllWalls = new List<WallSegment>();

    [Header("Cấp Độ Tường (1 - 6)")]
    [Range(1, 6)]
    public int currentLevel = 1;

    private Renderer[] cachedRenderers;
    private MaterialPropertyBlock propBlock;
    private Vector3 baseScale = Vector3.zero;

    public void SetBaseScale(Vector3 scale)
    {
        baseScale = scale;
    }

    void OnEnable()
    {
        if (!AllWalls.Contains(this)) AllWalls.Add(this);
    }

    void OnDisable()
    {
        AllWalls.Remove(this);
    }

    void Awake()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        propBlock = new MaterialPropertyBlock();
        baseScale = transform.localScale;
    }

    void Start()
    {
        if (baseScale == Vector3.zero) baseScale = transform.localScale;
        if (GameEconomy.Instance != null && currentLevel <= 1)
        {
            ApplyLevel(GameEconomy.Instance.wallLevel);
        }
        else
        {
            ApplyLevel(currentLevel);
        }
    }

    public void ApplyLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 1, 6);

        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
        }
        if (propBlock == null) propBlock = new MaterialPropertyBlock();

        // Định nghĩa 6 màu sắc theo yêu cầu:
        // Cấp 1: Mặc định (Vật liệu gốc ban đầu)
        // Cấp 2: Bạc hơn (Silver)
        // Cấp 3: Màu Vàng (Gold)
        // Cấp 4: Màu Bạch Kim (Platinum)
        // Cấp 5: Màu như Kim Cương (Diamond Cyan Crystal)
        // Cấp 6: Đen Titan (Titanium Black)
        Color wallColor;
        float metallic = 0.5f;
        float smoothness = 0.5f;
        Color emissionColor = Color.black;

        switch (currentLevel)
        {
            case 1:
                wallColor = Color.white; // Nguyên bản
                metallic = 0.1f;
                smoothness = 0.3f;
                break;

            case 2:
                // Cấp 2: Bạc hơn
                wallColor = new Color(0.82f, 0.86f, 0.94f, 1f);
                metallic = 0.85f;
                smoothness = 0.75f;
                break;

            case 3:
                // Cấp 3: Màu Vàng (Gold)
                wallColor = new Color(1.0f, 0.78f, 0.12f, 1f);
                metallic = 0.95f;
                smoothness = 0.8f;
                break;

            case 4:
                // Cấp 4: Màu Bạch Kim (Platinum)
                wallColor = new Color(0.94f, 0.97f, 1.0f, 1f);
                metallic = 0.92f;
                smoothness = 0.9f;
                break;

            case 5:
                // Cấp 5: Màu như Kim Cương (Diamond Gemstone)
                wallColor = new Color(0.15f, 0.92f, 1.0f, 1f);
                metallic = 0.25f;
                smoothness = 0.98f;
                emissionColor = new Color(0.05f, 0.35f, 0.45f, 1f);
                break;

            case 6:
                // Cấp 6: Đen Titan (Titanium Black)
                wallColor = new Color(0.10f, 0.11f, 0.14f, 1f);
                metallic = 0.98f;
                smoothness = 0.85f;
                break;

            default:
                wallColor = Color.white;
                break;
        }

        foreach (var rend in cachedRenderers)
        {
            if (rend == null) continue;
            rend.GetPropertyBlock(propBlock);

            // Gán màu cho Shader URP hoặc Standard
            propBlock.SetColor("_BaseColor", wallColor);
            propBlock.SetColor("_Color", wallColor);
            propBlock.SetFloat("_Metallic", metallic);
            propBlock.SetFloat("_Smoothness", smoothness);

            if (currentLevel == 5)
            {
                propBlock.SetColor("_EmissionColor", emissionColor);
            }
            else
            {
                propBlock.SetColor("_EmissionColor", Color.black);
            }

            rend.SetPropertyBlock(propBlock);
        }
    }

    public static void UpgradeAllWallsToLevel(int newLevel)
    {
        for (int i = AllWalls.Count - 1; i >= 0; i--)
        {
            if (AllWalls[i] != null)
            {
                AllWalls[i].ApplyLevel(newLevel);
            }
        }
    }

    // ================= NÂNG CẤP TỪNG ĐOẠN HOẶC CẢ HÀNG (CLASH OF CLANS STYLE) =================

    public int GetUpgradeCost()
    {
        if (currentLevel >= 6) return 0;
        if (GameEconomy.Instance != null)
        {
            return GameEconomy.Instance.GetWallUpgradeCost(currentLevel);
        }
        return 0;
    }

    public bool UpgradeSingle()
    {
        if (currentLevel >= 6) return false;

        int cost = GetUpgradeCost();
        if (cost <= 0 || (GameEconomy.Instance != null && GameEconomy.Instance.SpendCoins(cost)))
        {
            ApplyLevel(currentLevel + 1);
            TriggerUpgradePunch();
            return true;
        }
        else if (GameEconomy.Instance != null)
        {
            GameEconomy.Instance.TriggerNotEnoughCoins("Không đủ vàng để nâng cấp đoạn tường này!");
        }
        return false;
    }

    public void TriggerUpgradePunch(float delay = 0f)
    {
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(PunchScaleCoroutine(delay));
        }
    }

    private IEnumerator PunchScaleCoroutine(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (baseScale == Vector3.zero) baseScale = transform.localScale;
        Vector3 orig = baseScale;
        if (orig.x <= 1.05f && transform.localScale.x > 1.5f) orig = transform.localScale;
        if (orig == Vector3.one) orig = Vector3.one * 3f;

        float elapsed = 0f;
        float duration = 0.26f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Nảy nhẹ đàn hồi: nảy lên 1.25x rồi về 1.0x
            float bounce = 1f + Mathf.Sin(t * Mathf.PI) * 0.25f;
            transform.localScale = orig * bounce;
            yield return null;
        }

        transform.localScale = orig;
    }

    // Tìm tất cả các đoạn tường kết nối trong hàng theo phong cách Clash of Clans
    public List<WallSegment> GetConnectedRow(float maxDistance = 3.8f)
    {
        List<WallSegment> connected = new List<WallSegment>();
        Queue<WallSegment> queue = new Queue<WallSegment>();
        HashSet<WallSegment> visited = new HashSet<WallSegment>();

        queue.Enqueue(this);
        visited.Add(this);

        while (queue.Count > 0)
        {
            WallSegment cur = queue.Dequeue();
            connected.Add(cur);

            for (int i = 0; i < AllWalls.Count; i++)
            {
                WallSegment other = AllWalls[i];
                if (other == null || visited.Contains(other)) continue;

                float dist = Vector3.Distance(cur.transform.position, other.transform.position);
                if (dist <= maxDistance)
                {
                    visited.Add(other);
                    queue.Enqueue(other);
                }
            }
        }

        return connected;
    }

    public int CalculateRowUpgradeCost(List<WallSegment> row)
    {
        int total = 0;
        if (row == null) return 0;
        for (int i = 0; i < row.Count; i++)
        {
            if (row[i] != null && row[i].currentLevel < 6)
            {
                total += row[i].GetUpgradeCost();
            }
        }
        return total;
    }

    public bool UpgradeConnectedRow()
    {
        List<WallSegment> row = GetConnectedRow();
        List<WallSegment> upgradeable = new List<WallSegment>();
        for (int i = 0; i < row.Count; i++)
        {
            if (row[i] != null && row[i].currentLevel < 6)
            {
                upgradeable.Add(row[i]);
            }
        }

        if (upgradeable.Count == 0) return false;

        int totalCost = CalculateRowUpgradeCost(upgradeable);
        if (totalCost <= 0 || (GameEconomy.Instance != null && GameEconomy.Instance.SpendCoins(totalCost)))
        {
            for (int i = 0; i < upgradeable.Count; i++)
            {
                WallSegment wall = upgradeable[i];
                wall.ApplyLevel(wall.currentLevel + 1);
                wall.TriggerUpgradePunch(i * 0.04f); // Hiệu ứng sóng lan tỏa
            }
            return true;
        }
        else if (GameEconomy.Instance != null)
        {
            GameEconomy.Instance.TriggerNotEnoughCoins("Không đủ vàng để nâng cấp cả hàng tường!");
        }
        return false;
    }
}

