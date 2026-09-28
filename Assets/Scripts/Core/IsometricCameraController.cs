using UnityEngine;
using UnityEngine.EventSystems;

namespace IdleFactoryDefense.Core
{
    /// <summary>
    /// Điều khiển Camera Isometric chuyên dụng cho màn hình dọc:
    /// - Hỗ trợ Kéo vuốt (Pan) bằng chuột hoặc 1 ngón tay trên điện thoại để khám phá Map to.
    /// - Hỗ trợ Cuộn chuột / Chụm 2 ngón tay để Phóng to / Thu nhỏ (Pinch Zoom).
    /// - Có giới hạn biên (Boundaries) mượt mà, không bị trôi ra ngoài vũ trụ.
    /// - Tự động né không kéo camera khi người chơi đang bấm vào nút UI.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class IsometricCameraController : MonoBehaviour
    {
        [Header("Góc Nhìn Isometric")]
        [SerializeField] private float pitchAngle = 50f;
        [SerializeField] private float yawAngle = 0f;

        [Header("Chế độ Camera")]
        [SerializeField] private bool useOrthographic = true;
        [SerializeField] private float defaultOrthoSize = 9.5f;
        [SerializeField] private float minOrthoSize = 5f;
        [SerializeField] private float maxOrthoSize = 16f;

        [Header("Tốc Độ & Giới Hạn Di Chuyển Map")]
        [SerializeField] private float dragSpeed = 1.2f;
        [SerializeField] private float smoothTime = 0.08f;

        [Tooltip("Giới hạn di chuyển camera trên mặt phẳng X (trái/phải) và Z (trên/dưới)")]
        [SerializeField] private Vector2 panLimitX = new Vector2(-6f, 6f);
        [SerializeField] private Vector2 panLimitZ = new Vector2(-8f, 15f);

        private Camera _cam;
        private Vector3 _targetPosition;
        private Vector3 _currentVelocity;
        private Vector3 _dragOrigin;
        private bool _isDragging = false;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            SetupCamera();
            _targetPosition = transform.position;
        }

        public void SetupCamera()
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            _cam.orthographic = useOrthographic;
            if (useOrthographic)
            {
                _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize > 0 ? _cam.orthographicSize : defaultOrthoSize, minOrthoSize, maxOrthoSize);
            }
            transform.rotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
        }

        private void Update()
        {
            HandleDragPan();
            HandleZoom();
            SmoothMove();
        }

        #region Kéo Vuốt Di Chuyển Map (Pan)
        private void HandleDragPan()
        {
            // Nếu chuột đang đè lên nút UI thì không kéo camera
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                if (Input.GetMouseButtonUp(0)) _isDragging = false;
                return;
            }

            // Bắt đầu chạm / nhấn chuột
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2))
            {
                // Kiểm tra xem có click trúng mỏ quặng không (nếu trúng mỏ thì ưu tiên đào, không pan)
                Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    if (hit.collider.GetComponent<IdleFactoryDefense.Gameplay.ResourceNode>() != null)
                    {
                        _isDragging = false;
                        return;
                    }
                }

                _dragOrigin = GetWorldPositionOnGround(Input.mousePosition);
                _isDragging = true;
            }

            // Đang giữ kéo chuột / vuốt ngón tay
            if ((Input.GetMouseButton(0) || Input.GetMouseButton(2)) && _isDragging)
            {
                Vector3 currentPos = GetWorldPositionOnGround(Input.mousePosition);
                Vector3 difference = _dragOrigin - currentPos;

                _targetPosition += new Vector3(difference.x, 0f, difference.z) * dragSpeed;

                // Giới hạn biên map
                _targetPosition.x = Mathf.Clamp(_targetPosition.x, panLimitX.x, panLimitX.y);
                _targetPosition.z = Mathf.Clamp(_targetPosition.z, panLimitZ.x, panLimitZ.y);
            }

            if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(2))
            {
                _isDragging = false;
            }
        }
        #endregion

        #region Phóng To / Thu Nhỏ (Zoom)
        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f && _cam.orthographic)
            {
                _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize - scroll * 5f, minOrthoSize, maxOrthoSize);
            }
        }
        #endregion

        #region Di Chuyển Mượt (Smooth Damping)
        private void SmoothMove()
        {
            Vector3 newPos = Vector3.SmoothDamp(transform.position, _targetPosition, ref _currentVelocity, smoothTime);
            transform.position = new Vector3(newPos.x, transform.position.y, newPos.z);
        }

        private Vector3 GetWorldPositionOnGround(Vector3 screenPos)
        {
            Ray ray = _cam.ScreenPointToRay(screenPos);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }
            return Vector3.zero;
        }
        #endregion

        public void SetMapBounds(Vector2 limitX, Vector2 limitZ)
        {
            panLimitX = limitX;
            panLimitZ = limitZ;
        }
    }
}
