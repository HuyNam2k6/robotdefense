#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

namespace IdleFactoryDefense.Editor
{
    [InitializeOnLoad]
    public static class SetupRobotCraftingUI
    {
        static SetupRobotCraftingUI()
        {
            EditorApplication.delayCall += () =>
            {
                CheckAndAutoSetup();
            };
        }

        public static void CheckAndAutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            // Kiểm tra nếu chưa có Robot_Crafting_Panel
            if (GameObject.Find("Robot_Crafting_Panel") == null)
            {
                BuildCraftingUI();
            }
        }

        [MenuItem("Tools/🤖 Cài Đặt Bảng Chế Tạo Robot Nửa Màn Hình (4 Icon & Nút !)")]
        public static void BuildCraftingUI()
        {
            if (EditorApplication.isPlaying) return;

            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer < 0) uiLayer = 5;

            // 1. Tìm Canvas chính
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[CraftingUI] Chưa tìm thấy Canvas trong Scene!");
                return;
            }

            GameObject canvasObj = canvas.gameObject;

            // Xóa panel cũ nếu có
            Transform oldPanel = canvasObj.transform.Find("Robot_Crafting_Panel");
            if (oldPanel != null) Object.DestroyImmediate(oldPanel.gameObject);

            Font safeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (safeFont == null) safeFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // ================= 2. TẠO PANEL CHÍNH TO NỬA MÀN HÌNH (CĂN GIỮA) =================
            GameObject panelObj = new GameObject("Robot_Crafting_Panel", typeof(RectTransform));
            panelObj.layer = uiLayer;
            panelObj.transform.SetParent(canvasObj.transform, false);

            RectTransform pRect = panelObj.GetComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.5f, 0.5f);
            pRect.anchorMax = new Vector2(0.5f, 0.5f);
            pRect.pivot = new Vector2(0.5f, 0.5f);
            pRect.anchoredPosition = Vector2.zero;
            pRect.sizeDelta = new Vector2(880f, 980f); // To đúng nửa màn hình dọc 1080x1920

            // Nền tối cao cấp Sci-Fi
            Image bgImg = panelObj.AddComponent<Image>();
            bgImg.color = new Color(0.10f, 0.12f, 0.16f, 0.98f);

            Outline panelOutline = panelObj.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0f, 0.85f, 1f, 0.8f); // Viền Cyan công nghệ
            panelOutline.effectDistance = new Vector2(2.5f, -2.5f);

            // ================= 3. HEADER & NÚT ĐÓNG =================
            GameObject headerObj = new GameObject("Header", typeof(RectTransform));
            headerObj.layer = uiLayer;
            headerObj.transform.SetParent(panelObj.transform, false);
            RectTransform hRect = headerObj.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0f, 1f);
            hRect.anchorMax = new Vector2(1f, 1f);
            hRect.pivot = new Vector2(0.5f, 1f);
            hRect.anchoredPosition = Vector2.zero;
            hRect.sizeDelta = new Vector2(0f, 110f);

            Image hBg = headerObj.AddComponent<Image>();
            hBg.color = new Color(0.14f, 0.18f, 0.24f, 0.95f);

            // Tiêu đề
            GameObject titleObj = new GameObject("Title_Text", typeof(RectTransform));
            titleObj.layer = uiLayer;
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform tRect = titleObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 0.35f);
            tRect.anchorMax = new Vector2(0.85f, 1f);
            tRect.offsetMin = new Vector2(20f, 0f);
            tRect.offsetMax = Vector2.zero;
            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.text = "🏭 <b>TRUNG TÂM CHẾ TẠO RÔ BỐT</b>";
            titleTxt.font = safeFont;
            titleTxt.fontSize = 30;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color = new Color(1f, 0.85f, 0.2f); // Vàng Gold
            titleTxt.alignment = TextAnchor.MiddleLeft;

            // Phụ đề trạng thái quân
            GameObject subObj = new GameObject("Subtitle_Text", typeof(RectTransform));
            subObj.layer = uiLayer;
            subObj.transform.SetParent(headerObj.transform, false);
            RectTransform sRect = subObj.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0f, 0f);
            sRect.anchorMax = new Vector2(0.85f, 0.45f);
            sRect.offsetMin = new Vector2(20f, 5f);
            sRect.offsetMax = Vector2.zero;
            Text subTxt = subObj.AddComponent<Text>();
            subTxt.text = "🛡️ Quân số phòng thủ: <b>0/8</b> • Thời gian đúc: <b>15s</b>";
            subTxt.font = safeFont;
            subTxt.fontSize = 20;
            subTxt.color = new Color(0.3f, 0.9f, 1f);
            subTxt.alignment = TextAnchor.MiddleLeft;

            // Nút đóng [✕]
            GameObject closeBtnObj = new GameObject("Btn_Close", typeof(RectTransform));
            closeBtnObj.layer = uiLayer;
            closeBtnObj.transform.SetParent(headerObj.transform, false);
            RectTransform cRect = closeBtnObj.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(1f, 0.5f);
            cRect.anchorMax = new Vector2(1f, 0.5f);
            cRect.pivot = new Vector2(1f, 0.5f);
            cRect.anchoredPosition = new Vector2(-15f, 0f);
            cRect.sizeDelta = new Vector2(56f, 56f);

            Image cImg = closeBtnObj.AddComponent<Image>();
            cImg.color = new Color(0.85f, 0.22f, 0.22f, 0.95f);
            Button btnClose = closeBtnObj.AddComponent<Button>();

            GameObject closeTxtObj = new GameObject("Text", typeof(RectTransform));
            closeTxtObj.layer = uiLayer;
            closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform ctRect = closeTxtObj.GetComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;
            ctRect.sizeDelta = Vector2.zero;
            Text ctTxt = closeTxtObj.AddComponent<Text>();
            ctTxt.text = "✕";
            ctTxt.font = safeFont;
            ctTxt.fontSize = 28;
            ctTxt.fontStyle = FontStyle.Bold;
            ctTxt.color = Color.white;
            ctTxt.alignment = TextAnchor.MiddleCenter;

            // ================= 4. KHU VỰC 4 THẺ ROBOT (DANH SÁCH 4 CON) =================
            GameObject cardsListObj = new GameObject("Robots_List", typeof(RectTransform));
            cardsListObj.layer = uiLayer;
            cardsListObj.transform.SetParent(panelObj.transform, false);
            RectTransform clRect = cardsListObj.GetComponent<RectTransform>();
            clRect.anchorMin = new Vector2(0f, 0f);
            clRect.anchorMax = new Vector2(1f, 1f);
            clRect.offsetMin = new Vector2(20f, 95f);
            clRect.offsetMax = new Vector2(-20f, -125f);

            VerticalLayoutGroup vLayout = cardsListObj.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 14f;
            vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = true;

            Button[] craftBtns = new Button[4];
            Button[] infoBtns = new Button[4];

            // 4 Robot
            string[] names = { "Stan Pháo Nhện", "Mike Đấu Sĩ", "George Sát Thủ", "Leela Xạ Thủ" };
            string[] roles = { "💥 Ụ Pháo Di Động Tầm Xa (220 HP / 32 DMG)", "🛡️ Tanker Hộ Pháp Tiền Tuyến (320 HP / 45 DMG)", "⚡ Sát Thủ Cơ Động Lướt Nhanh (160 HP / 35 DMG)", "🎯 Xạ Thủ Bắn Tỉa Tầm Xa (140 HP / 50 DMG)" };
            Color[] cardColors = {
                new Color(0.18f, 0.16f, 0.12f, 0.95f), // Stan - Nâu cam
                new Color(0.13f, 0.17f, 0.22f, 0.95f), // Mike - Xanh thép
                new Color(0.18f, 0.12f, 0.20f, 0.95f), // George - Tím sấm sét
                new Color(0.12f, 0.19f, 0.18f, 0.95f)  // Leela - Xanh ngọc
            };

            for (int i = 0; i < 4; i++)
            {
                GameObject card = CreateRobotCardRow(cardsListObj.transform, i, names[i], roles[i], cardColors[i], safeFont, uiLayer, out Button btnCraft, out Button btnInfo);
                craftBtns[i] = btnCraft;
                infoBtns[i] = btnInfo;
            }

            // ================= 5. THANH TIẾN ĐỘ SẢN XUẤT HIỆN TẠI (DƯỚI CÙNG) =================
            GameObject prodBarObj = new GameObject("Current_Production_Bar", typeof(RectTransform));
            prodBarObj.layer = uiLayer;
            prodBarObj.transform.SetParent(panelObj.transform, false);
            RectTransform pbRect = prodBarObj.GetComponent<RectTransform>();
            pbRect.anchorMin = new Vector2(0f, 0f);
            pbRect.anchorMax = new Vector2(1f, 0f);
            pbRect.pivot = new Vector2(0.5f, 0f);
            pbRect.anchoredPosition = new Vector2(0f, 15f);
            pbRect.sizeDelta = new Vector2(-40f, 65f);

            Image pbBg = prodBarObj.AddComponent<Image>();
            pbBg.color = new Color(0.14f, 0.17f, 0.22f, 0.95f);

            Outline pbOutline = prodBarObj.AddComponent<Outline>();
            pbOutline.effectColor = new Color(1f, 0.7f, 0f, 0.5f);
            pbOutline.effectDistance = new Vector2(1f, -1f);

            // Fill Bar
            GameObject fillObj = new GameObject("Fill", typeof(RectTransform));
            fillObj.layer = uiLayer;
            fillObj.transform.SetParent(prodBarObj.transform, false);
            RectTransform fRect = fillObj.GetComponent<RectTransform>();
            fRect.anchorMin = Vector2.zero;
            fRect.anchorMax = Vector2.one;
            fRect.sizeDelta = Vector2.zero;

            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(1f, 0.65f, 0f, 0.35f); // Cam mờ
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0f;

            // Text tiến độ
            GameObject pTxtObj = new GameObject("Text", typeof(RectTransform));
            pTxtObj.layer = uiLayer;
            pTxtObj.transform.SetParent(prodBarObj.transform, false);
            RectTransform ptRect = pTxtObj.GetComponent<RectTransform>();
            ptRect.anchorMin = Vector2.zero;
            ptRect.anchorMax = Vector2.one;
            ptRect.sizeDelta = Vector2.zero;
            Text prodTxt = pTxtObj.AddComponent<Text>();
            prodTxt.text = "⚡ Nhà máy sẵn sàng nhận lệnh chế tạo (15s)";
            prodTxt.font = safeFont;
            prodTxt.fontSize = 20;
            prodTxt.fontStyle = FontStyle.Bold;
            prodTxt.color = Color.white;
            prodTxt.alignment = TextAnchor.MiddleCenter;

            // ================= 6. MODAL POPUP THÔNG SỐ CHI TIẾT [!] =================
            GameObject modalObj = new GameObject("Info_Modal_Panel", typeof(RectTransform));
            modalObj.layer = uiLayer;
            modalObj.transform.SetParent(panelObj.transform, false);
            RectTransform mRect = modalObj.GetComponent<RectTransform>();
            mRect.anchorMin = new Vector2(0.5f, 0.5f);
            mRect.anchorMax = new Vector2(0.5f, 0.5f);
            mRect.pivot = new Vector2(0.5f, 0.5f);
            mRect.anchoredPosition = Vector2.zero;
            mRect.sizeDelta = new Vector2(740f, 760f);

            Image mBg = modalObj.AddComponent<Image>();
            mBg.color = new Color(0.08f, 0.10f, 0.14f, 0.99f);

            Outline mOutline = modalObj.AddComponent<Outline>();
            mOutline.effectColor = new Color(1f, 0.85f, 0.2f, 0.9f);
            mOutline.effectDistance = new Vector2(2f, -2f);

            // Tiêu đề Modal
            GameObject mTitleObj = new GameObject("Modal_Title", typeof(RectTransform));
            mTitleObj.layer = uiLayer;
            mTitleObj.transform.SetParent(modalObj.transform, false);
            RectTransform mtRect = mTitleObj.GetComponent<RectTransform>();
            mtRect.anchorMin = new Vector2(0f, 1f);
            mtRect.anchorMax = new Vector2(1f, 1f);
            mtRect.pivot = new Vector2(0.5f, 1f);
            mtRect.anchoredPosition = new Vector2(0f, -20f);
            mtRect.sizeDelta = new Vector2(-40f, 75f);
            Text mTitleTxt = mTitleObj.AddComponent<Text>();
            mTitleTxt.text = "💥 <b>STAN PHÁO NHỆN</b>\n<color=#00FFFF>Ụ Pháo Di Động Tầm Xa</color>";
            mTitleTxt.font = safeFont;
            mTitleTxt.fontSize = 24;
            mTitleTxt.fontStyle = FontStyle.Bold;
            mTitleTxt.color = new Color(1f, 0.85f, 0.2f);
            mTitleTxt.alignment = TextAnchor.MiddleCenter;

            // Nội dung thông số
            GameObject mContentObj = new GameObject("Modal_Content", typeof(RectTransform));
            mContentObj.layer = uiLayer;
            mContentObj.transform.SetParent(modalObj.transform, false);
            RectTransform mcRect = mContentObj.GetComponent<RectTransform>();
            mcRect.anchorMin = new Vector2(0f, 0f);
            mcRect.anchorMax = new Vector2(1f, 1f);
            mcRect.offsetMin = new Vector2(30f, 85f);
            mcRect.offsetMax = new Vector2(-30f, -100f);
            Text mContentTxt = mContentObj.AddComponent<Text>();
            mContentTxt.text = "Thông số chi tiết...";
            mContentTxt.font = safeFont;
            mContentTxt.fontSize = 21;
            mContentTxt.lineSpacing = 1.25f;
            mContentTxt.color = Color.white;
            mContentTxt.alignment = TextAnchor.UpperLeft;

            // Nút Đóng Modal
            GameObject mCloseBtn = new GameObject("Btn_CloseModal", typeof(RectTransform));
            mCloseBtn.layer = uiLayer;
            mCloseBtn.transform.SetParent(modalObj.transform, false);
            RectTransform mcbRect = mCloseBtn.GetComponent<RectTransform>();
            mcbRect.anchorMin = new Vector2(0.5f, 0f);
            mcbRect.anchorMax = new Vector2(0.5f, 0f);
            mcbRect.pivot = new Vector2(0.5f, 0f);
            mcbRect.anchoredPosition = new Vector2(0f, 18f);
            mcbRect.sizeDelta = new Vector2(260f, 52f);

            Image mcbImg = mCloseBtn.AddComponent<Image>();
            mcbImg.color = new Color(0.20f, 0.50f, 0.70f, 0.95f);
            Button btnCloseModal = mCloseBtn.AddComponent<Button>();

            GameObject mcbTxtObj = new GameObject("Text", typeof(RectTransform));
            mcbTxtObj.layer = uiLayer;
            mcbTxtObj.transform.SetParent(mCloseBtn.transform, false);
            RectTransform mcbtRect = mcbTxtObj.GetComponent<RectTransform>();
            mcbtRect.anchorMin = Vector2.zero;
            mcbtRect.anchorMax = Vector2.one;
            mcbtRect.sizeDelta = Vector2.zero;
            Text mcbtTxt = mcbTxtObj.AddComponent<Text>();
            mcbtTxt.text = "ĐÃ HIỂU";
            mcbtTxt.font = safeFont;
            mcbtTxt.fontSize = 22;
            mcbtTxt.fontStyle = FontStyle.Bold;
            mcbtTxt.color = Color.white;
            mcbtTxt.alignment = TextAnchor.MiddleCenter;

            modalObj.SetActive(false);

            // ================= 7. GẮN COMPONENT ROBOTCRAFTINGUI =================
            RobotCraftingUI uiScript = canvasObj.GetComponent<RobotCraftingUI>();
            if (uiScript == null) uiScript = canvasObj.AddComponent<RobotCraftingUI>();

            uiScript.mainCraftingPanel = panelObj;
            uiScript.closePanelButton = btnClose;
            uiScript.headerTitleText = titleTxt;
            uiScript.armyStatusText = subTxt;

            uiScript.currentProductionBar = prodBarObj;
            uiScript.currentProductionText = prodTxt;
            uiScript.currentProgressBarFill = fillImg;

            uiScript.craftButtons = craftBtns;
            uiScript.infoButtons = infoBtns;

            uiScript.infoModalPanel = modalObj;
            uiScript.infoModalTitleText = mTitleTxt;
            uiScript.infoModalContentText = mContentTxt;
            uiScript.infoModalCloseButton = btnCloseModal;

            panelObj.SetActive(false);

            // Đấu nối vào BuildingShopUI để nút Chế Tạo trong Shop mở panel này
            BuildingShopUI shopUI = canvasObj.GetComponent<BuildingShopUI>();
            if (shopUI != null)
            {
                if (shopUI.craftRobotButton != null)
                {
                    shopUI.craftRobotButton.onClick.RemoveAllListeners();
                    shopUI.craftRobotButton.onClick.AddListener(() =>
                    {
                        shopUI.CloseShopPanel();
                        uiScript.OpenPanel();
                    });
                }
            }

            EditorUtility.SetDirty(canvasObj);
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();

            Debug.Log("<color=#00FF88><b>[SetupCraftingUI]</b> ĐÃ THIẾT LẬP BẢNG CHẾ TẠO ROBOT TO NỬA MÀN HÌNH (4 ICON & NÚT !) THÀNH CÔNG!</color>");
        }

        private static GameObject CreateRobotCardRow(Transform parent, int index, string name, string role, Color bgColor, Font font, int layer, out Button btnCraft, out Button btnInfo)
        {
            GameObject card = new GameObject($"Card_Robot_{index}", typeof(RectTransform));
            card.layer = layer;
            card.transform.SetParent(parent, false);

            Image bg = card.AddComponent<Image>();
            bg.color = bgColor;

            Outline outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(0.3f, 0.7f, 0.9f, 0.4f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Layout ngang
            HorizontalLayoutGroup hLayout = card.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(16, 16, 10, 10);
            hLayout.spacing = 16f;
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = true;

            // 1. Icon & Nút [!] Thông số
            GameObject infoBtnObj = new GameObject("Btn_Info", typeof(RectTransform));
            infoBtnObj.layer = layer;
            infoBtnObj.transform.SetParent(card.transform, false);
            RectTransform ibRect = infoBtnObj.GetComponent<RectTransform>();
            ibRect.sizeDelta = new Vector2(62f, 62f);

            Image ibImg = infoBtnObj.AddComponent<Image>();
            ibImg.color = new Color(0.95f, 0.75f, 0.1f, 0.95f); // Vàng neon nổi bật
            btnInfo = infoBtnObj.AddComponent<Button>();

            Outline ibOutline = infoBtnObj.AddComponent<Outline>();
            ibOutline.effectColor = Color.black;
            ibOutline.effectDistance = new Vector2(1f, -1f);

            GameObject ibTxtObj = new GameObject("Text", typeof(RectTransform));
            ibTxtObj.layer = layer;
            ibTxtObj.transform.SetParent(infoBtnObj.transform, false);
            RectTransform ibtRect = ibTxtObj.GetComponent<RectTransform>();
            ibtRect.anchorMin = Vector2.zero;
            ibtRect.anchorMax = Vector2.one;
            ibtRect.sizeDelta = Vector2.zero;
            Text ibTxt = ibTxtObj.AddComponent<Text>();
            ibTxt.text = "<b>!</b>";
            ibTxt.font = font;
            ibTxt.fontSize = 34;
            ibTxt.fontStyle = FontStyle.Bold;
            ibTxt.color = new Color(0.1f, 0.1f, 0.1f);
            ibTxt.alignment = TextAnchor.MiddleCenter;

            // 2. Cột thông tin tên & vai trò
            GameObject textCol = new GameObject("Text_Col", typeof(RectTransform));
            textCol.layer = layer;
            textCol.transform.SetParent(card.transform, false);
            RectTransform tcRect = textCol.GetComponent<RectTransform>();
            tcRect.sizeDelta = new Vector2(480f, 80f);

            VerticalLayoutGroup tcLayout = textCol.AddComponent<VerticalLayoutGroup>();
            tcLayout.spacing = 4f;
            tcLayout.childAlignment = TextAnchor.MiddleLeft;
            tcLayout.childControlWidth = true;
            tcLayout.childControlHeight = false;
            tcLayout.childForceExpandWidth = true;
            tcLayout.childForceExpandHeight = false;

            GameObject nameObj = new GameObject("Name_Text", typeof(RectTransform));
            nameObj.layer = layer;
            nameObj.transform.SetParent(textCol.transform, false);
            Text nameTxt = nameObj.AddComponent<Text>();
            nameTxt.text = $"<b>{name.ToUpper()}</b>";
            nameTxt.font = font;
            nameTxt.fontSize = 24;
            nameTxt.fontStyle = FontStyle.Bold;
            nameTxt.color = Color.white;

            GameObject roleObj = new GameObject("Role_Text", typeof(RectTransform));
            roleObj.layer = layer;
            roleObj.transform.SetParent(textCol.transform, false);
            Text roleTxt = roleObj.AddComponent<Text>();
            roleTxt.text = role;
            roleTxt.font = font;
            roleTxt.fontSize = 17;
            roleTxt.color = new Color(0.75f, 0.85f, 0.95f);

            // 3. Nút Chế tạo (15s)
            GameObject craftBtnObj = new GameObject("Btn_Craft", typeof(RectTransform));
            craftBtnObj.layer = layer;
            craftBtnObj.transform.SetParent(card.transform, false);
            RectTransform cbRect = craftBtnObj.GetComponent<RectTransform>();
            cbRect.sizeDelta = new Vector2(230f, 68f);

            Image cbImg = craftBtnObj.AddComponent<Image>();
            cbImg.color = new Color(0.18f, 0.58f, 0.35f, 0.95f); // Xanh lục sản xuất
            btnCraft = craftBtnObj.AddComponent<Button>();

            Outline cbOutline = craftBtnObj.AddComponent<Outline>();
            cbOutline.effectColor = new Color(0.2f, 1f, 0.5f, 0.6f);
            cbOutline.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject cbTxtObj = new GameObject("Text", typeof(RectTransform));
            cbTxtObj.layer = layer;
            cbTxtObj.transform.SetParent(craftBtnObj.transform, false);
            RectTransform cbtRect = cbTxtObj.GetComponent<RectTransform>();
            cbtRect.anchorMin = Vector2.zero;
            cbtRect.anchorMax = Vector2.one;
            cbtRect.sizeDelta = Vector2.zero;
            Text cbTxt = cbTxtObj.AddComponent<Text>();
            cbTxt.text = "⚡ <b>SẢN XUẤT</b>\n<color=#FFD700>(15 Giây)</color>";
            cbTxt.font = font;
            cbTxt.fontSize = 19;
            cbTxt.fontStyle = FontStyle.Bold;
            cbTxt.color = Color.white;
            cbTxt.alignment = TextAnchor.MiddleCenter;

            return card;
        }
    }
}
#endif
