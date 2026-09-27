# 🎮 IDLE FACTORY DEFENSE 3D

## 1. Tổng quan

**Tên tạm thời:** Idle Factory Defense

**Thể loại:**

* 3D Idle Game
* Incremental Game
* Factory / Base Building
* Automated Defense

**Nền tảng:** PC & Mobile (Android & iOS)
**Mục tiêu hiệu năng:** Khóa cứng **60 FPS** mượt mà, mát máy, tối ưu tuyệt đối cho chip tầm trung từ **Snapdragon 810 trở lên** (và các dòng Snap 6xx, 7xx, Dimensity tương đương).

**Góc nhìn:** 3D Isometric / Camera cố định

**Định dạng:** Single-Screen

Toàn bộ gameplay diễn ra trên **một khu vực duy nhất**. Người chơi không điều khiển nhân vật chạy qua nhiều bản đồ mà tập trung phát triển một mảnh đất từ vùng đất hoang thành một căn cứ công nghiệp tự động khổng lồ.


---

# 2. Ý tưởng cốt lõi

Người chơi bắt đầu với một **mảnh đất hoang hoàn toàn trống**.

Không có:

* Nhà máy
* Robot
* Vũ khí
* Tường phòng thủ
* Tiền
* Hệ thống tự động

Người chơi phải tự thu thập những tài nguyên đầu tiên để xây dựng hệ thống phòng thủ.

Ban đầu:

**Người chơi tự làm → xây dựng căn cứ → chống lại quái vật → nhận tiền → mua vũ khí.**

Sau đó:

**Robot làm thay người chơi → nhà máy tự sản xuất → hệ thống phòng thủ tự chiến đấu → người chơi chỉ tập trung nâng cấp.**

Mục tiêu cuối cùng là biến một vùng đất trống thành một:

> **Siêu nhà máy tự động vừa sản xuất tài nguyên vừa tự bảo vệ trước những quái vật ngày càng mạnh.**

---

# 3. Core Fantasy

Cảm giác chính mà game muốn mang lại:

> **"Từ hai bàn tay trắng xây dựng một căn cứ tự vận hành hoàn toàn."**

Người chơi bắt đầu bằng việc:

🪨 Nhặt đá
🧱 Nhặt gạch
🏗️ Xây dựng
🔫 Phòng thủ
💰 Kiếm tiền

Sau đó tiến tới:

🤖 Robot
⚙️ Máy móc
🏭 Nhà máy
⚡ Plasma
🚀 Vũ khí hạng nặng
👹 Boss

Và cuối cùng:

🌌 Một căn cứ công nghiệp khổng lồ tự vận hành.

---

# 4. Thiết kế Màn hình Dọc (Mobile Portrait Mode - Chơi 1 Tay)

Game được thiết kế chuẩn **Màn hình dọc (Portrait Mode - Tỉ lệ 9:16 và 9:20)** tối ưu cho điện thoại smartphone hiện đại, cho phép người chơi **cầm và điều khiển thoải mái bằng 1 tay (One-Handed Play)**.

Toàn bộ thế giới diễn ra trên **một màn hình duy nhất**, chia thành 3 tầng chức năng công thái học:

```
┌──────────────────────────────────────────────┐
│ [⚙️]  💰 Tiền: 1.2M    ⚡ Năng lượng: 80%   │ ← TẦNG TRÊN (Safe Area / Top Bar):
│          🏆 WAVE 14 / 20 (Thanh tiến trình)   │    Hiển thị thông số, tài nguyên, wave
├──────────────────────────────────────────────┤
│                                              │
│               👹 QUÁI VẬT                     │ ← TẦNG GIỮA (Chiến trường 3D Isometric):
│                   ↓↓↓                        │    - Quái tràn từ phía trên dốc xuống.
│               ⚡ TURRET PHÒNG THỦ            │    - Trụ súng dựng ở tiền tuyến chặn quái.
│                                              │    - Hậu phương: Căn cứ, Nhà máy đúc quặng,
│          🏭 BASE / CĂN CỨ                    │      đàn Robot chạy qua chạy lại đào đá/gạch.
│          🤖 ĐÀN ROBOT CÕNG QUẶNG             │    - Camera Isometric dọc ôm trọn sàn đấu.
│          🪨 BÃI ĐÁ        🧱 BÃI GẠCH        │
│                                              │
├──────────────────────────────────────────────┤
│ [⚡ OVERCLOCK]    [🛰️ VỆ TINH]    [🧲 DRONE]  │ ← TẦNG KỸ NĂNG (Dễ bấm bằng ngón cái):
├──────────────────────────────────────────────┤    Kích hoạt nhanh skill khẩn cấp
│ [🏭 XÂY DỰNG]   [🔫 TRỤ SÚNG]   [🤖 ROBOT]   │ ← TẦNG DƯỚI (Vùng ngón tay cái - Thumb Zone):
│ ┌──────────────────────────────────────────┐ │    Tabs Shop & Nâng cấp 1 chạm cực kỳ tiện lợi
│ │ Nâng Sát Thương Trụ   [+1 Cấp - 500 Gold]│ │
│ │ Tăng Tốc Độ Robot     [+1 Cấp - 300 Gold]│ │
│ └──────────────────────────────────────────┘ │
└──────────────────────────────────────────────┘
```

