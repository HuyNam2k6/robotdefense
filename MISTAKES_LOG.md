# 📜 NHẬT KÝ LỖI ĐÃ MẮC & NGUYÊN TẮC BẮT BUỘC TRÁNH LẶP LẠI (MISTAKES LOG)
*Dự án: Robot Defense 3D (Mobile Portrait) | Cập nhật ngày: 02/10/2026*

> **⚠️ ĐIỀU KHOẢN BẮT BUỘC:**  
> **TRƯỚC KHI BẮT ĐẦU CHẠY BẤT KỲ PROMPT NÀO, AI BẮT BUỘC PHẢI ĐỌC FILE NÀY ĐỂ ĐỐI CHIẾU VÀ TUYỆT ĐỐI KHÔNG TÁI PHẠM BẤT KỲ LỖI NÀO DƯỚI ĐÂY.**

---

## 🛑 1. DANH SÁCH CÁC LỖI NGHIÊM TRỌNG ĐÃ MẮC TRONG QUÁ KHỨ

### ❌ Lỗi 1: Hiểu sai yêu cầu người dùng, tự ý xóa/biến đổi luồng Cửa Hàng làm mất tính năng Mua Tường
- **Mô tả chi tiết:**
  - Khi người dùng yêu cầu sửa lỗi cửa hàng và làm cho icon con robot đào mỏ hiển thị rõ để bấm vào ra robot đào mỏ từ `prefabsbot`, AI đã tự ý biến nút Cửa hàng thành nút chỉ mua duy nhất con robot đào mỏ (`OnMiningRobotClicked`) và không mở Menu Cửa hàng nữa.
  - Hậu quả: Người chơi bị mất hoàn toàn khả năng mở cửa hàng để **Mua Tường (Walls)**, nâng cấp tường, nâng cấp cúp sắt.
- **Nguyên nhân gốc rễ:**
  - Giả định vội vàng, không giữ cái nhìn tổng thể về hệ thống kinh tế và xây dựng.
  - Tự ý thay đổi chức năng gốc của nút Cửa Hàng mà không suy xét đến các vật phẩm khác người dùng cần mua.
- **Nguyên tắc khắc phục & bắt buộc tuân thủ:**
  - **KHÔNG BAO GIỜ** được xóa bỏ, vô hiệu hóa hoặc thu hẹp chức năng của Cửa Hàng thành 1 nút đơn năng trừ khi người dùng nói rõ từ ngữ "hãy xóa bỏ hoàn toàn menu cửa hàng và các đồ khác".
  - Cửa hàng phải luôn đảm bảo mua được đầy đủ: **Bức Tường (Walls kéo dài)**, **Nâng cấp Tường**, **Robot Đào Mỏ**, **Nâng cấp Cúp**.
  - Nút Cửa Hàng ngoài màn hình (`Btn_ShoppingCart` / `shopOpenButton`) phải luôn mở được bảng Menu Cửa Hàng (`ToggleShopPanel`).

---

### ❌ Lỗi 2: Bóp méo kích thước Transform / Scale của Model 3D (Non-Uniform Scale Distortion)
- **Mô tả chi tiết:**
  - Model robot trong Scene và file Prefab bị chỉnh sửa tỉ lệ Scale không đều: `m_LocalScale: {x: 3.123151, y: 1.9746555, z: 4.249715}`.
  - Hậu quả: Con robot bị méo mó, bẹt, dị dạng khi xuất hiện trong game, phá hỏng tính thẩm mỹ.
- **Nguyên nhân gốc rễ:**
  - Kế thừa lại giá trị Scale bị chỉnh thủ công sai trên Scene trước đó và lưu vào Prefab, hoặc code can thiệp tùy tiện vào Scale.
- **Nguyên tắc khắc phục & bắt buộc tuân thủ:**
  - **TUYỆT ĐỐI KHÔNG** được bóp méo tỉ lệ Transform của model 3D (non-uniform scale).
  - Tỉ lệ của toàn bộ Prefab nhân vật, robot, trụ súng phải luôn là chuẩn đồng nhất **`Vector3.one` (1, 1, 1)**.
  - Khi Instantiate Prefab trong code C#, luôn gán rõ: `newObject.transform.localScale = Vector3.one;` để ngăn chặn mọi nguy cơ bị kế thừa scale méo.

