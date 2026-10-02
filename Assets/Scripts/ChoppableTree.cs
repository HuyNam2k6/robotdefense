using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script gắn vào các cây trên đảo (Pines, Cypress, Conifer...),
/// cho phép người chơi và robot chặt cây trong tương lai khai thác gỗ (Wood).
/// Hỗ trợ:
/// - Rung lắc cây khi bị chặt (Shake Effect)
/// - Đổ cây khi hết máu và thưởng thêm gỗ (Fell Sequence)
/// - Tự động mọc lại sau thời gian chỉ định (Respawn System)
/// - Danh sách cây tĩnh (ActiveTrees) giúp Robot chặt gỗ tìm mục tiêu gần nhất cực nhanh.
/// </summary>
[ExecuteAlways]
public class ChoppableTree : MonoBehaviour
{
    public static readonly List<ChoppableTree> ActiveTrees = new List<ChoppableTree>();

    [Header("--- Quản Lý Nhóm Cây ---")]
    [Tooltip("Đánh dấu nếu đây là thư mục cha chứa danh sách cây")]
    public bool isParentGroup = false;

    [Header("--- Máu & Khai Thác Cây ---")]
    [Tooltip("Số nhát chặt cần để đốn hạ hoàn toàn cây")]
    public int maxHits = 3;
    public int currentHits = 0;

    [Tooltip("Số gỗ nhận được mỗi nhát bổ")]
    public int woodPerHit = 1;

    [Tooltip("Số gỗ thưởng thêm khi cây đổ hoàn toàn")]
    public int bonusWoodOnFelled = 2;

    [Header("--- Hồi Sinh (Respawn) ---")]
    [Tooltip("Thời gian cây tự mọc lại sau khi bị đốn hạ (giây)")]
    public float respawnTime = 30f;

    [Header("--- Trạng Thái ---")]
    public bool isFelled = false;

    // Biến lưu trạng thái gốc để phục hồi
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    private Vector3 originalLocalScale;
    private Collider treeCollider;
    private List<Renderer> renderers = new List<Renderer>();
    private Coroutine shakeCoroutine;

    public bool CanBeChopped => !isFelled && gameObject.activeInHierarchy;

    void Awake()
    {
        if (isParentGroup || (transform.childCount > 0 && GetComponent<Renderer>() == null && GetComponent<Collider>() == null))
        {
            SetupChildrenTrees();
            return;
        }

        originalLocalPos = transform.localPosition;
        originalLocalRot = transform.localRotation;
        originalLocalScale = transform.localScale;

        // Đảm bảo cây có Collider vững chắc
        treeCollider = GetComponent<Collider>();
        if (treeCollider == null)
        {
            treeCollider = GetComponentInChildren<Collider>(true);
        }

        if (treeCollider != null)
        {
            treeCollider.enabled = true;
        }

        renderers.Clear();
        renderers.AddRange(GetComponentsInChildren<Renderer>(true));
    }

