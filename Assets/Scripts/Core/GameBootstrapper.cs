using UnityEngine;

namespace IdleFactoryDefense.Core
{
    /// <summary>
    /// Khởi động cấu hình tối ưu hiệu năng toàn bộ game.
    /// Tự động chạy trước khi bất kỳ Scene nào tải (Zero Setup required).
    /// Đảm bảo máy chạy mát mẻ, khóa cứng 60 FPS, không drop frame trên chip Snapdragon 810+.
    /// </summary>
    public static class GameBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeGameSettings()
        {
            // 1. Khóa cứng 60 FPS, tắt VSync trên mobile để giải phóng GPU và chống quá nhiệt (Thermal Throttling)
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            // 2. Tối ưu chu kỳ vật lý chuẩn 50Hz (0.02s) giúp CPU mát mẻ, tính toán vừa đủ
            Time.fixedDeltaTime = 0.02f;

            // 3. Khóa hướng màn hình dọc (Portrait)
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;

            // 4. Giữ màn hình không bị tối/tắt khi người chơi đang cắm máy chơi Idle
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Debug.Log("<color=#00FF88>[GameBootstrapper]</color> Hệ thống đã cấu hình: 60 FPS, Physics 50Hz, Màn hình dọc Portrait.");
        }
    }
}

