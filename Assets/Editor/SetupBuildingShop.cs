#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;

namespace IdleFactoryDefense.Editor
{
	public static class SetupBuildingShop
	{
		[MenuItem("Tools/🏪 Thiết Lập Cửa Hàng Xây Dựng (Pháo & Tường) 1-Click")]
		public static void SetupShop()
		{
			// 1. ĐẢM BẢO CÓ THƯ MỤC MATERIALS VÀ PREFABS
			if (!Directory.Exists("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
			if (!Directory.Exists("Assets/prefabs")) AssetDatabase.CreateFolder("Assets", "prefabs");

			// 2. TẠO MATERIAL HOLOGRAM XANH DƯƠNG
			Material holoMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Hologram_Blue.mat");
			if (holoMat == null)
			{
				Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
				if (shader == null) shader = Shader.Find("Unlit/Color");
				holoMat = new Material(shader);
				holoMat.color = new Color(0f, 0.75f, 1f, 0.65f); // Xanh dương neon công nghệ

				AssetDatabase.CreateAsset(holoMat, "Assets/Materials/Hologram_Blue.mat");
				AssetDatabase.SaveAssets();
			}

			// 3. TẠO PREFAB CHO Ụ PHÁO (LẤY TỪ SCENE ĐỂ GIỮ NGUYÊN CODE BẮN QUÁI)
			GameObject turretSceneObj = GameObject.Find("FlamethrowerTurret");
			GameObject turretPrefab = null;
			if (turretSceneObj != null)
			{
				turretPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(turretSceneObj, "Assets/prefabs/FlamethrowerTurret_Building.prefab", InteractionMode.AutomatedAction);
			}
			else
			{
				turretPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/FlamethrowerTurret_Building.prefab");
			}

			// 4. TẠO PREFAB CHO BỨC TƯỜNG (CÓ BOX COLLIDER)
			GameObject wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Wall_Building.prefab");
			if (wallPrefab == null)
			{
				// Tìm wall.fbx
				string[] guids = AssetDatabase.FindAssets("wall t:Model");
				string wallPath = "";
				foreach (string g in guids)
				{
					string p = AssetDatabase.GUIDToAssetPath(g);
					if (p.ToLower().EndsWith("/wall.fbx") || p.ToLower().EndsWith("\\wall.fbx"))
					{
						wallPath = p;
						break;
					}
				}

				if (!string.IsNullOrEmpty(wallPath))
				{
					GameObject wallModel = AssetDatabase.LoadAssetAtPath<GameObject>(wallPath);
					GameObject tempWall = Object.Instantiate(wallModel);
					tempWall.name = "Wall_Building";

					// Thêm BoxCollider nếu chưa có
					if (tempWall.GetComponentInChildren<Collider>() == null)
					{
						BoxCollider col = tempWall.AddComponent<BoxCollider>();
						Renderer r = tempWall.GetComponentInChildren<Renderer>();
						if (r != null)
						{
							col.center = tempWall.transform.InverseTransformPoint(r.bounds.center);
							col.size = r.bounds.size;
						}
					}

					wallPrefab = PrefabUtility.SaveAsPrefabAsset(tempWall, "Assets/prefabs/Wall_Building.prefab");
					Object.DestroyImmediate(tempWall);
				}
			}

			// 5. TẠO EVENTSYSTEM
			if (Object.FindAnyObjectByType<EventSystem>() == null)
			{
				GameObject es = new GameObject("EventSystem");
				es.AddComponent<EventSystem>();
				es.AddComponent<StandaloneInputModule>();
				Undo.RegisterCreatedObjectUndo(es, "Tạo EventSystem");
			}

			// 6. TẠO HOẶC LẤY CANVAS
			Canvas canvas = Object.FindAnyObjectByType<Canvas>();
			GameObject canvasObj = canvas != null ? canvas.gameObject : null;
			if (canvasObj == null)
			{
				canvasObj = new GameObject("Shop_Canvas");
				canvas = canvasObj.AddComponent<Canvas>();
				canvas.renderMode = RenderMode.ScreenSpaceOverlay;

				CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
				scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
				scaler.referenceResolution = new Vector2(1080, 1920);
				scaler.matchWidthOrHeight = 0.5f;

				canvasObj.AddComponent<GraphicRaycaster>();
				Undo.RegisterCreatedObjectUndo(canvasObj, "Tạo Shop_Canvas");
			}

			// 7. TẠO THANH CỬA HÀNG Ở ĐÁY MÀN HÌNH (THUMB ZONE)
			Transform oldShopBar = canvasObj.transform.Find("Building_Shop_Bar");
			if (oldShopBar != null) Object.DestroyImmediate(oldShopBar.gameObject);

			GameObject shopBar = new GameObject("Building_Shop_Bar");
			shopBar.transform.SetParent(canvasObj.transform, false);
			RectTransform barRect = shopBar.AddComponent<RectTransform>();
			barRect.anchorMin = new Vector2(0.5f, 0f);
			barRect.anchorMax = new Vector2(0.5f, 0f);
			barRect.pivot = new Vector2(0.5f, 0f);
			barRect.anchoredPosition = new Vector2(0f, 60f); // Cách đáy 60px
			barRect.sizeDelta = new Vector2(1000f, 180f);

			// Background thanh bar
			Image barBg = shopBar.AddComponent<Image>();
			barBg.color = new Color(0.1f, 0.12f, 0.16f, 0.85f); // Xám đen mờ bo viền

			HorizontalLayoutGroup layout = shopBar.AddComponent<HorizontalLayoutGroup>();
			layout.spacing = 25f;
			layout.padding = new RectOffset(20, 20, 20, 20);
			layout.childAlignment = TextAnchor.MiddleCenter;
			layout.childControlWidth = true;
			layout.childControlHeight = true;

			// TẠO NÚT BẤM
			Button btnTurret = CreateButton(shopBar.transform, "Btn_Turret", "🔫 Ụ PHÁO", new Color(0.85f, 0.3f, 0.2f));
			Button btnWall = CreateButton(shopBar.transform, "Btn_Wall", "🧱 BỨC TƯỜNG", new Color(0.2f, 0.6f, 0.85f));
			Button btnRotate = CreateButton(shopBar.transform, "Btn_Rotate", "🔄 XOAY", new Color(0.2f, 0.75f, 0.3f));
			Button btnCancel = CreateButton(shopBar.transform, "Btn_Cancel", "❌ HỦY", new Color(0.85f, 0.2f, 0.2f));

			btnRotate.gameObject.SetActive(false);
			btnCancel.gameObject.SetActive(false);

			// 8. TẠO HOẶC LẤY BUILDING SYSTEM TRONG SCENE
			BuildingSystem bSystem = Object.FindAnyObjectByType<BuildingSystem>();
			if (bSystem == null)
			{
				GameObject bManager = new GameObject("Building_Manager");
				bSystem = bManager.AddComponent<BuildingSystem>();
				Undo.RegisterCreatedObjectUndo(bManager, "Tạo Building_Manager");
			}

			bSystem.blueHologramMaterial = holoMat;
			bSystem.items = new BuildingSystem.PlaceableItem[2];

			// Item 1: Pháo
			bSystem.items[0] = new BuildingSystem.PlaceableItem
			{
				itemName = "Ụ Pháo Phòng Thủ",
				prefab = turretPrefab,
				cost = 50
			};

			// Item 2: Tường
			bSystem.items[1] = new BuildingSystem.PlaceableItem
			{
				itemName = "Bức Tường Kiên Cố",
				prefab = wallPrefab,
				cost = 10
			};

			// GẮN SCRIPT UI VÀ KẾT NỐI
			BuildingShopUI shopUI = shopBar.AddComponent<BuildingShopUI>();
			shopUI.buildingSystem = bSystem;
			shopUI.turretButton = btnTurret;
			shopUI.wallButton = btnWall;
			shopUI.rotateButton = btnRotate;
			shopUI.cancelButton = btnCancel;

			Selection.activeGameObject = shopBar;
			Debug.Log("<color=green><b>[SetupBuildingShop]</b> ĐÃ THIẾT LẬP CỬA HÀNG XÂY DỰNG THÀNH CÔNG!</color>");
			EditorUtility.DisplayDialog("Thành Công", "Đã thiết lập Cửa Hàng Xây Dựng thành công!\n\n1. Bấm nút Play.\n2. Bấm nút [Ụ PHÁO] hoặc [BỨC TƯỜNG] dưới đáy màn hình.\n3. Di chuột/chạm trên đất sẽ hiện hình bóng XANH DƯƠNG.\n4. Click chuột trái để đặt xuống (công trình thật sẽ đúng màu gốc 100%)!", "Tuyệt vời!");
		}

		static Button CreateButton(Transform parent, string name, string textContent, Color btnColor)
		{
			GameObject btnObj = new GameObject(name);
			btnObj.transform.SetParent(parent, false);

			Image img = btnObj.AddComponent<Image>();
			img.color = btnColor;

			Button btn = btnObj.AddComponent<Button>();
			ColorBlock cb = btn.colors;
			cb.highlightedColor = btnColor * 1.15f;
			cb.pressedColor = btnColor * 0.85f;
			btn.colors = cb;

			// Text
			GameObject textObj = new GameObject("Text");
			textObj.transform.SetParent(btnObj.transform, false);
			RectTransform textRect = textObj.AddComponent<RectTransform>();
			textRect.anchorMin = Vector2.zero;
			textRect.anchorMax = Vector2.one;
			textRect.sizeDelta = Vector2.zero;

			Text txt = textObj.AddComponent<Text>();
			txt.text = textContent;
			txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			txt.fontSize = 32;
			txt.fontStyle = FontStyle.Bold;
			txt.alignment = TextAnchor.MiddleCenter;
			txt.color = Color.white;

			// Thêm viền chữ cho nổi bật
			Outline outline = textObj.AddComponent<Outline>();
			outline.effectColor = Color.black;
			outline.effectDistance = new Vector2(2f, -2f);

			return btn;
		}
	}
}
#endif
