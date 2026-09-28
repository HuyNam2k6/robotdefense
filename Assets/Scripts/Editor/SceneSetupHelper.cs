using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using IdleFactoryDefense.Core;
using IdleFactoryDefense.Managers;
using IdleFactoryDefense.Gameplay;
using IdleFactoryDefense.UI;

namespace IdleFactoryDefense.Editor
{
    public static class SceneSetupHelper
    {
        [MenuItem("Tools/⚡ Setup Idle Factory Defense Scene (1 Click)")]
        public static void SetupCompleteScene()
        {
            Undo.SetCurrentGroupName("Setup Idle Factory Scene");
            int group = Undo.GetCurrentGroup();

            // 1. Setup Main Camera căn giữa sân chơi, hỗ trợ Pan & Zoom
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                Undo.RegisterCreatedObjectUndo(camObj, "Create Main Camera");
            }

            IsometricCameraController camCtrl = mainCam.GetComponent<IsometricCameraController>();
            if (camCtrl == null)
            {
                camCtrl = Undo.AddComponent<IsometricCameraController>(mainCam.gameObject);
            }
            mainCam.transform.position = new Vector3(0f, 18f, -6f);
            mainCam.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
            mainCam.orthographic = true;
            mainCam.orthographicSize = 10f;
            camCtrl.SetMapBounds(new Vector2(-6f, 6f), new Vector2(-10f, 18f));

            // 2. Setup GameManager
            ResourceManager resManager = Object.FindAnyObjectByType<ResourceManager>();
            if (resManager == null)
            {
                GameObject gm = new GameObject("GameManager");
                resManager = Undo.AddComponent<ResourceManager>(gm);
                Undo.RegisterCreatedObjectUndo(gm, "Create GameManager");
            }

            // 3. Tạo Sàn Đất Lớn (Big Map Ground)
            GameObject ground = GameObject.Find("Ground");
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "Ground";
                Undo.RegisterCreatedObjectUndo(ground, "Create Ground");
            }
            ground.transform.position = new Vector3(0f, -0.2f, 5f);
            ground.transform.localScale = new Vector3(14f, 0.4f, 26f); // Map to gấp đôi!

            Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            groundMat.color = new Color(0.14f, 0.16f, 0.20f); // Xám chì công nghiệp
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            // Vạch ranh giới tiền tuyến (Frontline Border)
            GameObject border = GameObject.Find("Frontline_Border");
            if (border == null)
            {
                border = GameObject.CreatePrimitive(PrimitiveType.Cube);
                border.name = "Frontline_Border";
                Undo.RegisterCreatedObjectUndo(border, "Create Border");
            }
            border.transform.position = new Vector3(0f, 0.05f, 4f);
            border.transform.localScale = new Vector3(14f, 0.1f, 0.3f);
            Material borderMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            borderMat.color = new Color(0.95f, 0.75f, 0.1f, 0.8f); // Vạch vàng cảnh báo
            border.GetComponent<MeshRenderer>().sharedMaterial = borderMat;

            // 4. Dựng Căn Cứ Chỉ Huy (Base HQ) ở phía dưới sàn
            GameObject baseHq = GameObject.Find("Base_HQ");
            if (baseHq == null)
            {
                baseHq = new GameObject("Base_HQ");
                baseHq.transform.position = new Vector3(0f, 0f, -4.5f);

                GameObject baseBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseBody.name = "Body";
                baseBody.transform.SetParent(baseHq.transform, false);
                baseBody.transform.localScale = new Vector3(3.5f, 1.4f, 2.5f);
                Material baseMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                baseMat.color = new Color(0.2f, 0.45f, 0.85f); // Xanh dương kim loại
                baseBody.GetComponent<MeshRenderer>().sharedMaterial = baseMat;

                GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tower.name = "RadarTower";
                tower.transform.SetParent(baseHq.transform, false);
                tower.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                tower.transform.localScale = new Vector3(1.2f, 0.6f, 1.2f);
                Material towerMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                towerMat.color = new Color(0.1f, 0.95f, 0.75f); // Xanh ngọc phát sáng
                tower.GetComponent<MeshRenderer>().sharedMaterial = towerMat;

                Undo.RegisterCreatedObjectUndo(baseHq, "Create Base HQ");
            }

            // 5. Tạo Mỏ Đá (Stone Node)
            GameObject stoneNode = GameObject.Find("Mỏ_Đá (Stone)");
            if (stoneNode == null)
            {
                stoneNode = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stoneNode.name = "Mỏ_Đá (Stone)";
                Undo.RegisterCreatedObjectUndo(stoneNode, "Create Stone Node");
            }
            stoneNode.transform.position = new Vector3(-3f, 0.7f, -1f);
            stoneNode.transform.localScale = new Vector3(1.8f, 1.5f, 1.8f);

