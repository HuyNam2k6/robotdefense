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

- **Hệ thống Nhà Máy & Quân Đội Đồng Minh (Combat Robot Army):**
  - **Nhà máy sản xuất Robot 3D (Robot Factory):** Xây dựng từ Cửa Hàng (BuildingSystem). Khi người chơi nhấn vào Nhà Máy trên đảo (hoặc từ Cửa Hàng), một **Bảng Chế Tạo Robot to nửa màn hình căn giữa** sẽ xuất hiện, gồm 4 Icon đại diện cho 4 Robot phòng thủ. Trong mỗi icon có nút **[!]** để xem chi tiết thông số (Máu, Sát thương, Tầm xa, Tốc độ, Kỹ năng) giúp người chơi tự do lựa chọn con robot muốn sản xuất. Khi bấm sản xuất, thời gian 15s bắt đầu chạy, kích hoạt toàn bộ animation cơ khí chân thực của nhà máy (cánh tay hàn xì, bánh răng cưa quay, thân máy rung dập, tia lửa hàn điện chớp tắt), sau đúng 15s xuất xưởng đúng con robot đã chọn lên băng chuyền ra đảo chiến đấu.
  - **4 Chủng Robot Chiến Đấu Đồng Minh (Tự do lựa chọn đúc theo chiến thuật):**
    1. *Stan (Pháo Nhện 4 chân):* Hỏa lực tầm xa, bắn pháo diện rộng và dẫm đạp đẩy lùi.
    2. *Mike (Đấu sĩ hộ pháp - Tanker):* Cận chiến trâu máu, cú đấm móc và vung kiếm càn quét tiền tuyến.
    3. *George (Sát thủ chân kiếm):* Cơ động siêu tốc, lướt chém song kiếm và cú đá xoay liên hoàn.
    4. *Leela (Xạ thủ bắn tỉa):* Tầm bắn siêu xa, phát bắn laser chuẩn xác và đá tự vệ khi bị áp sát.
  - **Cơ chế chiến đấu:** Robot tự động tuần tra quanh căn cứ, phát hiện quái vật, tiếp cận và tung chiêu tương ứng. Có thanh máu 3D hiển thị thời gian thực và hoạt ảnh ăn mừng khi chiến thắng.
  - **Tối ưu Snapdragon 810:** Toàn bộ trọng lực tính bằng Code (Rule 8 - Không dùng Rigidbody), giới hạn quân số tối đa 8 lính, chia sẻ Texture Palette chung để giảm Draw Calls.

- **Hệ thống Quái Vật & Đại Boss (Enemy Faction & Bosses):**
  - **Quái vật thường (Minions):**
    1. *Scrap Bug (Bọ Cơ Khí - `Enemy_AlienBug`):* Quái vật trinh sát di chuyển nhanh, máu vừa phải, cắn phá công trình và tấn công căn cứ.
    2. *Scrap Spider (Nhện Máy - `Enemy_Spider`):* Quái vật bọc giáp bò áp sát, càn quét phòng tuyến và thu hút hỏa lực của trụ súng.
  - **Đại Boss Tối Thượng (Ultimate Boss - Xuất hiện ở Wave 10 hoặc Đợt Boss):**
    - *Mecha Cyber Dragon [BOSS] (`Enemy_MechaDragon`):* Rồng Cơ Khí khổng lồ bay lượn trên không trung (Flying Unit). Sở hữu lượng máu khổng lồ (1000 - 1200 HP), giáp Titan hắc kim kiên cố và vũ khí hủy diệt: đạn cầu năng lượng Plasma (`Dragon_PlasmaBall`).
    - *Hành vi Boss:* Bay lượn trên bầu trời, vượt qua mọi chướng ngại vật mặt đất, khạc đạn plasma tầm xa oanh tạc thẳng vào Căn cứ, Người chơi và Quân đội Robot đồng minh.
    - *Nhiệm vụ người chơi:* Xây dựng dàn Trụ súng phòng không mặt đất, sản xuất đủ 4 Chủng Robot Chiến Đấu Đồng Minh (Stan, Mike, George, Leela) phối hợp hỏa lực tập trung để bắn hạ Boss rồng cơ khí, đem về chiến thắng vẻ vang cùng kho báu 250 Vàng!

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