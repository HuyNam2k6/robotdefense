# 📋 LỘ TRÌNH TRIỂN KHAI DỰ ÁN: IDLE FACTORY DEFENSE 3D (MOBILE PORTRAIT)
*Nền tảng: Mobile Android & iOS | Góc nhìn: 3D Isometric Màn Hình Dọc (9:16) | Tiêu chuẩn: Chạy mượt 60 FPS trên Snapdragon 810+*
*Hướng dẫn: Đánh dấu `[x]` vào các ô trống sau khi hoàn thành từng việc để theo dõi tiến độ.*

---

## 🚀 PHASE 0: SETUP PROJECT, MÀN HÌNH DỌC & TỐI ƯU SNAPDRAGON 810+
- [x] **Màn hình dọc (Portrait 9:16):** 
  - [ ] Thiết lập tỷ lệ màn hình trong Unity Game View là **9:16 (1080x1920)** hoặc **9:20** (Người dùng chỉnh trong Editor Game View).
  - [x] Cấu hình Player Settings: Khóa hướng màn hình chỉ cho phép chạy dọc (`Portrait`).
- [x] **Xử lý Safe Area:** Viết script `SafeAreaHandler.cs` tự động căn chỉnh RectTransform của Canvas để UI không bị che bởi tai thỏ, nốt ruồi camera hay thanh điều hướng vuốt.
- [x] **Khóa 60 FPS & Mát máy (Chống bóp xung Snap 810):**
  - [x] Viết script `GameBootstrapper.cs`: Đặt `Application.targetFrameRate = 60;` và `QualitySettings.vSyncCount = 0;`.
  - [x] Cấu hình Physics: Đặt `Time.fixedDeltaTime = 0.02f` (50Hz) để giữ CPU mát mẻ.
- [x] **Cấu hình URP Low-End:** Thiết lập Universal Render Pipeline: Tắt Realtime Shadow mềm nặng, dùng FXAA nhẹ, bật GPU Instancing cho Material.
- [x] **Git Local Repository:** Khởi tạo `git init` và tạo `.gitignore` chuẩn Unity lưu mã nguồn an toàn tại máy.
- [x] **Camera Isometric Dọc:** Đặt Main Camera góc nghiêng 40° - 45° hướng xuống, điều chỉnh Orthographic Size bao quát trọn vẹn Căn cứ ở dưới và đường quái tràn từ phía trên dốc xuống (`IsometricCameraController.cs`).
- [ ] **Mặt sàn (Ground):** Tạo sàn đất hình chữ nhật dài theo chiều dọc màn hình.
- [x] 📌 **Git Checkpoint:** Commit (*"Phase 0: Setup Portrait Mode, 60FPS Snap810 & Isometric Camera"*).

---

## 🪨 PHASE 1: CLICKER 1 TAY & TÀI NGUYÊN ĐẦU TIÊN (VÙNG ĐẤT HOANG)
- [ ] **Mỏ quặng thô:** Đặt 2 khối Cube đại diện cho mỏ **Đá (Stone)** và mỏ **Gạch (Brick)** ở khu vực hậu phương căn cứ (dưới màn hình, gần ngón tay cái).
- [ ] **Palette màu Low-poly chung:** Tạo 1 Material dùng chung bức ảnh Texture Atlas màu (256x256) cho tất cả các khối để tối ưu Draw Call.
- [ ] **ResourceManager (Zero GC):** Tạo GameManager quản lý biến `stoneCount` và `brickCount` kiểu dữ liệu `long` hoặc `double` (chuẩn bị cho số lớn trong game Idle).
- [ ] **Cơ chế Chạm/Click 1 ngón tay:** Viết script bắt sự kiện chạm vào mỏ Đá/Gạch -> Hiệu ứng nảy nhẹ (Squash & Stretch juice) -> Tăng số lượng tài nguyên.
- [ ] **Top UI Bar:** Thanh thông số đặt sát mép trên (dưới vùng Safe Area), hiển thị số Đá và Gạch mượt mà bằng TextMeshPro.
- [ ] 📌 **Git Checkpoint:** Commit (*"Phase 1: Portrait Resource Clicker & Top Bar UI"*).

---

## 🏰 PHASE 2: XÂY DỰNG TRỤ PHÒNG THỦ & QUÁI VẬT TẤN CÔNG
- [ ] **Nút Xây Dựng 1 Chạm:** Nằm ở vùng ngón cái phía dưới màn hình (Đủ 5 Đá + 5 Gạch -> Nút sáng lên -> Bấm xây trụ).
- [ ] **Object Pooling Manager (Bắt buộc cho Snap 810):**
  - [ ] Xây dựng hệ thống `ObjectPooler.cs` nạp sẵn 20 viên đạn và 20 quái vật trong bộ nhớ RAM từ đầu.
  - [ ] Tuyệt đối không dùng `Instantiate` / `Destroy` trong game loop để GC Alloc = 0 bytes/frame.
