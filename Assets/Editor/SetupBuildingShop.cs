#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;

namespace IdleFactoryDefense.Editor
{
	[InitializeOnLoad]
	public static class SetupBuildingShop
	{
		static SetupBuildingShop()
		{
			EditorApplication.delayCall += () =>
			{
				CheckAndAutoSetup();
			};
		}

		public static void CheckAndAutoSetup()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode) return;

			// Kiểm tra nếu còn UI cũ hoặc chưa có nút Xây Nhà Máy
			bool hasOldUI = GameObject.Find("Building_Shop_Bar") != null || GameObject.Find("Btn_OpenShop") != null || GameObject.Find("Shop_Panel") != null;
			bool missingFactoryBtn = GameObject.Find("Btn_BuildFactory") == null;
			if (hasOldUI || missingFactoryBtn)
			{
				SetupShop();
			}
		}

		[MenuItem("Tools/🛒 Cập Nhật Cửa Hàng (Xây Nhà Máy & Chế Tạo Robot 15s)")]
		public static void SetupShop()
		{
			if (EditorApplication.isPlaying)
			{
				EditorUtility.DisplayDialog("Lưu ý quan trọng", 
					"Bạn đang bật Play Mode!\nVui lòng TẮT PLAY MODE trước khi chạy để lưu vĩnh viễn vào Scene!", 
					"Đã hiểu");
				return;
			}

			int uiLayer = LayerMask.NameToLayer("UI");
			if (uiLayer < 0) uiLayer = 5;

			// 1. Đảm bảo có thư mục
			if (!Directory.Exists("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
			if (!Directory.Exists("Assets/prefabs")) AssetDatabase.CreateFolder("Assets", "prefabs");

			// 2. Tạo Material Hologram
			Material holoMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Hologram_Blue.mat");
			if (holoMat == null)
			{
				Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
				holoMat = new Material(shader);
				holoMat.color = new Color(0f, 0.75f, 1f, 0.65f);
				AssetDatabase.CreateAsset(holoMat, "Assets/Materials/Hologram_Blue.mat");
				AssetDatabase.SaveAssets();
			}

			// 3. TẠO HOẶC LẤY PREFAB TƯỜNG (CÓ WALLSEGMENT & BOXCOLLIDER)
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

					if (tempWall.GetComponent<WallSegment>() == null)
					{
						tempWall.AddComponent<WallSegment>();
					}

					wallPrefab = PrefabUtility.SaveAsPrefabAsset(tempWall, "Assets/prefabs/Wall_Building.prefab");
					Object.DestroyImmediate(tempWall);
				}
			}
			else
			{
				// Đảm bảo prefab đã có WallSegment
				GameObject root = PrefabUtility.LoadPrefabContents("Assets/prefabs/Wall_Building.prefab");
				if (root.GetComponent<WallSegment>() == null)
				{
					root.AddComponent<WallSegment>();
					PrefabUtility.SaveAsPrefabAsset(root, "Assets/prefabs/Wall_Building.prefab");
				}
				PrefabUtility.UnloadPrefabContents(root);
			}

			// 4. Tạo hoặc nạp Turret Prefab & Factory Prefab
			GameObject turretPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/FlamethrowerTurret_Building.prefab");
			GameObject factoryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/RobotFactory_Building.prefab");

			// 5. TẠO GAMEECONOMY TRONG SCENE
			GameEconomy economy = Object.FindAnyObjectByType<GameEconomy>();
			if (economy == null)
			{
				GameObject ecoObj = new GameObject("Game_Economy");
				economy = ecoObj.AddComponent<GameEconomy>();
				Undo.RegisterCreatedObjectUndo(ecoObj, "Tạo Game_Economy");
			}

			// 6. Gán prefab Robot cho GameEconomy từ thư mục prefabsbot
			GameObject bipedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabsbot/BipedRobot_Prefab.prefab");
			if (bipedPrefab == null)
				bipedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/BipedRobot/BipedRobot_Prefab.prefab");
			if (bipedPrefab != null)
			{
				economy.workerRobotPrefab = bipedPrefab;
			}

			// 7. TẠO HOẶC CẬP NHẬT BUILDINGSYSTEM
			BuildingSystem buildingSys = Object.FindAnyObjectByType<BuildingSystem>();
			if (buildingSys == null)
			{
				GameObject bsObj = new GameObject("Building_Manager");
				buildingSys = bsObj.AddComponent<BuildingSystem>();
				Undo.RegisterCreatedObjectUndo(bsObj, "Tạo Building_Manager");
			}

			buildingSys.blueHologramMaterial = holoMat;
			buildingSys.items = new BuildingSystem.PlaceableItem[3];

			// Item 0: Pháo
			buildingSys.items[0] = new BuildingSystem.PlaceableItem
			{
				itemName = "Ụ Pháo Thủ",
				prefab = turretPrefab,
				cost = 0,
				isWall = false,
				segmentLength = 1f
			};

			// Item 1: Tường (kéo dài)
			buildingSys.items[1] = new BuildingSystem.PlaceableItem
			{
				itemName = "Bức Tường",
				prefab = wallPrefab,
				cost = 0,
				isWall = true,
				segmentLength = 1.0f
			};

			// Item 2: Nhà Máy Sản Xuất Robot
			buildingSys.items[2] = new BuildingSystem.PlaceableItem
			{
				itemName = "Nhà Máy Robot",
				prefab = factoryPrefab,
				cost = 0,
				isWall = false,
				segmentLength = 1f
			};

			// 8. TẠO EVENTSYSTEM
			if (Object.FindAnyObjectByType<EventSystem>() == null)
			{
				GameObject es = new GameObject("EventSystem");
				es.AddComponent<EventSystem>();
				es.AddComponent<StandaloneInputModule>();
				Undo.RegisterCreatedObjectUndo(es, "Tạo EventSystem");
			}

			// 9. TẠO CANVAS
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
				scaler.matchWidthOrHeight = 0f;

				canvasObj.AddComponent<GraphicRaycaster>();
				Undo.RegisterCreatedObjectUndo(canvasObj, "Tạo Shop_Canvas");
			}
			else
			{
				canvasObj.layer = uiLayer;
			}

			// Đảm bảo Canvas luôn có scale chuẩn (1, 1, 1)
			canvasObj.transform.localScale = Vector3.one;
			RectTransform crt = canvasObj.GetComponent<RectTransform>();
			if (crt != null) crt.localScale = Vector3.one;

			// 10. DỌN DẸP CÁC PHẦN TỬ CŨ TRÊN CANVAS VÀ TOÀN SCENE
			string[] oldNames = { "Shop_Panel", "Btn_OpenShop", "Placement_HUD", "Banner_NotEnoughCoins", "Building_Shop_Bar", "Btn_Turret", "Btn_Wall", "Btn_ShoppingCart", "Dashboard_Panel" };
			foreach (var name in oldNames)
			{
				Transform t = canvasObj.transform.Find(name);
				if (t != null) Object.DestroyImmediate(t.gameObject);
			}

			// Quét toàn bộ Scene để loại bỏ tận gốc bất kỳ UI cũ nào
			foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
			{
				if (t != null && (t.name == "Building_Shop_Bar" || t.name == "Btn_OpenShop" || t.name == "Shop_Panel"))
				{
					Object.DestroyImmediate(t.gameObject);
				}
			}

			Font safeFont = GetSafeFont();

			// ================= A. NÚT ICON GIỎ HÀNG (SHOPPING CART) Ở GÓC PHẢI =================
			// Theo yêu cầu: Nhấn vào icon hình giỏ hàng thì mới hiện bảng cửa hàng dashboard
			GameObject cartBtnObj = new GameObject("Btn_ShoppingCart", typeof(RectTransform));
			cartBtnObj.layer = uiLayer;
			cartBtnObj.transform.SetParent(canvasObj.transform, false);

			RectTransform cartBtnRect = cartBtnObj.GetComponent<RectTransform>();
			cartBtnRect.anchorMin = new Vector2(1f, 0f);
			cartBtnRect.anchorMax = new Vector2(1f, 0f);
			cartBtnRect.pivot = new Vector2(1f, 0f);
			cartBtnRect.anchoredPosition = new Vector2(-30f, 35f);
			cartBtnRect.sizeDelta = new Vector2(115f, 115f);

			Image cartBtnImg = cartBtnObj.AddComponent<Image>();
			cartBtnImg.color = new Color(0.12f, 0.16f, 0.26f, 0.96f);

			Outline cartBtnOutline = cartBtnObj.AddComponent<Outline>();
			cartBtnOutline.effectColor = new Color(1f, 0.82f, 0.15f, 1f);
			cartBtnOutline.effectDistance = new Vector2(2.5f, -2.5f);

			Button btnCart = cartBtnObj.AddComponent<Button>();

			GameObject cartTxtObj = new GameObject("Text", typeof(RectTransform));
			cartTxtObj.layer = uiLayer;
			cartTxtObj.transform.SetParent(cartBtnObj.transform, false);
			RectTransform cartTxtRect = cartTxtObj.GetComponent<RectTransform>();
			cartTxtRect.anchorMin = Vector2.zero;
			cartTxtRect.anchorMax = Vector2.one;
			cartTxtRect.sizeDelta = Vector2.zero;

			Text cartTxt = cartTxtObj.AddComponent<Text>();
			cartTxt.text = "<size=44>🛒</size>\n<size=15><b>CỬA HÀNG</b></size>";
			cartTxt.font = safeFont;
			cartTxt.fontSize = 18;
			cartTxt.alignment = TextAnchor.MiddleCenter;
			cartTxt.color = new Color(1f, 0.88f, 0.25f);

			// ================= B. BẢNG CỬA HÀNG DASHBOARD DẠNG NGANG (HORIZONTAL DASHBOARD) =================
			// Theo yêu cầu: Cửa hàng ở dạng dashboard ngang, chỉ hiện khi bấm vào icon giỏ hàng
			GameObject dashboardObj = new GameObject("Dashboard_Panel", typeof(RectTransform));
			dashboardObj.layer = uiLayer;
			dashboardObj.transform.SetParent(canvasObj.transform, false);

			RectTransform dashRect = dashboardObj.GetComponent<RectTransform>();
			dashRect.anchorMin = new Vector2(0f, 0f);
			dashRect.anchorMax = new Vector2(1f, 0f);
			dashRect.pivot = new Vector2(0.5f, 0f);
			dashRect.anchoredPosition = new Vector2(0f, 15f);
			dashRect.sizeDelta = new Vector2(-30f, 340f); // Trải ngang đáy màn hình, cao 340px

			Image dashBg = dashboardObj.AddComponent<Image>();
			dashBg.color = new Color(0.08f, 0.10f, 0.16f, 0.97f);

			Outline dashOutline = dashboardObj.AddComponent<Outline>();
			dashOutline.effectColor = new Color(0.2f, 0.75f, 1.0f, 0.65f);
			dashOutline.effectDistance = new Vector2(2f, -2f);

			// 1. Header Bar của Dashboard (Tiêu đề + Tiền tệ + Nút đóng)
			GameObject dHeader = new GameObject("Header_Bar", typeof(RectTransform));
			dHeader.layer = uiLayer;
			dHeader.transform.SetParent(dashboardObj.transform, false);
			RectTransform dHeaderRect = dHeader.GetComponent<RectTransform>();
			dHeaderRect.anchorMin = new Vector2(0f, 1f);
			dHeaderRect.anchorMax = new Vector2(1f, 1f);
			dHeaderRect.pivot = new Vector2(0.5f, 1f);
			dHeaderRect.anchoredPosition = new Vector2(0f, -8f);
			dHeaderRect.sizeDelta = new Vector2(-20f, 50f);

			// Tiêu đề Dashboard
			GameObject titleObj = new GameObject("Title_Text", typeof(RectTransform));
			titleObj.layer = uiLayer;
			titleObj.transform.SetParent(dHeader.transform, false);
			RectTransform titleRect = titleObj.GetComponent<RectTransform>();
			titleRect.anchorMin = new Vector2(0f, 0f);
			titleRect.anchorMax = new Vector2(0.42f, 1f);
			titleRect.sizeDelta = Vector2.zero;
			Text titleText = titleObj.AddComponent<Text>();
			titleText.text = "🛒 <b>CỬA HÀNG ĐIỀU HÀNH</b>";
			titleText.font = safeFont;
			titleText.fontSize = 20;
			titleText.fontStyle = FontStyle.Bold;
			titleText.alignment = TextAnchor.MiddleLeft;
			titleText.color = new Color(1f, 0.85f, 0.25f);

			// Khu vực hiển thị Tiền Tệ & Tài Nguyên
			GameObject resAreaObj = new GameObject("Resource_Area", typeof(RectTransform));
			resAreaObj.layer = uiLayer;
			resAreaObj.transform.SetParent(dHeader.transform, false);
			RectTransform resAreaRect = resAreaObj.GetComponent<RectTransform>();
			resAreaRect.anchorMin = new Vector2(0.42f, 0f);
			resAreaRect.anchorMax = new Vector2(0.88f, 1f);
			resAreaRect.sizeDelta = Vector2.zero;

			HorizontalLayoutGroup resLayout = resAreaObj.AddComponent<HorizontalLayoutGroup>();
			resLayout.childAlignment = TextAnchor.MiddleCenter;
			resLayout.spacing = 20f;
			resLayout.childForceExpandWidth = true;

			// Text Vàng
			GameObject coinTxtObj = new GameObject("Coin_Text", typeof(RectTransform));
			coinTxtObj.layer = uiLayer;
			coinTxtObj.transform.SetParent(resAreaObj.transform, false);
			Text coinText = coinTxtObj.AddComponent<Text>();
			coinText.text = "🪙 <b>0</b> Vàng";
			coinText.font = safeFont;
			coinText.fontSize = 18;
			coinText.alignment = TextAnchor.MiddleCenter;
			coinText.color = new Color(1f, 0.88f, 0.2f);

			// Text Đá
			GameObject stoneTxtObj = new GameObject("Stone_Text", typeof(RectTransform));
			stoneTxtObj.layer = uiLayer;
			stoneTxtObj.transform.SetParent(resAreaObj.transform, false);
			Text stoneText = stoneTxtObj.AddComponent<Text>();
			stoneText.text = "🪨 <b>0</b> Đá";
			stoneText.font = safeFont;
			stoneText.fontSize = 18;
			stoneText.alignment = TextAnchor.MiddleCenter;
			stoneText.color = new Color(0.6f, 0.85f, 1f);

			// Nút Đóng ✕
			GameObject closeBtnObj = new GameObject("Btn_Close", typeof(RectTransform));
			closeBtnObj.layer = uiLayer;
			closeBtnObj.transform.SetParent(dHeader.transform, false);
			RectTransform closeRect = closeBtnObj.GetComponent<RectTransform>();
			closeRect.anchorMin = new Vector2(1f, 0.5f);
			closeRect.anchorMax = new Vector2(1f, 0.5f);
			closeRect.pivot = new Vector2(1f, 0.5f);
			closeRect.anchoredPosition = new Vector2(0f, 0f);
			closeRect.sizeDelta = new Vector2(46f, 44f);

			Image closeImg = closeBtnObj.AddComponent<Image>();
			closeImg.color = new Color(0.85f, 0.2f, 0.2f, 0.95f);
			Button btnClose = closeBtnObj.AddComponent<Button>();

			GameObject closeTxtObj = new GameObject("Text", typeof(RectTransform));
			closeTxtObj.layer = uiLayer;
			closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
			RectTransform closeTxtRect = closeTxtObj.GetComponent<RectTransform>();
			closeTxtRect.anchorMin = Vector2.zero;
			closeTxtRect.anchorMax = Vector2.one;
			closeTxtRect.sizeDelta = Vector2.zero;
			Text closeTxt = closeTxtObj.AddComponent<Text>();
			closeTxt.text = "✕";
			closeTxt.font = safeFont;
			closeTxt.fontSize = 24;
			closeTxt.fontStyle = FontStyle.Bold;
			closeTxt.alignment = TextAnchor.MiddleCenter;
			closeTxt.color = Color.white;

			// 2. Nội dung Dashboard: 3 Khối Thẻ Nằm Ngang (Horizontal Layout)
			GameObject cardsRowObj = new GameObject("Cards_Row", typeof(RectTransform));
			cardsRowObj.layer = uiLayer;
			cardsRowObj.transform.SetParent(dashboardObj.transform, false);
			RectTransform crRect = cardsRowObj.GetComponent<RectTransform>();
			crRect.anchorMin = new Vector2(0f, 0f);
			crRect.anchorMax = new Vector2(1f, 1f);
			crRect.offsetMin = new Vector2(15f, 12f);
			crRect.offsetMax = new Vector2(-15f, -62f);

			HorizontalLayoutGroup rowLayout = cardsRowObj.AddComponent<HorizontalLayoutGroup>();
			rowLayout.spacing = 14f;
			rowLayout.childAlignment = TextAnchor.MiddleCenter;
			rowLayout.childControlWidth = true;
			rowLayout.childControlHeight = true;
			rowLayout.childForceExpandWidth = true;
			rowLayout.childForceExpandHeight = true;

			// ----- CARD 1: ROBOT ĐÀO & CÚP SẮT -----
			GameObject card1 = CreateDashboardCard(cardsRowObj.transform, "Card_Mining", "⛏️ KHAI THÁC & THỢ MỎ", new Color(0.11f, 0.15f, 0.21f, 0.95f), new Color(0.2f, 0.6f, 0.5f, 0.5f), safeFont, uiLayer);
			Transform card1Content = card1.transform.Find("Content");
			Button btnBuyWorker = CreateShopItemButton(card1Content, "Btn_BuyWorker", "🤖 <b>MUA ROBOT ĐÀO</b>\n<color=#FFD700>🪙 0 Vàng</color>", new Color(0.16f, 0.42f, 0.26f), safeFont, uiLayer);
			Text buyWorkerTxt = btnBuyWorker.GetComponentInChildren<Text>();

			Button btnUpgradePickaxe = CreateShopItemButton(card1Content, "Btn_UpgradePickaxe", "⛏️ <b>CÚP SẮT (CẤP 1)</b>\n<color=#FFD700>⭐ Nâng Cấp: 0🪙</color>", new Color(0.20f, 0.36f, 0.52f), safeFont, uiLayer);
			Text upgradePickaxeTxt = btnUpgradePickaxe.GetComponentInChildren<Text>();

			// ----- CARD 2: PHÒNG THỦ & NHÀ MÁY ROBOT -----
			GameObject card2 = CreateDashboardCard(cardsRowObj.transform, "Card_Defense", "🛡️ PHÒNG THỦ & NHÀ MÁY", new Color(0.15f, 0.12f, 0.16f, 0.95f), new Color(0.6f, 0.3f, 0.45f, 0.5f), safeFont, uiLayer);
			Transform card2Content = card2.transform.Find("Content");

			// Nút 1: Xây Nhà Máy Robot
			Button btnBuildFactory = CreateShopItemButton(card2Content, "Btn_BuildFactory", "🏭 <b>XÂY NHÀ MÁY ROBOT</b>\n<color=#FFD700>🪙 0 Vàng (Bấm Đặt)</color>", new Color(0.20f, 0.45f, 0.35f), safeFont, uiLayer);
			Text buildFactoryTxt = btnBuildFactory.GetComponentInChildren<Text>();

			// Nút 2: Chế Tạo Robot (15s)
			Button btnCraftRobot = CreateShopItemButton(card2Content, "Btn_CraftRobot", "🤖 <b>CHẾ TẠO ROBOT (15s)</b>\n<color=#00FFFF>Stan/Mike/George/Leela</color>", new Color(0.45f, 0.25f, 0.55f), safeFont, uiLayer);
			Text craftRobotTxt = btnCraftRobot.GetComponentInChildren<Text>();

			// ----- CARD 3: TƯỜNG -----
			GameObject card3 = CreateDashboardCard(cardsRowObj.transform, "Card_Wall", "🧱 BỨC TƯỜNG", new Color(0.18f, 0.14f, 0.10f, 0.95f), new Color(0.75f, 0.5f, 0.2f, 0.5f), safeFont, uiLayer);
			Transform card3Content = card3.transform.Find("Content");
			Button btnWall = CreateShopItemButton(card3Content, "Btn_Wall", "🧱 <b>MUA TƯỜNG (KÉO DÀI)</b>\n<color=#FFD700>🪙 0 Vàng/Đoạn</color>", new Color(0.2f, 0.52f, 0.72f), safeFont, uiLayer);
			Text wallTxt = btnWall.GetComponentInChildren<Text>();

			Button btnUpgradeWall = CreateShopItemButton(card3Content, "Btn_UpgradeWall", "⭐ <b>NÂNG CẤP TƯỜNG</b>\n<color=#00FFFF>✦ Bạc Sáng</color> • <color=#FFD700>0🪙</color>", new Color(0.55f, 0.35f, 0.15f), safeFont, uiLayer);
			Text upgradeWallTxt = btnUpgradeWall.GetComponentInChildren<Text>();

			// ================= C. BANNER CẢNH BÁO "KHÔNG ĐỦ VÀNG" =================
			GameObject warnBannerObj = new GameObject("Banner_NotEnoughCoins", typeof(RectTransform));
			warnBannerObj.layer = uiLayer;
			warnBannerObj.transform.SetParent(canvasObj.transform, false);

			RectTransform warnRect = warnBannerObj.GetComponent<RectTransform>();
			warnRect.anchorMin = new Vector2(0.5f, 0.75f);
			warnRect.anchorMax = new Vector2(0.5f, 0.75f);
			warnRect.pivot = new Vector2(0.5f, 0.5f);
			warnRect.anchoredPosition = Vector2.zero;
			warnRect.sizeDelta = new Vector2(520f, 85f);

			Image warnBg = warnBannerObj.AddComponent<Image>();
			warnBg.color = new Color(0.85f, 0.12f, 0.12f, 0.92f);

			Outline warnOutline = warnBannerObj.AddComponent<Outline>();
			warnOutline.effectColor = new Color(1f, 0.85f, 0.1f, 0.95f);
			warnOutline.effectDistance = new Vector2(2f, -2f);

			GameObject warnTxtObj = new GameObject("Text", typeof(RectTransform));
			warnTxtObj.layer = uiLayer;
			warnTxtObj.transform.SetParent(warnBannerObj.transform, false);
			RectTransform wtRect = warnTxtObj.GetComponent<RectTransform>();
			wtRect.anchorMin = Vector2.zero;
			wtRect.anchorMax = Vector2.one;
			wtRect.sizeDelta = Vector2.zero;

			Text warnTxt = warnTxtObj.AddComponent<Text>();
			warnTxt.text = "⚠️ <b>KHÔNG ĐỦ VÀNG!</b>";
			warnTxt.font = safeFont;
			warnTxt.fontSize = 24;
			warnTxt.fontStyle = FontStyle.Bold;
			warnTxt.alignment = TextAnchor.MiddleCenter;
			warnTxt.color = Color.white;

			warnBannerObj.SetActive(false);

			// ================= D. HUD NỔI KHI ĐANG KÉO ĐẶT =================
			GameObject hudObj = new GameObject("Placement_HUD", typeof(RectTransform));
			hudObj.layer = uiLayer;
			hudObj.transform.SetParent(canvasObj.transform, false);

			RectTransform hudRect = hudObj.GetComponent<RectTransform>();
			hudRect.anchorMin = new Vector2(0.5f, 0f);
			hudRect.anchorMax = new Vector2(0.5f, 0f);
			hudRect.pivot = new Vector2(0.5f, 0f);
			hudRect.anchoredPosition = new Vector2(0f, 160f);
			hudRect.sizeDelta = new Vector2(450f, 90f);

			Image hudBg = hudObj.AddComponent<Image>();
			hudBg.color = new Color(0.1f, 0.12f, 0.18f, 0.92f);

			HorizontalLayoutGroup hudLayout = hudObj.AddComponent<HorizontalLayoutGroup>();
			hudLayout.spacing = 20f;
			hudLayout.childAlignment = TextAnchor.MiddleCenter;
			hudLayout.childControlWidth = false;
			hudLayout.childControlHeight = false;

			Button btnRotate = CreateHUDButton(hudObj.transform, "Btn_Rotate", "⟳ XOAY", new Color(0.2f, 0.6f, 0.85f), safeFont, uiLayer);
			Button btnCancel = CreateHUDButton(hudObj.transform, "Btn_Cancel", "✕ HỦY", new Color(0.85f, 0.25f, 0.25f), safeFont, uiLayer);

			hudObj.SetActive(false);

			// Mặc định ban đầu: Ẩn Dashboard, hiện Icon Giỏ Hàng theo yêu cầu!
			dashboardObj.SetActive(false);
			cartBtnObj.SetActive(true);

			// ================= E. GẮN COMPONENT BUILDINGSHOPUI =================
			BuildingShopUI shopUI = canvasObj.GetComponent<BuildingShopUI>();
			if (shopUI == null) shopUI = canvasObj.AddComponent<BuildingShopUI>();

			shopUI.buildingSystem = buildingSys;
			shopUI.shoppingCartButton = btnCart;
			shopUI.dashboardPanel = dashboardObj;
			shopUI.closeDashboardButton = btnClose;

			shopUI.coinText = coinText;
			shopUI.stoneText = stoneText;

			shopUI.buyWorkerButton = btnBuyWorker;
			shopUI.buyWorkerText = buyWorkerTxt;
			shopUI.upgradePickaxeButton = btnUpgradePickaxe;
			shopUI.upgradePickaxeText = upgradePickaxeTxt;

			shopUI.buildFactoryButton = btnBuildFactory;
			shopUI.buildFactoryText = buildFactoryTxt;
			shopUI.craftRobotButton = btnCraftRobot;
			shopUI.craftRobotText = craftRobotTxt;
			shopUI.defenseRobotButton = btnCraftRobot;
			shopUI.defenseRobotText = craftRobotTxt;

			shopUI.wallButton = btnWall;
			shopUI.wallButtonText = wallTxt;
			shopUI.upgradeWallButton = btnUpgradeWall;
			shopUI.upgradeWallText = upgradeWallTxt;

			shopUI.notEnoughCoinsBanner = warnBannerObj;
			shopUI.notEnoughCoinsText = warnTxt;

			shopUI.placementHUD = hudObj;
			shopUI.rotateIconButton = btnRotate;
			shopUI.cancelIconButton = btnCancel;

			WallSelectionManager wallSelectMgr = canvasObj.GetComponent<WallSelectionManager>();
			if (wallSelectMgr == null) wallSelectMgr = canvasObj.AddComponent<WallSelectionManager>();
			wallSelectMgr.buildingSystem = buildingSys;
			wallSelectMgr.shopUI = shopUI;

			EditorUtility.SetDirty(canvasObj);
			EditorUtility.SetDirty(buildingSys);
			EditorUtility.SetDirty(economy);

			var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
			if (activeScene.isLoaded)
			{
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
				UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
			}
			AssetDatabase.SaveAssets();

			Debug.Log("<color=#00FF88><b>[SetupShop]</b> ĐÃ THIẾT LẬP CỬA HÀNG DASHBOARD NGANG & ICON GIỎ HÀNG THÀNH CÔNG!</color>");
		}

		private static GameObject CreateDashboardCard(Transform parent, string name, string title, Color bgColor, Color outlineColor, Font font, int layer)
		{
			GameObject card = new GameObject(name, typeof(RectTransform));
			card.layer = layer;
			card.transform.SetParent(parent, false);

			Image bg = card.AddComponent<Image>();
			bg.color = bgColor;

			Outline outline = card.AddComponent<Outline>();
			outline.effectColor = outlineColor;
			outline.effectDistance = new Vector2(1.5f, -1.5f);

			VerticalLayoutGroup vLayout = card.AddComponent<VerticalLayoutGroup>();
			vLayout.padding = new RectOffset(8, 8, 8, 8);
			vLayout.spacing = 8f;
			vLayout.childAlignment = TextAnchor.UpperCenter;
			vLayout.childControlWidth = true;
			vLayout.childControlHeight = true;
			vLayout.childForceExpandWidth = true;
			vLayout.childForceExpandHeight = false;

			GameObject headerObj = new GameObject("Title", typeof(RectTransform));
			headerObj.layer = layer;
			headerObj.transform.SetParent(card.transform, false);
			LayoutElement titleLe = headerObj.AddComponent<LayoutElement>();
			titleLe.minHeight = 26f;
			titleLe.preferredHeight = 26f;

			Text t = headerObj.AddComponent<Text>();
			t.text = $"<b>{title}</b>";
			t.font = font;
			t.fontSize = 15;
			t.alignment = TextAnchor.MiddleCenter;
			t.color = new Color(0.85f, 0.95f, 1f);

			GameObject contentObj = new GameObject("Content", typeof(RectTransform));
			contentObj.layer = layer;
			contentObj.transform.SetParent(card.transform, false);

			VerticalLayoutGroup contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
			contentLayout.spacing = 8f;
			contentLayout.childAlignment = TextAnchor.UpperCenter;
			contentLayout.childControlWidth = true;
			contentLayout.childControlHeight = true;
			contentLayout.childForceExpandWidth = true;
			contentLayout.childForceExpandHeight = true;

			return card;
		}

		private static Button CreateShopItemButton(Transform parent, string name, string text, Color bgColor, Font font, int layer)
		{
			GameObject btnObj = new GameObject(name, typeof(RectTransform));
			btnObj.layer = layer;
			btnObj.transform.SetParent(parent, false);

			LayoutElement le = btnObj.AddComponent<LayoutElement>();
			le.minHeight = 72f;

			Image img = btnObj.AddComponent<Image>();
			img.color = bgColor;

			Outline outline = btnObj.AddComponent<Outline>();
			outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
			outline.effectDistance = new Vector2(1f, -1f);

			Button btn = btnObj.AddComponent<Button>();

			GameObject txtObj = new GameObject("Text", typeof(RectTransform));
			txtObj.layer = layer;
			txtObj.transform.SetParent(btnObj.transform, false);
			RectTransform tr = txtObj.GetComponent<RectTransform>();
			tr.anchorMin = Vector2.zero;
			tr.anchorMax = Vector2.one;
			tr.sizeDelta = Vector2.zero;

			Text t = txtObj.AddComponent<Text>();
			t.text = text;
			t.font = font;
			t.fontSize = 17;
			t.alignment = TextAnchor.MiddleCenter;
			t.color = Color.white;

			return btn;
		}

		private static Button CreateHUDButton(Transform parent, string name, string label, Color bgColor, Font font, int layer)
		{
			GameObject go = new GameObject(name, typeof(RectTransform));
			go.layer = layer;
			go.transform.SetParent(parent, false);

			RectTransform rt = go.GetComponent<RectTransform>();
			rt.sizeDelta = new Vector2(160f, 60f);

			Image img = go.AddComponent<Image>();
			img.color = bgColor;

			Button btn = go.AddComponent<Button>();

			GameObject to = new GameObject("Text", typeof(RectTransform));
			to.layer = layer;
			to.transform.SetParent(go.transform, false);
			RectTransform tot = to.GetComponent<RectTransform>();
			tot.anchorMin = Vector2.zero;
			tot.anchorMax = Vector2.one;
			tot.sizeDelta = Vector2.zero;

			Text txt = to.AddComponent<Text>();
			txt.text = label;
			txt.font = font;
			txt.fontSize = 20;
			txt.fontStyle = FontStyle.Bold;
			txt.alignment = TextAnchor.MiddleCenter;
			txt.color = Color.white;

			return btn;
		}

		private static Font GetSafeFont()
		{
			Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
			return f;
		}
	}
}
#endif
