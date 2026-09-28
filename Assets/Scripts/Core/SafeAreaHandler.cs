using UnityEngine;

namespace IdleFactoryDefense.Core
{
    /// <summary>
    /// Tự động co giãn RectTransform theo vùng an toàn (Safe Area) của thiết bị.
    /// Giúp UI trên màn hình dọc không bị lẹm vào tai thỏ, nốt ruồi camera hay thanh vuốt Home của iPhone/Android.
    /// Gắn component này vào Panel gốc (Root Panel) bên trong Canvas.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaHandler : MonoBehaviour
    {
        private RectTransform _panelRect;
        private Rect _lastSafeArea = Rect.zero;
        private Vector2Int _lastScreenSize = Vector2Int.zero;
        private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;

        private void Awake()
        {
            _panelRect = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            // Kiểm tra nếu màn hình thay đổi kích thước hoặc xoay thì cập nhật lại
            if (_lastSafeArea != Screen.safeArea || 
                _lastScreenSize.x != Screen.width || 
                _lastScreenSize.y != Screen.height || 
                _lastOrientation != Screen.orientation)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            if (_panelRect == null) return;

            Rect safeArea = Screen.safeArea;
            _lastSafeArea = safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            _lastOrientation = Screen.orientation;

            // Chuyển đổi tọa độ Safe Area pixel sang tỷ lệ Anchor (0.0 đến 1.0)
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            _panelRect.anchorMin = anchorMin;
            _panelRect.anchorMax = anchorMax;
            _panelRect.offsetMin = Vector2.zero;
            _panelRect.offsetMax = Vector2.zero;
        }
    }
}