---

### ❌ Lỗi 3: Tham chiếu sai thư mục Prefab & phụ thuộc đối tượng tạm trên Scene
- **Mô tả chi tiết:**
  - Người dùng đặt file Prefab chuẩn tại thư mục `Assets/prefabsbot/BipedRobot_Prefab.prefab`, nhưng code trước đó lại cố tìm `GameObject.Find("mine")` hoặc load từ `Assets/Models/...` (vốn là file fbx/prefab thô chưa có script `WorkerBot`).
  - Hậu quả: Khi mua robot, hệ thống sinh ra object lỗi hoặc không tìm thấy prefab trong game.
- **Nguyên nhân gốc rễ:**
  - Không đọc và kiểm tra chính xác vị trí thư mục và GUID của prefab mà người dùng chỉ định.
- **Nguyên tắc khắc phục & bắt buộc tuân thủ:**
  - Luôn kiểm tra đúng thư mục Prefab người dùng yêu cầu (ví dụ: `Assets/prefabsbot/`).
  - Không dựa vào `GameObject.Find` với các tên tạm bợ để nhân bản. Luôn gán trực tiếp Prefab từ Project Asset hoặc load đúng đường dẫn `Assets/prefabsbot/`.

---

### ❌ Lỗi 4: Phá vỡ bố cục UI màn hình dọc (Hardcoded Screen Size)
- **Mô tả chi tiết:**
  - Từng ép cứng kích thước `dashboardPanel.sizeDelta = new Vector2(900f, 200f)` và căn giữa màn hình trong `Awake()`, làm bảng che chắn toàn bộ góc nhìn màn hình dọc (9:16).
  - Hậu quả: Giao diện tràn viền, mất thẩm mỹ và cản trở thao tác điều khiển nhân vật / quan sát chiến trường.
- **Nguyên tắc khắc phục & bắt buộc tuân thủ:**
  - Mọi UI phải thiết kế chuẩn Mobile Portrait (Màn hình dọc 9:16, 1080x1920).
  - Bảng Cửa hàng phải nằm ở đáy màn hình (Thumb zone), có thanh cuộn hoặc dạng ngăn kéo mở lên gọn gàng, không set kích thước cứng chắn ngang tâm mắt người chơi.

---

### ❌ Lỗi 5: Lỗi xoay hướng đặt tường không đồng bộ (Rotation Desync & Góc Xoay 45° Không Chuẩn Lưới)
- **Mô tả chi tiết:**
  - Nút "XOAY" trong code cũ từng dùng bước xoay 45° (`RotatePreview(45f)`). Xoay 45° làm tường bị nghiêng chéo, không ăn khớp với hệ trục lưới (Grid) 0° và 90°.
  - Khi đặt tường, code tự ép `wallRot = isHorizontal ? Quaternion.identity : Quaternion.Euler(0, 90, 0)`. Khi người chơi chỉ click 1 phát để đặt đoạn đơn (`dx=0, dz=0`), code luôn ép về hướng Ngang (`Quaternion.identity`), bất chấp người chơi vừa bấm nút XOAY để xoay Dọc!
- **Nguyên nhân gốc rễ:**
  - Góc xoay của Hologram Preview bị tách rời khỏi logic Instantiate và logic đặt đơn.
- **Nguyên tắc khắc phục & bắt buộc tuân thủ:**
  - Nút XOAY cho công trình lưới phải xoay chuẩn **90 độ** (0°, 90°, 180°, 270°).
  - Khi đặt 1 đoạn đơn (không kéo dài), đoạn sinh ra PHẢI LẤY ĐÚNG 100% GÓC XOAY `currentYRotation` đang hiển thị trên con trỏ Preview.
  - Khi kéo chuột: hàng ngang ép 0°, hàng dọc ép 90°, và cập nhật lại `currentYRotation` để preview tiếp theo không bị nhảy hướng.

---