* **Công thái học (Thumb-Zone):** Toàn bộ các nút bấm tương tác nhiều (Nâng cấp, Xây dựng, Bật Overclock, Bấm Vệ tinh) được dồn về nửa dưới màn hình để ngón tay cái thao tác mượt mà không cần với tay.
* **Xử lý Safe Area:** Đỉnh màn hình tự động thụt xuống tránh camera đục lỗ/tai thỏ trên các dòng Android và iPhone.


---

# 5. GIAI ĐOẠN 1 – VÙNG ĐẤT HOANG

Khi bắt đầu game, người chơi chỉ có:

🏝️ Một mảnh đất trống.

Trên khu đất xuất hiện một số tài nguyên cơ bản:

🪨 Đá
🧱 Gạch

Nhiệm vụ đầu tiên:

> **Thu thập 3 viên đá và 3 viên gạch.**

UI:

🪨 Đá: 0 / 3
🧱 Gạch: 0 / 3

Người chơi click vào tài nguyên để thu thập.

Ví dụ:

Click đá:

**+1 Đá**

Click gạch:

**+1 Gạch**

Đủ:

🪨 3 / 3 ✓
🧱 3 / 3 ✓

→ Mở khóa xây dựng.

---

# 6. GIAI ĐOẠN 2 – XÂY CĂN CỨ ĐẦU TIÊN

Người chơi sử dụng 3 đá + 3 gạch để xây:

## 🏰 Trụ Phòng Thủ

Đây là công trình đầu tiên của căn cứ.

Trụ có khả năng tự động tấn công quái vật.

Sau khi hoàn thành:

> **Căn cứ đầu tiên đã được xây dựng.**

Một animation xây dựng diễn ra:

Từ đất trống:

🏗️ → 🧱 → 🏰

Sau đó hệ thống thông báo:

> ⚠️ Phát hiện sinh vật lạ.

---

# 7. GIAI ĐOẠN 3 – QUÁI VẬT ĐẦU TIÊN

Quái vật xuất hiện ở rìa khu vực.

Ví dụ:

## 👹 Scrap Crawler

Một sinh vật được tạo thành từ rác thải, kim loại và vật liệu bỏ đi.

Thông số:

HP: 100

Nó từ từ tiến về phía căn cứ.

Trụ phòng thủ tự động phát hiện mục tiêu.

🔫 BẮN
🔫 BẮN
🔫 BẮN

HP:

100 → 70 → 40 → 10 → 0

💥 Quái vật bị tiêu diệt.

---

# 8. HỆ THỐNG PHẦN THƯỞNG

Sau khi tiêu diệt quái vật, người chơi nhận tiền.

Ví dụ:

👹 Scrap Crawler

→ 💰 +100

Tiền trở thành tài nguyên quan trọng để phát triển hệ thống phòng thủ.

Càng tiêu diệt quái mạnh:

→ càng nhiều tiền.

---

# 9. GIAI ĐOẠN 4 – CỬA HÀNG VŨ KHÍ

Sau khi có tiền, người chơi mở khóa:

## 🛒 WEAPON SHOP

Cửa hàng cho phép mua và nâng cấp vũ khí.

### Cấp độ vũ khí

**Lv.1 – Auto Cannon**

Damage: 10
Attack Speed: 1.0/s

**Lv.2 – Plasma Cannon**

