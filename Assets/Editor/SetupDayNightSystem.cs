#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

namespace RobotDefense.Editor
{
    [InitializeOnLoad]
    public static class SetupDayNightSystem
    {
        static SetupDayNightSystem()
        {
            EditorApplication.delayCall += () =>
            {
                CheckAndAutoSetup();
            };
        }

        [MenuItem("Tools/☀️ Thiết Lập Panel DayNight Lên Hierarchy (Ẩn / Hiện)")]
        public static void CheckAndAutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.isLoaded) return;

            bool sceneDirty = false;

            // 1. Tìm hoặc tạo GameObject [Day_Night_System] trong Scene Hierarchy
            DayNightSystem dayNightSystem = Object.FindAnyObjectByType<DayNightSystem>();
            if (dayNightSystem == null)
            {
                GameObject dnsObj = new GameObject("[Day_Night_System]");
                dayNightSystem = dnsObj.AddComponent<DayNightSystem>();
                Undo.RegisterCreatedObjectUndo(dnsObj, "Tạo [Day_Night_System]");
                sceneDirty = true;
            }

            // 2. Tìm Canvas chính (Shop_Canvas)
            Canvas canvas = DayNightSystem.GetMainScreenCanvas();
            if (canvas == null)
            {
                Debug.LogWarning("[SetupDayNightSystem] Không tìm thấy Canvas chính trong Scene!");
                return;
            }

            // 3. Tìm hoặc tạo Panel_DayNightHUD nằm trực tiếp trong Shop_Canvas ở Hierarchy
            Transform existingPanelTr = canvas.transform.Find("Panel_DayNightHUD");
            GameObject panelObj;

            Font safeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            int uiLayer = LayerMask.NameToLayer("UI");

            if (existingPanelTr == null)
            {
                panelObj = new GameObject("Panel_DayNightHUD", typeof(RectTransform));
                panelObj.layer = uiLayer;
                panelObj.transform.SetParent(canvas.transform, false);

                RectTransform rt = panelObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-18f, -38f);
                rt.sizeDelta = new Vector2(250f, 68f);

                Image bg = panelObj.AddComponent<Image>();
                bg.color = new Color(0.08f, 0.10f, 0.16f, 0.92f);

                Outline outline = panelObj.AddComponent<Outline>();
                outline.effectColor = new Color(0.2f, 0.75f, 1f, 0.85f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);

                // StatusTitle
                GameObject titleObj = new GameObject("StatusTitle", typeof(RectTransform), typeof(Text));
                titleObj.layer = uiLayer;
                titleObj.transform.SetParent(panelObj.transform, false);
                RectTransform tRt = titleObj.GetComponent<RectTransform>();
                tRt.anchorMin = new Vector2(0f, 0.5f);
                tRt.anchorMax = new Vector2(0.58f, 1f);
                tRt.anchoredPosition = new Vector2(8f, -2f);
                tRt.sizeDelta = Vector2.zero;

                Text statusTxt = titleObj.GetComponent<Text>();
                statusTxt.font = safeFont;
                statusTxt.fontSize = 18;
                statusTxt.fontStyle = FontStyle.Bold;
                statusTxt.alignment = TextAnchor.MiddleLeft;
                statusTxt.color = new Color(1f, 0.88f, 0.2f, 1f);
                statusTxt.supportRichText = true;
                statusTxt.text = "☀️ <b>NGÀY 1</b>";

                // CountdownTimer
                GameObject timerObj = new GameObject("CountdownTimer", typeof(RectTransform), typeof(Text));
                timerObj.layer = uiLayer;
                timerObj.transform.SetParent(panelObj.transform, false);
                RectTransform tmRt = timerObj.GetComponent<RectTransform>();
                tmRt.anchorMin = new Vector2(0f, 0f);
                tmRt.anchorMax = new Vector2(0.58f, 0.5f);
                tmRt.anchoredPosition = new Vector2(8f, 2f);
                tmRt.sizeDelta = Vector2.zero;

                Text timerTxt = timerObj.GetComponent<Text>();
                timerTxt.font = safeFont;
                timerTxt.fontSize = 18;
                timerTxt.fontStyle = FontStyle.Bold;
                timerTxt.alignment = TextAnchor.MiddleLeft;
                timerTxt.color = Color.white;
                timerTxt.text = "⏱️ <b>02:00</b>";

                // Btn_DefenseAction
                GameObject btnObj = new GameObject("Btn_DefenseAction", typeof(RectTransform), typeof(Image), typeof(Button));
                btnObj.layer = uiLayer;
                btnObj.transform.SetParent(panelObj.transform, false);
                RectTransform bRt = btnObj.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0.58f, 0.1f);
                bRt.anchorMax = new Vector2(0.96f, 0.9f);
                bRt.sizeDelta = Vector2.zero;

                Image btnImg = btnObj.GetComponent<Image>();
                btnImg.color = new Color(0.85f, 0.45f, 0.12f, 1f);

                GameObject btnTxtObj = new GameObject("BtnText", typeof(RectTransform), typeof(Text));
                btnTxtObj.layer = uiLayer;
                btnTxtObj.transform.SetParent(btnObj.transform, false);
                RectTransform btRt = btnTxtObj.GetComponent<RectTransform>();
                btRt.anchorMin = Vector2.zero;
                btRt.anchorMax = Vector2.one;
                btRt.sizeDelta = Vector2.zero;

                Text bTxt = btnTxtObj.GetComponent<Text>();
                bTxt.font = safeFont;
                bTxt.fontSize = 16;
                bTxt.fontStyle = FontStyle.Bold;
                bTxt.alignment = TextAnchor.MiddleCenter;
                bTxt.color = Color.white;
                bTxt.supportRichText = true;
                bTxt.text = "🛡️ <b>PHÒNG THỦ</b>";

                Undo.RegisterCreatedObjectUndo(panelObj, "Tạo Panel_DayNightHUD");
                sceneDirty = true;
            }
            else
            {
                panelObj = existingPanelTr.gameObject;
            }

            // 4. Kết nối tham chiếu vào DayNightSystem
            if (dayNightSystem != null)
            {
                dayNightSystem.dayNightHUDPanel = panelObj;
                dayNightSystem.statusTitleText = panelObj.transform.Find("StatusTitle")?.GetComponent<Text>();
                dayNightSystem.timerCountdownText = panelObj.transform.Find("CountdownTimer")?.GetComponent<Text>();
                Transform bTr = panelObj.transform.Find("Btn_DefenseAction");
                if (bTr != null)
                {
                    dayNightSystem.defenseActionButton = bTr.GetComponent<Button>();
                    dayNightSystem.actionButtonText = bTr.Find("BtnText")?.GetComponent<Text>();
                }

                // Cập nhật trạng thái hiển thị theo cài đặt showDayNightHUD (mặc định để ẩn theo yêu cầu)
                panelObj.SetActive(dayNightSystem.showDayNightHUD);
                EditorUtility.SetDirty(dayNightSystem);
                sceneDirty = true;
            }

            if (sceneDirty)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                Debug.Log("<color=#00FF88>[SetupDayNightSystem]</color> Đã tạo xong Panel_DayNightHUD hiện rõ ràng trên Hierarchy dưới Shop_Canvas (có thể bật/tắt ẩn hiện tùy ý)!");
            }
        }
    }
}
#endif