            ResourceNode stoneRes = stoneNode.GetComponent<ResourceNode>() ?? stoneNode.AddComponent<ResourceNode>();
            SetField(stoneRes, "resourceType", ResourceType.Stone);

            Material stoneMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            stoneMat.color = new Color(0.6f, 0.65f, 0.75f);
            stoneNode.GetComponent<MeshRenderer>().sharedMaterial = stoneMat;

            // 6. Tạo Mỏ Gạch (Brick Node)
            GameObject brickNode = GameObject.Find("Mỏ_Gạch (Brick)");
            if (brickNode == null)
            {
                brickNode = GameObject.CreatePrimitive(PrimitiveType.Cube);
                brickNode.name = "Mỏ_Gạch (Brick)";
                Undo.RegisterCreatedObjectUndo(brickNode, "Create Brick Node");
            }
            brickNode.transform.position = new Vector3(3f, 0.7f, -1f);
            brickNode.transform.localScale = new Vector3(1.8f, 1.5f, 1.8f);

            ResourceNode brickRes = brickNode.GetComponent<ResourceNode>() ?? brickNode.AddComponent<ResourceNode>();
            SetField(brickRes, "resourceType", ResourceType.Brick);

            Material brickMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            brickMat.color = new Color(0.9f, 0.45f, 0.22f);
            brickNode.GetComponent<MeshRenderer>().sharedMaterial = brickMat;

            // 7. Tạo Bệ Đặt Trụ Phòng Thủ (Turret Platform) ở tiền tuyến
            GameObject platform = GameObject.Find("Bệ_Trụ_Súng (Platform)");
            if (platform == null)
            {
                platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                platform.name = "Bệ_Trụ_Súng (Platform)";
                platform.transform.position = new Vector3(0f, 0.1f, 3.2f);
                platform.transform.localScale = new Vector3(2f, 0.2f, 2f);
                Material platMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                platMat.color = new Color(0.3f, 0.35f, 0.42f);
                platform.GetComponent<MeshRenderer>().sharedMaterial = platMat;
                Undo.RegisterCreatedObjectUndo(platform, "Create Platform");
            }

            // 8. Cập nhật Canvas UI
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                Undo.DestroyObjectImmediate(canvas.gameObject);
            }

            GameObject canvasObj = new GameObject("Canvas_GameUI");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");

            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
            }

            // SafeArea Panel
            GameObject safePanel = new GameObject("SafeAreaPanel", typeof(RectTransform));
            safePanel.transform.SetParent(canvasObj.transform, false);
            safePanel.AddComponent<SafeAreaHandler>();
            RectTransform safeRect = safePanel.GetComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.offsetMin = Vector2.zero;
            safeRect.offsetMax = Vector2.zero;

            // Top Bar Panel
            GameObject topBar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
            topBar.transform.SetParent(safePanel.transform, false);
            RectTransform topRect = topBar.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0f, 1f);
            topRect.anchorMax = new Vector2(1f, 1f);
            topRect.pivot = new Vector2(0.5f, 1f);
            topRect.anchoredPosition = new Vector2(0f, -20f);
            topRect.sizeDelta = new Vector2(-60f, 110f);

            Image bgImg = topBar.GetComponent<Image>();
            bgImg.color = new Color(0.08f, 0.10f, 0.14f, 0.9f);

            TopBarUI topBarUI = topBar.AddComponent<TopBarUI>();

            HorizontalLayoutGroup layout = topBar.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.padding = new RectOffset(20, 20, 10, 10);
            layout.spacing = 20;

            TextMeshProUGUI stoneText = CreateTextItem(topBar.transform, "StoneText", "STONE: 0", new Color(0.8f, 0.88f, 1f));
            TextMeshProUGUI brickText = CreateTextItem(topBar.transform, "BrickText", "BRICK: 0", new Color(1f, 0.65f, 0.45f));
            TextMeshProUGUI moneyText = CreateTextItem(topBar.transform, "MoneyText", "GOLD: $0", new Color(1f, 0.88f, 0.25f));

            SetField(topBarUI, "stoneText", stoneText);
            SetField(topBarUI, "brickText", brickText);
            SetField(topBarUI, "moneyText", moneyText);

            Undo.CollapseUndoOperations(group);
            Debug.Log("<color=#00FFAA><b>[SceneSetupHelper]</b> Đã mở rộng Map to gấp đôi! Bro có thể giữ chuột kéo để lướt map và cuộn chuột để zoom!</color>");
        }

        private static TextMeshProUGUI CreateTextItem(Transform parent, string name, string text, Color color)
        {
            GameObject textObj = new GameObject(name, typeof(RectTransform));
            textObj.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();

            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font == null) font = TMP_Settings.defaultFontAsset;
            if (font != null) tmp.font = font;

            tmp.text = text;
            tmp.color = color;
            tmp.fontSize = 38;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(target, value);
        }
    }
}