Damage: 100
Attack Speed: 0.8/s

**Lv.3 – Laser Turret**

Damage: 500
Attack Speed: liên tục

**Lv.4 – Missile Launcher**

Damage: 2.000
Attack Speed: chậm
Đặc điểm: sát thương diện rộng

**Lv.5 – Railgun**

Damage: 10.000
Đặc điểm: xuyên nhiều mục tiêu

**Lv.6 – Plasma Beam**

Damage: cực lớn

**Lv.7 – Orbital Cannon**

Vũ khí cấp độ cuối / Ultimate.

---

# 10. ĐẶC ĐIỂM CỦA VŨ KHÍ

Các vũ khí không chỉ khác nhau về Damage.

Mỗi loại có một vai trò.

### 🔫 Auto Cannon

* Bắn nhanh
* Damage thấp
* Phù hợp quái yếu

### ⚡ Plasma Cannon

* Bắn chậm
* Damage cao
* Phù hợp quái có HP lớn

### 🔥 Laser

* Tấn công liên tục
* Phù hợp mục tiêu đơn

### 🚀 Missile

* Tốc độ chậm
* Damage diện rộng
* Phù hợp nhiều quái

### 💥 Railgun

* Damage cực cao
* Xuyên nhiều quái

### ☄️ Plasma Beam

* Cực mạnh
* Cooldown dài

Nhờ vậy việc nâng cấp có ý nghĩa gameplay chứ không chỉ là:

> Damage +100 → +200 → +300.

---

# 11. NGUYÊN TẮC QUAN TRỌNG – TỰ ĐỘNG CHIẾN ĐẤU

Người chơi **không trực tiếp điều khiển súng**.

Không cần:

* Ngắm chuột
* Bấm chuột để bắn
* Điều khiển nhân vật chiến đấu

Vũ khí tự động:

👹 Quái xuất hiện

↓

🔫 Vũ khí tự tìm mục tiêu

↓

💥 Tấn công

↓

👹 Quái chết

↓

💰 Nhận thưởng

Điều này giữ đúng bản chất:

> **Idle Game**

---

# 12. GIAI ĐOẠN 5 – TỰ ĐỘNG HÓA

Sau khi căn cứ đã có khả năng phòng thủ, người chơi bắt đầu mở khóa hệ thống tự động hóa.

## 🤖 Worker Robot

Robot đầu tiên có nhiệm vụ thu thập tài nguyên.

Robot có thể:

* Nhặt đá
* Nhặt gạch
* Vận chuyển nguyên liệu
* Đưa nguyên liệu về kho

Ban đầu:

Người chơi:

🧍 → 🪨 → 🏠

Sau khi có robot:

🤖 → 🪨 → 🏠

Người chơi không cần tự thu thập nữa.

---

# 13. HỆ THỐNG SẢN XUẤT

Sau Worker Robot:

## ⚙️ Resource Generator

Máy tự động sản xuất tài nguyên.

Ví dụ:

Level 1:

+1 tài nguyên / giây

Level 2:

+5 / giây

Level 3:

+25 / giây

Level 4:

+125 / giây

Level 5:

+625 / giây

Đây chính là:

> **Passive Generator**

---

# 14. EXPONENTIAL UPGRADES

Game sử dụng tăng trưởng lũy tiến để tạo cảm giác phát triển mạnh.

Ví dụ:

Level 1:

1 / sec

Level 2:

5 / sec

Level 3:

25 / sec

Level 4:

125 / sec

Level 5:

625 / sec

Level 10:

3.906.250 / sec

Mục tiêu là để người chơi cảm nhận:

> **Nhà máy càng phát triển thì tốc độ phát triển càng nhanh.**

---

# 15. NHÀ MÁY

Sau Resource Generator, người chơi mở:

## 🏭 Factory

Factory là trung tâm sản xuất.

Nó có thể tạo:

* Linh kiện
* Kim loại
* Đạn
* Năng lượng
* Robot
* Vũ khí

Nhà máy càng nâng cấp:

→ tốc độ sản xuất càng nhanh.

---

# 16. DÂY CHUYỀN SẢN XUẤT

Khi phát triển đủ lớn, khu đất bắt đầu xuất hiện dây chuyền.

Ví dụ:

🪨 Quặng

↓

⚙️ Máy nghiền