- [ ] **Quái vật đầu tiên (Scrap Crawler):**
  - [ ] Lấy từ Pool, xuất hiện từ đỉnh trên màn hình, bò thẳng theo chiều dọc dốc xuống Căn cứ.
  - [ ] Di chuyển bằng phép tính tịnh tiến `Vector3.MoveTowards` nhẹ CPU.
- [ ] **Trụ súng tự động (Turret_Lv1):**
  - [ ] Dựng tại tiền tuyến ngăn giữa Căn cứ và Đường quái.
  - [ ] Tự quét quái trong bán kính ngắm bằng `Physics.OverlapSphereNonAlloc` có LayerMask.
  - [ ] Tự xoay nòng và bắn đạn lấy từ Object Pool.
- [ ] **Máu căn cứ & Va chạm:** Quái bị bắn hết máu -> Tắt về Pool -> Rơi tiền. Nếu quái lọt vào Căn cứ -> Trừ máu Base.
- [ ] 📌 **Git Checkpoint:** Commit (*"Phase 2: Turret Defense & Enemy Spawner with Object Pooling"*).

---

## 💰 PHASE 3: TIỀN TỆ & MENU SHOP NÂNG CẤP DƯỚI ĐÁY MÀN HÌNH (THUMB ZONE)
- [ ] **Tiền tệ (Gold/Cash):** Quái chết sinh tiền, bay vào kho tài nguyên.
- [ ] **Bottom Navigation Tabs:** Thanh menu gắn sát đáy màn hình chia 3 Tab ngón cái dễ chạm:
  - [ ] *Tab 1: Xây Dựng (Buildings)*
  - [ ] *Tab 2: Nâng Cấp Trụ (Turret Upgrades)*
  - [ ] *Tab 3: Đàn Robot (Robots & Automation)*
- [ ] **Các mục nâng cấp Trụ:**
  - [ ] Nút 1: Nâng Sát thương Trụ (Damage Upgrade).
  - [ ] Nút 2: Nâng Tốc độ bắn Trụ (Fire Rate Upgrade).
  - [ ] Nút 3: Nâng Tầm bắn Trụ (Range Upgrade).
- [ ] **Công thức giá tăng lũy tiến:** `Cost = BaseCost * (1.5 ^ Level)`.
- [ ] **Hiển thị số rút gọn chuẩn Idle:** Viết hàm format số lớn cho gọn màn hình dọc: `1K, 1M, 1B, 1T, 1AA, 1AB...`.
- [ ] 📌 **Git Checkpoint:** Commit (*"Phase 3: Bottom Shop Tabs & Turret Upgrades"*).

---

## 🤖 PHASE 4: TỰ ĐỘNG HÓA BẰNG ROBOT (AUTOMATION ERA)
- [ ] **Worker Robot (Robot Khai thác):**
  - [ ] Dựng mô hình Robot Low-poly nhỏ nhắn, ngộ nghĩnh.
  - [ ] Vòng lặp AI nhẹ (Waypoint): Đi từ Căn cứ -> Đến mỏ Đá -> Đào 1s -> Cõng đá về Căn cứ -> Đổ vào kho (+1 Đá).
- [ ] **Robot Vận chuyển / Băng chuyền:** Tự động đưa quặng từ Căn cứ vào lò luyện tự động sản xuất linh kiện nâng cấp.
- [ ] **Tab Quản lý Robot ở đáy màn hình:** Dùng Tiền mua thêm số lượng Robot đào đá/gạch -> Toàn bộ việc thu thập tài nguyên được tự động hóa 100%, người chơi rảnh tay.
- [ ] 📌 **Git Checkpoint:** Commit (*"Phase 4: Worker Robots & Full Automation"*).

---

## 📈 PHASE 5: HỆ THỐNG WAVE & CƠ CHẾ LÙI WAVE (FARM TIỀN KHÔNG BAO GIỜ THUA)
- [ ] **Wave Manager:** Quản lý đợt quái tràn xuống theo chiều dọc.
- [ ] **Thanh tiến trình Wave ở Top Bar:** Hiển thị Wave hiện tại (VD: Wave 15/20) và thanh máu Boss.
- [ ] **Công thức quái vật leo thang:**
  - [ ] `HP Quái = Base_HP * (1.15 ^ Wave)`.
  - [ ] `Tiền rớt = Base_Reward * (1.12 ^ Wave)`.