    [ContextMenu("Kích Hoạt Choppable Cho Toàn Bộ Cây Con")]
    public void SetupChildrenTrees()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            ChoppableTree ct = child.GetComponent<ChoppableTree>();
            if (ct == null)
            {
                ct = child.gameObject.AddComponent<ChoppableTree>();
            }
            ct.isParentGroup = false;
        }
    }

    void OnEnable()
    {
        if (!ActiveTrees.Contains(this))
            ActiveTrees.Add(this);
    }

    void OnDisable()
    {
        ActiveTrees.Remove(this);
    }

    /// <summary>
    /// Hàm gọi khi người chơi hoặc Robot bổ rìu chặt cây.
    /// </summary>
    /// <param name="hits">Số nhát chặt</param>
    /// <param name="chopper">Transform của người hoặc robot chặt</param>
    /// <returns>True nếu chặt thành công</returns>
    public bool Chop(int hits = 1, Transform chopper = null)
    {
        if (isFelled) return false;

        currentHits += hits;

        // Thêm gỗ vào hệ thống kinh tế
        int woodGained = woodPerHit * hits;
        if (GameEconomy.Instance != null)
        {
            GameEconomy.Instance.AddWood(woodGained);
        }

        // Hướng rung lắc cây dựa theo vị trí người chặt
        Vector3 hitDir = Vector3.forward;
        if (chopper != null)
        {
            hitDir = (transform.position - chopper.position).normalized;
            hitDir.y = 0f;
        }

        // Hiệu ứng rung lắc thân cây
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeEffect(hitDir));

        Debug.Log($"<color=#7CFC00>🪓 [Chặt Cây] {gameObject.name}: Nhát {currentHits}/{maxHits}! +{woodGained} Gỗ (Tổng gỗ: {GameEconomy.Instance?.woodCount ?? 0})</color>");

        // Nếu đã đủ số nhát -> Cây ngã đổ
        if (currentHits >= maxHits)
        {
            StartCoroutine(FellAndRespawnSequence(hitDir));
        }

        return true;
    }

    /// <summary>
    /// Hiệu ứng rung lắc thân cây khi bị rìu bổ trúng
    /// </summary>
    private IEnumerator ShakeEffect(Vector3 hitDir)
    {
        float duration = 0.22f;
        float elapsed = 0f;
        float shakeAngle = 4.5f;

        Vector3 axis = Vector3.Cross(hitDir.normalized, Vector3.up).normalized;
        if (axis == Vector3.zero) axis = Vector3.right;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;
            float damping = 1f - percent;
            float angle = Mathf.Sin(percent * Mathf.PI * 6f) * shakeAngle * damping;

            transform.localRotation = originalLocalRot * Quaternion.AngleAxis(angle, axis);
            yield return null;
        }

        transform.localRotation = originalLocalRot;
        shakeCoroutine = null;
    }

    /// <summary>
    /// Quá trình cây đổ xuống đất, thưởng gỗ, biến mất rồi mọc lại
    /// </summary>
    private IEnumerator FellAndRespawnSequence(Vector3 fallDir)
    {
        isFelled = true;

        // Thưởng thêm gỗ khi hạ gục hoàn toàn cây
        if (bonusWoodOnFelled > 0 && GameEconomy.Instance != null)
        {
            GameEconomy.Instance.AddWood(bonusWoodOnFelled);
            Debug.Log($"<color=#32CD32>🌲 [Cây Đổ] Thưởng thêm +{bonusWoodOnFelled} Gỗ! Tổng gỗ: {GameEconomy.Instance.woodCount}</color>");
        }

        // Tắt collider để không cản đường người chơi / robot khi cây đã đổ
        if (treeCollider != null) treeCollider.enabled = false;

        // 1. Hiệu ứng ngã đổ nghiêng 75 độ
        Vector3 fallAxis = Vector3.Cross(fallDir.normalized, Vector3.up).normalized;
        if (fallAxis == Vector3.zero) fallAxis = Vector3.right;

        float fallDuration = 0.85f;
        float fallElapsed = 0f;
        Quaternion startRot = transform.localRotation;
        Quaternion targetRot = originalLocalRot * Quaternion.AngleAxis(75f, fallAxis);

        while (fallElapsed < fallDuration)
        {
            fallElapsed += Time.deltaTime;
            float t = fallElapsed / fallDuration;
            // Easing in expo để cây rơi nhanh dần
            float ease = t * t;
            transform.localRotation = Quaternion.Slerp(startRot, targetRot, ease);
            yield return null;
        }

        // 2. Thu nhỏ dần và ẩn cây
        float shrinkDuration = 0.45f;
        float shrinkElapsed = 0f;
        Vector3 curScale = transform.localScale;

        while (shrinkElapsed < shrinkDuration)
        {
            shrinkElapsed += Time.deltaTime;
            float t = shrinkElapsed / shrinkDuration;
            transform.localScale = Vector3.Lerp(curScale, Vector3.zero, t);
            yield return null;
        }

        SetRenderersVisible(false);
        transform.localPosition = originalLocalPos;
        transform.localRotation = originalLocalRot;
        transform.localScale = originalLocalScale;

        // 3. Đợi thời gian hồi sinh
        yield return new WaitForSeconds(respawnTime);

        // 4. Mọc lại từ lòng đất (Grow animation)
        SetRenderersVisible(true);
        transform.localScale = Vector3.zero;

        float growDuration = 1.2f;
        float growElapsed = 0f;

        while (growElapsed < growDuration)
        {
            growElapsed += Time.deltaTime;
            float t = growElapsed / growDuration;
            // Back out bounce effect
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float bounce = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);

            transform.localScale = originalLocalScale * Mathf.Clamp01(bounce);
            yield return null;
        }

        transform.localScale = originalLocalScale;
        transform.localRotation = originalLocalRot;
        transform.localPosition = originalLocalPos;

        // Phục hồi collider và máu cây
        if (treeCollider != null) treeCollider.enabled = true;
        currentHits = 0;
        isFelled = false;

        Debug.Log($"<color=#00FFFF>🌱 [Cây Hồi Sinh] Cây {gameObject.name} đã mọc lại xanh tốt, sẵn sàng cho khai thác!</color>");
    }

    private void SetRenderersVisible(bool visible)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] != null) renderers[i].enabled = visible;
        }
    }

    /// <summary>
    /// Hàm tiện ích tĩnh: Tìm cây gần nhất còn sống để Robot chặt gỗ tự tìm đường tới
    /// </summary>
    public static ChoppableTree FindClosestChoppableTree(Vector3 fromPosition, float maxRadius = 150f)
    {
        ChoppableTree best = null;
        float bestDistSqr = maxRadius * maxRadius;

        for (int i = 0; i < ActiveTrees.Count; i++)
        {
            ChoppableTree tree = ActiveTrees[i];
            if (tree != null && tree.CanBeChopped)
            {
                float distSqr = (tree.transform.position - fromPosition).sqrMagnitude;
                if (distSqr < bestDistSqr)
                {
                    bestDistSqr = distSqr;
                    best = tree;
                }
            }
        }

        return best;
    }
}