↓

🔥 Lò luyện

↓

🔩 Kim loại

↓

🏭 Máy lắp ráp

↓

🤖 Robot

↓

⚡ Hệ thống phòng thủ

Toàn bộ quá trình diễn ra trực tiếp trên màn hình.

Đây là điểm tạo nên cảm giác:

> **Nhà máy thực sự đang hoạt động.**

---

# 17. ROBOT

Robot không chỉ có một loại.

### 🤖 Worker Bot

Thu thập tài nguyên.

### 🤖 Carrier Bot

Vận chuyển tài nguyên.

### 🤖 Engineer Bot

Bảo trì máy móc.

### 🤖 Combat Bot

Hỗ trợ phòng thủ.

### 🤖 Mega Bot

Robot cấp cao được mở khóa ở milestone lớn.

Mỗi loại có animation và hành vi riêng.

---

# 18. HỆ THỐNG QUÁI VẬT

Quái vật cũng phát triển theo tiến trình của người chơi.

### Lv.1 – Scrap Crawler

HP: 100

### Lv.5 – Scrap Beast

HP: 5.000

### Lv.10 – Armored Beast

HP: 100.000
Armor: 50%

### Lv.20 – Plasma Mutant

HP: 1.000.000
Kháng Plasma

### Lv.50 – Mechanical Titan

HP: 100.000.000

### Boss

## ☠️ SCRAP TITAN

HP cực lớn.

Boss xuất hiện theo những mốc đặc biệt.

---

# 19. BOSS WAVE

Sau một khoảng thời gian:

> ⚠️ WARNING

> **SCRAP TITAN INCOMING**

Toàn bộ căn cứ chuyển sang trạng thái cảnh báo.

* Đèn đỏ bật
* Robot di chuyển nhanh hơn
* Vũ khí bắt đầu tích năng lượng
* Hiệu ứng cảnh báo xuất hiện

Boss bước vào màn hình.

Sau đó:

⚡ PLASMA CANNON CHARGE

3...

2...

1...

💥 **PLASMA BEAM**

Boss bị tiêu diệt.

Người chơi nhận phần thưởng cực lớn.

---

# 20. PHẦN THƯỞNG BOSS

Boss có thể cho:

💰 Tiền
💎 Crystal
⚙️ Module
🏆 Milestone
🔫 Weapon Unlock

Ví dụ:

Boss:

**Scrap Titan**

Reward:

💰 +10.000.000
💎 +100 Crystal
🔓 Unlock: Railgun

---

# 21. CỬA HÀNG

Cửa hàng có thể chia thành:

## 🛒 Weapons

Mua vũ khí.

## ⚙️ Machines

Mua máy sản xuất.

## 🤖 Robots

Mua robot.

## 💎 Special

Mua nâng cấp đặc biệt.

Tất cả đều mở dưới dạng panel trên màn hình chính.

---

# 22. HỆ THỐNG NÂNG CẤP

Người chơi có nhiều loại nâng cấp.

### 🏭 Production

Tăng tốc độ sản xuất.

### 🔫 Weapon Damage

Tăng sát thương.

### ⚡ Attack Speed

Tăng tốc độ bắn.

### 🎯 Range

Tăng phạm vi.

### 🤖 Robot Speed

Tăng tốc độ robot.

### 💰 Income

Tăng tiền nhận được.

### 💤 Offline Production

Tăng tài nguyên nhận khi offline.

---

# 23. MILESTONE

Người chơi luôn có những mục tiêu lớn.

Ví dụ:

| Mốc             | Phần thưởng        |
| --------------- | ------------------ |
| 3 đá + 3 gạch   | Xây căn cứ         |
| Giết 1 quái     | Mở Shop            |
| 10 quái         | Worker Bot         |
| 100 quái        | Resource Generator |
| 1.000 quái      | Factory            |
| 10.000 quái     | Plasma Cannon      |
| 100.000 quái    | Laser              |
| 1.000.000 quái  | Railgun            |
| 10.000.000 quái | Boss Factory       |

Milestone giúp người chơi luôn có mục tiêu tiếp theo.

---

# 24. PHÁT TRIỂN HÌNH ẢNH CỦA KHU ĐẤT

Đây là một phần quan trọng.

Khu đất phải **thay đổi trực quan theo tiến trình**.

### Giai đoạn 1

