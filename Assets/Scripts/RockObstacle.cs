using UnityEngine;

/// <summary>
/// Đảm bảo tất cả các tảng đá (Rocks) đều có Collider vật lý vững chắc và gán Tag "Rock",
/// ngăn người chơi và robot đi xuyên qua đá.
/// Tự động thêm MeshCollider (convex) hoặc BoxCollider.
/// Chạy cả trong Editor (ExecuteAlways) lẫn khi Play game.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class RockObstacle : MonoBehaviour
{
    [Header("Cài đặt")]
    [Tooltip("Tự động quét và gắn Collider cho toàn bộ đá con nếu đây là thư mục chứa đá")]
    public bool isParentContainer = true;

    void Awake()
    {
        EnsureColliders();
    }

    void Start()
    {
        EnsureColliders();
    }

    void OnEnable()
    {
        EnsureColliders();
    }

    [ContextMenu("Kích Hoạt & Tạo Collider Cho Đá")]
    public void EnsureColliders()
    {
        if (isParentContainer || (transform.childCount > 0 && GetComponent<MeshFilter>() == null && GetComponent<Collider>() == null))
        {
            // Quét tất cả các tảng đá con
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                SetupSingleRock(child.gameObject);
            }
        }
        else
        {
            SetupSingleRock(gameObject);
        }
    }

    public static void SetupSingleRock(GameObject rockObj)
    {
        if (rockObj == null) return;

        // 1. Tự động gán Tag "Rock" nếu Tag này đã được khai báo trong dự án
        try
        {
            if (rockObj.tag != "Rock")
            {
                rockObj.tag = "Rock";
            }
        }
        catch
        {
            // Bỏ qua nếu Tag "Rock" chưa được thêm vào TagManager
        }

        // 2. Kiểm tra xem đã có Collider nào trên GameObject hoặc các con chưa
        Collider existingCol = rockObj.GetComponentInChildren<Collider>(true);
        if (existingCol != null)
        {
            existingCol.enabled = true;
            return;
        }

        // 3. Nếu chưa có Collider, tìm MeshFilter để tạo MeshCollider
        MeshFilter mf = rockObj.GetComponentInChildren<MeshFilter>(true);
        if (mf != null && mf.sharedMesh != null)
        {
            MeshCollider mc = mf.gameObject.GetComponent<MeshCollider>();
            if (mc == null)
            {
                mc = mf.gameObject.AddComponent<MeshCollider>();
            }
            mc.sharedMesh = mf.sharedMesh;
            // Bật convex để hỗ trợ va chạm mượt mà với Trigger và CharacterController
            mc.convex = true;
            mc.enabled = true;
            return;
        }

        // 4. Fallback: Nếu không tìm thấy MeshFilter, tạo BoxCollider bọc quanh
        BoxCollider bc = rockObj.GetComponent<BoxCollider>();
        if (bc == null)
        {
            bc = rockObj.AddComponent<BoxCollider>();
        }
        bc.enabled = true;
    }
}
