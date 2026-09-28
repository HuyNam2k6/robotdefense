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

            // 1. Setup Main Camera căn giữa sân chơi
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
            // Đặt góc nhìn nghiêng nhìn thẳng tâm sân
            mainCam.transform.position = new Vector3(0f, 16f, -12f);
            mainCam.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
            mainCam.orthographic = true;
            mainCam.orthographicSize = 9.5f;

            // 2. Setup GameManager
            ResourceManager resManager = Object.FindFirstObjectByType<ResourceManager>();
            if (resManager == null)
            {
                GameObject gm = new GameObject("GameManager");
                resManager = Undo.AddComponent<ResourceManager>(gm);
                Undo.RegisterCreatedObjectUndo(gm, "Create GameManager");
            }

            // 3. Tạo Sàn Đất (Ground) Dọc
            GameObject ground = GameObject.Find("Ground");
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "Ground";
                Undo.RegisterCreatedObjectUndo(ground, "Create Ground");
            }
            ground.transform.position = new Vector3(0f, -0.2f, 2f);
            ground.transform.localScale = new Vector3(8.5f, 0.4f, 15f);

            Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            groundMat.color = new Color(0.15f, 0.17f, 0.22f); // Xám than chì hiện đại
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            // 4. Dựng Căn Cứ Chỉ Huy (Base HQ) ở phía dưới sàn
            GameObject baseHq = GameObject.Find("Base_HQ");
            if (baseHq == null)
            {
                baseHq = new GameObject("Base_HQ");
                baseHq.transform.position = new Vector3(0f, 0f, -3.5f);

                GameObject baseBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseBody.name = "Body";
                baseBody.transform.SetParent(baseHq.transform, false);
                baseBody.transform.localScale = new Vector3(3f, 1.2f, 2f);
                Material baseMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                baseMat.color = new Color(0.2f, 0.5f, 0.85f); // Xanh dương kim loại
                baseBody.GetComponent<MeshRenderer>().sharedMaterial = baseMat;

                // Tháp chỉ huy có anten
                GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tower.name = "Tower";
                tower.transform.SetParent(baseHq.transform, false);
                tower.transform.localPosition = new Vector3(0f, 1.1f, 0f);
                tower.transform.localScale = new Vector3(1f, 0.6f, 1f);
                Material towerMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                towerMat.color = new Color(0.9f, 0.75f, 0.2f); // Vàng neon
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
            stoneNode.transform.position = new Vector3(-2.2f, 0.6f, -0.5f);
            stoneNode.transform.localScale = new Vector3(1.6f, 1.4f, 1.6f);

            ResourceNode stoneRes = stoneNode.GetComponent<ResourceNode>() ?? stoneNode.AddComponent<ResourceNode>();
            SetField(stoneRes, "resourceType", ResourceType.Stone);

            Material stoneMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            stoneMat.color = new Color(0.6f, 0.65f, 0.75f); // Xám đá sáng
            stoneNode.GetComponent<MeshRenderer>().sharedMaterial = stoneMat;

            // 6. Tạo Mỏ Gạch (Brick Node)
            GameObject brickNode = GameObject.Find("Mỏ_Gạch (Brick)");
            if (brickNode == null)
            {
                brickNode = GameObject.CreatePrimitive(PrimitiveType.Cube);
                brickNode.name = "Mỏ_Gạch (Brick)";
                Undo.RegisterCreatedObjectUndo(brickNode, "Create Brick Node");
            }
            brickNode.transform.position = new Vector3(2.2f, 0.6f, -0.5f);
            brickNode.transform.localScale = new Vector3(1.6f, 1.4f, 1.6f);

            ResourceNode brickRes = brickNode.GetComponent<ResourceNode>() ?? brickNode.AddComponent<ResourceNode>();
            SetField(brickRes, "resourceType", ResourceType.Brick);

            Material brickMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            brickMat.color = new Color(0.9f, 0.45f, 0.22f); // Cam đất nung
            brickNode.GetComponent<MeshRenderer>().sharedMaterial = brickMat;

            // 7. Tạo / Cập nhật Canvas UI
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
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

            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
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
            Debug.Log("<color=#00FFAA><b>[SceneSetupHelper]</b> Đã cập nhật Scene với Căn cứ Base HQ và sửa lỗi Text thành công! Bấm Play ngay nào!</color>");
        }

        private static TextMeshProUGUI CreateTextItem(Transform parent, string name, string text, Color color)
        {
            GameObject textObj = new GameObject(name, typeof(RectTransform));
            textObj.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();

            // Gán font an toàn để không bị lỗi NullReferenceException
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font == null)
            {
                font = TMP_Settings.defaultFontAsset;
            }
            if (font != null)
            {
                tmp.font = font;
            }

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