🏝️ Đất hoang

Chỉ có:

* Đá
* Gạch
* Cây
* Đất

### Giai đoạn 2

🏕️ Căn cứ nhỏ

Có:

* Tường
* Kho
* Turret

### Giai đoạn 3

🏭 Xưởng

Có:

* Máy móc
* Băng chuyền
* Robot

### Giai đoạn 4

⚙️ Nhà máy tự động

Có:

* Nhiều dây chuyền
* Robot
* Turret
* Plasma

### Giai đoạn 5

🌌 Mega Factory

Có:

* Nhà máy khổng lồ
* Plasma Cannon
* Laser
* Railgun
* Robot
* Boss Defense

Như vậy người chơi **thực sự nhìn thấy thành quả của mình.**

---

# 25. OFFLINE PROGRESS

Idle Game bắt buộc nên có Offline Progress.

Ví dụ người chơi thoát game:

Production:

**100.000 / giây**

Sau 3 tiếng quay lại:

Offline Time:

**3 giờ**

Offline Production:

**1.080.000.000**

Người chơi nhận:

💰 +1.080.000.000

Có animation:

> **Trong lúc bạn vắng mặt, nhà máy đã tiếp tục hoạt động.**

---

# 26. PRESTIGE

Khi căn cứ đạt tới mức cực lớn, người chơi có thể reset.

Ví dụ:

Factory:

**Level 100**

Production:

**1.000.000.000 / sec**

Người chơi chọn:

## 💎 PRESTIGE

Reset:

* Nhà máy
* Vũ khí
* Robot
* Tiền
* Production

Nhưng nhận:

## 💎 Energy Core

Ví dụ:

100 Energy Core

Dùng để mua nâng cấp vĩnh viễn:

+25% Production
+20% Weapon Damage
+50% Offline Income
+10% Robot Speed

Sau Prestige:

Người chơi bắt đầu lại.

Nhưng nhanh hơn rất nhiều.

---

# 27. VÒNG LẶP CORE LOOP HOÀN CHỈNH

## Giai đoạn đầu:

```text
🏝️ ĐẤT HOANG
      ↓
🪨 NHẶT ĐÁ
      ↓
🧱 NHẶT GẠCH
      ↓
3 ĐÁ + 3 GẠCH
      ↓
🏰 XÂY TRỤ PHÒNG THỦ
```

## Giai đoạn chiến đấu:

```text
👹 QUÁI XUẤT HIỆN
      ↓
🔫 TURRET TỰ BẮN
      ↓
💥 QUÁI CHẾT
      ↓
💰 NHẬN TIỀN
```

## Giai đoạn nâng cấp:

```text
💰 TIỀN
      ↓
🛒 CỬA HÀNG
      ↓
🔫 MUA VŨ KHÍ
      ↓
💥 DAMAGE CAO HƠN
      ↓
👹 GIẾT QUÁI MẠNH HƠN
      ↓
💰 KIẾM NHIỀU TIỀN HƠN
```

## Giai đoạn Idle:

```text
🤖 ROBOT
      ↓
🪨 TỰ THU THẬP
      ↓
⚙️ MÁY TỰ SẢN XUẤT
      ↓
🏭 FACTORY
      ↓
📈 PRODUCTION TĂNG
      ↓
💰 TÀI NGUYÊN TĂNG
```

## Giai đoạn cuối:

```text
🏭 MEGA FACTORY
      ↓
🤖 ROBOT TỰ HOẠT ĐỘNG
      ↓
⚙️ NHÀ MÁY TỰ SẢN XUẤT
      ↓
🔫 VŨ KHÍ TỰ PHÒNG THỦ
      ↓
👹 BOSS
      ↓
💥 BOSS CHẾT
      ↓
💰💎 PHẦN THƯỞNG
      ↓
🏆 MILESTONE
      ↓
💎 PRESTIGE
      ↓
🔄 BẮT ĐẦU VÒNG MỚI VỚI SỨC MẠNH LỚN HƠN
      ↓
   LẶP LẠI
```

---

# 28. CORE LOOP TỔNG THỂ

