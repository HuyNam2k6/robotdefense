#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace IdleFactoryDefense.Editor
{
    public static class SetupMobileUI
    {
        [MenuItem("Tools/📱 Tạo Giao Diện Điều Khiển Mobile (Joystick & Buttons)")]
        public static void CreateMobileUI()
        {
            // 1. TẠO HOẶC TÌM EVENTSYSTEM
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            }

            // 2. TẠO CANVAS
            GameObject canvasObj = GameObject.Find("Mobile_Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("Mobile_Canvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
            }

            Transform canvasTr = canvasObj.transform;

            // 3. TẠO VIRTUAL JOYSTICK Ở GÓC DƯỚI TRÁI
            Transform oldJoystick = canvasTr.Find("Joystick_Zone");
            if (oldJoystick != null) Object.DestroyImmediate(oldJoystick.gameObject);

            GameObject joyZone = new GameObject("Joystick_Zone");
            joyZone.transform.SetParent(canvasTr, false);
            RectTransform joyZoneRect = joyZone.AddComponent<RectTransform>();
            joyZoneRect.anchorMin = new Vector2(0, 0);
            joyZoneRect.anchorMax = new Vector2(0, 0);
            joyZoneRect.pivot = new Vector2(0, 0);
            joyZoneRect.anchoredPosition = new Vector2(80, 80);
            joyZoneRect.sizeDelta = new Vector2(300, 300);

            // Background hình tròn mờ
            GameObject joyBg = new GameObject("Joystick_Background");
            joyBg.transform.SetParent(joyZone.transform, false);
            RectTransform joyBgRect = joyBg.AddComponent<RectTransform>();
            joyBgRect.sizeDelta = new Vector2(260, 260);
            Image bgImg = joyBg.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            // Handle (núm gạt)
            GameObject joyHandle = new GameObject("Joystick_Handle");
            joyHandle.transform.SetParent(joyBg.transform, false);
            RectTransform joyHandleRect = joyHandle.AddComponent<RectTransform>();
            joyHandleRect.sizeDelta = new Vector2(100, 100);
            Image handleImg = joyHandle.AddComponent<Image>();
            handleImg.color = new Color(1f, 0.8f, 0.2f, 0.9f);

            // Gắn component VirtualJoystick
            VirtualJoystick vj = joyZone.AddComponent<VirtualJoystick>();
            vj.background = joyBgRect;
            vj.handle = joyHandleRect;

            // 4. TÌM PLAYER TRONG SCENE
            PlayerController player = Object.FindObjectOfType<PlayerController>();
            if (player != null)
            {
                player.joystick = vj;
                EditorUtility.SetDirty(player);
            }

            // 5. TẠO CÁC NÚT BẤM ACTION Ở GÓC DƯỚI PHẢI
            Transform oldActions = canvasTr.Find("Action_Buttons");
            if (oldActions != null) Object.DestroyImmediate(oldActions.gameObject);

            GameObject actionZone = new GameObject("Action_Buttons");
            actionZone.transform.SetParent(canvasTr, false);
            RectTransform actionRect = actionZone.AddComponent<RectTransform>();
            actionRect.anchorMin = new Vector2(1, 0);
            actionRect.anchorMax = new Vector2(1, 0);
            actionRect.pivot = new Vector2(1, 0);
            actionRect.anchoredPosition = new Vector2(-60, 80);
            actionRect.sizeDelta = new Vector2(350, 350);

            // Nút 1: ĐẤM / ĐÀO ĐÁ (Nút tròn to)
            GameObject punchBtnObj = CreateRoundButton(actionZone.transform, "Punch_Button", new Vector2(0, 0), new Vector2(160, 160), new Color(0.9f, 0.3f, 0.2f, 0.9f), "⚔️ ĐẤM");
            Button punchBtn = punchBtnObj.GetComponent<Button>();
            if (player != null)
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(punchBtn.onClick, player.OnPunchButtonPressed);
            }

            // Nút 2: VẪY TAY (Nút tròn nhỏ cạnh bên)
            GameObject waveBtnObj = CreateRoundButton(actionZone.transform, "Wave_Button", new Vector2(-160, 40), new Vector2(110, 110), new Color(0.2f, 0.7f, 0.9f, 0.9f), "👋 CHÀO");
            Button waveBtn = waveBtnObj.GetComponent<Button>();
            if (player != null)
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(waveBtn.onClick, player.OnWaveButtonPressed);
            }

            Selection.activeGameObject = canvasObj;
            Debug.Log("<color=#00FF88><b>[Mobile UI]</b> ĐÃ TẠO XONG JOYSTICK VÀ NÚT BẤM MOBILE CỰC ĐẸP!</color>");
        }

        private static GameObject CreateRoundButton(Transform parent, string name, Vector2 pos, Vector2 size, Color color, string label)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.color = color;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.pressedColor = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f, 1f);
            btn.colors = cb;

            // Text nhãn bên trong
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text txt = textObj.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = (int)(size.x * 0.22f);
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            return btnObj;
        }
    }
}
#endif