- [ ] **Cơ chế Lùi Wave (AFK Farm Mode):**
  - [ ] Khi quái quá đông phá vỡ Căn cứ: Game tự động lùi về Wave trước để căn cứ tự bắn quái farm tiền tích lũy.
  - [ ] Hiện nút nổi "Thử Lại Wave [X]" ngay tầm ngón tay cái khi người chơi đã nâng cấp xong đồ xịn.
- [ ] 📌 **Git Checkpoint:** Commit (*"Phase 5: Wave Scaling & Auto-Retreat Farm Mode"*).

---

## ⚡ PHASE 6: BỘ 3 TÍNH NĂNG ĐẶC BIỆT MỚI (SPECIAL ABILITIES)
- [ ] **1. Ép Xung Quá Tải (Overclock / Red Alert):**
  - [ ] Nút tròn to `[⚡ OVERCLOCK]` đặt phía trên thanh Bottom Tabs (rất dễ ấn ngón cái).
  - [ ] Hiệu ứng kích hoạt (10s): **x3 tốc bắn Trụ** + **x2 tốc chạy Robot** + Màn hình viền đỏ chớp cảnh báo + tia điện giật.
  - [ ] Hết 10s: Xịt khói tản nhiệt 2s (Cooldown 60s có vòng tròn hồi chiêu).
- [ ] **2. Chi Viện Vệ Tinh (Orbital Strike & Drop Pod):**
  - [ ] Dựng mô hình Đĩa Radar trên nóc Căn cứ.
  - [ ] *Tia Laser Quỹ Đạo:* Bấm nút Vệ tinh -> Chạm ngón tay vào bầy quái trên màn hình -> Chùm laser vũ trụ từ trời phóng xuống thiêu rụi quái trong vùng (Dùng LineRenderer tối ưu).
  - [ ] *Kén Tiếp Tế (Drop Pod):* Mỗi 3 phút rơi dù 1 kén từ đỉnh màn hình xuống -> Chạm vào nhận bọc tiền lớn hoặc buff tạm thời.
- [ ] **3. Drone Mini Đồng Hành Hút Đồ (Companion Drone):**
  - [ ] 1 chú Robot bay tí hon bám sát theo vị trí ngón tay chạm/vuốt trên màn hình cảm ứng.
  - [ ] Tia nam châm tự động hút tài nguyên & tiền rơi quanh ngón tay về kho.
  - [ ] Tích hợp súng laser mini tỉa phụ và làm chậm (Slow 20%) con quái đi đầu.
- [ ] 📌 **Git Checkpoint:** Commit (*"Phase 6: Special Abilities - Overclock, Orbital Strike & Drone"*).

---

## 💾 PHASE 7: OFFLINE PROGRESS & TỐI ƯU CUỐI CÙNG (CHIP SNAPDRAGON 810+)
- [ ] **Lưu Game Cục Bộ (Local Save System):**
  - [ ] Lưu toàn bộ vàng, tài nguyên, cấp độ nâng cấp, wave cao nhất vào file JSON mã hóa nhẹ lưu trực tiếp trong bộ nhớ máy (`Application.persistentDataPath`).
  - [ ] Tự động lưu mỗi 30 giây và khi pause/thoát ứng dụng (`OnApplicationPause`, `OnApplicationQuit`).
- [ ] **Nhận Thưởng Khi Rời Game (Offline Earnings):**
  - [ ] Ghi nhận mốc thời gian thoát game (`DateTime.UtcNow`).
  - [ ] Khi người chơi mở lại game: Tính toán số phút vắng mặt -> Quy đổi số tiền & đá mà đàn Robot đã tự động đào trong lúc offline -> Hiện bảng thông báo Popup nhận thưởng cực kỳ thỏa mãn.
- [ ] **Kiểm Tra Hiệu Năng Thực Tế (Profiling):**
  - [ ] Kiểm tra Draw Calls (Batches) duy trì ổn định **dưới 30 Batches**.
  - [ ] Kiểm tra GC Alloc = 0 bytes trong khi đánh trận.
  - [ ] Đảm bảo FPS luôn giữ mốc 60 FPS ổn định, nhiệt độ máy mát mẻ trên Snapdragon 810+.
- [ ] **Âm thanh & Rung cảm ứng (Haptics & SFX):**
  - [ ] Thêm âm thanh cơ khí máy móc, tiếng súng đạn xả liên thanh, tiếng bíp báo động.
  - [ ] Thêm rung nhẹ (Haptic feedback) khi bấm nâng cấp hoặc khi kích hoạt Overclock.
- [ ] 📌 **Git Checkpoint:** Commit (*"Phase 7: Offline Progress, Profile 60FPS Snap810 & Final Polish"*).
