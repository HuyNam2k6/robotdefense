#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using System.IO;

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
            BuildCraftingUI();
        }

        [MenuItem("Tools/🤖 Cài Đặt Bảng Chế Tạo Rô Bốt Chuẩn Giao Diện Mới")]
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
            pRect.sizeDelta = new Vector2(920f, 1140f); // Tỉ lệ chuẩn nửa màn hình dọc

            // Nền tối cao cấp Cyber Navy
            Image bgImg = panelObj.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.08f, 0.13f, 0.98f);

            Outline panelOutline = panelObj.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0f, 0.85f, 1f, 0.85f); // Viền Cyan công nghệ
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
            hRect.sizeDelta = new Vector2(0f, 130f);

            Image hBg = headerObj.AddComponent<Image>();
            hBg.color = new Color(0.07f, 0.12f, 0.19f, 0.95f);

            // Icon Ống Ngắm Target vàng bên trái
            GameObject crosshairObj = new GameObject("Crosshair_Icon", typeof(RectTransform), typeof(Text));
            crosshairObj.layer = uiLayer;
            crosshairObj.transform.SetParent(headerObj.transform, false);
            RectTransform chRt = crosshairObj.GetComponent<RectTransform>();
            chRt.anchorMin = new Vector2(0f, 0.5f);
            chRt.anchorMax = new Vector2(0f, 0.5f);
            chRt.pivot = new Vector2(0f, 0.5f);
            chRt.anchoredPosition = new Vector2(20f, 5f);
            chRt.sizeDelta = new Vector2(60f, 60f);
            Text chTxt = crosshairObj.GetComponent<Text>();
            chTxt.font = safeFont;
            chTxt.text = "🎯";
            chTxt.fontSize = 42;
            chTxt.alignment = TextAnchor.MiddleCenter;

            // Tiêu đề: TRUNG TÂM CHẾ TẠO RÔ BỐT
            GameObject titleObj = new GameObject("Title_Text", typeof(RectTransform), typeof(Text));
            titleObj.layer = uiLayer;
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform tRect = titleObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 0.45f);
            tRect.anchorMax = new Vector2(0.85f, 1f);
            tRect.offsetMin = new Vector2(85f, 0f);
            tRect.offsetMax = Vector2.zero;
            Text titleTxt = titleObj.GetComponent<Text>();
            titleTxt.font = safeFont;
            titleTxt.text = "<b>TRUNG TÂM CHẾ TẠO</b> <color=#FFB703><b>RÔ BỐT</b></color>";
            titleTxt.fontSize = 30;
            titleTxt.color = Color.white;
            titleTxt.alignment = TextAnchor.MiddleLeft;

            // Dòng phụ: Subtitle & Quân số
            GameObject subObj = new GameObject("Subtitle_Text", typeof(RectTransform), typeof(Text));
            subObj.layer = uiLayer;
            subObj.transform.SetParent(headerObj.transform, false);
            RectTransform subRect = subObj.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 0f);
            subRect.anchorMax = new Vector2(0.85f, 0.45f);
            subRect.offsetMin = new Vector2(85f, 6f);
            subRect.offsetMax = Vector2.zero;
            Text subTxt = subObj.GetComponent<Text>();
            subTxt.font = safeFont;
            subTxt.text = "Hoàn thành các nhiệm vụ để nhận thưởng và mở khóa công nghệ mới.\n⚡ <b>Quân số:</b> <color=#00FF88>0/4</color>";
            subTxt.fontSize = 14;
            subTxt.color = new Color(0.75f, 0.85f, 0.95f, 0.9f);
            subTxt.alignment = TextAnchor.MiddleLeft;

            // Nút đóng [X] màu đỏ góc trên bên phải
            GameObject closeBtnObj = new GameObject("Close_Button", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtnObj.layer = uiLayer;
            closeBtnObj.transform.SetParent(headerObj.transform, false);
            RectTransform cBtnRect = closeBtnObj.GetComponent<RectTransform>();
            cBtnRect.anchorMin = new Vector2(1f, 1f);
            cBtnRect.anchorMax = new Vector2(1f, 1f);
            cBtnRect.pivot = new Vector2(1f, 1f);
            cBtnRect.anchoredPosition = new Vector2(-18f, -18f);
            cBtnRect.sizeDelta = new Vector2(50f, 50f);

            Image cBtnImg = closeBtnObj.GetComponent<Image>();
            cBtnImg.color = new Color(0.85f, 0.18f, 0.18f, 1f);

            GameObject closeTxtObj = new GameObject("X_Text", typeof(RectTransform), typeof(Text));
            closeTxtObj.layer = uiLayer;
            closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform cTxtRt = closeTxtObj.GetComponent<RectTransform>();
            cTxtRt.anchorMin = Vector2.zero;
            cTxtRt.anchorMax = Vector2.one;
            cTxtRt.sizeDelta = Vector2.zero;
            Text closeTxt = closeTxtObj.GetComponent<Text>();
            closeTxt.font = safeFont;
            closeTxt.text = "✖";
            closeTxt.fontSize = 24;
            closeTxt.fontStyle = FontStyle.Bold;
            closeTxt.color = Color.white;
            closeTxt.alignment = TextAnchor.MiddleCenter;

            // ================= 4. KHU VỰC 4 THẺ ROBOT (CARDS CONTAINER) =================
            GameObject cardsContainer = new GameObject("Cards_Container", typeof(RectTransform));
            cardsContainer.layer = uiLayer;
            cardsContainer.transform.SetParent(panelObj.transform, false);
            RectTransform ccRect = cardsContainer.GetComponent<RectTransform>();
            ccRect.anchorMin = new Vector2(0f, 0f);
            ccRect.anchorMax = new Vector2(1f, 1f);
            ccRect.offsetMin = new Vector2(18f, 85f);
            ccRect.offsetMax = new Vector2(-18f, -145f);

            VerticalLayoutGroup vlg = cardsContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Dữ liệu hiển thị 4 thẻ
            string[] badges = new string[] { "01", "02", "03", "04" };
            string[] names = new string[] { "STAN PHÁO NHỆN", "MIKE ĐẤU SĨ", "GEORGE SÁT THỦ", "LEELA XẠ THỦ" };
            string[] descs = new string[]
            {
                "Pháo Nhện Hỏa Lực Tầm Xa (70 HP / 30 DMG)",
                "Tanker Hộ Pháp Tiền Tuyến (80 HP / 45 DMG)",
                "Sát Thủ Cơ Động Lướt Nhanh (60 HP / 35 DMG)",
                "Xạ Thủ Tầm Xa (60 HP / 40 DMG)"
            };
            string[] iconFileNames = new string[] { "Icon_Stan.png", "Icon_Mike.png", "Icon_George.png", "Icon_Leela.png" };

            Button[] craftButtons = new Button[4];
            Button[] infoButtons = new Button[4];
            Text[] robotNameTexts = new Text[4];
            Text[] robotCostTexts = new Text[4];
            Text[] robotDescTexts = new Text[4];
            Image[] robotIconImages = new Image[4];
            Sprite[] robotSprites = new Sprite[4];

            // Tải 4 Sprite icon
            for (int i = 0; i < 4; i++)
            {
                string iconPath = $"Assets/UI/RobotIcons/{iconFileNames[i]}";
                robotSprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            }

            for (int i = 0; i < 4; i++)
            {
                int botIndex = i;
                GameObject cardObj = new GameObject($"Card_{badges[i]}_{names[i]}", typeof(RectTransform), typeof(Image));
                cardObj.layer = uiLayer;
                cardObj.transform.SetParent(cardsContainer.transform, false);

                RectTransform cardRt = cardObj.GetComponent<RectTransform>();
                cardRt.sizeDelta = new Vector2(0f, 180f);

                Image cardBg = cardObj.GetComponent<Image>();
                cardBg.color = new Color(0.06f, 0.11f, 0.17f, 0.95f);

                Outline cardOutline = cardObj.AddComponent<Outline>();
                cardOutline.effectColor = new Color(0f, 0.65f, 0.85f, 0.7f);
                cardOutline.effectDistance = new Vector2(1.5f, -1.5f);

                // --- 1. Badge số 01, 02, 03, 04 ---
                GameObject badgeObj = new GameObject("Badge", typeof(RectTransform), typeof(Image));
                badgeObj.layer = uiLayer;
                badgeObj.transform.SetParent(cardObj.transform, false);
                RectTransform bRt = badgeObj.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0f, 0.5f);
                bRt.anchorMax = new Vector2(0f, 0.5f);
                bRt.pivot = new Vector2(0f, 0.5f);
                bRt.anchoredPosition = new Vector2(12f, 0f);
                bRt.sizeDelta = new Vector2(46f, 75f);
                Image bImg = badgeObj.GetComponent<Image>();
                bImg.color = new Color(1f, 0.72f, 0.05f, 1f); // Vàng Cam nổi bật

                GameObject bTxtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                bTxtObj.layer = uiLayer;
                bTxtObj.transform.SetParent(badgeObj.transform, false);
                RectTransform btRt = bTxtObj.GetComponent<RectTransform>();
                btRt.anchorMin = Vector2.zero;
                btRt.anchorMax = Vector2.one;
                btRt.sizeDelta = Vector2.zero;
                Text btTxt = bTxtObj.GetComponent<Text>();
                btTxt.font = safeFont;
                btTxt.text = badges[i];
                btTxt.fontSize = 24;
                btTxt.fontStyle = FontStyle.Bold;
                btTxt.color = new Color(0.08f, 0.08f, 0.08f, 1f);
                btTxt.alignment = TextAnchor.MiddleCenter;

                // --- 2. Khung Icon Robot ---
                GameObject iconFrameObj = new GameObject("Icon_Frame", typeof(RectTransform), typeof(Image));
                iconFrameObj.layer = uiLayer;
                iconFrameObj.transform.SetParent(cardObj.transform, false);
                RectTransform ifRt = iconFrameObj.GetComponent<RectTransform>();
                ifRt.anchorMin = new Vector2(0f, 0.5f);
                ifRt.anchorMax = new Vector2(0f, 0.5f);
                ifRt.pivot = new Vector2(0f, 0.5f);
                ifRt.anchoredPosition = new Vector2(68f, 0f);
                ifRt.sizeDelta = new Vector2(130f, 130f);
                Image ifBg = iconFrameObj.GetComponent<Image>();
                ifBg.color = new Color(0.04f, 0.07f, 0.12f, 0.9f);
                Outline ifOutline = iconFrameObj.AddComponent<Outline>();
                ifOutline.effectColor = new Color(0f, 0.8f, 1f, 0.5f);
                ifOutline.effectDistance = new Vector2(1f, -1f);

                // Image ảnh robot
                GameObject iconImgObj = new GameObject("Robot_Icon", typeof(RectTransform), typeof(Image));
                iconImgObj.layer = uiLayer;
                iconImgObj.transform.SetParent(iconFrameObj.transform, false);
                RectTransform iiRt = iconImgObj.GetComponent<RectTransform>();
                iiRt.anchorMin = Vector2.zero;
                iiRt.anchorMax = Vector2.one;
                iiRt.offsetMin = new Vector2(6f, 6f);
                iiRt.offsetMax = new Vector2(-6f, -6f);
                Image rImg = iconImgObj.GetComponent<Image>();
                if (robotSprites[i] != null) rImg.sprite = robotSprites[i];
                rImg.preserveAspect = true;
                robotIconImages[i] = rImg;

                // --- 3. Cụm Thông Tin Ở Giữa (Tên, Mô tả, Chi phí) ---
                GameObject infoColObj = new GameObject("Info_Column", typeof(RectTransform));
                infoColObj.layer = uiLayer;
                infoColObj.transform.SetParent(cardObj.transform, false);
                RectTransform icRt = infoColObj.GetComponent<RectTransform>();
                icRt.anchorMin = new Vector2(0f, 0f);
                icRt.anchorMax = new Vector2(1f, 1f);
                icRt.offsetMin = new Vector2(215f, 12f);
                icRt.offsetMax = new Vector2(-215f, -12f);

                // Tên Robot & Nút [!]
                GameObject nameRow = new GameObject("Name_Row", typeof(RectTransform));
                nameRow.layer = uiLayer;
                nameRow.transform.SetParent(infoColObj.transform, false);
                RectTransform nrRt = nameRow.GetComponent<RectTransform>();
                nrRt.anchorMin = new Vector2(0f, 0.65f);
                nrRt.anchorMax = new Vector2(1f, 1f);
                nrRt.offsetMin = Vector2.zero;
                nrRt.offsetMax = Vector2.zero;

                GameObject nameTxtObj = new GameObject("Name_Text", typeof(RectTransform), typeof(Text));
                nameTxtObj.layer = uiLayer;
                nameTxtObj.transform.SetParent(nameRow.transform, false);
                RectTransform ntRt = nameTxtObj.GetComponent<RectTransform>();
                ntRt.anchorMin = new Vector2(0f, 0f);
                ntRt.anchorMax = new Vector2(0.85f, 1f);
                ntRt.offsetMin = Vector2.zero;
                ntRt.offsetMax = Vector2.zero;
                Text nTxt = nameTxtObj.GetComponent<Text>();
                nTxt.font = safeFont;
                nTxt.text = $"<b>{names[i]}</b>";
                nTxt.fontSize = 21;
                nTxt.color = Color.white;
                nTxt.alignment = TextAnchor.MiddleLeft;
                robotNameTexts[i] = nTxt;

                // Nút [!] Thông số
                GameObject infoBtnObj = new GameObject("Info_Button", typeof(RectTransform), typeof(Image), typeof(Button));
                infoBtnObj.layer = uiLayer;
                infoBtnObj.transform.SetParent(nameRow.transform, false);
                RectTransform ibRt = infoBtnObj.GetComponent<RectTransform>();
                ibRt.anchorMin = new Vector2(1f, 0.5f);
                ibRt.anchorMax = new Vector2(1f, 0.5f);
                ibRt.pivot = new Vector2(1f, 0.5f);
                ibRt.anchoredPosition = new Vector2(0f, 0f);
                ibRt.sizeDelta = new Vector2(32f, 32f);
                Image ibImg = infoBtnObj.GetComponent<Image>();
                ibImg.color = new Color(0f, 0.7f, 0.9f, 0.85f);
                Button infoBtn = infoBtnObj.GetComponent<Button>();
                infoButtons[i] = infoBtn;

                GameObject ibTxtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                ibTxtObj.layer = uiLayer;
                ibTxtObj.transform.SetParent(infoBtnObj.transform, false);
                RectTransform ibtRt = ibTxtObj.GetComponent<RectTransform>();
                ibtRt.anchorMin = Vector2.zero;
                ibtRt.anchorMax = Vector2.one;
                ibtRt.sizeDelta = Vector2.zero;
                Text ibTxt = ibTxtObj.GetComponent<Text>();
                ibTxt.font = safeFont;
                ibTxt.text = "!";
                ibTxt.fontSize = 20;
                ibTxt.fontStyle = FontStyle.Bold;
                ibTxt.color = Color.black;
                ibTxt.alignment = TextAnchor.MiddleCenter;

                // Mô tả kỹ năng
                GameObject descTxtObj = new GameObject("Desc_Text", typeof(RectTransform), typeof(Text));
                descTxtObj.layer = uiLayer;
                descTxtObj.transform.SetParent(infoColObj.transform, false);
                RectTransform dtRt = descTxtObj.GetComponent<RectTransform>();
                dtRt.anchorMin = new Vector2(0f, 0.32f);
                dtRt.anchorMax = new Vector2(1f, 0.65f);
                dtRt.offsetMin = Vector2.zero;
                dtRt.offsetMax = Vector2.zero;
                Text dTxt = descTxtObj.GetComponent<Text>();
                dTxt.font = safeFont;
                dTxt.text = descs[i];
                dTxt.fontSize = 13;
                dTxt.color = new Color(0.65f, 0.8f, 0.95f, 0.9f);
                dTxt.alignment = TextAnchor.MiddleLeft;
                robotDescTexts[i] = dTxt;

                // Hiển thị Chi Phí: 🪙 Vàng: 0  🪵 Gỗ: 0  💎 Kim Cương: 0
                GameObject costTxtObj = new GameObject("Cost_Text", typeof(RectTransform), typeof(Text));
                costTxtObj.layer = uiLayer;
                costTxtObj.transform.SetParent(infoColObj.transform, false);
                RectTransform ctRt = costTxtObj.GetComponent<RectTransform>();
                ctRt.anchorMin = new Vector2(0f, 0f);
                ctRt.anchorMax = new Vector2(1f, 0.32f);
                ctRt.offsetMin = Vector2.zero;
                ctRt.offsetMax = Vector2.zero;
                Text cTxt = costTxtObj.GetComponent<Text>();
                cTxt.font = safeFont;
                cTxt.text = "🪙 <b>0</b>  🪵 <b>0</b>  💎 <b>0</b>";
                cTxt.fontSize = 14;
                cTxt.color = new Color(1f, 0.88f, 0.3f, 1f);
                cTxt.alignment = TextAnchor.MiddleLeft;
                robotCostTexts[i] = cTxt;

                // --- 4. Cụm Bên Phải: Pill Thời Gian & Nút [⚡ SẢN XUẤT] ---
                GameObject rightColObj = new GameObject("Right_Column", typeof(RectTransform));
                rightColObj.layer = uiLayer;
                rightColObj.transform.SetParent(cardObj.transform, false);
                RectTransform rcRt = rightColObj.GetComponent<RectTransform>();
                rcRt.anchorMin = new Vector2(1f, 0f);
                rcRt.anchorMax = new Vector2(1f, 1f);
                rcRt.pivot = new Vector2(1f, 0.5f);
                rcRt.anchoredPosition = new Vector2(-15f, 0f);
                rcRt.sizeDelta = new Vector2(185f, 0f);

                // Pill thời gian / phần thưởng
                GameObject pillObj = new GameObject("Reward_Pill", typeof(RectTransform), typeof(Image));
                pillObj.layer = uiLayer;
                pillObj.transform.SetParent(rightColObj.transform, false);
                RectTransform pRt = pillObj.GetComponent<RectTransform>();
                pRt.anchorMin = new Vector2(0f, 0.58f);
                pRt.anchorMax = new Vector2(1f, 0.92f);
                pRt.offsetMin = Vector2.zero;
                pRt.offsetMax = Vector2.zero;
                Image pImg = pillObj.GetComponent<Image>();
                pImg.color = new Color(0.05f, 0.16f, 0.18f, 0.9f);
                Outline pillOutline = pillObj.AddComponent<Outline>();
                pillOutline.effectColor = new Color(0f, 0.85f, 0.65f, 0.6f);
                pillOutline.effectDistance = new Vector2(1f, -1f);

                GameObject pillTxtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                pillTxtObj.layer = uiLayer;
                pillTxtObj.transform.SetParent(pillObj.transform, false);
                RectTransform ptRt = pillTxtObj.GetComponent<RectTransform>();
                ptRt.anchorMin = Vector2.zero;
                ptRt.anchorMax = Vector2.one;
                ptRt.sizeDelta = Vector2.zero;
                Text pillTxt = pillTxtObj.GetComponent<Text>();
                pillTxt.font = safeFont;
                pillTxt.text = "⚡ <color=#00FFAA><b>15 Giây</b></color>";
                pillTxt.fontSize = 13;
                pillTxt.color = Color.white;
                pillTxt.alignment = TextAnchor.MiddleCenter;

                // NÚT BẤM SẢN XUẤT XANH LÁ GRADIENT
                GameObject craftBtnObj = new GameObject("Craft_Button", typeof(RectTransform), typeof(Image), typeof(Button));
                craftBtnObj.layer = uiLayer;
                craftBtnObj.transform.SetParent(rightColObj.transform, false);
                RectTransform cbRt = craftBtnObj.GetComponent<RectTransform>();
                cbRt.anchorMin = new Vector2(0f, 0.08f);
                cbRt.anchorMax = new Vector2(1f, 0.50f);
                cbRt.offsetMin = Vector2.zero;
                cbRt.offsetMax = Vector2.zero;

                Image cbImg = craftBtnObj.GetComponent<Image>();
                cbImg.color = new Color(0f, 0.72f, 0.32f, 1f); // Xanh lá cây công nghệ
                Outline cbOutline = craftBtnObj.AddComponent<Outline>();
                cbOutline.effectColor = new Color(0.2f, 1f, 0.5f, 0.8f);
                cbOutline.effectDistance = new Vector2(1.5f, -1.5f);

                Button cBtn = craftBtnObj.GetComponent<Button>();
                craftButtons[i] = cBtn;

                GameObject cbTxtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                cbTxtObj.layer = uiLayer;
                cbTxtObj.transform.SetParent(craftBtnObj.transform, false);
                RectTransform cbtRt = cbTxtObj.GetComponent<RectTransform>();
                cbtRt.anchorMin = Vector2.zero;
                cbtRt.anchorMax = Vector2.one;
                cbtRt.sizeDelta = Vector2.zero;
                Text cbTxt = cbTxtObj.GetComponent<Text>();
                cbTxt.font = safeFont;
                cbTxt.text = "⚡ <b>SẢN XUẤT</b>";
                cbTxt.fontSize = 17;
                cbTxt.fontStyle = FontStyle.Bold;
                cbTxt.color = Color.white;
                cbTxt.alignment = TextAnchor.MiddleCenter;
            }

            // ================= 5. FOOTER BAR (HỘP CẢNH BÁO / MẸO CHIẾN THUẬT) =================
            GameObject footerObj = new GameObject("Footer_Bar", typeof(RectTransform), typeof(Image));
            footerObj.layer = uiLayer;
            footerObj.transform.SetParent(panelObj.transform, false);
            RectTransform ftRect = footerObj.GetComponent<RectTransform>();
            ftRect.anchorMin = new Vector2(0f, 0f);
            ftRect.anchorMax = new Vector2(1f, 0f);
            ftRect.pivot = new Vector2(0.5f, 0f);
            ftRect.anchoredPosition = new Vector2(0f, 14f);
            ftRect.sizeDelta = new Vector2(-36f, 56f);

            Image ftBg = footerObj.GetComponent<Image>();
            ftBg.color = new Color(0.12f, 0.08f, 0.03f, 0.9f);
            Outline ftOutline = footerObj.AddComponent<Outline>();
            ftOutline.effectColor = new Color(1f, 0.72f, 0.05f, 0.85f); // Viền vàng cam
            ftOutline.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject ftTxtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            ftTxtObj.layer = uiLayer;
            ftTxtObj.transform.SetParent(footerObj.transform, false);
            RectTransform fttRt = ftTxtObj.GetComponent<RectTransform>();
            fttRt.anchorMin = Vector2.zero;
            fttRt.anchorMax = Vector2.one;
            fttRt.offsetMin = new Vector2(12f, 0f);
            fttRt.offsetMax = new Vector2(-12f, 0f);
            Text ftTxt = ftTxtObj.GetComponent<Text>();
            ftTxt.font = safeFont;
            ftTxt.text = "⚠️ <color=#FFB703><b>Mẹo:</b></color> Nhấn máy sản xuất năng lượng để tăng tốc quá trình hoàn thành nhiệm vụ.";
            ftTxt.fontSize = 13;
            ftTxt.color = new Color(0.9f, 0.9f, 0.9f, 0.95f);
            ftTxt.alignment = TextAnchor.MiddleLeft;

            // ================= 6. MODAL POPUP THÔNG SỐ CHI TIẾT [!] =================
            GameObject modalObj = new GameObject("Info_Modal_Panel", typeof(RectTransform), typeof(Image));
            modalObj.layer = uiLayer;
            modalObj.transform.SetParent(panelObj.transform, false);
            RectTransform mRect = modalObj.GetComponent<RectTransform>();
            mRect.anchorMin = new Vector2(0.5f, 0.5f);
            mRect.anchorMax = new Vector2(0.5f, 0.5f);
            mRect.pivot = new Vector2(0.5f, 0.5f);
            mRect.anchoredPosition = Vector2.zero;
            mRect.sizeDelta = new Vector2(740f, 780f);

            Image mBg = modalObj.GetComponent<Image>();
            mBg.color = new Color(0.04f, 0.07f, 0.12f, 0.99f);
            Outline mOutline = modalObj.AddComponent<Outline>();
            mOutline.effectColor = new Color(1f, 0.75f, 0f, 0.9f);
            mOutline.effectDistance = new Vector2(3f, -3f);

            // Header Modal
            GameObject mHeadObj = new GameObject("Modal_Header", typeof(RectTransform), typeof(Text));
            mHeadObj.layer = uiLayer;
            mHeadObj.transform.SetParent(modalObj.transform, false);
            RectTransform mhRt = mHeadObj.GetComponent<RectTransform>();
            mhRt.anchorMin = new Vector2(0f, 1f);
            mhRt.anchorMax = new Vector2(1f, 1f);
            mhRt.pivot = new Vector2(0.5f, 1f);
            mhRt.anchoredPosition = new Vector2(0f, -20f);
            mhRt.sizeDelta = new Vector2(-40f, 80f);
            Text mhTxt = mHeadObj.GetComponent<Text>();
            mhTxt.font = safeFont;
            mhTxt.text = "THÔNG SỐ ROBOT";
            mhTxt.fontSize = 26;
            mhTxt.fontStyle = FontStyle.Bold;
            mhTxt.color = Color.white;
            mhTxt.alignment = TextAnchor.MiddleCenter;

            // Content Modal
            GameObject mBodyObj = new GameObject("Modal_Content", typeof(RectTransform), typeof(Text));
            mBodyObj.layer = uiLayer;
            mBodyObj.transform.SetParent(modalObj.transform, false);
            RectTransform mbRt = mBodyObj.GetComponent<RectTransform>();
            mbRt.anchorMin = new Vector2(0f, 0f);
            mbRt.anchorMax = new Vector2(1f, 1f);
            mbRt.offsetMin = new Vector2(35f, 90f);
            mbRt.offsetMax = new Vector2(-35f, -110f);
            Text mbTxt = mBodyObj.GetComponent<Text>();
            mbTxt.font = safeFont;
            mbTxt.text = "Chi tiết thông số...";
            mbTxt.fontSize = 17;
            mbTxt.color = new Color(0.9f, 0.95f, 1f);
            mbTxt.alignment = TextAnchor.UpperLeft;

            // Nút đóng Modal
            GameObject mCloseBtnObj = new GameObject("Modal_Close_Button", typeof(RectTransform), typeof(Image), typeof(Button));
            mCloseBtnObj.layer = uiLayer;
            mCloseBtnObj.transform.SetParent(modalObj.transform, false);
            RectTransform mcbRt = mCloseBtnObj.GetComponent<RectTransform>();
            mcbRt.anchorMin = new Vector2(0.5f, 0f);
            mcbRt.anchorMax = new Vector2(0.5f, 0f);
            mcbRt.pivot = new Vector2(0.5f, 0f);
            mcbRt.anchoredPosition = new Vector2(0f, 22f);
            mcbRt.sizeDelta = new Vector2(240f, 52f);
            Image mcbImg = mCloseBtnObj.GetComponent<Image>();
            mcbImg.color = new Color(0.85f, 0.2f, 0.2f, 1f);

            GameObject mcbTxtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            mcbTxtObj.layer = uiLayer;
            mcbTxtObj.transform.SetParent(mCloseBtnObj.transform, false);
            RectTransform mcbtRt = mcbTxtObj.GetComponent<RectTransform>();
            mcbtRt.anchorMin = Vector2.zero;
            mcbtRt.anchorMax = Vector2.one;
            mcbtRt.sizeDelta = Vector2.zero;
            Text mcbTxt = mcbTxtObj.GetComponent<Text>();
            mcbTxt.font = safeFont;
            mcbTxt.text = "<b>ĐÓNG</b>";
            mcbTxt.fontSize = 18;
            mcbTxt.color = Color.white;
            mcbTxt.alignment = TextAnchor.MiddleCenter;

            modalObj.SetActive(false);

            // ================= 7. GẮN COMPONENT ROBOTCRAFTINGUI VÀ ĐẤU DÂY =================
            RobotCraftingUI uiComp = canvasObj.GetComponent<RobotCraftingUI>();
            if (uiComp == null) uiComp = canvasObj.AddComponent<RobotCraftingUI>();

            uiComp.mainCraftingPanel = panelObj;
            uiComp.closePanelButton = closeBtnObj.GetComponent<Button>();
            uiComp.headerTitleText = titleTxt;
            uiComp.armyStatusText = subTxt;

            uiComp.craftButtons = craftButtons;
            uiComp.infoButtons = infoButtons;
            uiComp.robotNameTexts = robotNameTexts;
            uiComp.robotCostTexts = robotCostTexts;
            uiComp.robotDescTexts = robotDescTexts;
            uiComp.robotIconImages = robotIconImages;
            uiComp.robotSprites = robotSprites;

            uiComp.infoModalPanel = modalObj;
            uiComp.infoModalTitleText = mhTxt;
            uiComp.infoModalContentText = mbTxt;
            uiComp.infoModalCloseButton = mCloseBtnObj.GetComponent<Button>();

            // Mặc định ẩn panel khi game bắt đầu (chỉ mở khi chạm Nhà máy)
            panelObj.SetActive(false);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=#00FF88><b>[RobotCraftingUI]</b> ĐÃ THIẾT KẾ VÀ DỰNG HOÀN THIỆN BẢNG CHẾ TẠO RÔ BỐT CHUẨN GIAO DIỆN MỚI 100%!</color>");
        }
    }
}
#endif