```text
                 🏝️ VÙNG ĐẤT HOANG
                         ↓
                🪨🧱 THU THẬP
                         ↓
                   🏰 XÂY CĂN CỨ
                         ↓
                   👹 QUÁI VẬT
                         ↓
                 🔫 PHÒNG THỦ
                         ↓
                     💰 TIỀN
                         ↓
                  🛒 CỬA HÀNG
                         ↓
                   🔫 VŨ KHÍ MỚI
                         ↓
                  👹 QUÁI MẠNH HƠN
                         ↓
                    💰💎 REWARD
                         ↓
                    🤖 ROBOT
                         ↓
                  ⚙️ TỰ ĐỘNG HÓA
                         ↓
                     🏭 FACTORY
                         ↓
                 📈 PRODUCTION
                         ↓
                 🏆 MILESTONE
                         ↓
                     ☠️ BOSS
                         ↓
                    💎 PRESTIGE
                         ↓
                  🔄 VÒNG LẶP MỚI
```

---

# 29. CẢM GIÁC TIẾN TRIỂN CỦA NGƯỜI CHƠI

Người chơi phải cảm nhận được 5 bước phát triển:

### Bước 1 – Sinh tồn

> "Mình phải tự nhặt tài nguyên."

### Bước 2 – Xây dựng

> "Mình đã có căn cứ."

### Bước 3 – Phòng thủ

> "Căn cứ có thể tự giết quái."

### Bước 4 – Tự động hóa

> "Robot và máy móc đang làm mọi thứ thay mình."

### Bước 5 – Thống trị

> "Mình sở hữu một nhà máy khổng lồ có thể tự sản xuất và tự tiêu diệt Boss."

Đây chính là **fantasy progression** của game.

---

# 30. PHONG CÁCH HÌNH ẢNH

## 3D Stylized Low-poly

Không làm realistic để giảm độ phức tạp sản xuất.

Phong cách:

* Low-poly
* Chibi / Chunky
* Sci-fi
* Industrial
* Màu sắc rõ ràng
* Vật thể lớn, dễ nhìn
* Animation đơn giản nhưng có sức sống

Màu chủ đạo có thể sử dụng:

🟠 Orange
⚪ White
⚫ Dark Gray
🔵 Cyan / Plasma Blue

---

# 31. CÁC MODEL CỐT LÕI

Bộ asset cơ bản:

### Environment

* Đất
* Đá
* Gạch
* Cây
* Rác
* Quặng

### Building

* Kho
* Trụ phòng thủ
* Factory
* Resource Generator
* Workshop
* Weapon Station

### Machines

* Máy nghiền
* Lò luyện
* Máy lắp ráp
* Băng chuyền
* Robot Factory

### Robots

* Worker Bot
* Carrier Bot
* Engineer Bot
* Combat Bot

### Weapons

* Auto Cannon
* Plasma Cannon
* Laser
* Missile Launcher
* Railgun
* Plasma Beam
* Orbital Cannon

### Enemies

* Scrap Crawler
* Scrap Beast
* Armored Beast
* Plasma Mutant
* Mechanical Titan
* Scrap Titan Boss

---

# 32. UI CHÍNH

Thanh trên:

```text
💰 MONEY
🔩 MATERIAL
⚡ ENERGY
💎 CRYSTAL
🏆 FACTORY LEVEL
```

Thanh dưới:

```text
[🏗️ XÂY DỰNG]
[🛒 SHOP]
[🔫 VŨ KHÍ]
[⚙️ NÂNG CẤP]
[🏆 MILESTONE]
[💎 PRESTIGE]
```

Ở giữa:

**Toàn bộ khu đất và nhà máy 3D.**

---

# 33. NGUYÊN TẮC THIẾT KẾ QUAN TRỌNG

Game phải giữ 5 nguyên tắc:

### 1. Single Screen

Không biến thành game chạy map.

### 2. Idle

Các hệ thống quan trọng phải có khả năng tự vận hành.

### 3. Incremental

Con số phải tăng mạnh theo thời gian.

### 4. Visual Progression

Không chỉ số tăng mà **khu đất thực sự thay đổi**.

### 5. Automation

Người chơi càng chơi lâu:

**càng ít phải làm thủ công.**

---

# 34. CÂU MÔ TẢ GAME

