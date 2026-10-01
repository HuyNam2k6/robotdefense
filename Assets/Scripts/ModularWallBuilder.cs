using UnityEngine;

[ExecuteInEditMode]
public class ModularWallBuilder : MonoBehaviour
{
	[Header("--- Mẫu Tường ---")]
	[Tooltip("Kéo Prefab hoặc Model tường vào đây (Ví dụ: wall.fbx)")]
	public GameObject wallPrefab;

	[Tooltip("(Tùy chọn) Cột trụ đặt ở 2 đầu tường nếu muốn")]
	public GameObject pillarPrefab;

	[Header("--- Kích Thước & Điểm Kéo ---")]
	[Tooltip("Độ dài của một miếng tường (mét). Mặc định đồ Kenney là 1.0 hoặc 2.0 mét.")]
	public float segmentLength = 1f;

	[Tooltip("Vị trí điểm cuối của tường (bạn có thể kéo mũi tên trực tiếp trong Scene View).")]
	public Vector3 localEndPoint = new Vector3(0f, 0f, 5f);

	[Header("--- Cài Đặt Địa Hình ---")]
	[Tooltip("Tự động bám sát độ cao mặt đất / Terrain.")]
	public bool snapToGround = false;
	public LayerMask groundLayer = ~0;

	// Hàm tạo tường tự động
	public void GenerateWalls()
	{
		if (wallPrefab == null) return;

		// 1. Xóa các đoạn tường cũ
		ClearChildren();

		Vector3 startPos = transform.position;
		Vector3 endPos = transform.TransformPoint(localEndPoint);
		Vector3 direction = endPos - startPos;
		float totalDistance = direction.magnitude;

		if (totalDistance < 0.1f) return;

		direction.Normalize();
		Quaternion wallRotation = Quaternion.LookRotation(direction, Vector3.up);

		// 2. Tính số lượng miếng tường cần sinh ra
		int count = Mathf.Max(1, Mathf.RoundToInt(totalDistance / segmentLength));
		float actualStep = totalDistance / count;

		// 3. Đặt cột trụ đầu tiên nếu có
		if (pillarPrefab != null)
		{
			CreateSegment(pillarPrefab, startPos, wallRotation);
		}

		// 4. Sinh các đoạn tường nối tiếp nhau
		for (int i = 0; i < count; i++)
		{
			// Đặt ở giữa mỗi nhịp để tường khớp mép
			Vector3 pos = startPos + direction * (i * actualStep + actualStep * 0.5f);

			if (snapToGround)
			{
				if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 15f, groundLayer))
				{
					pos.y = hit.point.y;
				}
			}

			CreateSegment(wallPrefab, pos, wallRotation);
		}

		// 5. Đặt cột trụ cuối cùng nếu có
		if (pillarPrefab != null)
		{
			CreateSegment(pillarPrefab, endPos, wallRotation);
		}
	}

	void CreateSegment(GameObject prefab, Vector3 worldPos, Quaternion rotation)
	{
		GameObject obj = Instantiate(prefab, worldPos, rotation, transform);
		obj.name = prefab.name;
	}

	public void ClearChildren()
	{
		for (int i = transform.childCount - 1; i >= 0; i--)
		{
			DestroyImmediate(transform.GetChild(i).gameObject);
		}
	}

	// Tự động đo kích thước mẫu tường nếu người dùng không biết đặt segmentLength bao nhiêu
	public void AutoDetectLength()
	{
		if (wallPrefab == null) return;

		Renderer r = wallPrefab.GetComponentInChildren<Renderer>();
		if (r != null)
		{
			Vector3 size = r.bounds.size;
			// Lấy kích thước lớn nhất giữa X và Z
			float maxHorizontal = Mathf.Max(size.x, size.z);
			if (maxHorizontal > 0.1f)
			{
				segmentLength = Mathf.Round(maxHorizontal * 10f) / 10f;
				Debug.Log($"<color=green>[WallBuilder]</color> Đã tự động nhận diện chiều dài miếng tường: {segmentLength}m");
				GenerateWalls();
			}
		}
	}
}

