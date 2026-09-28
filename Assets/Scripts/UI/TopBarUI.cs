using UnityEngine;
using TMPro;
using IdleFactoryDefense.Managers;

namespace IdleFactoryDefense.UI
{
    /// <summary>
    /// Điều khiển thanh thông số tài nguyên ở đỉnh màn hình dọc (Top Bar).
    /// Chỉ cập nhật khi có event bắn ra, không gọi GetComponent hay ghép chuỗi trong Update().
    /// Giúp tiết kiệm 100% CPU trên các dòng chip di động.
    /// </summary>
    public class TopBarUI : MonoBehaviour
    {
        [Header("Giao Diện TextMeshPro")]
        [SerializeField] private TextMeshProUGUI stoneText;
        [SerializeField] private TextMeshProUGUI brickText;
        [SerializeField] private TextMeshProUGUI moneyText;

        private void OnEnable()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnStoneChanged += UpdateStoneUI;
                ResourceManager.Instance.OnBrickChanged += UpdateBrickUI;
                ResourceManager.Instance.OnMoneyChanged += UpdateMoneyUI;
                ResourceManager.Instance.NotifyAll();
            }
        }

        private void Start()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnStoneChanged += UpdateStoneUI;
                ResourceManager.Instance.OnBrickChanged += UpdateBrickUI;
                ResourceManager.Instance.OnMoneyChanged += UpdateMoneyUI;
                ResourceManager.Instance.NotifyAll();
            }
        }

        private void OnDisable()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnStoneChanged -= UpdateStoneUI;
                ResourceManager.Instance.OnBrickChanged -= UpdateBrickUI;
                ResourceManager.Instance.OnMoneyChanged -= UpdateMoneyUI;
            }
        }

        private void UpdateStoneUI(long stone)
        {
            if (stoneText != null)
            {
                stoneText.text = $"STONE: {ResourceManager.FormatNumber(stone)}";
            }
        }

        private void UpdateBrickUI(long brick)
        {
            if (brickText != null)
            {
                brickText.text = $"BRICK: {ResourceManager.FormatNumber(brick)}";
            }
        }

        private void UpdateMoneyUI(double money)
        {
            if (moneyText != null)
            {
                moneyText.text = $"GOLD: ${ResourceManager.FormatNumber(money)}";
            }
        }
    }
}