> **Idle Factory Defense là game 3D Idle Single-Screen, nơi người chơi bắt đầu từ một vùng đất hoang, tự thu thập tài nguyên để xây dựng căn cứ phòng thủ, tiêu diệt quái vật để kiếm tiền, mua vũ khí và từng bước tự động hóa toàn bộ hệ thống bằng robot và nhà máy. Càng phát triển, căn cứ càng lớn mạnh, sản xuất càng nhanh và có khả năng chống lại những quái vật mạnh hơn.**

---

# 35. TÓM TẮT GAME TRONG MỘT CÂU

> **"Bắt đầu với 3 viên đá và 3 viên gạch, xây dựng một căn cứ, biến nó thành nhà máy tự động và cuối cùng tạo ra một pháo đài công nghiệp đủ sức tiêu diệt những con quái vật khổng lồ."**

---

# 36. CORE LOOP CUỐI CÙNG

**TAP / CLICK**

↓

**THU THẬP**

↓

**XÂY DỰNG**

↓

**PHÒNG THỦ**

↓

**TIÊU DIỆT QUÁI**

↓

**NHẬN TIỀN**

↓

**MUA VŨ KHÍ**

↓

**NÂNG CẤP**

↓

**MỞ KHÓA ROBOT**

↓

**TỰ ĐỘNG HÓA**

↓

**XÂY FACTORY**

↓

**PASSIVE PRODUCTION**

↓

**MỞ KHÓA CÔNG NGHỆ**

↓

**ĐÁNH BOSS**

↓

**MILESTONE**

↓

**PRESTIGE**

↓

**BẮT ĐẦU VÒNG MỚI VỚI SỨC MẠNH LỚN HƠN**

**→ LẶP LẠI.**

---

# 37. TÍNH NĂNG ĐẶC BIỆT MỚI (SPECIAL FEATURES)

## 37.1 ⚡ Chế độ "Ép Xung Quá Tải" (Overclock / Red Alert Mode)
* **Mô tả:** Cơ chế "cứu nguy khẩn cấp" khi căn cứ bị quái vật áp sát hoặc khi đánh Boss.
* **Kích hoạt:** Bấm nút **[OVERCLOCK]** trên màn hình chính (hoặc phím tắt Spacebar).
* **Hiệu ứng kích hoạt (Kéo dài 10 giây):**
  * Toàn bộ trụ súng tăng **+300% tốc độ bắn** (xả đạn liên thanh cực đại).
  * Toàn bộ Robot vận chuyển & khai thác tăng **+200% tốc độ di chuyển**.
  * Nhà máy sản xuất tài nguyên với tốc độ gấp 3 lần.
  * **Hiệu ứng đồ họa/âm thanh:** Màn hình viền chớp đỏ cảnh báo (Red Alert), tia điện plasma chớp sáng quanh các cỗ máy, âm thanh máy móc gầm rú tăng áp.
* **Thời gian hồi chiêu (Cooldown):** 60 giây. Sau 10s quá tải, hệ thống xịt khói tản nhiệt 2 giây.
* **Nâng cấp công nghệ:** Có thể dùng tiền nâng cấp: Giảm Cooldown từ 60s → 45s → 30s, hoặc tăng thời gian duy trì lên 15 giây.

---

## 37.2 🛰️ Chi Viện Vệ Tinh Vũ Trụ (Orbital Strike & Supply Drop)
* **Mô tả:** Kết nối tín hiệu với trạm không gian quỹ đạo phía trên bầu khí quyển.
* **Công trình yêu cầu:** Xây dựng **Đĩa Radar Vệ Tinh (Satellite Dish)** trên căn cứ.
* **2 Loại Chi Viện:**
  1. 🔴 **Tia Laser Quỹ Đạo (Orbital Beam Strike):**
     * Người chơi click chuột vào vị trí bất kỳ trên bầy quái vật.
     * Một chùm tia năng lượng khổng lồ chiếu thẳng từ trên trời xuống, quét sạch và đốt cháy toàn bộ quái vật trong vùng ảnh hưởng trong 3 giây.
  2. 📦 **Thả Hòm Tiếp Tế Quỹ Đạo (Orbital Supply Pod):**
     * Định kỳ (mỗi 3 - 5 phút), một kén tiếp tế bốc cháy lao từ vũ trụ thả dù xuống căn cứ.
     * Click vào kén để nhận ngay: Một bọc Tiền lớn, Tài nguyên hiếm hoặc buff x2 sát thương tạm thời.

---

