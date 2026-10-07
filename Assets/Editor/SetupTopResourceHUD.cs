#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace RobotDefense.Editor
{
    [InitializeOnLoad]
    public static class SetupTopResourceHUD
    {
        static SetupTopResourceHUD()
        {
            EditorApplication.delayCall += () =>
            {
                CheckAndAutoSetup();
            };
        }

        public static void CheckAndAutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null && canvas.GetComponentInChildren<TopResourceHUD>() == null)
            {
                SetupHUD();
            }
        }

        // [MenuItem("Tools/💎 Thiết Lập Thanh Tài Nguyên Đỉnh Màn Hình (Top HUD Bar)")]
        public static void SetupHUD()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[TopResourceHUD] Chưa tìm thấy Canvas trong Scene!");
                return;
            }

            TopResourceHUD existing = canvas.GetComponentInChildren<TopResourceHUD>();
            if (existing != null)
            {
                Debug.Log("<color=#00FF88>[TopResourceHUD]</color> Thanh tài nguyên đã có sẵn trên Canvas!");
                return;
            }

            GameObject hudObj = new GameObject("[Top_Resource_HUD]", typeof(RectTransform));
            hudObj.layer = LayerMask.NameToLayer("UI");
            hudObj.transform.SetParent(canvas.transform, false);
            hudObj.AddComponent<TopResourceHUD>();

            Undo.RegisterCreatedObjectUndo(hudObj, "Tạo Top Resource HUD");
            EditorUtility.SetDirty(canvas.gameObject);
            Debug.Log("<color=#00FF88>[TopResourceHUD]</color> Đã thiết lập thành công Thanh Tài Nguyên Đỉnh Màn Hình (Vàng, Đá, Gỗ, Điện, Kim Cương, Cài Đặt)!");
        }
    }
}
#endif
