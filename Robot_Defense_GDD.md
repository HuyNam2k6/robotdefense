1# GAME DESIGN DOCUMENT (GDD) - GAME SPRINT 2026

## 1. Project Overview
- **Game Title:** Robot Defense 3D
- **Genre:** 3D Idle Game / Tower Defense
- **Engine/Platform:** Unity (Mobile Portrait)
- **Elevator Pitch:** Robot Defense 3D là tựa game Idle kết hợp thủ thành, nơi người chơi đi từ hai bàn tay trắng khai thác đá, gạch, gỗ để xây dựng nên một nhà máy công nghiệp tự vận hành. Mục tiêu là liên tục nâng cấp hệ thống ụ súng tự động để sinh tồn trước các đợt tấn công của quái vật cơ khí.
- **Unique Selling Proposition (USP):** Khác biệt với các game Idle clicker đơn điệu, game lai tạo (Hybrid) giữa cơ chế quản lý công nhân tự động hóa (Factory Builder) và chiến thuật phòng thủ tháp (Tower Defense), thiết kế chuẩn màn hình dọc (One-handed) dồn toàn bộ thao tác vào vùng ngón cái (Thumb-zone) cực kỳ tiện lợi cho di động.

## 2. Core Gameplay & Mechanics
- **Core Loop:** Luồng vận hành khép kín:
  - *Auto (Tự động hóa):* 1 loại Robot (Worker Bot) duy nhất tự động chạy ra mỏ, đào quặng và mang về Căn cứ không cần bấm thủ công. Trụ súng tự động quét mục tiêu và xả đạn tiêu diệt quái vật.
  - *Earn (Tạo tài nguyên):* Vàng (Gold) sinh ra từ việc hạ gục quái vật. Đá (Stone), Gỗ (Wood) và Gạch (Brick) sinh ra trực tiếp từ hoạt động khai thác của Robot (không qua dây chuyền tinh chế phức tạp).
  - *Upgrade (Nâng cấp):* Dùng Vàng để nâng cấp hỏa lực (Sát thương, Tốc độ đánh). Dùng Đá, Gỗ, Gạch để nâng cấp kinh tế (Tuyển thêm Robot, Tăng sản lượng, Tăng tốc độ chạy).
  - *Progress (Tiến trình):* Vượt qua các cột mốc Wave quái vật mạnh dần. Mục tiêu cuối cùng của bản Sprint này là đánh bại Boss xuất hiện ở Wave 10.
- **Game Layout:** Màn hình dọc (Portrait 9:16) chia 3 tầng:
  - *Tầng Trên (Top Bar):* Hiển thị thông số 4 loại tài nguyên (Đá, Gỗ, Gạch, Vàng) và tiến trình Wave.
  - *Tầng Giữa (3D Isometric):* Chiến trường nơi Robot khai thác ở nửa dưới và Quái vật tràn xuống từ nửa trên, bị chặn lại bởi dàn Trụ súng.
  - *Tầng Dưới (Thumb Zone):* Bảng menu cửa hàng và các nút nâng cấp 1 chạm bằng ngón cái. Thêm 1 nút kỹ năng khẩn cấp (Overclock) để ép xung tốc độ bắn trong 5 giây.

## 3. Economy & Upgrade Systems
- **Tài nguyên (Resources):**
  - Vàng (Gold): Rớt từ quái vật. Dùng nâng cấp chiến đấu.
  - Đá (Stone), Gỗ (Wood), Gạch (Brick): Sinh ra từ mỏ. Dùng nâng cấp kinh tế & xây dựng.
- **Hệ thống Upgrades (Công thức giá lũy tiến):**
  - **Upgrade 1: Sát thương trụ súng (Damage)**
    - *Chức năng:* Tăng damage đầu ra của súng.
    - *Công thức giá:* Cost = 100 Vàng * 1.15^Level
  - **Upgrade 2: Tốc độ bắn (Fire Rate)**
    - *Chức năng:* Giảm thời gian nạp đạn (Tối đa 5 viên/s).
    - *Công thức giá:* Cost = 200 Vàng * 1.25^Level
  - **Upgrade 3: Tuyển thêm Robot (Add Worker)**
    - *Chức năng:* Tăng thêm số lượng robot tự đi đào mỏ (Tối đa 10 Robot).
    - *Công thức giá:* Cost = (300 Gỗ + 300 Đá) * 1.4^Level
  - **Upgrade 4: Sản lượng mỏ (Resource Output)**
    - *Chức năng:* Tăng lượng tài nguyên mang về mỗi chuyến.
    - *Công thức giá:* Cost = (150 Gạch + 150 Gỗ) * 1.18^Level

## 4. Game Feel & Feedback
- **Visual Feedback:**
  - *Floating Text:* Số nảy (bounce) bay lên từ xác quái (VD: +100 Vàng) và chữ đỏ báo mất máu căn cứ.
  - *Particles:* Chớp lửa đầu nòng súng (Muzzle flash) khi xả đạn, vụn kim loại đen nổ tung khi quái chết, mùn cưa và vụn đá văng ra khi Robot khai thác.
  - *Animation:* Hiệu ứng đàn hồi (Squash & Stretch) trên các nút bấm UI; Robot có dáng đi ngộ nghĩnh và rung lắc nhẹ khi đang đào.
- **Audio Feedback:**
  - đang tìm
- **Audio Feedback (Đang trong quá trình tìm kiếm & tuyển chọn):**
  - *BGM (Đang tìm kiếm):* Định hướng nhạc nền phong cách Industrial Sci-fi (tiếng búa gõ mỏ, âm bass điện tử), chuyển nhịp dồn dập ở Boss Wave 10.
  - *SFX (Đang tìm kiếm):* Định hướng tiếng đạn nổ đanh gọn, tiếng đập đá cộc cộc, tiếng chặt gỗ chát chát, và còi báo động (Siren) khi Căn cứ còn dưới 20% HP.

## 5. AI Implementation Log
*Nhật ký các công cụ AI đã sử dụng và cách ứng dụng vào quy trình phát triển Game Sprint:*
- **AI Code (Claude / Antigravity Agent):**
  - *Ứng dụng:* Viết cấu trúc script di chuyển Player, tối ưu Object Pooling (Zero Allocation), và logic xoay nòng súng 3D.
  - *Prompt mẫu:* "Viết script TurretHeadRotator.cs cho Unity. Cho phép nòng súng chỉ xoay ngang (trục Y) hướng về mục tiêu, khóa cứng theo 8 hướng (snap 45 độ), tối ưu hóa hàm toán học không dùng vòng lặp thừa."
- **AI Art (Midjourney / Meshy 3D):**
  - *Ứng dụng:* Lên ý tưởng concept UI màn hình dọc và Icon bộ 4 tài nguyên (Đá, Gạch, Gỗ, Vàng).
  - *Prompt mẫu:* "Game UI resource icons set, flat vector style, including a gray stone, a red brick, a wooden log, and a gold coin, sci-fi borders, dark background --ar 16:9"
- **AI Audio (Suno AI):**
  - *Ứng dụng:* Tạo nhạc nền BGM phong cách cơ khí, phù hợp với không khí xây dựng nhà máy.
  - *Prompt mẫu:* "Cyberpunk industrial background track, 120 bpm, electronic bass, factory ambient noises, intense synth melodies, suitable for idle tower defense game, looping."