### ❌ Lỗi 6: Hàng tường bị lệch giật cấp bậc thang do lấy độ cao Y riêng lẻ trên địa hình dốc (Uneven Ground Height Misalignment)
- **Mô tả chi tiết:**
  - Khi kéo 1 hàng tường dài, mỗi đoạn tường `i` lại gọi `SampleGroundY(x, z)` riêng lẻ. Do mặt đất (Terrain) có độ dốc lượn sóng, mỗi đoạn tiếp theo bị tụt độ cao Y xuống theo sườn đồi.
  - Dưới góc nhìn Camera nghiêng 60°, sự chênh lệch độ cao Y làm cho hàng tường bị giật cấp bậc thang (stair-stepping), vừa bị tụt xuống đáy màn hình vừa tạo các mối nối gãy khúc lệch nhau, không tạo thành một khối tường thẳng tắp.
- **Nguyên nhân gốc rễ:**
  - Thiếu cơ chế khóa độ cao chuẩn (`fixedY`) cho toàn bộ hàng tường trong cùng một lượt kéo.
  - `groundLayer` từng nhận cả collider của tường, robot, hoặc vật thể khác khi raycast.
- **Nguyên tắc khắc phục & bắt buộc tuân thủ:**
  - Khi kéo một hàng tường:
    1. Cố định trục vuông góc: Kéo hàng Ngang thì toàn bộ đoạn có `z = startSnap.z` cố định 100%; kéo hàng Dọc thì `x = startSnap.x` cố định 100%.
    2. Cố định độ cao Y: Toàn bộ các đoạn trong cùng 1 hàng phải dùng chung `fixedY = startSnap.y` để mặt trên của tường thẳng tắp và phẳng lì như thước kẻ.
    3. `SampleGroundY` phải lọc bỏ collider của Wall, Bot, Turret để không bao giờ bị nhảy độ cao sai lệch.

---

### ❌ Lỗi 6: Lỗi compile C# trong file Editor khiến Unity rơi vào Safe Mode
- **Mô tả chi tiết:**
  - Dùng sai tên API `EditorGUILayout.Label` (thay vì `EditorGUILayout.LabelField`) dẫn đến lỗi biên dịch CS0117 trong assembly Editor.
  - Hậu quả: Unity 6 tự động kích hoạt Safe Mode, cô lập mã nguồn và tự động mở sang Scene trống rỗng (Untitled), làm người dùng tưởng toàn bộ dự án bị hỏng/mất trắng.
- **Nguyên nhân gốc rễ:**
  - Viết code Editor nhưng không kiểm tra kỹ tên phương thức chính xác của Unity API trước khi lưu file.
- **Nguyên tắc khắc phục & bắt buộc tuân thủ:**
  - Bắt buộc kiểm tra chuẩn xác chữ ký hàm của Unity Editor API trước khi xuất mã.
  - Ngay sau khi tạo hoặc sửa bất kỳ file C# nào, phải kiểm tra nhật ký `Editor.log` để xác nhận việc biên dịch thành công 100%, tuyệt đối không được để sót lỗi CS nào.
  - Nếu Unity bị đưa về Scene rỗng `Untitled`, hướng dẫn mở lại đúng Scene gốc `Assets/Scenes/SampleScene.unity`.

---

### ❌ Lỗi 7: Can thiệp ModelImporter và SaveAndReimport() trong Editor script gây lỗi Fatal Error MemoryStream trên Unity 6
- **Mô tả chi tiết:**
  - Khi cố gắng can thiệp `ModelImporter.clipAnimations` và gọi `SaveAndReimport()` bên trong Editor script để ép bật `Loop Time` cho file FBX của quái vật, bộ nhớ đệm RAM stream (`MemoryStream`) của Unity 6 bị đọc lệch con trỏ (`Position out of bounds`).
  - Hậu quả: Unity văng cửa sổ `Fatal Error! The file 'MemoryStream' is corrupted! Remove it and launch unity again!` và cưỡng chế thoát (crash).