## 37.3 🤖 Drone Mini Đồng Hành Hút Đồ (Companion Drone)
* **Mô tả:** Một chú robot bay tí hon đóng vai trò "trợ lý thông minh" luôn bay lượn theo con trỏ chuột của người chơi.
* **Chức năng chính:**
  * 🧲 **Tự động hút tài nguyên (Auto-Loot Magnet):** Bất cứ khi nào quặng văng ra hoặc quái chết rơi tiền gần con trỏ chuột, Drone sẽ lập tức phóng tia nam châm hút sạch về kho, giúp người chơi rảnh tay không cần click từng món.
  * 🔫 **Bắn phụ trợ (Micro Laser):** Trang bị súng laze mini tự động tỉa quái vật đang đến gần chuột hoặc bắn làm chậm (Slow 20%) quái vật nguy hiểm.
  * 🎨 **Tùy biến ngoại hình:** Mở khóa các skin vui nhộn (Drone mắt lồi, Drone tai mèo, Drone đầu lâu bóng đêm...).

---

# 38. BỘ QUY CHUẨN TỐI ƯU HÓA HIỆU NĂNG (SNAPDRAGON 810+)
*Áp dụng nghiêm ngặt để đảm bảo game luôn chạy ổn định 60 FPS, không drop frame, không nóng ran máy trên các chip di động tầm trung từ Snapdragon 810 trở lên.*

### 38.1 ⚡ Khóa Tốc Độ Khung Hình (Frame Rate & Thermal Throttling)
* Code khởi động: `Application.targetFrameRate = 60;` và `QualitySettings.vSyncCount = 0;`.
* Chống hiện tượng CPU/GPU chạy quá tải gây quá nhiệt (Overheating) khiến chip Snap 810 bị bóp xung (Thermal Throttling).

### 38.2 ♻️ Cơ Chế Object Pooling Triệt Để (Zero Garbage Collection Spike)
* **Tuyệt đối cấm:** Dùng `Instantiate()` và `Destroy()` liên tục trong game loop đối với:
  * Đạn súng (Bullets / Missiles).
  * Quái vật sinh ra và chết đi (Enemies).
  * Chữ số sát thương bay lên (Damage Popups / Floating Text).
  * Hiệu ứng hạt nổ (Particle Systems).
* **Bắt buộc:** Khởi tạo sẵn một kho đạn/quái (Pool) ngay từ đầu màn, chỉ bật (`SetActive(true)`) và tắt (`SetActive(false)`) khi dùng lại.
* Giữ GC Allocation = 0 bytes/frame để loại bỏ hoàn toàn các vi giật (Micro-stutters) khi bầy quái ùa ra đông.

### 38.3 🎨 Đồ Họa Stylized Low-Poly & Gộp Draw Calls (Batches < 30)
* **1 Bảng màu Texture Palette chung:** Toàn bộ công trình, robot, quái vật, mỏ đá dùng chung 1 bức ảnh texture atlas màu kích thước 256x256 pixel.
* **GPU Instancing & Dynamic Batching:** Toàn bộ khối quặng, quái vật cùng loại được GPU vẽ cùng 1 lần gọi (1 Draw Call duy nhất).
* **URP Shader tối giản:** Sử dụng `Universal Render Pipeline/Simple Lit` hoặc `Unlit`. Tắt khử răng cưa nặng (chỉ dùng FXAA), đổ bóng mềm chất lượng thấp/vừa.

### 38.4 ⚙️ Tối Ưu Vật Lý (Physics Engine)
* Đặt chu kỳ tính toán vật lý: `Time.fixedDeltaTime = 0.02f` (50Hz).
* Không dùng `MeshCollider` phức tạp cho quái và đạn; chỉ dùng `BoxCollider` hoặc `SphereCollider` nguyên bản.
* Đạn bắn tầm xa ưu tiên dùng Raycast hoặc di chuyển tịnh tiến bằng toán học thuần thay vì Rigidbody AddForce nặng nề.

### 38.5 🤖 Thuật Toán Di Chuyển AI Nhẹ (Lightweight Pathfinding)
* Quái vật và Robot không gọi `NavMesh.CalculatePath()` liên tục mỗi frame.
* Sử dụng hệ thống điểm mốc (Waypoints) hoặc phép tính khoảng cách hình học đơn giản (`Vector3.MoveTowards`) để CPU mát nhất có thể.



