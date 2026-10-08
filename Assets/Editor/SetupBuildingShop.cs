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

			// Kiểm tra nếu chưa có Tab Kho Hàng hoặc còn banner ưu đãi ở Page_Defense
			bool missingWarehouseTab = GameObject.Find("Tab_Warehouse") == null;
			bool hasOldPromoBanner = GameObject.Find("Promo_StarterPack") != null;
			if (missingWarehouseTab || hasOldPromoBanner)
			{
				SetupShop();
			}
		}

		[MenuItem("Tools/🛡️ Cập Nhật Giao Diện Phòng Thủ & Kho Hàng (Bỏ Banner Ưu Đãi)")]
		public static void SetupShop()
		{
			if (EditorApplication.isPlaying)
			{
				EditorUtility.DisplayDialog("Lưu ý", 
					"Vui lòng TẮT PLAY MODE trước khi chạy để lưu thiết lập vào Scene!", 
					"Đã hiểu");
				return;
			}

			int uiLayer = LayerMask.NameToLayer("UI");
			if (uiLayer < 0) uiLayer = 5;

			// 1. Thư mục và Sprites vũ khí
			string iconDir = "Assets/UI/WeaponIcons";
			if (!Directory.Exists(iconDir)) Directory.CreateDirectory(iconDir);
			EnsureWeaponIcons(iconDir);

			// 2. Prefabs
			GameObject phaotuhanhPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Gun/prefabsGun/Phaotuhanhl.prefab");
			GameObject thunderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Gun/prefabsGun/Thunder.prefab");
			GameObject flamePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Gun/prefabsGun/FlamethrowerTurret.prefab");
			GameObject wallPrefab = EnsureWallPrefab();
			GameObject factoryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/RobotFactory_Building.prefab");

			// 3. Nạp Sprites
			Sprite iconPhaotuhanh = AssetDatabase.LoadAssetAtPath<Sprite>($"{iconDir}/Icon_Phaotuhanh.png");
			Sprite iconThunder = AssetDatabase.LoadAssetAtPath<Sprite>($"{iconDir}/Icon_Thunder.png");
			Sprite iconFlame = AssetDatabase.LoadAssetAtPath<Sprite>($"{iconDir}/Icon_Flamethrower.png");
			Sprite iconWall = AssetDatabase.LoadAssetAtPath<Sprite>($"{iconDir}/Icon_Wall.png");
			Sprite iconStarter = AssetDatabase.LoadAssetAtPath<Sprite>($"{iconDir}/Icon_StarterPack.png");

			// 4. GameEconomy
			GameEconomy economy = Object.FindAnyObjectByType<GameEconomy>();
			if (economy == null)
			{
				GameObject ecoObj = new GameObject("Game_Economy");
				economy = ecoObj.AddComponent<GameEconomy>();
				Undo.RegisterCreatedObjectUndo(ecoObj, "Tạo Game_Economy");
			}

			GameObject bipedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabsbot/BipedRobot_Prefab.prefab");
			if (bipedPrefab == null) bipedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/BipedRobot/BipedRobot_Prefab.prefab");
			if (bipedPrefab != null) economy.workerRobotPrefab = bipedPrefab;

			// 5. Cấu hình BuildingSystem (5 Items)
			BuildingSystem buildingSys = Object.FindAnyObjectByType<BuildingSystem>();
			if (buildingSys == null)
			{
				GameObject bsObj = new GameObject("Building_Manager");
				buildingSys = bsObj.AddComponent<BuildingSystem>();
				Undo.RegisterCreatedObjectUndo(bsObj, "Tạo Building_Manager");
			}

			buildingSys.items = new BuildingSystem.PlaceableItem[5];
			// 0: Pháo Tự Hành
			buildingSys.items[0] = new BuildingSystem.PlaceableItem
			{
				itemName = "Pháo Tự Hành",
				prefab = phaotuhanhPrefab != null ? phaotuhanhPrefab : flamePrefab,
				cost = 1500,
				icon = iconPhaotuhanh,
				isWall = false,
				segmentLength = 1f
			};
			// 1: Trụ Sét Thunder
			buildingSys.items[1] = new BuildingSystem.PlaceableItem
			{
				itemName = "Trụ Sét",
				prefab = thunderPrefab != null ? thunderPrefab : flamePrefab,
				cost = 2500,
				icon = iconThunder,
				isWall = false,
				segmentLength = 1f
			};
			// 2: Súng Phun Lửa Flamethrower
			buildingSys.items[2] = new BuildingSystem.PlaceableItem
			{
				itemName = "Súng Phun Lửa",
				prefab = flamePrefab,
				cost = 3200,
				icon = iconFlame,
				isWall = false,
				segmentLength = 1f
			};
			// 3: Bức Tường (Kéo dài)
			buildingSys.items[3] = new BuildingSystem.PlaceableItem
			{
				itemName = "Bức Tường",
				prefab = wallPrefab,
				cost = 100,
				icon = iconWall,
				isWall = true,
				segmentLength = 2.85f
			};
			// 4: Nhà Máy Robot
			buildingSys.items[4] = new BuildingSystem.PlaceableItem
			{
				itemName = "Nhà Máy Robot",
				prefab = factoryPrefab,
				cost = 0,
				isWall = false,
				segmentLength = 1f
			};

			// 6. EventSystem
			if (Object.FindAnyObjectByType<EventSystem>() == null)
			{
				GameObject es = new GameObject("EventSystem");
				es.AddComponent<EventSystem>();
				es.AddComponent<StandaloneInputModule>();
				Undo.RegisterCreatedObjectUndo(es, "Tạo EventSystem");
			}

			// 7. Tạo hoặc lấy Canvas
			Canvas canvas = Object.FindAnyObjectByType<Canvas>();
			GameObject canvasObj = canvas != null ? canvas.gameObject : null;
			if (canvasObj == null)
			{
				canvasObj = new GameObject("Shop_Canvas");
				canvasObj.layer = uiLayer;
				canvas = canvasObj.AddComponent<Canvas>();
				canvas.renderMode = RenderMode.ScreenSpaceOverlay;
				Undo.RegisterCreatedObjectUndo(canvasObj, "Tạo Shop_Canvas");
			}

			CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
			if (scaler == null) scaler = canvasObj.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1080, 1920);
			scaler.matchWidthOrHeight = 0.5f;
			scaler.dynamicPixelsPerUnit = 2.5f; // SẮC NÉT CHỮ TRÊN MÀN HÌNH ĐIỆN THOẠI (KHÔNG BỊ MỜ)
			scaler.referencePixelsPerUnit = 100;

			if (canvasObj.GetComponent<GraphicRaycaster>() == null)
				canvasObj.AddComponent<GraphicRaycaster>();

			canvasObj.transform.localScale = Vector3.one;
			RectTransform crt = canvasObj.GetComponent<RectTransform>();
			if (crt != null) crt.localScale = Vector3.one;

			// 8. DỌN DẸP TOÀN BỘ UI CŨ
			string[] oldNames = { "Shop_Panel", "Dashboard_Panel", "Btn_ShoppingCart", "Btn_OpenShop", "Placement_HUD", "Banner_NotEnoughCoins", "Building_Shop_Bar", "Btn_DefenseToggle" };
			foreach (var name in oldNames)
			{
				Transform t = canvasObj.transform.Find(name);
				if (t != null) Object.DestroyImmediate(t.gameObject);
			}

			Font safeFont = GetSafeFont();

			// ================= A. NÚT MỞ PHÒNG THỦ Ở GÓC DƯỚI PHẢI =================
			GameObject toggleBtnObj = new GameObject("Btn_DefenseToggle", typeof(RectTransform));
			toggleBtnObj.layer = uiLayer;
			toggleBtnObj.transform.SetParent(canvasObj.transform, false);

			RectTransform tbrt = toggleBtnObj.GetComponent<RectTransform>();
			tbrt.anchorMin = new Vector2(1f, 0f);
			tbrt.anchorMax = new Vector2(1f, 0f);
			tbrt.pivot = new Vector2(1f, 0f);
			tbrt.anchoredPosition = new Vector2(-28f, 28f);
			tbrt.sizeDelta = new Vector2(120f, 120f);

			Image tbImg = toggleBtnObj.AddComponent<Image>();
			tbImg.color = new Color(0.08f, 0.14f, 0.24f, 0.96f);

			Outline tbOutline = toggleBtnObj.AddComponent<Outline>();
			tbOutline.effectColor = new Color(0f, 0.85f, 1f, 0.9f);
			tbOutline.effectDistance = new Vector2(2f, -2f);

			Button btnToggle = toggleBtnObj.AddComponent<Button>();

			GameObject tbTxtObj = new GameObject("Text", typeof(RectTransform));
			tbTxtObj.layer = uiLayer;
			tbTxtObj.transform.SetParent(toggleBtnObj.transform, false);
			RectTransform tbtr = tbTxtObj.GetComponent<RectTransform>();
			tbtr.anchorMin = Vector2.zero;
			tbtr.anchorMax = Vector2.one;
			tbtr.sizeDelta = Vector2.zero;

			Text tbTxt = tbTxtObj.AddComponent<Text>();
			tbTxt.text = "<size=38>🛡️</size>\n<size=13><b>PHÒNG THỦ</b></size>";
			tbTxt.font = safeFont;
			tbTxt.fontSize = 16;
			tbTxt.alignment = TextAnchor.MiddleCenter;
			tbTxt.color = new Color(0.95f, 0.85f, 0.25f);

			// ================= B. BẢNG PHÒNG THỦ CHÍNH (KHÔNG CÒN BANNER ƯU ĐÃI) =================
			GameObject mainPanelObj = new GameObject("Dashboard_Panel", typeof(RectTransform));
			mainPanelObj.layer = uiLayer;
			mainPanelObj.transform.SetParent(canvasObj.transform, false);

			RectTransform panelRect = mainPanelObj.GetComponent<RectTransform>();
			panelRect.anchorMin = new Vector2(0.5f, 0f);
			panelRect.anchorMax = new Vector2(0.5f, 0f);
			panelRect.pivot = new Vector2(0.5f, 0f);
			panelRect.anchoredPosition = new Vector2(0f, 15f);
			panelRect.sizeDelta = new Vector2(1040f, 780f);

			Image panelBg = mainPanelObj.AddComponent<Image>();
			panelBg.color = new Color(0.05f, 0.08f, 0.14f, 0.98f);

			Outline panelOutline = mainPanelObj.AddComponent<Outline>();
			panelOutline.effectColor = new Color(0.12f, 0.45f, 0.85f, 0.85f);
			panelOutline.effectDistance = new Vector2(2.5f, -2.5f);

			// ================= 1. SIDEBAR BÊN TRÁI (LEFT TABS) =================
			GameObject sidebarObj = new GameObject("Left_Sidebar", typeof(RectTransform));
			sidebarObj.layer = uiLayer;
			sidebarObj.transform.SetParent(mainPanelObj.transform, false);

			RectTransform sideRect = sidebarObj.GetComponent<RectTransform>();
			sideRect.anchorMin = new Vector2(0f, 0f);
			sideRect.anchorMax = new Vector2(0f, 1f);
			sideRect.pivot = new Vector2(0f, 0.5f);
			sideRect.anchoredPosition = new Vector2(14f, 0f);
			sideRect.sizeDelta = new Vector2(165f, -28f);

			VerticalLayoutGroup sideLayout = sidebarObj.AddComponent<VerticalLayoutGroup>();
			sideLayout.spacing = 8f;
			sideLayout.childAlignment = TextAnchor.UpperCenter;
			sideLayout.childControlWidth = true;
			sideLayout.childControlHeight = false;
			sideLayout.childForceExpandWidth = true;
			sideLayout.childForceExpandHeight = false;

			// Tab 0: PHÒNG THỦ (Mặc định chọn - Màu Vàng)
			var (btnTabDef, imgTabDef) = CreateSidebarTab(sidebarObj.transform, "Tab_Defense", "🛡️", "PHÒNG THỦ", true, safeFont, uiLayer);
			// Tab 1: KHO HÀNG (Chuyển gói ưu đãi & lưu trữ vào đây)
			var (btnTabWar, imgTabWar) = CreateSidebarTab(sidebarObj.transform, "Tab_Warehouse", "📦", "KHO HÀNG", false, safeFont, uiLayer);
			// Tab 2: MÁY MÓC
			var (btnTabMach, imgTabMach) = CreateSidebarTab(sidebarObj.transform, "Tab_Machinery", "⚙️", "MÁY MÓC", false, safeFont, uiLayer);
			// Tab 3: ROBOT
			var (btnTabRob, imgTabRob) = CreateSidebarTab(sidebarObj.transform, "Tab_Robot", "🤖", "ROBOT", false, safeFont, uiLayer);
			// Tab 4: ĐẶC BIỆT
			var (btnTabSpec, imgTabSpec) = CreateSidebarTab(sidebarObj.transform, "Tab_Special", "⭐", "ĐẶC BIỆT", false, safeFont, uiLayer);

			// ================= 2. KHU VỰC NỘI DUNG CHÍNH (CONTENT AREA) =================
			GameObject contentAreaObj = new GameObject("Content_Area", typeof(RectTransform));
			contentAreaObj.layer = uiLayer;
			contentAreaObj.transform.SetParent(mainPanelObj.transform, false);

			RectTransform caRect = contentAreaObj.GetComponent<RectTransform>();
			caRect.anchorMin = Vector2.zero;
			caRect.anchorMax = Vector2.one;
			caRect.offsetMin = new Vector2(192f, 14f);
			caRect.offsetMax = new Vector2(-14f, -14f);

			// --- 2.1. HEADER BAR (KHÔNG CÓ CHỮ CỬA HÀNG) ---
			GameObject headerObj = new GameObject("Header_Bar", typeof(RectTransform));
			headerObj.layer = uiLayer;
			headerObj.transform.SetParent(contentAreaObj.transform, false);

			RectTransform hrRect = headerObj.GetComponent<RectTransform>();
			hrRect.anchorMin = new Vector2(0f, 1f);
			hrRect.anchorMax = new Vector2(1f, 1f);
			hrRect.pivot = new Vector2(0.5f, 1f);
			hrRect.anchoredPosition = Vector2.zero;
			hrRect.sizeDelta = new Vector2(0f, 54f);

			// Tiêu đề: PHÒNG THỦ CĂN CỨ
			GameObject titleGroup = new GameObject("Title_Group", typeof(RectTransform));
			titleGroup.layer = uiLayer;
			titleGroup.transform.SetParent(headerObj.transform, false);
			RectTransform tgRect = titleGroup.GetComponent<RectTransform>();
			tgRect.anchorMin = new Vector2(0f, 0f);
			tgRect.anchorMax = new Vector2(0.48f, 1f);
			tgRect.sizeDelta = Vector2.zero;

			Text titleTxt = titleGroup.AddComponent<Text>();
			titleTxt.text = "<size=20><b>🛡️ PHÒNG THỦ CĂN CỨ</b></size>\n<size=12><color=#8EA5C8>Mua và bố trí hỏa lực bảo vệ căn cứ!</color></size>";
			titleTxt.font = safeFont;
			titleTxt.fontSize = 16;
			titleTxt.alignment = TextAnchor.MiddleLeft;
			titleTxt.color = Color.white;

			// Đồng hồ đếm ngược
			GameObject countObj = new GameObject("Countdown_Text", typeof(RectTransform));
			countObj.layer = uiLayer;
			countObj.transform.SetParent(headerObj.transform, false);
			RectTransform cntRect = countObj.GetComponent<RectTransform>();
			cntRect.anchorMin = new Vector2(0.48f, 0f);
			cntRect.anchorMax = new Vector2(0.72f, 1f);
			cntRect.sizeDelta = Vector2.zero;

			Text countTxt = countObj.AddComponent<Text>();
			countTxt.text = "⏳ Làm mới sau:\n<color=#00FFFF><b>04:32</b></color>";
			countTxt.font = safeFont;
			countTxt.fontSize = 14;
			countTxt.alignment = TextAnchor.MiddleCenter;
			countTxt.color = new Color(0.8f, 0.9f, 1f);

			// Nút "Làm mới 50 💎"
			GameObject refBtnObj = new GameObject("Btn_Refresh", typeof(RectTransform));
			refBtnObj.layer = uiLayer;
			refBtnObj.transform.SetParent(headerObj.transform, false);
			RectTransform rfRect = refBtnObj.GetComponent<RectTransform>();
			rfRect.anchorMin = new Vector2(0.73f, 0.12f);
			rfRect.anchorMax = new Vector2(0.92f, 0.88f);
			rfRect.sizeDelta = Vector2.zero;

			Image rfImg = refBtnObj.AddComponent<Image>();
			rfImg.color = new Color(0.92f, 0.65f, 0.12f);
			Button btnRefresh = refBtnObj.AddComponent<Button>();

			GameObject rfTxtObj = new GameObject("Text", typeof(RectTransform));
			rfTxtObj.layer = uiLayer;
			rfTxtObj.transform.SetParent(refBtnObj.transform, false);
			RectTransform rftr = rfTxtObj.GetComponent<RectTransform>();
			rftr.anchorMin = Vector2.zero;
			rftr.anchorMax = Vector2.one;
			rftr.sizeDelta = Vector2.zero;
			Text rfTxt = rfTxtObj.AddComponent<Text>();
			rfTxt.text = "🎬 <b>Làm mới</b>\n<size=12><color=#633800>50 💎</color></size>";
			rfTxt.font = safeFont;
			rfTxt.fontSize = 14;
			rfTxt.alignment = TextAnchor.MiddleCenter;
			rfTxt.color = new Color(0.15f, 0.1f, 0.02f);

			// Nút Đóng ✕
			GameObject closeBtnObj = new GameObject("Btn_Close", typeof(RectTransform));
			closeBtnObj.layer = uiLayer;
			closeBtnObj.transform.SetParent(headerObj.transform, false);
			RectTransform clRect = closeBtnObj.GetComponent<RectTransform>();
			clRect.anchorMin = new Vector2(0.93f, 0.12f);
			clRect.anchorMax = new Vector2(1f, 0.88f);
			clRect.sizeDelta = Vector2.zero;

			Image clImg = closeBtnObj.AddComponent<Image>();
			clImg.color = new Color(0.78f, 0.18f, 0.18f, 0.95f);
			Button btnClose = closeBtnObj.AddComponent<Button>();

			GameObject clTxtObj = new GameObject("Text", typeof(RectTransform));
			clTxtObj.layer = uiLayer;
			clTxtObj.transform.SetParent(closeBtnObj.transform, false);
			RectTransform cltr = clTxtObj.GetComponent<RectTransform>();
			cltr.anchorMin = Vector2.zero;
			cltr.anchorMax = Vector2.one;
			cltr.sizeDelta = Vector2.zero;
			Text clTxt = clTxtObj.AddComponent<Text>();
			clTxt.text = "✕";
			clTxt.font = safeFont;
			clTxt.fontSize = 22;
			clTxt.fontStyle = FontStyle.Bold;
			clTxt.alignment = TextAnchor.MiddleCenter;
			clTxt.color = Color.white;

			// ================= 3. TRANG 1: PHÒNG THỦ (PAGE DEFENSE - ĐÃ XÓA BANNER ƯU ĐÃI) =================
			// Theo yêu cầu: Xóa bỏ banner ưu đãi ở dưới, để các thẻ vũ khí mở rộng toàn bộ không gian
			GameObject pageDefObj = new GameObject("Page_Defense", typeof(RectTransform));
			pageDefObj.layer = uiLayer;
			pageDefObj.transform.SetParent(contentAreaObj.transform, false);
			RectTransform pdRect = pageDefObj.GetComponent<RectTransform>();
			pdRect.anchorMin = Vector2.zero;
			pdRect.anchorMax = Vector2.one;
			pdRect.offsetMin = Vector2.zero;
			pdRect.offsetMax = new Vector2(0f, -60f); // Dưới header

			// HÀNG 4 THẺ VŨ KHÍ PHÒNG THỦ (CHIẾM TRỌN DIỆN TÍCH RỘNG RÃI)
			GameObject cardsRowObj = new GameObject("Cards_Row", typeof(RectTransform));
			cardsRowObj.layer = uiLayer;
			cardsRowObj.transform.SetParent(pageDefObj.transform, false);
			RectTransform crRect = cardsRowObj.GetComponent<RectTransform>();
			crRect.anchorMin = Vector2.zero;
			crRect.anchorMax = Vector2.one;
			crRect.offsetMin = Vector2.zero;
			crRect.offsetMax = Vector2.zero;

			HorizontalLayoutGroup rowLayout = cardsRowObj.AddComponent<HorizontalLayoutGroup>();
			rowLayout.spacing = 12f;
			rowLayout.padding = new RectOffset(4, 4, 8, 8);
			rowLayout.childAlignment = TextAnchor.MiddleCenter;
			rowLayout.childControlWidth = true;
			rowLayout.childControlHeight = true;
			rowLayout.childForceExpandWidth = true;
			rowLayout.childForceExpandHeight = true;

			// Card 1: Pháo Tự Hành (Phaotuhanh)
			Button btnBuyPhaotu = CreateWeaponDefenseCard(cardsRowObj.transform, "Card_Phaotuhanh", "Pháo Tự Hành", iconPhaotuhanh, 
				"Bắn loạt rocket tầm xa hỏa lực mạnh.", "🎯 Tầm xa +30%", "1,500", safeFont, uiLayer);

			// Card 2: Trụ Sét (Thunder)
			Button btnBuyThunder = CreateWeaponDefenseCard(cardsRowObj.transform, "Card_Thunder", "Trụ Sét", iconThunder, 
				"Phóng chuỗi tia sét giật tê liệt địch.", "⚡ Tê liệt +25%", "2,500", safeFont, uiLayer);

			// Card 3: Súng Phun Lửa (FlamethrowerTurret)
			Button btnBuyFlame = CreateWeaponDefenseCard(cardsRowObj.transform, "Card_Flamethrower", "Súng Phun Lửa", iconFlame, 
				"Thiêu đốt quái vật liên tục diện rộng.", "🔥 Thiêu đốt +40%", "3,200", safeFont, uiLayer);

			// Card 4: Tường Phòng Thủ (Wall)
			Button btnBuyWallItem = CreateWeaponDefenseCard(cardsRowObj.transform, "Card_Wall", "Tường Phòng Thủ", iconWall, 
				"Kéo dài chặn đường tiến công của địch.", "🛡️ Phòng thủ +50%", "100/Đoạn", safeFont, uiLayer);

			// ================= 4. TRANG 2: KHO HÀNG (PAGE WAREHOUSE - NƠI CHỨA GÓI ƯU ĐÃI & KHO LƯU TRỮ) =================
			GameObject pageWarObj = CreateSubPage(contentAreaObj.transform, "Page_Warehouse", uiLayer);
			Button btnStarterPack = PopulateWarehousePage(pageWarObj.transform, iconStarter, safeFont, uiLayer);

			// ================= 5. CÁC TRANG CÒN LẠI (MÁY MÓC, ROBOT, ĐẶC BIỆT) =================
			GameObject pageMachObj = CreateSubPage(contentAreaObj.transform, "Page_Machinery", uiLayer);
			GameObject pageRobObj = CreateSubPage(contentAreaObj.transform, "Page_Robot", uiLayer);
			GameObject pageSpecObj = CreateSubPage(contentAreaObj.transform, "Page_Special", uiLayer);

			// Điền nội dung cho Trang Robot
			var (btnBuyWorker, txtBuyWorker, btnBuildFac, txtBuildFac, btnCraftRob, txtCraftRob) = PopulateRobotPage(pageRobObj.transform, safeFont, uiLayer);

			// Điền nội dung cho Trang Đặc Biệt (Nâng cấp Tường & Cúp Sắt)
			var (btnUpPick, txtUpPick, btnUpWall, txtUpWall) = PopulateSpecialPage(pageSpecObj.transform, safeFont, uiLayer);

			pageWarObj.SetActive(false);
			pageMachObj.SetActive(false);
			pageRobObj.SetActive(false);
			pageSpecObj.SetActive(false);

			// ================= 6. BANNER CẢNH BÁO "KHÔNG ĐỦ VÀNG" =================
			GameObject warnBannerObj = new GameObject("Banner_NotEnoughCoins", typeof(RectTransform));
			warnBannerObj.layer = uiLayer;
			warnBannerObj.transform.SetParent(canvasObj.transform, false);

			RectTransform warnRect = warnBannerObj.GetComponent<RectTransform>();
			warnRect.anchorMin = new Vector2(0.5f, 0.72f);
			warnRect.anchorMax = new Vector2(0.5f, 0.72f);
			warnRect.pivot = new Vector2(0.5f, 0.5f);
			warnRect.sizeDelta = new Vector2(520f, 85f);

			Image warnBg = warnBannerObj.AddComponent<Image>();
			warnBg.color = new Color(0.85f, 0.12f, 0.12f, 0.94f);
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
			warnTxt.fontSize = 22;
			warnTxt.fontStyle = FontStyle.Bold;
			warnTxt.alignment = TextAnchor.MiddleCenter;
			warnTxt.color = Color.white;

			warnBannerObj.SetActive(false);

			// ================= 7. HUD NỔI KHI ĐANG ĐẶT (PLACEMENT HUD) =================
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

			// Mặc định ẩn dashboard, chỉ hiện nút mở
			mainPanelObj.SetActive(false);
			toggleBtnObj.SetActive(true);

			// ================= 8. KẾT NỐI VÀO SCRIPT BUILDINGSHOPUI =================
			BuildingShopUI shopUI = canvasObj.GetComponent<BuildingShopUI>();
			if (shopUI == null) shopUI = canvasObj.AddComponent<BuildingShopUI>();

			shopUI.buildingSystem = buildingSys;
			shopUI.shoppingCartButton = btnToggle;
			shopUI.dashboardPanel = mainPanelObj;
			shopUI.closeDashboardButton = btnClose;

			// Tabs
			shopUI.tabDefenseButton = btnTabDef;
			shopUI.tabWarehouseButton = btnTabWar;
			shopUI.tabMachineryButton = btnTabMach;
			shopUI.tabRobotButton = btnTabRob;
			shopUI.tabSpecialButton = btnTabSpec;

			shopUI.tabDefenseBg = imgTabDef;
			shopUI.tabWarehouseBg = imgTabWar;
			shopUI.tabMachineryBg = imgTabMach;
			shopUI.tabRobotBg = imgTabRob;
			shopUI.tabSpecialBg = imgTabSpec;

			shopUI.pageDefense = pageDefObj;
			shopUI.pageWarehouse = pageWarObj;
			shopUI.pageMachinery = pageMachObj;
			shopUI.pageRobot = pageRobObj;
			shopUI.pageSpecial = pageSpecObj;

			// 4 vũ khí phòng thủ
			shopUI.buyPhaotuhanhButton = btnBuyPhaotu;
			shopUI.buyThunderButton = btnBuyThunder;
			shopUI.buyFlamethrowerButton = btnBuyFlame;
			shopUI.buyWallItemButton = btnBuyWallItem;
			shopUI.buyStarterPackButton = btnStarterPack;

			// Header
			shopUI.countdownText = countTxt;
			shopUI.refreshShopButton = btnRefresh;

			// Robot & Special hooks
			shopUI.buyWorkerButton = btnBuyWorker;
			shopUI.buyWorkerText = txtBuyWorker;
			shopUI.buildFactoryButton = btnBuildFac;
			shopUI.buildFactoryText = txtBuildFac;
			shopUI.craftRobotButton = btnCraftRob;
			shopUI.craftRobotText = txtCraftRob;
			shopUI.defenseRobotButton = btnCraftRob;
			shopUI.defenseRobotText = txtCraftRob;

			shopUI.upgradePickaxeButton = btnUpPick;
			shopUI.upgradePickaxeText = txtUpPick;
			shopUI.upgradeWallButton = btnUpWall;
			shopUI.upgradeWallText = txtUpWall;

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

			Debug.Log("<color=#00FF88><b>[SetupShop]</b> ĐÃ XÓA BANNER ƯU ĐÃI KHỎI PHÒNG THỦ & CHUYỂN VÀO TAB KHO HÀNG THÀNH CÔNG!</color>");
		}

		private static (Button, Image) CreateSidebarTab(Transform parent, string name, string icon, string title, bool isActive, Font font, int layer)
		{
			GameObject tabObj = new GameObject(name, typeof(RectTransform));
			tabObj.layer = layer;
			tabObj.transform.SetParent(parent, false);

			LayoutElement le = tabObj.AddComponent<LayoutElement>();
			le.minHeight = 76f;
			le.preferredHeight = 76f;

			Image bg = tabObj.AddComponent<Image>();
			bg.color = isActive ? new Color(0.96f, 0.65f, 0.12f) : new Color(0.08f, 0.14f, 0.24f, 0.95f);

			Outline outline = tabObj.AddComponent<Outline>();
			outline.effectColor = new Color(0.18f, 0.42f, 0.72f);
			outline.effectDistance = new Vector2(1.5f, -1.5f);

			Button btn = tabObj.AddComponent<Button>();

			GameObject txtObj = new GameObject("Text", typeof(RectTransform));
			txtObj.layer = layer;
			txtObj.transform.SetParent(tabObj.transform, false);
			RectTransform tr = txtObj.GetComponent<RectTransform>();
			tr.anchorMin = Vector2.zero;
			tr.anchorMax = Vector2.one;
			tr.sizeDelta = Vector2.zero;

			Text t = txtObj.AddComponent<Text>();
			t.text = $"<size=26>{icon}</size>\n<size=12><b>{title}</b></size>";
			t.font = font;
			t.fontSize = 14;
			t.alignment = TextAnchor.MiddleCenter;
			t.color = isActive ? new Color(0.12f, 0.08f, 0.02f) : Color.white;

			return (btn, bg);
		}

		private static Button CreateWeaponDefenseCard(Transform parent, string name, string title, Sprite iconSprite, string desc, string perk, string price, Font font, int layer)
		{
			GameObject cardObj = new GameObject(name, typeof(RectTransform));
			cardObj.layer = layer;
			cardObj.transform.SetParent(parent, false);

			Image bg = cardObj.AddComponent<Image>();
			bg.color = new Color(0.07f, 0.12f, 0.22f, 0.98f);

			Outline outline = cardObj.AddComponent<Outline>();
			outline.effectColor = new Color(0.15f, 0.38f, 0.68f);
			outline.effectDistance = new Vector2(1.5f, -1.5f);

			VerticalLayoutGroup vLayout = cardObj.AddComponent<VerticalLayoutGroup>();
			vLayout.padding = new RectOffset(12, 12, 14, 14);
			vLayout.spacing = 10f;
			vLayout.childAlignment = TextAnchor.UpperCenter;
			vLayout.childControlWidth = true;
			vLayout.childControlHeight = false;
			vLayout.childForceExpandWidth = true;
			vLayout.childForceExpandHeight = false;

			// Tên vũ khí
			GameObject titleObj = new GameObject("Title", typeof(RectTransform));
			titleObj.layer = layer;
			titleObj.transform.SetParent(cardObj.transform, false);
			LayoutElement tle = titleObj.AddComponent<LayoutElement>();
			tle.minHeight = 28f;
			tle.preferredHeight = 28f;
			Text tText = titleObj.AddComponent<Text>();
			tText.text = $"<size=17><b>{title}</b></size>";
			tText.font = font;
			tText.fontSize = 20;
			tText.alignment = TextAnchor.MiddleCenter;
			tText.color = Color.white;
			Shadow sTitle = titleObj.AddComponent<Shadow>();
			sTitle.effectColor = new Color(0f, 0f, 0f, 0.85f);
			sTitle.effectDistance = new Vector2(1.5f, -1.5f);

			// Icon ảnh 3D vũ khí (Mở rộng kích thước to rõ hơn)
			GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
			iconObj.layer = layer;
			iconObj.transform.SetParent(cardObj.transform, false);
			LayoutElement ile = iconObj.AddComponent<LayoutElement>();
			ile.minHeight = 185f;
			ile.preferredHeight = 185f;

			Image iconImg = iconObj.AddComponent<Image>();
			if (iconSprite != null) iconImg.sprite = iconSprite;
			iconImg.preserveAspect = true;

			// Mô tả
			GameObject descObj = new GameObject("Desc", typeof(RectTransform));
			descObj.layer = layer;
			descObj.transform.SetParent(cardObj.transform, false);
			LayoutElement dle = descObj.AddComponent<LayoutElement>();
			dle.minHeight = 52f;
			dle.preferredHeight = 52f;
			Text dText = descObj.AddComponent<Text>();
			dText.text = desc;
			dText.font = font;
			dText.fontSize = 15;
			dText.alignment = TextAnchor.MiddleCenter;
			dText.color = new Color(0.82f, 0.90f, 0.98f);

			// Chip Perk
			GameObject perkObj = new GameObject("Perk", typeof(RectTransform));
			perkObj.layer = layer;
			perkObj.transform.SetParent(cardObj.transform, false);
			LayoutElement ple = perkObj.AddComponent<LayoutElement>();
			ple.minHeight = 28f;
			ple.preferredHeight = 28f;
			Text pText = perkObj.AddComponent<Text>();
			pText.text = $"<color=#67E8F9><b>{perk}</b></color>";
			pText.font = font;
			pText.fontSize = 15;
			pText.alignment = TextAnchor.MiddleCenter;

			// Nút Mua Xanh Lá
			GameObject btnObj = new GameObject("Btn_Buy", typeof(RectTransform));
			btnObj.layer = layer;
			btnObj.transform.SetParent(cardObj.transform, false);
			LayoutElement ble = btnObj.AddComponent<LayoutElement>();
			ble.minHeight = 54f;
			ble.preferredHeight = 54f;

			Image bImg = btnObj.AddComponent<Image>();
			bImg.color = new Color(0.08f, 0.68f, 0.35f);

			Outline bOutline = btnObj.AddComponent<Outline>();
			bOutline.effectColor = new Color(0.02f, 0.35f, 0.18f);
			bOutline.effectDistance = new Vector2(1f, -1f);

			Button btn = btnObj.AddComponent<Button>();

			GameObject bTxtObj = new GameObject("Text", typeof(RectTransform));
			bTxtObj.layer = layer;
			bTxtObj.transform.SetParent(btnObj.transform, false);
			RectTransform btr = bTxtObj.GetComponent<RectTransform>();
			btr.anchorMin = Vector2.zero;
			btr.anchorMax = Vector2.one;
			btr.sizeDelta = Vector2.zero;
			Text bTxt = bTxtObj.AddComponent<Text>();
			bTxt.text = $"🪙 <b>{price}</b>";
			bTxt.font = font;
			bTxt.fontSize = 19;
			bTxt.alignment = TextAnchor.MiddleCenter;
			bTxt.color = Color.white;
			Shadow sPrice = bTxtObj.AddComponent<Shadow>();
			sPrice.effectColor = new Color(0f, 0f, 0f, 0.9f);
			sPrice.effectDistance = new Vector2(1.5f, -1.5f);

			return btn;
		}

		private static Button PopulateWarehousePage(Transform parent, Sprite iconStarter, Font font, int layer)
		{
			// Tiêu đề Kho Hàng
			GameObject headerBox = new GameObject("Warehouse_Header", typeof(RectTransform));
			headerBox.layer = layer;
			headerBox.transform.SetParent(parent, false);
			LayoutElement hle = headerBox.AddComponent<LayoutElement>();
			hle.minHeight = 50f;
			hle.preferredHeight = 50f;
			Text ht = headerBox.AddComponent<Text>();
			ht.text = "📦 <b>KHO HÀNG CĂN CỨ & GÓI ƯU ĐÃI</b>\n<size=13><color=#93C5FD>Lưu trữ các gói vật phẩm, trang bị và vũ khí đã sở hữu</color></size>";
			ht.font = font;
			ht.fontSize = 16;
			ht.alignment = TextAnchor.MiddleCenter;
			ht.color = Color.white;

			// Thẻ 1: Gói Khởi Đầu (-50%) đưa vào Kho Hàng
			GameObject starterCard = new GameObject("Card_StarterPack_InWarehouse", typeof(RectTransform));
			starterCard.layer = layer;
			starterCard.transform.SetParent(parent, false);
			LayoutElement sle = starterCard.AddComponent<LayoutElement>();
			sle.minHeight = 110f;
			sle.preferredHeight = 110f;

			Image scBg = starterCard.AddComponent<Image>();
			scBg.color = new Color(0.09f, 0.15f, 0.25f, 0.98f);
			Outline scOutline = starterCard.AddComponent<Outline>();
			scOutline.effectColor = new Color(0.95f, 0.65f, 0.12f, 0.7f);
			scOutline.effectDistance = new Vector2(1.5f, -1.5f);

			// Icon bên trái
			GameObject icObj = new GameObject("Icon", typeof(RectTransform));
			icObj.layer = layer;
			icObj.transform.SetParent(starterCard.transform, false);
			RectTransform icr = icObj.GetComponent<RectTransform>();
			icr.anchorMin = new Vector2(0f, 0.5f);
			icr.anchorMax = new Vector2(0f, 0.5f);
			icr.pivot = new Vector2(0f, 0.5f);
			icr.anchoredPosition = new Vector2(16f, 0f);
			icr.sizeDelta = new Vector2(85f, 85f);
			Image ici = icObj.AddComponent<Image>();
			ici.sprite = iconStarter;

			// Nội dung
			GameObject descObj = new GameObject("Desc", typeof(RectTransform));
			descObj.layer = layer;
			descObj.transform.SetParent(starterCard.transform, false);
			RectTransform dr = descObj.GetComponent<RectTransform>();
			dr.anchorMin = new Vector2(0f, 0f);
			dr.anchorMax = new Vector2(0.68f, 1f);
			dr.offsetMin = new Vector2(115f, 8f);
			dr.offsetMax = new Vector2(0f, -8f);

			Text dt = descObj.AddComponent<Text>();
			dt.text = "<size=17><b>Gói Khởi Đầu Chiến Dịch</b></size> <color=#EF4444><b>(-50%)</b></color>\n<size=13><color=#93C5FD>Bao gồm 1 Pháo Tự Hành + 1 Trụ Sét Thunder</color></size>\n<size=12><color=#FCD34D>✦ Mở gói để nhận ngay hỏa lực bảo vệ căn cứ</color></size>";
			dt.font = font;
			dt.fontSize = 15;
			dt.alignment = TextAnchor.MiddleLeft;
			dt.color = Color.white;

			// Nút Mở / Mua Gói
			GameObject btnObj = new GameObject("Btn_BuyStarterPack", typeof(RectTransform));
			btnObj.layer = layer;
			btnObj.transform.SetParent(starterCard.transform, false);
			RectTransform br = btnObj.GetComponent<RectTransform>();
			br.anchorMin = new Vector2(0.70f, 0.18f);
			br.anchorMax = new Vector2(0.98f, 0.82f);
			br.sizeDelta = Vector2.zero;

			Image bi = btnObj.AddComponent<Image>();
			bi.color = new Color(0.95f, 0.65f, 0.12f);
			Button btnPack = btnObj.AddComponent<Button>();

			GameObject btObj = new GameObject("Text", typeof(RectTransform));
			btObj.layer = layer;
			btObj.transform.SetParent(btnObj.transform, false);
			RectTransform btr = btObj.GetComponent<RectTransform>();
			btr.anchorMin = Vector2.zero;
			btr.anchorMax = Vector2.one;
			btr.sizeDelta = Vector2.zero;
			Text bt = btObj.AddComponent<Text>();
			bt.text = "🪙 <b>4,990</b>\n<size=11><color=#633800>MỞ GÓI</color></size>";
			bt.font = font;
			bt.fontSize = 16;
			bt.alignment = TextAnchor.MiddleCenter;
			bt.color = new Color(0.15f, 0.1f, 0.02f);

			// Thẻ 2: Hòm Vũ Khí Dự Trữ
			CreateWarehouseInfoBox(parent, "Card_WeaponsStock", "🔫 <b>HÒM VŨ KHÍ BỐ TRÍ DỰ TRỮ</b>", 
				"✦ Pháo Tự Hành, Trụ Sét, Súng Phun Lửa đã sẵn sàng xuất kích\n<i>Bấm sang tab 'Phòng Thủ' để chọn và kéo thả trên bản đồ!</i>", font, layer);

			// Thẻ 3: Kho Tài Nguyên & Thiết Bị
			CreateWarehouseInfoBox(parent, "Card_ResourceStock", "🏗️ <b>KHO CÔNG TRÌNH & THIẾT BỊ</b>", 
				"✦ Chứa các đoạn tường dự phòng, máy móc và công cụ nâng cấp\n<i>Được kết nối đồng bộ trực tiếp với hệ thống kinh tế căn cứ.</i>", font, layer);

			return btnPack;
		}

		private static void CreateWarehouseInfoBox(Transform parent, string name, string title, string desc, Font font, int layer)
		{
			GameObject box = new GameObject(name, typeof(RectTransform));
			box.layer = layer;
			box.transform.SetParent(parent, false);
			LayoutElement le = box.AddComponent<LayoutElement>();
			le.minHeight = 88f;
			le.preferredHeight = 88f;

			Image bg = box.AddComponent<Image>();
			bg.color = new Color(0.07f, 0.11f, 0.18f, 0.95f);
			Outline ol = box.AddComponent<Outline>();
			ol.effectColor = new Color(0.15f, 0.35f, 0.6f);
			ol.effectDistance = new Vector2(1f, -1f);

			VerticalLayoutGroup vl = box.AddComponent<VerticalLayoutGroup>();
			vl.padding = new RectOffset(16, 16, 10, 10);
			vl.spacing = 6f;
			vl.childAlignment = TextAnchor.UpperLeft;
			vl.childControlWidth = true;
			vl.childControlHeight = false;

			GameObject to = new GameObject("Title", typeof(RectTransform));
			to.layer = layer;
			to.transform.SetParent(box.transform, false);
			Text tt = to.AddComponent<Text>();
			tt.text = title;
			tt.font = font;
			tt.fontSize = 15;
			tt.color = new Color(0.85f, 0.95f, 1f);

			GameObject descObj = new GameObject("Desc", typeof(RectTransform));
			descObj.layer = layer;
			descObj.transform.SetParent(box.transform, false);
			Text dt = descObj.AddComponent<Text>();
			dt.text = desc;
			dt.font = font;
			dt.fontSize = 13;
			dt.color = new Color(0.68f, 0.78f, 0.92f);
		}

		private static GameObject CreateSubPage(Transform parent, string name, int layer)
		{
			GameObject pageObj = new GameObject(name, typeof(RectTransform));
			pageObj.layer = layer;
			pageObj.transform.SetParent(parent, false);
			RectTransform pr = pageObj.GetComponent<RectTransform>();
			pr.anchorMin = Vector2.zero;
			pr.anchorMax = Vector2.one;
			pr.offsetMin = Vector2.zero;
			pr.offsetMax = new Vector2(0f, -60f);

			VerticalLayoutGroup vl = pageObj.AddComponent<VerticalLayoutGroup>();
			vl.spacing = 14f;
			vl.padding = new RectOffset(16, 16, 16, 16);
			vl.childAlignment = TextAnchor.UpperCenter;
			vl.childControlWidth = true;
			vl.childControlHeight = false;
			vl.childForceExpandWidth = true;

			return pageObj;
		}

		private static (Button, Text, Button, Text, Button, Text) PopulateRobotPage(Transform parent, Font font, int layer)
		{
			Button btnWorker = CreateStandardButton(parent, "Btn_BuyWorker", "🤖 <b>MUA ROBOT ĐÀO MỎ</b>\n<color=#FFD700>🪙 0 Vàng</color>", new Color(0.14f, 0.38f, 0.25f), font, layer);
			Text txtWorker = btnWorker.GetComponentInChildren<Text>();

			Button btnFac = CreateStandardButton(parent, "Btn_BuildFactory", "🏭 <b>XÂY NHÀ MÁY ROBOT</b>\n<color=#00FFFF>✦ Chế Tạo Đội Quân Phòng Thủ</color>", new Color(0.18f, 0.45f, 0.38f), font, layer);
			Text txtFac = btnFac.GetComponentInChildren<Text>();

			Button btnCraft = CreateStandardButton(parent, "Btn_CraftRobot", "🤖 <b>CHẾ TẠO ROBOT PHÒNG THỦ (15s)</b>\n<color=#C084FC>Stan/Mike/George/Leela</color>", new Color(0.42f, 0.22f, 0.55f), font, layer);
			Text txtCraft = btnCraft.GetComponentInChildren<Text>();

			return (btnWorker, txtWorker, btnFac, txtFac, btnCraft, txtCraft);
		}

		private static (Button, Text, Button, Text) PopulateSpecialPage(Transform parent, Font font, int layer)
		{
			Button btnPick = CreateStandardButton(parent, "Btn_UpgradePickaxe", "⛏️ <b>NÂNG CẤP CÚP SẮT (CẤP 1)</b>\n<color=#FFD700>⭐ Nâng Cấp: 0🪙</color>", new Color(0.18f, 0.32f, 0.48f), font, layer);
			Text txtPick = btnPick.GetComponentInChildren<Text>();

			Button btnWall = CreateStandardButton(parent, "Btn_UpgradeWall", "⭐ <b>NÂNG CẤP TOÀN BỘ BỨC TƯỜNG</b>\n<color=#00FFFF>✦ Bạc Sáng • Vàng Kim • Đen Titan</color>", new Color(0.55f, 0.35f, 0.15f), font, layer);
			Text txtWall = btnWall.GetComponentInChildren<Text>();

			return (btnPick, txtPick, btnWall, txtWall);
		}

		private static Button CreateStandardButton(Transform parent, string name, string label, Color bgColor, Font font, int layer)
		{
			GameObject btnObj = new GameObject(name, typeof(RectTransform));
			btnObj.layer = layer;
			btnObj.transform.SetParent(parent, false);

			LayoutElement le = btnObj.AddComponent<LayoutElement>();
			le.minHeight = 85f;
			le.preferredHeight = 85f;

			Image bg = btnObj.AddComponent<Image>();
			bg.color = bgColor;

			Outline outline = btnObj.AddComponent<Outline>();
			outline.effectColor = new Color(0f, 0f, 0f, 0.5f);
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
			t.text = label;
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

		private static GameObject EnsureWallPrefab()
		{
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

					if (tempWall.GetComponent<WallSegment>() == null) tempWall.AddComponent<WallSegment>();

					wallPrefab = PrefabUtility.SaveAsPrefabAsset(tempWall, "Assets/prefabs/Wall_Building.prefab");
					Object.DestroyImmediate(tempWall);
				}
			}
			return wallPrefab;
		}

		private static void EnsureWeaponIcons(string dir)
		{
			string[] files = { "Icon_Phaotuhanh.png", "Icon_Thunder.png", "Icon_Flamethrower.png", "Icon_Wall.png", "Icon_StarterPack.png" };
			bool needRefresh = false;

			for (int i = 0; i < files.Length; i++)
			{
				string path = $"{dir}/{files[i]}";
				if (!File.Exists(path))
				{
					CreateWeaponIcon(path, i);
					needRefresh = true;
				}
			}

			if (needRefresh)
			{
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
		}

		private static void CreateWeaponIcon(string savePath, int type)
		{
			int size = 256;
			Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
			Color baseBg = new Color(0.06f, 0.09f, 0.16f, 0.98f);

			Color borderColor = type == 0 ? new Color(0.2f, 0.7f, 1.0f) :
								type == 1 ? new Color(0.0f, 0.95f, 1.0f) :
								type == 2 ? new Color(1.0f, 0.45f, 0.1f) :
								type == 3 ? new Color(0.7f, 0.75f, 0.85f) :
											new Color(1.0f, 0.8f, 0.2f);

			Color coreColor = type == 0 ? new Color(1.0f, 0.75f, 0.2f) :   // Rocket vàng cam
							  type == 1 ? new Color(0.2f, 0.95f, 1.0f) :   // Thunder cyan
							  type == 2 ? new Color(1.0f, 0.35f, 0.1f) :   // Flame cam đỏ
							  type == 3 ? new Color(0.5f, 0.7f, 0.9f) :    // Wall
										  new Color(0.95f, 0.7f, 0.1f);    // Combo

			Vector2 center = new Vector2(size / 2f, size / 2f);

			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float d = Vector2.Distance(new Vector2(x, y), center);

					if (d > size * 0.48f)
					{
						tex.SetPixel(x, y, Color.clear);
					}
					else if (d > size * 0.44f)
					{
						tex.SetPixel(x, y, borderColor);
					}
					else
					{
						float grad = (float)y / size;
						Color c = Color.Lerp(baseBg, baseBg * 1.5f, grad);

						bool isShape = false;
						if (type == 0) // Pháo tự hành: bệ phóng đôi + tên lửa
						{
							if ((x >= 65 && x <= 105 && y >= 70 && y <= 180) ||
								(x >= 151 && x <= 191 && y >= 70 && y <= 180) ||
								(x >= 55 && x <= 201 && y >= 60 && y <= 95))
								isShape = true;
							if ((y > 180 && y <= 215) && (Mathf.Abs(x - 85) <= (215 - y) * 0.65f || Mathf.Abs(x - 171) <= (215 - y) * 0.65f))
								isShape = true;
						}
						else if (type == 1) // Trụ Sét: Tháp Tesla + tia sét
						{
							if (d < 45f || (d < 80f && d > 65f) || (x >= 115 && x <= 141 && y >= 45 && y <= 190))
								isShape = true;
							if ((Mathf.Abs(x + y * 0.5f - 180) < 15 && y > 95 && y < 195) ||
								(Mathf.Abs(x - y * 0.6f - 30) < 15 && y > 55 && y < 145))
								isShape = true;
						}
						else if (type == 2) // Phun Lửa: Nòng súng đôi + ngọn lửa
						{
							if ((x >= 95 && x <= 161 && y >= 55 && y <= 150) ||
								(x >= 110 && x <= 146 && y >= 145 && y <= 185))
								isShape = true;
							if (Vector2.Distance(new Vector2(x, y), new Vector2(128, 195)) < 35f && y >= 175)
								isShape = true;
						}
						else if (type == 3) // Tường
						{
							if ((x >= 45 && x <= 211 && y >= 65 && y <= 175) && (x % 40 > 6))
								isShape = true;
						}
						else // Combo Starter pack
						{
							if ((d < 65f && d > 40f) || (Mathf.Abs(x - 128) < 14 && Mathf.Abs(y - 128) < 80) || (Mathf.Abs(y - 128) < 14 && Mathf.Abs(x - 128) < 80))
								isShape = true;
						}

						tex.SetPixel(x, y, isShape ? coreColor : c);
					}
				}
			}

			tex.Apply();
			byte[] bytes = tex.EncodeToPNG();
			File.WriteAllBytes(savePath, bytes);

			AssetDatabase.ImportAsset(savePath, ImportAssetOptions.ForceUpdate);
			TextureImporter ti = AssetImporter.GetAtPath(savePath) as TextureImporter;
			if (ti != null)
			{
				ti.textureType = TextureImporterType.Sprite;
				ti.spriteImportMode = SpriteImportMode.Single;
				ti.alphaIsTransparency = true;
				ti.mipmapEnabled = false;
				ti.SaveAndReimport();
			}
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
