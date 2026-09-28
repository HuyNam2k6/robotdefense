using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using IdleFactoryDefense.Managers;

namespace IdleFactoryDefense.Gameplay
{
    public enum ResourceType
    {
        Stone,
        Brick
    }

    /// <summary>
    /// Gắn vào mỏ quặng (Đá hoặc Gạch) trên sân đấu.
    /// Hỗ trợ cả Click chuột máy tính lẫn Chạm cảm ứng điện thoại (IPointerClickHandler).
    /// Có hiệu ứng đàn hồi nảy nhẹ (Squash & Stretch Juice) cực kỳ đã tay.
    /// </summary>
    public class ResourceNode : MonoBehaviour, IPointerClickHandler
    {
        [Header("Cấu Hình Mỏ")]
        [SerializeField] private ResourceType resourceType = ResourceType.Stone;
        [SerializeField] private long harvestAmountPerClick = 1;

        [Header("Hiệu Ứng Nảy (Juice)")]
        [SerializeField] private float bounceScale = 1.25f;
        [SerializeField] private float bounceDuration = 0.12f;

        private Vector3 _originalScale;
        private Coroutine _bounceRoutine;

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        // Hỗ trợ Click chuột truyền thống
        private void OnMouseDown()
        {
            Harvest();
        }

        // Hỗ trợ New Input System & Cảm ứng Mobile
        public void OnPointerClick(PointerEventData eventData)
        {
            Harvest();
        }

        public void Harvest()
        {
            // 1. Cộng tài nguyên vào kho
            if (ResourceManager.Instance != null)
            {
                if (resourceType == ResourceType.Stone)
                {
                    ResourceManager.Instance.AddStone(harvestAmountPerClick);
                }
                else if (resourceType == ResourceType.Brick)
                {
                    ResourceManager.Instance.AddBrick(harvestAmountPerClick);
                }
            }

            // 2. Chạy hiệu ứng đàn hồi nảy nhẹ không tốn tài nguyên GC
            if (_bounceRoutine != null) StopCoroutine(_bounceRoutine);
            _bounceRoutine = StartCoroutine(BounceAnimation());
        }

        private IEnumerator BounceAnimation()
        {
            float elapsed = 0f;
            Vector3 targetScale = _originalScale * bounceScale;
            float halfDuration = bounceDuration * 0.5f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(_originalScale, targetScale, elapsed / halfDuration);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(targetScale, _originalScale, elapsed / halfDuration);
                yield return null;
            }

            transform.localScale = _originalScale;
            _bounceRoutine = null;
        }
    }
}
