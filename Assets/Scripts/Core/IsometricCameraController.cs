using UnityEngine;

namespace IdleFactoryDefense.Core
{
    /// <summary>
    /// Điều khiển Main Camera góc nhìn 3D Isometric chuẩn cho màn hình dọc (Portrait).
    /// Tự động căn chỉnh bao trọn Căn cứ (ở dưới) và Đường Quái tràn xuống (ở trên).
    /// </summary>
    [ExecuteAlways]
    public class IsometricCameraController : MonoBehaviour
    {
        [Header("Góc Nhìn Isometric")]
        [Tooltip("Góc nghiêng từ trên nhìn xuống (30° - 45° chuẩn Isometric)")]
        [SerializeField] private float pitchAngle = 45f;

        [Tooltip("Góc xoay quanh trục thẳng đứng")]
        [SerializeField] private float yawAngle = 30f;

        [Header("Khoảng Cách & Vị Trí Căn Cứ")]
        [SerializeField] private Vector3 targetLookPosition = new Vector3(0f, 0f, 2f);
        [SerializeField] private float distance = 15f;

        [Header("Chế độ Camera")]
        [SerializeField] private bool useOrthographic = true;
        [SerializeField] private float orthographicSize = 9f;

        private Camera _cam;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            SetupCamera();
        }

        private void OnValidate()
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            SetupCamera();
        }

        public void SetupCamera()
        {
            if (_cam == null) return;

            _cam.orthographic = useOrthographic;
            if (useOrthographic)
            {
                _cam.orthographicSize = orthographicSize;
            }

            // Tính toán góc quay
            Quaternion rotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
            transform.rotation = rotation;

            // Đặt vị trí lùi lại từ điểm nhìn mục tiêu
            Vector3 offset = rotation * new Vector3(0f, 0f, -distance);
            transform.position = targetLookPosition + offset;
        }
    }
}
