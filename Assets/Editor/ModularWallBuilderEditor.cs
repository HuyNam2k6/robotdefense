using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ModularWallBuilder))]
public class ModularWallBuilderEditor : Editor
{
	// MENU TẠO 1 CLICK: Bấm cái là tự sinh ra GameObject có sẵn mẫu tường, chỉ việc cầm kéo!
	[MenuItem("GameObject/3D Object/Modular Wall Builder", false, 10)]
	[MenuItem("Tools/Tạo Tường Tự Động (Wall Builder)")]
	public static void CreateWallBuilder()
	{
		GameObject go = new GameObject("Wall_Builder");
		Selection.activeGameObject = go;
		ModularWallBuilder builder = go.AddComponent<ModularWallBuilder>();

		// Tự tìm file wall.fbx trong project
		string[] guids = AssetDatabase.FindAssets("wall t:Model");
		foreach (string guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (path.ToLower().EndsWith("/wall.fbx") || path.ToLower().EndsWith("\\wall.fbx"))
			{
				builder.wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				break;
			}
		}

		// Tự tìm file wall-pillar.fbx nếu có
		string[] pillarGuids = AssetDatabase.FindAssets("wall-pillar t:Model");
		foreach (string guid in pillarGuids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (path.ToLower().EndsWith("/wall-pillar.fbx") || path.ToLower().EndsWith("\\wall-pillar.fbx"))
			{
				builder.pillarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				break;
			}
		}

		// Tự đo kích thước mẫu tường và sinh thử 1 đoạn mẫu
		builder.AutoDetectLength();
		builder.GenerateWalls();

		// Đặt vị trí ngay tại điểm đang nhìn trong Scene View cho tiện
		if (SceneView.lastActiveSceneView != null)
		{
			go.transform.position = SceneView.lastActiveSceneView.pivot;
		}

		Undo.RegisterCreatedObjectUndo(go, "Tạo Wall Builder");
	}

	void OnSceneGUI()
	{
		ModularWallBuilder builder = (ModularWallBuilder)target;
		if (builder == null) return;

		Transform t = builder.transform;
		Vector3 worldStart = t.position;
		Vector3 worldEnd = t.TransformPoint(builder.localEndPoint);

		// 1. Vẽ đường nét đứt nối từ điểm bắt đầu tới điểm kết thúc
		Handles.color = Color.yellow;
		Handles.DrawDottedLine(worldStart, worldEnd, 4f);

		// 2. Vẽ tay cầm điều khiển (Position Handle / Arrow) ở điểm cuối
		EditorGUI.BeginChangeCheck();
		Vector3 newWorldEnd = Handles.PositionHandle(worldEnd, t.rotation);

		// Vẽ thêm mũi tên to màu xanh lá chỉ hướng kéo cho dễ nhìn
		Handles.color = Color.green;
		Vector3 dir = (worldEnd - worldStart).normalized;
		if (dir != Vector3.zero)
		{
			Handles.ArrowHandleCap(0, worldEnd, Quaternion.LookRotation(dir), 1.5f, EventType.Repaint);
		}

		if (EditorGUI.EndChangeCheck())
		{
			Undo.RecordObject(builder, "Kéo Dài Tường");
			builder.localEndPoint = t.InverseTransformPoint(newWorldEnd);
			builder.GenerateWalls();
		}
	}

	public override void OnInspectorGUI()
	{
		ModularWallBuilder builder = (ModularWallBuilder)target;

		DrawDefaultInspector();

		EditorGUILayout.Space(10);
		EditorGUILayout.LabelField("--- CÔNG CỤ NHANH ---", EditorStyles.boldLabel);

		// Nút tự động đo kích thước
		if (GUILayout.Button("📏 Tự Động Đo Kích Thước Miếng Tường", GUILayout.Height(30)))
		{
			Undo.RecordObject(builder, "Tự đo kích thước tường");
			builder.AutoDetectLength();
		}

		// Nút tái tạo tường
		if (GUILayout.Button("🔄 Cập Nhật / Vẽ Lại Tường", GUILayout.Height(30)))
		{
			Undo.RecordObject(builder, "Vẽ lại tường");
			builder.GenerateWalls();
		}

		// Nút chốt tường
		GUI.backgroundColor = new Color(0.3f, 1f, 0.4f);
		if (GUILayout.Button("✅ Chốt Tường (Bake thành GameObject bình thường)", GUILayout.Height(35)))
		{
			if (EditorUtility.DisplayDialog("Xác nhận chốt tường", 
				"Các bức tường sẽ được giữ nguyên vĩnh viễn và gỡ bỏ công cụ kéo này ra. Bạn có muốn tiếp tục?", "Đồng ý", "Hủy"))
			{
				Undo.DestroyObjectImmediate(builder);
			}
		}

		GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
		if (GUILayout.Button("🗑️ Xóa Toàn Bộ Tường Này", GUILayout.Height(25)))
		{
			Undo.RecordObject(builder, "Xóa toàn bộ tường");
			builder.ClearChildren();
		}
		GUI.backgroundColor = Color.white;
	}
}
