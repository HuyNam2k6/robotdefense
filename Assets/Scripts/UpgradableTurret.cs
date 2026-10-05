using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TurretType
{
    Thunder,            // Sấm Sét / Lightning Turret
    Phaotuhanh,         // Pháo Tự Hành MLRS / Rocket Launcher Turret
    FlamethrowerTurret  // Pháo Lửa / Flamethrower Turret
}

[DisallowMultipleComponent]
public class UpgradableTurret : MonoBehaviour
{
    public static readonly List<UpgradableTurret> AllTurrets = new List<UpgradableTurret>();

    [Header("--- Cấu Hình Trụ ---")]
    public TurretType turretType;
    public string turretDisplayName = "Trụ Phòng Thủ";

    [Header("--- Cấp Độ Hiện Tại (1 - 5) ---")]
    [Range(1, 5)]
    public int currentLevel = 1;
    public const int MaxLevel = 5;

    [Header("--- Chi Phí Nâng Cấp Từng Cấp (1->2, 2->3, 3->4, 4->5) ---")]
    public int[] upgradeCosts = new int[] { 60, 120, 220, 380 };

    [Header("--- Tham Chiếu Component ---")]
    public LightningTurret lightningTurret;
    public RocketLauncherTurret rocketLauncher;
    public TurretController turretController;
    public TurretWeapon turretWeapon;

    // Quản lý Renderers và Hiệu Ứng Nhấp Nháy Màu Xanh Dương <-> Màu Gốc
    private Renderer[] cachedRenderers;
    private MaterialPropertyBlock propBlock;
    private Vector3 baseScale = Vector3.zero;
    private bool isSelected = false;
    private Coroutine punchCoroutine;

    private struct RendererColorBackup
    {
        public Renderer renderer;
        public Color baseColor;
        public Color emissionColor;
    }
    private List<RendererColorBackup> colorBackups = new List<RendererColorBackup>();

    void OnEnable()
    {
        if (!AllTurrets.Contains(this)) AllTurrets.Add(this);
    }

    void OnDisable()
    {
        AllTurrets.Remove(this);
    }

    void Awake()
    {
        baseScale = transform.localScale;
        propBlock = new MaterialPropertyBlock();

        DetectAndInitTurretComponents();
        EnsureColliderExists();
        CacheRenderersAndColors();
    }

    void Start()
    {
        if (baseScale == Vector3.zero) baseScale = transform.localScale;
        ApplyLevel(currentLevel);
    }

    void Update()
    {
        // Hiệu ứng nhấp nháy màu xanh dương và trở về màu nguyên bản liên tục khi được chọn
        if (isSelected && colorBackups != null && colorBackups.Count > 0)
        {
            // Dao động hình sin êm ái: 0.0 (Màu nguyên bản) <---> 1.0 (Màu xanh dương)
            float t = (Mathf.Sin(Time.time * 6.5f) + 1f) * 0.5f;

            Color neonBlue = new Color(0.12f, 0.65f, 1.0f, 1f);
            Color blueEmission = new Color(0.15f, 0.75f, 2.2f, 1f) * t;

            for (int i = 0; i < colorBackups.Count; i++)
            {
                var b = colorBackups[i];
                if (b.renderer == null) continue;

                b.renderer.GetPropertyBlock(propBlock);

                // Pha trộn giữa màu gốc và màu xanh dương
                Color blendedColor = Color.Lerp(b.baseColor, neonBlue, t);
                propBlock.SetColor("_BaseColor", blendedColor);
                propBlock.SetColor("_Color", blendedColor);

                // Phát sáng màu xanh dương nhấp nháy
                propBlock.SetColor("_EmissionColor", blueEmission);

                b.renderer.SetPropertyBlock(propBlock);
            }
        }
    }