- **Nguyên nhân gốc rễ:**
  - Can thiệp trực tiếp vào luồng re-import của ModelImporter FBX ở runtime Editor trong Unity 6 khiến engine native C++ xung đột với các lệnh lưu Asset/Prefab đang diễn ra cùng thời điểm.
- **Nguyên tắc khắc phục & bắt buộc tuân thủ:**
  - **TUYỆT ĐỐI KHÔNG** gọi `SaveAndReimport()` trên `ModelImporter` một cách tùy tiện trong Editor scripts khi đang thao tác nhiều asset cùng lúc.
  - Để bật `Loop Time` cho animation, phương pháp an toàn nhất là:
    1. Hướng dẫn người dùng chỉnh thủ công 1 cú click trên tab `Animation` của file FBX trong Inspector rồi bấm `Apply`.
    2. Hoặc trích xuất clip bằng `Object.Instantiate` tạo file `.anim` độc lập mà không can thiệp vào file gốc FBX.

---

### ❌ Lỗi 8: Ghi đè file DLL vào Library/ScriptAssemblies và dùng [InitializeOnLoadMethod] tự động re-import
- **Mô tả chi tiết:**
  - Tự ý dùng lệnh biên dịch ngoài rồi copy file `.dll` đè vào thư mục `Library/ScriptAssemblies/` trong lúc Unity đang mở, dẫn đến file mã máy và file ký hiệu `.pdb` bị lệch timestamp (`Symbol file doesn't match image`).
  - Đồng thời script trong `Assets/Editor` chứa `[InitializeOnLoadMethod]` tự chạy ngầm mỗi lần khởi động dự án hoặc reload domain, cố tình re-import asset làm tràn bộ nhớ đệm RAM serialization.
  - Hậu quả: Unity văng hộp thoại `Fatal Error! The file 'MemoryStream' is corrupted! Remove it and launch unity again! [Position out of bounds!]`.
- **Nguyên nhân gốc rễ:**
  - Can thiệp thô bạo vào thư mục nội bộ `Library/` của Unity thay vì để Unity Editor tự biên dịch C#.
- **Nguyên tắc khắc phục & bắt buộc tuân thủ:**
  - **TUYỆT ĐỐI KHÔNG** copy đè file `.dll` vào `Library/ScriptAssemblies/`. Để Unity tự biên dịch C# một cách tự nhiên.
  - **TUYỆT ĐỐI KHÔNG** để các thuộc tính `[InitializeOnLoadMethod]` tự động trigger `AssetDatabase.Refresh()` hay `SaveAndReimport()` khi load dự án.
  - Khi gặp lỗi `MemoryStream is corrupted`: Đóng hoàn toàn Unity, xóa thư mục `Temp/` và thư mục `Library/ScriptAssemblies/`, sau đó mở lại dự án từ Unity Hub.

---

## 📋 2. QUY TRÌNH BẮT BUỘC TRƯỚC KHI CHẠY MỖI PROMPT (PRE-FLIGHT CHECKLIST)

Mỗi khi nhận được 1 prompt mới từ người dùng, AI phải thực hiện tuần tự:

```text
BƯỚC 1: ĐỌC LẠI FILE NÀY (MISTAKES_LOG.MD) & RULE.MD
   ↓
BƯỚC 2: PHÂN TÍCH YÊU CẦU CỦA NGƯỜI DÙNG
   - Người dùng muốn gì?
   - Có tính năng cũ nào cần được BẢO TỒN (như Mua Tường, Nâng Cấp)?
   - Có file/prefab cụ thể nào được chỉ định (như thư mục prefabsbot)?
   ↓
BƯỚC 3: ĐỐI CHIẾU VỚI DANH SÁCH LỖI
   - Giải pháp dự kiến có nguy cơ làm mất tính năng cũ không?
   - Giải pháp dự kiến có làm méo scale model 3D không?
   - Đường dẫn asset có chuẩn xác không?
   ↓
BƯỚC 4: THỰC THI & KIỂM TRA TOÀN DIỆN
   - Chạy test, kiểm tra code không có lỗi compile.
   - Đảm bảo hệ thống cũ (Mua tường, Cúp, Robot) hoạt động bình thường song song với tính năng mới.
```

