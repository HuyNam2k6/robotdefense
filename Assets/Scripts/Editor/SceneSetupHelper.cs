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
    /// <summary>
    /// Công cụ 1 Click thiết lập tự động toàn bộ Scene:
    /// - Căn Camera Isometric dọc chuẩn 9:16
    /// - Tạo sàn đất dọc
    /// - Tạo mỏ Đá và mỏ Gạch (kèm màu sắc và script nảy)
    /// - Tạo GameManager (ResourceManager)
    /// - Tạo Canvas UI chuẩn Safe Area hiển thị tài nguyên
    /// </summary>
    public static class SceneSetupHelper
    {
        [MenuItem("Tools/⚡ Setup Idle Factory Defense Scene (1 Click)")]
        public static void SetupCompleteScene()
        {
            Undo.SetCurrentGroupName("Setup Idle Factory Scene");
            int group = Undo.GetCurrentGroup();

            // 1. Setup Main Camera
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
            camCtrl.SetupCamera();

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
                ground.transform.position = new Vector3(0f, -0.5f, 2f);
                ground.transform.localScale = new Vector3(8f, 0.2f, 15f);

                // Màu đất xám tối công nghiệp
                Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                groundMat.color = new Color(0.18f, 0.20f, 0.24f);
                ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

                Undo.RegisterCreatedObjectUndo(ground, "Create Ground");
            }

            // 4. Tạo Mỏ Đá (Stone Node)
            GameObject stoneNode = GameObject.Find("Mỏ_Đá (Stone)");
            if (stoneNode == null)
            {
                stoneNode = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stoneNode.name = "Mỏ_Đá (Stone)";
                stoneNode.transform.position = new Vector3(-2f, 0.5f, 0f);
                stoneNode.transform.localScale = new Vector3(1.5f, 1.2f, 1.5f);

                ResourceNode resNode = stoneNode.AddComponent<ResourceNode>();
                // Reflection gán ResourceType = Stone
                var field = typeof(ResourceNode).GetField("resourceType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) field.SetValue(resNode, ResourceType.Stone);

                Material stoneMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                stoneMat.color = new Color(0.55f, 0.60f, 0.68f); // Màu xám đá sáng
                stoneNode.GetComponent<MeshRenderer>().sharedMaterial = stoneMat;

                Undo.RegisterCreatedObjectUndo(stoneNode, "Create Stone Node");
            }

            // 5. Tạo Mỏ Gạch (Brick Node)
            GameObject brickNode = GameObject.Find("Mỏ_Gạch (Brick)");
            if (brickNode == null)
            {
                brickNode = GameObject.CreatePrimitive(PrimitiveType.Cube);
                brickNode.name = "Mỏ_Gạch (Brick)";
                brickNode.transform.position = new Vector3(2f, 0.5f, 0f);
                brickNode.transform.localScale = new Vector3(1.5f, 1.2f, 1.5f);

                ResourceNode resNode = brickNode.AddComponent<ResourceNode>();
                var field = typeof(ResourceNode).GetField("resourceType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) field.SetValue(resNode, ResourceType.Brick);

                Material brickMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                brickMat.color = new Color(0.85f, 0.42f, 0.25f); // Màu cam gạch
                brickNode.GetComponent<MeshRenderer>().sharedMaterial = brickMat;

                Undo.RegisterCreatedObjectUndo(brickNode, "Create Brick Node");
            }

            // 6. Tạo Canvas UI + SafeArea + TopBarUI
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas_GameUI");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
                canvasObj.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");

                // Đảm bảo có EventSystem
                if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    GameObject esObj = new GameObject("EventSystem");
                    esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
                }

                // Panel Safe Area
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
                topRect.sizeDelta = new Vector2(-40f, 120f);

                Image bgImg = topBar.GetComponent<Image>();
                bgImg.color = new Color(0.08f, 0.10f, 0.14f, 0.85f); // Khung đen mờ hiện đại

                TopBarUI topBarUI = topBar.AddComponent<TopBarUI>();

                // Tạo 3 Text: Stone, Brick, Money
                HorizontalLayoutGroup layout = topBar.AddComponent<HorizontalLayoutGroup>();
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = true;
                layout.padding = new RectOffset(20, 20, 10, 10);
                layout.spacing = 15;

                TextMeshProUGUI stoneText = CreateTextItem(topBar.transform, "StoneText", "🪨 0", new Color(0.8f, 0.85f, 0.95f));
                TextMeshProUGUI brickText = CreateTextItem(topBar.transform, "BrickText", "🧱 0", new Color(1f, 0.65f, 0.45f));
                TextMeshProUGUI moneyText = CreateTextItem(topBar.transform, "MoneyText", "💰 0", new Color(1f, 0.88f, 0.25f));

                // Gán vào TopBarUI qua reflection
                SetField(topBarUI, "stoneText", stoneText);
                SetField(topBarUI, "brickText", brickText);
                SetField(topBarUI, "moneyText", moneyText);
            }

            Undo.CollapseUndoOperations(group);
            Debug.Log("<color=#00FFAA><b>[SceneSetupHelper]</b> Đã tự động thiết lập toàn bộ Scene thành công 100%! Hãy bấm Play để test!</color>");
        }

        private static TextMeshProUGUI CreateTextItem(Transform parent, string name, string text, Color color)
        {
            GameObject textObj = new GameObject(name, typeof(RectTransform));
            textObj.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = color;
            tmp.fontSize = 42;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            return tmp;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(target, value);
        }
    }
}
