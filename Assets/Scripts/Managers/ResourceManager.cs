using System;
using UnityEngine;

namespace IdleFactoryDefense.Managers
{
    /// <summary>
    /// Quản lý toàn bộ tài nguyên cốt lõi của căn cứ: Đá (Stone), Gạch (Brick), Tiền (Money).
    /// Áp dụng cơ chế Zero-Allocation để tránh sinh rác bộ nhớ (GC Spike) trên Snapdragon 810+.
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [Header("Tài Nguyên Ban Đầu")]
        [SerializeField] private long initialStone = 0;
        [SerializeField] private long initialBrick = 0;
        [SerializeField] private double initialMoney = 0;

        public long StoneCount { get; private set; }
        public long BrickCount { get; private set; }
        public double Money { get; private set; }

        // Events thông báo cho UI (chỉ bắn event khi số thay đổi, không chạy Update liên tục)
        public event Action<long> OnStoneChanged;
        public event Action<long> OnBrickChanged;
        public event Action<double> OnMoneyChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            StoneCount = initialStone;
            BrickCount = initialBrick;
            Money = initialMoney;
        }

        private void Start()
        {
            // Bắn event khởi tạo cho UI hiển thị ban đầu
            NotifyAll();
        }

        public void NotifyAll()
        {
            OnStoneChanged?.Invoke(StoneCount);
            OnBrickChanged?.Invoke(BrickCount);
            OnMoneyChanged?.Invoke(Money);
        }

        #region Thao Tác Thêm Tài Nguyên
        public void AddStone(long amount)
        {
            if (amount <= 0) return;
            StoneCount += amount;
            OnStoneChanged?.Invoke(StoneCount);
        }

        public void AddBrick(long amount)
        {
            if (amount <= 0) return;
            BrickCount += amount;
            OnBrickChanged?.Invoke(BrickCount);
        }

        public void AddMoney(double amount)
        {
            if (amount <= 0) return;
            Money += amount;
            OnMoneyChanged?.Invoke(Money);
        }
        #endregion

        #region Thao Tác Tiêu Tiêu Dùng Tài Nguyên
        public bool TrySpendStone(long cost)
        {
            if (cost <= 0) return true;
            if (StoneCount < cost) return false;

            StoneCount -= cost;
            OnStoneChanged?.Invoke(StoneCount);
            return true;
        }

        public bool TrySpendBrick(long cost)
        {
            if (cost <= 0) return true;
            if (BrickCount < cost) return false;

            BrickCount -= cost;
            OnBrickChanged?.Invoke(BrickCount);
            return true;
        }

        public bool TrySpendMoney(double cost)
        {
            if (cost <= 0) return true;
            if (Money < cost) return false;

            Money -= cost;
            OnMoneyChanged?.Invoke(Money);
            return true;
        }
        #endregion

        #region Tiện Ích Định Dạng Số Lớn Cho Màn Hình Dọc (1K, 1M, 1B...)
        private static readonly string[] Suffixes = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc" };

        public static string FormatNumber(double value)
        {
            if (value < 1000) return value.ToString("F0");

            int suffixIndex = 0;
            while (value >= 1000 && suffixIndex < Suffixes.Length - 1)
            {
                value /= 1000.0;
                suffixIndex++;
            }

            return $"{value:F1}{Suffixes[suffixIndex]}";
        }
        #endregion
    }
}