    /// <summary>
    /// Bật hoặc tắt trạng thái chọn trụ
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selected)
        {
            TriggerPunchHop();
        }
        else
        {
            ResetVisualsToOriginal();
        }
    }

    public void ResetVisualsToOriginal()
    {
        if (propBlock == null || colorBackups == null) return;

        for (int i = 0; i < colorBackups.Count; i++)
        {
            var b = colorBackups[i];
            if (b.renderer == null) continue;

            b.renderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", b.baseColor);
            propBlock.SetColor("_Color", b.baseColor);
            propBlock.SetColor("_EmissionColor", b.emissionColor);
            b.renderer.SetPropertyBlock(propBlock);
        }
    }

    private void DetectAndInitTurretComponents()
    {
        string n = gameObject.name.ToLower();

        lightningTurret = GetComponent<LightningTurret>() ?? GetComponentInChildren<LightningTurret>();
        rocketLauncher = GetComponent<RocketLauncherTurret>() ?? GetComponentInChildren<RocketLauncherTurret>();
        turretController = GetComponent<TurretController>() ?? GetComponentInChildren<TurretController>();
        turretWeapon = GetComponent<TurretWeapon>() ?? GetComponentInChildren<TurretWeapon>();

        if (lightningTurret != null || n.Contains("thunder") || n.Contains("lightn") || n.Contains("emp"))
        {
            turretType = TurretType.Thunder;
            turretDisplayName = "⚡ THUNDER (SẤM SÉT)";
            upgradeCosts = new int[] { 60, 120, 220, 380 };
            if (lightningTurret == null) lightningTurret = gameObject.AddComponent<LightningTurret>();
        }
        else if (rocketLauncher != null || n.Contains("phao") || n.Contains("missile") || n.Contains("rocket"))
        {
            turretType = TurretType.Phaotuhanh;
            turretDisplayName = "🚀 PHÁO TỰ HÀNH (MLRS)";
            upgradeCosts = new int[] { 80, 160, 280, 450 };
            if (rocketLauncher == null) rocketLauncher = gameObject.AddComponent<RocketLauncherTurret>();
        }
        else if (turretController != null || n.Contains("flame") || n.Contains("turret"))
        {
            turretType = TurretType.FlamethrowerTurret;
            turretDisplayName = "🔥 FLAMETHROWER TURRET";
            upgradeCosts = new int[] { 50, 110, 200, 350 };
            if (turretController == null) turretController = gameObject.AddComponent<TurretController>();
            if (turretWeapon == null) turretWeapon = GetComponentInChildren<TurretWeapon>();
        }
    }

    private void CacheRenderersAndColors()
    {
        colorBackups.Clear();
        cachedRenderers = GetComponentsInChildren<Renderer>(true);

        foreach (var r in cachedRenderers)
        {
            if (r == null || r is LineRenderer || r is ParticleSystemRenderer) continue;

            RendererColorBackup backup = new RendererColorBackup();
            backup.renderer = r;

            if (r.sharedMaterial != null)
            {
                backup.baseColor = r.sharedMaterial.HasProperty("_BaseColor") ? r.sharedMaterial.GetColor("_BaseColor") :
                                   (r.sharedMaterial.HasProperty("_Color") ? r.sharedMaterial.GetColor("_Color") : Color.white);
                backup.emissionColor = r.sharedMaterial.HasProperty("_EmissionColor") ? r.sharedMaterial.GetColor("_EmissionColor") : Color.black;
            }
            else
            {
                backup.baseColor = Color.white;
                backup.emissionColor = Color.black;
            }

            colorBackups.Add(backup);
        }
    }

    /// <summary>
    /// Đảm bảo trụ luôn có Collider để người chơi click raycast chọn được
    /// </summary>
    public void EnsureColliderExists()
    {
        Collider existingCol = GetComponentInChildren<Collider>();
        if (existingCol == null)
        {
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            Renderer[] rends = GetComponentsInChildren<Renderer>();
            if (rends != null && rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++)
                {
                    if (rends[i] is LineRenderer || rends[i] is ParticleSystemRenderer) continue;
                    b.Encapsulate(rends[i].bounds);
                }

                col.center = transform.InverseTransformPoint(b.center);
                Vector3 worldSize = b.size;
                col.size = new Vector3(
                    Mathf.Max(1.6f, worldSize.x / Mathf.Max(0.01f, transform.lossyScale.x)),
                    Mathf.Max(2.2f, worldSize.y / Mathf.Max(0.01f, transform.lossyScale.y)),
                    Mathf.Max(1.6f, worldSize.z / Mathf.Max(0.01f, transform.lossyScale.z))
                );
            }
            else
            {
                col.center = new Vector3(0f, 1.5f, 0f);
                col.size = new Vector3(2.5f, 3.2f, 2.5f);
            }
        }
    }

    public int GetUpgradeCost()
    {
        if (currentLevel >= MaxLevel) return 0;
        int idx = currentLevel - 1;
        if (upgradeCosts != null && idx >= 0 && idx < upgradeCosts.Length)
        {
            return upgradeCosts[idx];
        }
        return 100;
    }

    /// <summary>
    /// Nâng cấp trụ lên cấp kế tiếp
    /// </summary>
    public bool Upgrade()
    {
        if (currentLevel >= MaxLevel) return false;

        int cost = GetUpgradeCost();
        if (cost <= 0 || (GameEconomy.Instance != null && GameEconomy.Instance.SpendCoins(cost)))
        {
            currentLevel++;
            ApplyLevel(currentLevel);
            TriggerPunchHop();
            Debug.Log($"<color=#00FFFF>[Nâng Cấp]</color> Đã nâng cấp thành công {turretDisplayName} lên Cấp {currentLevel}!");
            return true;
        }
        else if (GameEconomy.Instance != null)
        {
            GameEconomy.Instance.TriggerNotEnoughCoins($"Không đủ vàng để nâng cấp {turretDisplayName}!");
        }

        return false;
    }

    public void ApplyLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 1, MaxLevel);

        switch (turretType)
        {
            case TurretType.Thunder:
                if (lightningTurret != null)
                {
                    lightningTurret.damagePerSecond = 15f + (currentLevel - 1) * 20f; // 15, 35, 55, 75, 100
                    lightningTurret.attackRange = 15f + (currentLevel - 1) * 2.5f;     // 15, 17.5, 20, 22.5, 25
                    lightningTurret.lightningWidth = 0.2f + (currentLevel - 1) * 0.05f;
                }
                break;

            case TurretType.Phaotuhanh:
                if (rocketLauncher != null)
                {
                    rocketLauncher.rocketsPerVolley = 16 + (currentLevel - 1) * 6;     // 16, 22, 28, 34, 40
                    rocketLauncher.fireCooldown = Mathf.Max(0.9f, 2.0f - (currentLevel - 1) * 0.25f); // 2.0s -> 1.0s
                    rocketLauncher.attackRange = 30f + (currentLevel - 1) * 4f;        // 30, 34, 38, 42, 46
                    
                    // Use reflection to bypass CS1061 in case RocketLauncherTurret is reverted
                    var field = rocketLauncher.GetType().GetField("rocketDamage");
                    if (field != null)
                    {
                        field.SetValue(rocketLauncher, 25f + (currentLevel - 1) * 15f);
                    }
                }
                break;

            case TurretType.FlamethrowerTurret:
                if (turretController != null)
                {
                    turretController.attackRange = 25f + (currentLevel - 1) * 4f;     // 25, 29, 33, 37, 41
                }
                if (turretWeapon != null)
                {
                    turretWeapon.cooldown = Mathf.Max(0.2f, 0.65f - (currentLevel - 1) * 0.1f); // 0.65s -> 0.25s
                    turretWeapon.bulletDamage = 15 + (currentLevel - 1) * 20;          // 15, 35, 55, 75, 95
                }
                break;
        }
    }

    public string GetStatsDescription()
    {
        switch (turretType)
        {
            case TurretType.Thunder:
                float curDps = 15f + (currentLevel - 1) * 20f;
                float curRange = 15f + (currentLevel - 1) * 2.5f;
                if (currentLevel < MaxLevel)
                {
                    float nextDps = 15f + currentLevel * 20f;
                    float nextRange = 15f + currentLevel * 2.5f;
                    return $"⚡ DPS: <b>{curDps}</b> ➔ <color=#00FFFF><b>{nextDps}</b></color>  |  Tầm: <b>{curRange}m</b> ➔ <color=#00FFFF><b>{nextRange}m</b></color>";
                }
                return $"⚡ DPS: <b>{curDps}</b> (TỐI ĐA)  |  Tầm Bắn: <b>{curRange}m</b>";

            case TurretType.Phaotuhanh:
                int curRockets = 16 + (currentLevel - 1) * 6;
                float curCd = Mathf.Max(0.9f, 2.0f - (currentLevel - 1) * 0.25f);
                if (currentLevel < MaxLevel)
                {
                    int nextRockets = 16 + currentLevel * 6;
                    float nextCd = Mathf.Max(0.9f, 2.0f - currentLevel * 0.25f);
                    return $"🚀 Loạt: <b>{curRockets} quả</b> ➔ <color=#00FFFF><b>{nextRockets} quả</b></color>  |  Nạp: <b>{curCd:F1}s</b> ➔ <color=#00FFFF><b>{nextCd:F1}s</b></color>";
                }
                return $"🚀 Loạt Bắn: <b>{curRockets} quả</b> (TỐI ĐA)  |  Nạp: <b>{curCd:F1}s</b>";

            case TurretType.FlamethrowerTurret:
                int curDmg = 15 + (currentLevel - 1) * 20;
                float curFireCd = Mathf.Max(0.2f, 0.65f - (currentLevel - 1) * 0.1f);
                if (currentLevel < MaxLevel)
                {
                    int nextDmg = 15 + currentLevel * 20;
                    float nextFireCd = Mathf.Max(0.2f, 0.65f - currentLevel * 0.1f);
                    return $"🔥 Sát Thương: <b>{curDmg}</b> ➔ <color=#00FFFF><b>{nextDmg}</b></color>  |  Tốc Độ: <b>{curFireCd:F2}s</b> ➔ <color=#00FFFF><b>{nextFireCd:F2}s</b></color>";
                }
                return $"🔥 Sát Thương: <b>{curDmg}</b> (TỐI ĐA)  |  Tốc Độ: <b>{curFireCd:F2}s</b>";

            default:
                return $"Cấp độ: {currentLevel}";
        }
    }

    public Vector3 GetMarkerPosition()
    {
        Collider c = GetComponentInChildren<Collider>();
        if (c != null)
        {
            return new Vector3(c.bounds.center.x, c.bounds.max.y + 0.6f, c.bounds.center.z);
        }
        return transform.position + Vector3.up * 2.8f;
    }

    public void TriggerPunchHop()
    {
        if (gameObject.activeInHierarchy)
        {
            if (punchCoroutine != null) StopCoroutine(punchCoroutine);
            punchCoroutine = StartCoroutine(DoPunchHop());
        }
    }

    private IEnumerator DoPunchHop()
    {
        if (baseScale == Vector3.zero) baseScale = transform.localScale;
        Vector3 orig = baseScale;

        float elapsed = 0f;
        float duration = 0.22f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float bounce = 1f + Mathf.Sin(t * Mathf.PI) * 0.18f;
            transform.localScale = orig * bounce;
            yield return null;
        }

        transform.localScale = orig;
        punchCoroutine = null;
    }

    // ================= TỰ ĐỘNG GẮN COMPONENT KHI CHẠY SCENE =================
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void AutoSetupAllTurretsInScene()
    {
        // 1. Quét tìm theo script
        LightningTurret[] lTurrets = Object.FindObjectsByType<LightningTurret>(FindObjectsInactive.Include);
        foreach (var lt in lTurrets)
        {
            if (lt.GetComponent<UpgradableTurret>() == null)
            {
                lt.gameObject.AddComponent<UpgradableTurret>();
            }
        }

        RocketLauncherTurret[] rTurrets = Object.FindObjectsByType<RocketLauncherTurret>(FindObjectsInactive.Include);
        foreach (var rt in rTurrets)
        {
            if (rt.GetComponent<UpgradableTurret>() == null)
            {
                rt.gameObject.AddComponent<UpgradableTurret>();
            }
        }

        TurretController[] tControllers = Object.FindObjectsByType<TurretController>(FindObjectsInactive.Include);
        foreach (var tc in tControllers)
        {
            if (tc.GetComponent<UpgradableTurret>() == null)
            {
                tc.gameObject.AddComponent<UpgradableTurret>();
            }
        }

        // 2. Quét tìm theo tên đối tượng cụ thể theo yêu cầu: thunder, phaotuhanh, FlamethrowerTurret
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var obj in allObjects)
        {
            string n = obj.name.ToLower();
            if (n == "thunder" || n.StartsWith("thunder") || 
                n == "phaotuhanhl" || n.StartsWith("phaotu") || 
                n.Contains("flamethrowerturret"))
            {
                if (obj.GetComponent<UpgradableTurret>() == null)
                {
                    obj.AddComponent<UpgradableTurret>();
                }
            }
        }
    }
}

// Trigger refresh
