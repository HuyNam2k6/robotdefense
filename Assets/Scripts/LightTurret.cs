using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LightTurret - Trụ Tháp Đèn Pha Chiếu Sáng Đêm:
/// 1. Tự động gắn hoặc tạo 1 nguồn đèn Spotlight chuyên dụng rọi sáng mặt đất quanh căn cứ.
/// 2. Ban ngày: Tắt đèn pha để tiết kiệm năng lượng.
/// 3. Ban đêm: Đèn bật sáng rực rỡ, tự động xoay quét tuần tra (Sweep / Searchlight) hỗ trợ quan sát.
/// 4. ĐỘC LẬP 100%: Nguồn sáng Spotlight này là đèn cục bộ gắn tại trụ, 
///    tuyệt đối KHÔNG làm thay đổi Directional Light hay Ambient của toàn bộ môi trường ngoài trời.
/// </summary>
[DisallowMultipleComponent]
public class LightTurret : MonoBehaviour
{
    public static readonly List<LightTurret> AllLightTurrets = new List<LightTurret>();

    [Header("=== CẤU HÌNH ĐÈN PHA (SPOTLIGHT) ===")]
    [Tooltip("Nguồn sáng rọi Spotlight chuyên dụng")]
    public Light spotLight;

    [Tooltip("Góc rọi sáng của đèn pha (độ)")]
    public float spotAngle = 55f;

    [Tooltip("Tầm rọi sáng của đèn pha (mét)")]
    public float lightRange = 25f;

    [Tooltip("Cường độ ánh sáng ban đêm")]
    public float nightIntensity = 3.8f;

    [Tooltip("Màu ánh sáng đèn pha (Vàng ấm)")]
    public Color lightColor = new Color(1.0f, 0.94f, 0.82f);

    [Header("=== CƠ CHẾ QUÉT TUẦN TRA (SEARCHLIGHT SWEEP) ===")]
    [Tooltip("Có tự động xoay quét qua lại không")]
    public bool autoSweep = true;

    [Tooltip("Tốc độ quét tuần tra")]
    public float sweepSpeed = 1.2f;

    [Tooltip("Góc quét tuần tra tối đa (độ)")]
    public float sweepAngle = 45f;

    private Quaternion baseLocalRotation;
    private float sweepTimer = 0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        // Quét tìm các đối tượng LightTurret hoặc có tên lightturret / lightturent
        if (FindFirstObjectByType<LightTurret>() == null)
        {
            GameObject[] allGos = FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
            for (int i = 0; i < allGos.Length; i++)
            {
                string n = allGos[i].name.ToLower();
                if (n.Contains("lightturret") || n.Contains("lightturent") || n.Contains("searchlight"))
                {
                    allGos[i].AddComponent<LightTurret>();
                    Debug.Log("<color=#00FF88>[LightTurret]</color> Đã tự động kích hoạt LightTurret trên: " + allGos[i].name);
                    return;
                }
            }

            // Tự động tạo 1 trụ đèn pha tuần tra độc lập tại khu vực phòng thủ căn cứ
            GameObject defaultTower = new GameObject("[Base_Light_Searchlight]");
            Vector3 basePos = BaseHQ.Instance != null ? BaseHQ.Instance.transform.position : new Vector3(0f, 9.6f, -31.5f);
            defaultTower.transform.position = basePos + new Vector3(0f, 0f, 3.5f);
            defaultTower.AddComponent<LightTurret>();
            Debug.Log("<color=#00FF88>[LightTurret]</color> Đã tự động tạo Trụ Đèn Pha độc lập [Base_Light_Searchlight] tại căn cứ để soi sáng chiến trường ban đêm!");
        }
    }

    private void OnEnable()
    {
        if (!AllLightTurrets.Contains(this))
        {
            AllLightTurrets.Add(this);
        }
    }

    private void OnDisable()
    {
        AllLightTurrets.Remove(this);
    }

    private void Awake()
    {
        EnsureSpotLightExists();
        baseLocalRotation = transform.localRotation;
    }

    private void Start()
    {
        EnsureSpotLightExists();
        UpdateLightState(true);
    }

    private void Update()
    {
        UpdateLightState(false);

        // Quét tuần tra trong đêm
        if (autoSweep && spotLight != null && spotLight.enabled && spotLight.intensity > 0.1f)
        {
            sweepTimer += Time.deltaTime * sweepSpeed;
            float currentAngle = Mathf.Sin(sweepTimer) * sweepAngle;
            if (spotLight != null)
            {
                spotLight.transform.localRotation = Quaternion.Euler(35f, currentAngle, 0f);
            }
        }
    }

    /// <summary>
    /// Đảm bảo luôn có 1 nguồn sáng Spotlight cục bộ gắn tại trụ đèn
    /// </summary>
    public void EnsureSpotLightExists()
    {
        if (spotLight != null) return;

        // Quét tìm Light dạng Spot con cháu
        Light[] childLights = GetComponentsInChildren<Light>(true);
        for (int i = 0; i < childLights.Length; i++)
        {
            if (childLights[i].type == LightType.Spot)
            {
                spotLight = childLights[i];
                break;
            }
        }

        if (spotLight == null)
        {
            GameObject lightObj = new GameObject("[Turret_Searchlight_Spot]");
            lightObj.transform.SetParent(transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 2.8f, 0.4f);
            lightObj.transform.localRotation = Quaternion.Euler(35f, 0f, 0f); // Chiếu chúc xuống mặt đất

            spotLight = lightObj.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.spotAngle = spotAngle;
            spotLight.innerSpotAngle = spotAngle * 0.7f;
            spotLight.range = lightRange;
            spotLight.color = lightColor;
            spotLight.shadows = LightShadows.Soft;
            spotLight.shadowStrength = 0.5f;
            spotLight.intensity = 0f; // Khởi tạo ban đầu
        }
    }

    /// <summary>
    /// Cập nhật trạng thái đèn: Ban đêm bật sáng rọi, Ban ngày tắt để tiết kiệm năng lượng
    /// </summary>
    private void UpdateLightState(bool instant)
    {
        if (spotLight == null) return;

        bool isNight = DayNightSystem.Instance != null && DayNightSystem.Instance.isNight;
        float targetIntensity = isNight ? nightIntensity : 0f;

        if (instant)
        {
            spotLight.intensity = targetIntensity;
            spotLight.enabled = isNight;
        }
        else
        {
            spotLight.intensity = Mathf.MoveTowards(spotLight.intensity, targetIntensity, Time.deltaTime * 3.5f);
            if (spotLight.intensity <= 0.01f && !isNight)
            {
                spotLight.enabled = false;
            }
            else if (isNight && !spotLight.enabled)
            {
                spotLight.enabled = true;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.95f, 0.5f, 0.6f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.8f, 0.5f);
    }
}
