# 📋 LỘ TRÌNH TRIỂN KHAI DỰ ÁN: ROBOT DEFENSE 3D (MOBILE PORTRAIT)
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

## 🤖 PHASE 4: TỰ ĐỘNG HÓA BẰNG ROBOT (WORKER BOT)
- [ ] **Worker Robot (Robot Khai thác):**
  - [ ] Dựng mô hình Robot Low-poly nhỏ nhắn.
  - [ ] Vòng lặp AI (Waypoint): Đi từ Căn cứ -> Đến mỏ (Đá/Gỗ/Gạch) -> Đào 1s -> Mang quặng về Căn cứ -> Tăng tài nguyên tương ứng.
- [ ] **Tab Quản lý Robot (Bottom UI):** Dùng Tài nguyên mua thêm số lượng Worker Bot (Tối đa 10 con) để tự động hóa 100%.
- [ ] 📌 **Git Checkpoint:** Commit (*"Phase 4: Worker Bots & Automation"*).

---

## 📈 PHASE 5: HỆ THỐNG WAVE & ĐÁNH BOSS (END GAME SPRINT)
- [ ] **Wave Manager:** Quản lý đợt quái tràn xuống theo chiều dọc.
- [ ] **Thanh tiến trình Wave ở Top Bar:** Hiển thị Wave hiện tại (VD: Wave 1/10) và thanh máu Boss.
- [ ] **Boss Cuối (Wave 10):** Sinh ra Boss khổng lồ ở Wave 10. Đánh bại Boss hiện màn hình Victory (Mục tiêu Game Sprint).
- [ ] **Công thức quái vật leo thang:** HP Quái = Base_HP * (1.15 ^ Wave).
- [ ] 📌 **Git Checkpoint:** Commit (*"Phase 5: Wave Manager & Boss 10"*).
