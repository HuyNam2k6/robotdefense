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
			// 0. CẢNH BÁO NẾU ĐANG CHẠY PLAY MODE
			if (EditorApplication.isPlaying)
			{
				EditorUtility.DisplayDialog("Lưu ý quan trọng", 
					"Bạn đang bật nút Play (Play Mode)!\n\nVui lòng BẤM TẮT NÚT PLAY trước khi chạy thiết lập để giao diện và công trình được lưu vĩnh viễn vào Scene!", 
					"Đã hiểu");
				return;
			}

			int uiLayer = LayerMask.NameToLayer("UI");
			if (uiLayer < 0) uiLayer = 5;

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
				canvasObj.layer = uiLayer;
				canvas = canvasObj.AddComponent<Canvas>();
				canvas.renderMode = RenderMode.ScreenSpaceOverlay;

				CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
				scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
				scaler.referenceResolution = new Vector2(1080, 1920);
				scaler.matchWidthOrHeight = 0f; // Khớp theo bề ngang điện thoại/màn hình

				canvasObj.AddComponent<GraphicRaycaster>();
				Undo.RegisterCreatedObjectUndo(canvasObj, "Tạo Shop_Canvas");
			}
			else
			{
				canvasObj.layer = uiLayer;
			}

			// 7. DỌN DẸP GIAO DIỆN CŨ
			Transform oldShopBar = canvasObj.transform.Find("Building_Shop_Bar");
			if (oldShopBar != null) Object.DestroyImmediate(oldShopBar.gameObject);

			Transform oldHud = canvasObj.transform.Find("Placement_HUD");
			if (oldHud != null) Object.DestroyImmediate(oldHud.gameObject);

			Transform oldOpenBtn = canvasObj.transform.Find("Btn_OpenShop");
			if (oldOpenBtn != null) Object.DestroyImmediate(oldOpenBtn.gameObject);

			Transform oldShopPanel = canvasObj.transform.Find("Shop_Panel");
			if (oldShopPanel != null) Object.DestroyImmediate(oldShopPanel.gameObject);

			// --- A. NÚT HÌNH CỬA HÀNG TRÊN MÀN HÌNH CHÍNH ---
			GameObject openShopObj = new GameObject("Btn_OpenShop", typeof(RectTransform));
			openShopObj.layer = uiLayer;
			openShopObj.transform.SetParent(canvasObj.transform, false);

			RectTransform openBtnRect = openShopObj.GetComponent<RectTransform>();
			openBtnRect.anchorMin = new Vector2(0.5f, 0f);
			openBtnRect.anchorMax = new Vector2(0.5f, 0f);
			openBtnRect.pivot = new Vector2(0.5f, 0f);
			openBtnRect.anchoredPosition = new Vector2(0f, 50f);
			openBtnRect.sizeDelta = new Vector2(260f, 95f);

			Image openBtnImg = openShopObj.AddComponent<Image>();
			openBtnImg.color = new Color(0.95f, 0.55f, 0.15f); // Vàng cam nổi bật

			Button btnOpenShop = openShopObj.AddComponent<Button>();
			ColorBlock openCb = btnOpenShop.colors;
			openCb.highlightedColor = new Color(1f, 0.65f, 0.25f);
			openCb.pressedColor = new Color(0.8f, 0.45f, 0.1f);
			btnOpenShop.colors = openCb;

			GameObject openTextObj = new GameObject("Text", typeof(RectTransform));
			openTextObj.layer = uiLayer;
			openTextObj.transform.SetParent(openShopObj.transform, false);
			RectTransform openTextRect = openTextObj.GetComponent<RectTransform>();
			openTextRect.anchorMin = Vector2.zero;
			openTextRect.anchorMax = Vector2.one;
			openTextRect.sizeDelta = Vector2.zero;

			Text openTxt = openTextObj.AddComponent<Text>();
			openTxt.text = "🏪 <b>CỬA HÀNG</b>\n<color=#FFFAAA>🪙 0đ (Mua)</color>";
			openTxt.font = GetSafeFont();
			openTxt.fontSize = 22;
			openTxt.fontStyle = FontStyle.Bold;
			openTxt.alignment = TextAnchor.MiddleCenter;
			openTxt.color = Color.white;
			Outline openOutline = openTextObj.AddComponent<Outline>();
			openOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
			openOutline.effectDistance = new Vector2(1.5f, -1.5f);

			// --- B. BẢNG CỬA HÀNG POPUP (SHOP PANEL) ---
			GameObject shopPanelObj = new GameObject("Shop_Panel", typeof(RectTransform));
			shopPanelObj.layer = uiLayer;
			shopPanelObj.transform.SetParent(canvasObj.transform, false);

			RectTransform panelRect = shopPanelObj.GetComponent<RectTransform>();
			panelRect.anchorMin = new Vector2(0.5f, 0f);
			panelRect.anchorMax = new Vector2(0.5f, 0f);
			panelRect.pivot = new Vector2(0.5f, 0f);
			panelRect.anchoredPosition = new Vector2(0f, 60f);
			panelRect.sizeDelta = new Vector2(760f, 260f);

			Image panelBg = shopPanelObj.AddComponent<Image>();
			panelBg.color = new Color(0.08f, 0.11f, 0.17f, 0.96f); // Xám đen bóng sang trọng

			// Header của Panel
			GameObject pHeaderObj = new GameObject("Panel_Header", typeof(RectTransform));
			pHeaderObj.layer = uiLayer;
			pHeaderObj.transform.SetParent(shopPanelObj.transform, false);
			RectTransform pHeaderRect = pHeaderObj.GetComponent<RectTransform>();
			pHeaderRect.anchorMin = new Vector2(0f, 1f);
			pHeaderRect.anchorMax = new Vector2(1f, 1f);
			pHeaderRect.pivot = new Vector2(0.5f, 1f);
			pHeaderRect.anchoredPosition = new Vector2(0f, -8f);
			pHeaderRect.sizeDelta = new Vector2(0f, 36f);

			Text pHeaderText = pHeaderObj.AddComponent<Text>();
			pHeaderText.text = "🏪 CỬA HÀNG XÂY DỰNG  •  🪙 VÍ: 0đ";
			pHeaderText.font = GetSafeFont();
			pHeaderText.fontSize = 20;
			pHeaderText.fontStyle = FontStyle.Bold;
			pHeaderText.alignment = TextAnchor.MiddleCenter;
			pHeaderText.color = new Color(1f, 0.85f, 0.25f);

			// Nút đóng bảng (✕)
			Button btnCloseShop = CreateCircleIconButton(shopPanelObj.transform, "Btn_ClosePanel", "✕", new Vector2(340f, -12f), 45f, new Color(0.85f, 0.25f, 0.25f), uiLayer);

			// Container chứa 2 card (Ụ Pháo và Tường)
			GameObject cardContainer = new GameObject("Card_Container", typeof(RectTransform));
			cardContainer.layer = uiLayer;
			cardContainer.transform.SetParent(shopPanelObj.transform, false);
			RectTransform cardContRect = cardContainer.GetComponent<RectTransform>();
			cardContRect.anchorMin = new Vector2(0f, 0f);
			cardContRect.anchorMax = new Vector2(1f, 0.78f);
			cardContRect.offsetMin = new Vector2(20f, 15f);
			cardContRect.offsetMax = new Vector2(-20f, 0f);

			HorizontalLayoutGroup cardLayout = cardContainer.AddComponent<HorizontalLayoutGroup>();
			cardLayout.spacing = 30f;
			cardLayout.childAlignment = TextAnchor.MiddleCenter;
			cardLayout.childControlWidth = true;
			cardLayout.childControlHeight = true;
			cardLayout.childForceExpandWidth = false;
			cardLayout.childForceExpandHeight = false;

			Button btnTurret = CreateShopCard(cardContainer.transform, "Btn_Turret", "🔫", "Ụ PHÁO THỦ", "0🪙 (0đ)", new Color(0.85f, 0.32f, 0.22f), uiLayer);
			Button btnWall = CreateShopCard(cardContainer.transform, "Btn_Wall", "🧱", "BỨC TƯỜNG (KÉO)", "0🪙 (0đ)", new Color(0.2f, 0.65f, 0.95f), uiLayer);

			// Mặc định ban đầu bảng Shop ẩn, chỉ hiện nút Cửa Hàng
			shopPanelObj.SetActive(false);

			// --- C. BẢNG ĐIỀU KHIỂN NỔI KHI ĐANG ĐẶT (PLACEMENT HUD) ---
			GameObject hudObj = new GameObject("Placement_HUD", typeof(RectTransform));
			hudObj.layer = uiLayer;
			hudObj.transform.SetParent(canvasObj.transform, false);

			RectTransform hudRect = hudObj.GetComponent<RectTransform>();
			hudRect.anchorMin = new Vector2(0.5f, 0f);
			hudRect.anchorMax = new Vector2(0.5f, 0f);
			hudRect.pivot = new Vector2(0.5f, 0f);
			hudRect.anchoredPosition = new Vector2(0f, 200f);
			hudRect.sizeDelta = new Vector2(500f, 110f);

			GameObject tipObj = new GameObject("TipText", typeof(RectTransform));
			tipObj.layer = uiLayer;
			tipObj.transform.SetParent(hudObj.transform, false);
			RectTransform tipRect = tipObj.GetComponent<RectTransform>();
			tipRect.anchorMin = new Vector2(0f, 1f);
			tipRect.anchorMax = new Vector2(1f, 1f);
			tipRect.pivot = new Vector2(0.5f, 1f);
			tipRect.anchoredPosition = new Vector2(0f, 0f);
			tipRect.sizeDelta = new Vector2(500f, 30f);

			Text tipTxt = tipObj.AddComponent<Text>();
			tipTxt.text = "💡 Giữ & kéo chuột để kéo dài tường • Bấm ⟳ để xoay";
			tipTxt.font = GetSafeFont();
			tipTxt.fontSize = 18;
			tipTxt.alignment = TextAnchor.MiddleCenter;
			tipTxt.color = Color.white;
			Outline tipOutline = tipObj.AddComponent<Outline>();
			tipOutline.effectColor = Color.black;

			Button btnRotateIcon = CreateCircleIconButton(hudObj.transform, "Btn_RotateIcon", "⟳", new Vector2(-60f, -65f), 85f, new Color(0f, 0.78f, 0.65f), uiLayer);
			Button btnCancelIcon = CreateCircleIconButton(hudObj.transform, "Btn_CancelIcon", "✕", new Vector2(60f, -65f), 85f, new Color(0.9f, 0.22f, 0.22f), uiLayer);

			hudObj.SetActive(false);

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

			// Item 1: Pháo (Giá 0đ)
			bSystem.items[0] = new BuildingSystem.PlaceableItem
			{
				itemName = "Ụ Pháo Phòng Thủ",
				prefab = turretPrefab,
				cost = 0,
				isWall = false
			};

			// Item 2: Tường (Giá 0đ, Bật cơ chế kéo dài)
			bSystem.items[1] = new BuildingSystem.PlaceableItem
			{
				itemName = "Bức Tường Kiên Cố",
				prefab = wallPrefab,
				cost = 0,
				isWall = true,
				segmentLength = 1f
			};

			// GẮN SCRIPT UI VÀ KẾT NỐI
			BuildingShopUI shopUI = canvasObj.GetComponent<BuildingShopUI>();
			if (shopUI == null) shopUI = canvasObj.AddComponent<BuildingShopUI>();

			shopUI.buildingSystem = bSystem;
			shopUI.shopOpenButton = btnOpenShop;
			shopUI.shopPanel = shopPanelObj;
			shopUI.closeShopButton = btnCloseShop;
			shopUI.turretButton = btnTurret;
			shopUI.wallButton = btnWall;
			shopUI.placementHUD = hudObj;
			shopUI.rotateIconButton = btnRotateIcon;
			shopUI.cancelIconButton = btnCancelIcon;

			Selection.activeGameObject = openShopObj;
			UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

			Debug.Log("<color=green><b>[SetupBuildingShop]</b> ĐÃ THIẾT LẬP NÚT CỬA HÀNG VÀ BẢNG POPUP THÀNH CÔNG!</color>");
			EditorUtility.DisplayDialog("Thành Công", 
				"Đã nâng cấp Cửa Hàng Xây Dựng thành công!\n\n" +
				"✨ CƠ CHẾ HOẠT ĐỘNG MỚI:\n" +
				"1. Màn hình ban đầu: Chỉ hiện 1 nút [🏪 CỬA HÀNG] gọn gàng ở góc dưới.\n" +
				"2. Nhấn vào nút Cửa Hàng: Bảng popup sẽ mở ra hiển thị Ụ PHÁO và BỨC TƯỜNG (Giá: 0đ).\n" +
				"3. Nhấn vào Ụ Pháo hoặc Tường: Bảng tự đóng lại, bóng xanh Hologram xuất hiện để bạn kéo/đặt!\n" +
				"4. Khi đang đặt: Nổi lên nút tròn Biểu tượng xoay (⟳) và nút Hủy (✕).\n" +
				"5. Kéo dài tường: Giữ chuột và kéo để xây cả hàng tường cùng lúc!", 
				"Tuyệt vời!");
		}

		// Tạo thẻ món hàng trong shop (Card giao diện đẹp, giá 0đ)
		static Button CreateShopCard(Transform parent, string name, string iconEmoji, string title, string price, Color cardColor, int uiLayer)
		{
			GameObject cardObj = new GameObject(name, typeof(RectTransform));
			cardObj.layer = uiLayer;
			cardObj.transform.SetParent(parent, false);

			Image bgImg = cardObj.AddComponent<Image>();
			bgImg.color = cardColor;

			LayoutElement le = cardObj.AddComponent<LayoutElement>();
			le.preferredWidth = 260f;
			le.preferredHeight = 110f;
			le.minWidth = 200f;
			le.minHeight = 80f;

			Button btn = cardObj.AddComponent<Button>();
			ColorBlock cb = btn.colors;
			cb.highlightedColor = cardColor * 1.15f;
			cb.pressedColor = cardColor * 0.85f;
			btn.colors = cb;

			// Nội dung chữ bên trong card
			GameObject textObj = new GameObject("Text", typeof(RectTransform));
			textObj.layer = uiLayer;
			textObj.transform.SetParent(cardObj.transform, false);
			RectTransform textRect = textObj.GetComponent<RectTransform>();
			textRect.anchorMin = Vector2.zero;
			textRect.anchorMax = Vector2.one;
			textRect.sizeDelta = Vector2.zero;

			Text txt = textObj.AddComponent<Text>();
			txt.text = $"{iconEmoji} <b>{title}</b>\n<color=#FFD700>Giá: {price}</color>";
			txt.font = GetSafeFont();
			txt.fontSize = 22;
			txt.alignment = TextAnchor.MiddleCenter;
			txt.color = Color.white;

			Outline outline = textObj.AddComponent<Outline>();
			outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
			outline.effectDistance = new Vector2(1.5f, -1.5f);

			return btn;
		}

		// Tạo nút tròn có BIỂU TƯỢNG (chỉ icon, không có chữ thừa)
		static Button CreateCircleIconButton(Transform parent, string name, string iconSymbol, Vector2 anchoredPos, float size, Color btnColor, int uiLayer)
		{
			GameObject btnObj = new GameObject(name, typeof(RectTransform));
			btnObj.layer = uiLayer;
			btnObj.transform.SetParent(parent, false);

			RectTransform rt = btnObj.GetComponent<RectTransform>();
			rt.anchorMin = new Vector2(0.5f, 1f);
			rt.anchorMax = new Vector2(0.5f, 1f);
			rt.pivot = new Vector2(0.5f, 1f);
			rt.anchoredPosition = anchoredPos;
			rt.sizeDelta = new Vector2(size, size);

			Image img = btnObj.AddComponent<Image>();
			img.color = btnColor;

			Button btn = btnObj.AddComponent<Button>();
			ColorBlock cb = btn.colors;
			cb.highlightedColor = btnColor * 1.2f;
			cb.pressedColor = btnColor * 0.8f;
			btn.colors = cb;

			// Icon symbol bên trong (to, đậm, không chữ)
			GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
			iconObj.layer = uiLayer;
			iconObj.transform.SetParent(btnObj.transform, false);
			RectTransform iconRt = iconObj.GetComponent<RectTransform>();
			iconRt.anchorMin = Vector2.zero;
			iconRt.anchorMax = Vector2.one;
			iconRt.sizeDelta = Vector2.zero;

			Text txt = iconObj.AddComponent<Text>();
			txt.text = iconSymbol;
			txt.font = GetSafeFont();
			txt.fontSize = Mathf.RoundToInt(size * 0.55f); // Icon to chiếm 55% kích thước nút
			txt.fontStyle = FontStyle.Bold;
			txt.alignment = TextAnchor.MiddleCenter;
			txt.color = Color.white;

			Outline outline = iconObj.AddComponent<Outline>();
			outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
			outline.effectDistance = new Vector2(2f, -2f);

			return btn;
		}

		static Font GetSafeFont()
		{
			Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
			if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			if (font == null)
			{
				Font[] allFonts = Resources.FindObjectsOfTypeAll<Font>();
				if (allFonts != null && allFonts.Length > 0) font = allFonts[0];
			}
			return font;
		}
	}
}
#endif

