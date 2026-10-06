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

### ❌ Lỗi 9: Truy cập biến/thuộc tính chưa được khai báo ở script mục tiêu và đứt gãy chuỗi truyền dữ liệu (CS1061)
- **Mô tả chi tiết:**
  - `UpgradableTurret` gọi `rocketLauncher.rocketDamage = ...` để nâng cấp sát thương pháo tự hành MLRS, nhưng `RocketLauncherTurret` chưa từng khai báo trường `rocketDamage`. Đồng thời `HomingRocket` cũng không có biến `damage` để truyền vào `ExplosionEffect`.
  - Hậu quả: Gây lỗi biên dịch CS1061 chặn toàn bộ dự án Unity không chạy được.
- **Nguyên nhân gốc rễ:**
  - Viết logic nâng cấp chỉ số ở script điều khiển cấp cao nhưng không kiểm tra định nghĩa thành phần ở script con và không kiểm tra chuỗi truyền sát thương (Turret -> Rocket -> Explosion -> Enemy).
- **Nguyên tắc khắc phục & bắt buộc tuân thủ:**
  - Khi tham chiếu hoặc gán bất kỳ biến/thuộc tính nào giữa các script, bắt buộc kiểm tra xem class đích đã khai báo biến public đó chưa.
  - Sau khi sửa bất kỳ mã nguồn C# nào, bắt buộc phải biên dịch thử nghiệm (chạy `dotnet build` hoặc kiểm tra log Unity) để xác nhận 0 Error trước khi kết thúc tác vụ.
  - Luôn đảm bảo chuỗi dữ liệu (Data flow) thông suốt: Trụ bắn gán `damage` cho đạn -> đạn nổ truyền `damage` cho hiệu ứng vụ nổ `ExplosionEffect` -> gọi `TakeDamage` lên quái.

---

### ❌ Lỗi 10: Người chơi bị rơi xuyên lòng đất khi dính đạn hoặc hiệu ứng nổ (Player Falling Through Terrain Ground)
- **Mô tả chi tiết:**
  - Khi người chơi đứng trong tầm bắn của trụ hoặc ở gần vụ nổ, người chơi bị rơi tụt xuống lòng đất vô tận.
- **Nguyên nhân gốc rễ:**
  - Đạn `Cube (1)` có BoxCollider dạng đặc cứng (`isTrigger: false`) mà không có Rigidbody. Khi đạn bay va chạm với CharacterController của người chơi từ trên xuống, thuật toán depenetration của PhysX đẩy người chơi lún mạnh xuống dưới mặt đất xuyên qua Terrain collider.
  - `ExplosionEffect` dùng `GameObject.CreatePrimitive(PrimitiveType.Sphere)` sinh ra SphereCollider đặc cứng nở to trong frame đầu tiên.
  - `PlayerController` chỉ phụ thuộc vào `cc.isGrounded`. Một khi người chơi bị đẩy lún qua mặt đất chỉ vài centimet, `cc.isGrounded` trở thành `false` và trọng lực kéo người chơi rơi tự do không có điểm dừng vì thiếu cơ chế kiểm tra chặn sàn (Ground Clamping).
- **Nguyên tắc khắc phục & bắt buộc tuân thủ:**
  - Mọi collider của đạn/tên lửa (`Bullet`, `HomingRocket`) bắt buộc phải là Trigger (`isTrigger: true`).
  - Đạn và tên lửa của phe mình phải luôn bỏ qua va chạm với Player (`CompareTag("Player")` và `GetComponentInParent<PlayerController>()`).
  - Trong `PlayerController.cs`, bắt buộc phải có hàm bảo vệ độ cao mặt đất `EnforceGroundBoundary()` (dùng `Terrain.SampleHeight` và `Physics.Raycast` xuống đất). Nếu `transform.position.y < groundY`, lập tức kéo người chơi trở lại mặt sàn và triệt tiêu vận tốc rơi âm.

---

### ❌ Lỗi 11: Viên đạn chỉ in Debug.Log mà quên gọi gây sát thương thực tế (Missing TakeDamage Call)
- **Mô tả chi tiết:**
  - Trụ FlamethrowerTurret bắn đạn trúng quái vật AlienBug nhưng HP quái không bị trừ giọt nào.
- **Nguyên nhân gốc rễ:**
  - Trong `Bullet.cs:OnHit()`, chỉ có dòng `Debug.Log($"[Đạn] Bắn trúng quái... Sát thương: {damage}")` mà không có dòng nào gọi `enemy.TakeDamage(damage)`.
- **Nguyên tắc khắc phục & bắt buộc tuân thủ:**
  - Bất cứ script vũ khí/đạn nào khi va chạm với quái phải gọi `enemy.TakeDamage(damage)` hoặc `SendMessage("TakeDamage", damage)`.
  - Phải kiểm tra vòng đời truyền sát thương từ Trụ -> Đạn -> Quái vật (TakeDamage) -> Die() -> Rơi vàng.

---

### ❌ Lỗi 12: Đặt thuộc tính main.duration khi ParticleSystem đang chạy lúc sinh hiệu ứng đạn va chạm
- **Mô tả chi tiết:**
  - Khi đạn bắn trúng quái vật hoặc địa hình, console văng hàng loạt lỗi đỏ: `Setting the duration while system is still playing is not supported. Please wait until the system has stopped and all particles have expired or call Stop with ParticleSystemStopBehavior.StopEmittingAndClear to completely stop the system.` tại `Bullet:SpawnImpactEffect (at Assets/Scripts/Bullet.cs:121)`.
- **Nguyên nhân gốc rễ:**
  - Mỗi lần đạn trúng đích, code lại tạo `new GameObject()` và gọi `AddComponent<ParticleSystem>()`. Khi vừa AddComponent, ParticleSystem mặc định được kích hoạt chạy ngay. Việc gán `main.duration = ...` khi hệ thống đang chạy bị Unity cấm tuyệt đối.
  - Ngoài ra, việc liên tục tạo và Destroy GameObject chứa ParticleSystem ở mỗi phát bắn gây tràn bộ nhớ rác (GC Allocation spike), làm tụt FPS trên thiết bị di động.
- **Nguyên tắc khắc phục & bắt buộc tuân thủ:**
  - **TUYỆT ĐỐI KHÔNG** tạo mới GameObject và AddComponent ParticleSystem trong vòng lặp đạn bắn.
  - Sử dụng **Shared ParticleSystem Pool (World Space)** kết hợp `ParticleSystem.Emit(ParticleSystem.EmitParams ep, int count)`.
  - Khởi tạo hệ thống hạt một lần duy nhất, gọi `Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear)` trước khi cấu hình, và chỉ phát hạt bằng `EmitParams` tại vị trí va chạm. Giải pháp này triệt tiêu 100% lỗi duration, đạt chuẩn 0 GC Alloc và giữ vững 60 FPS cho Snapdragon 810+.

---

### ❌ Lỗi 13: Kéo công trình chạm Player làm Player bị bắn lên trời và đứng yên
- **Mô tả chi tiết:**
  - Khi kéo Bức Tường (dài 3m) dính vào Player rồi thả tay ra, Player bị đẩy bắn thẳng đứng lên không trung và đứng yên lơ lửng.
- **Nguyên nhân gốc rễ:**
  1. `CheckPlacementValidity()` chỉ kiểm tra khoảng cách tâm phẳng `distXZ < 1.8f`. Nhưng đoạn tường dài 3m có nửa chiều dài 1.5m + bán kính Player 0.5m = 2.0m. Khi Player chạm vào đầu mút đoạn tường ở khoảng cách 1.9m - 2.5m, kiểm tra bị lọt (coi là hợp lệ) và cho phép đặt.
  2. Khi đặt xuống, Collider của tường bật lại, hàm `PlayerController.EnforceGroundBoundary()` bắn raycast thẳng đứng từ trên đầu Player xuống để dò mặt đất và vô tình bắn trúng nóc của Bức Tường. Hàm gán `groundY = nóc tường` và dịch chuyển Player lên trời!
- **Nguyên tắc khắc phục & bắt buộc tuân thủ:**
  1. Trong `WallSelectionManager`: Kiểm tra va chạm với Player bằng bán kính an toàn $\ge 2.95\text{m}$, kết hợp `Physics.OverlapBox` 3D bao trọn toàn bộ hình hộp đoạn tường và quét tag "Player". Hễ chạm vào Player là lập tức coi là KHÔNG HỢP LỆ (hiển thị màu đỏ), và khi thả tay ra TỰ ĐỘNG QUAY VỀ VỊ TRÍ BAN ĐẦU.
  2. Trong `PlayerController.EnforceGroundBoundary()`: Bỏ qua hoàn toàn collider của `WallSegment` và `UpgradableTurret`, không bao giờ nhận nóc công trình làm mặt đất để dịch chuyển Player. Đồng thời chặn nhảy Y nếu chênh lệch $> 1.2\text{m}$.

---

### ❌ Lỗi 14: Vỏ vũ khí không đổi màu khi nâng cấp và nút Nâng Cấp không nhận click
- **Mô tả chi tiết:**
  - Khi nâng cấp các vũ khí (Thunder, Phaotuhanhl, FlamethrowerTurret), thân pháo không đổi màu theo từng cấp (Bạc, Vàng, Bạch Kim, Kim Cương, Titan) và nút "NÂNG CẤP" trên Card UI bấm vào không nhận hoặc bị đóng giao diện.
- **Nguyên nhân gốc rễ:**
  1. Trong `UpgradableTurret.cs`, khi `isSelected == true`, hàm `Update()` liên tục ghi đè màu xanh dương neon (`neonBlue`) và phát sáng emission 2.2f mỗi frame, che lấp và át hoàn toàn màu sắc thực tế của cấp độ. Đồng thời các model FBX của trụ dùng vật liệu nhúng có texture diffuse tối màu, việc chỉ đổi property block `_BaseColor` khiến màu mới bị nhân với texture tối làm mất màu ánh kim rực rỡ.
  2. Nút nâng cấp không nhận: `EventSystem.current.IsPointerOverGameObject()` không nhận diện được cảm ứng (Touch / Mobile Simulator) nếu không truyền fingerId, dẫn đến khi chạm vào nút nâng cấp thì hệ thống bắn raycast 3D ra phía sau và gọi `DeselectAll()`, đóng UI ngay trước khi nút kịp nhận sự kiện PointerUp/OnClick. Đồng thời Text bên trong Button mặc định có `raycastTarget = true` nuốt sự kiện click của Button.
- **Nguyên tắc khắc phục & bắt buộc tuân thủ:**
  1. Tuyệt đối không ghi đè hiệu ứng nhấp nháy màu xanh lên thân vũ khí trong `Update()`. Trụ được chọn chỉ nảy lên (`TriggerPunchHop`) và có Selection Marker 3D trên đỉnh đầu; vỏ vũ khí phải luôn hiển thị 100% màu sắc và chất liệu kim loại của cấp độ hiện tại.
  2. Tạo hệ thống Level Material URP Lit chuẩn đẹp cho từng cấp độ (Cấp 2: Bạc ánh kim, Cấp 3: Vàng kim hoàng gia, Cấp 4: Bạch kim ngọc + lấp lánh, Cấp 5: Kim cương xanh pha lê phát quang + lấp lánh, Cấp 6: Titan đen tím công nghệ + lấp lánh). Cấp 1 khôi phục 100% material ban đầu của prefab.
  3. Dùng hàm kiểm tra UI đa nền tảng `IsPointerOverUI()` quét cả chuột và `Input.GetTouch(i).fingerId` để không bao giờ bị click xuyên thấu làm đóng UI. Đặt `label.raycastTarget = false;` cho tất cả nhãn chữ trên các nút bấm và gọi `wallCardPanel.transform.SetAsLastSibling()`.

---

### ❌ Lỗi 15: Player bị rơi xuyên lòng đất hoặc bay thẳng lên trời do lỗi Raycast và thiếu kẹp trần/sàn vật lý đa tầng
- **Mô tả chi tiết:**
  - Player đôi khi bị kéo bay thẳng lên trời khi đi qua gốc cây/tảng đá, hoặc bị lún rơi xuyên qua lòng đất xuống biển sâu khi di chuyển hoặc rơi tự do.
- **Nguyên nhân gốc rễ:**
  1. Raycast dò mặt đất trước đây bắn từ 10m trên cao (`pos.y + 10f`) xuống, bắt nhầm các collider ở trên cao (cành cây, tảng đá treo, nóc nhà ga, quái vật...). Qua từng frame, Player bị kéo bay leo lên trời. Ngoài ra xung lực đẩy tách va chạm (PhysX depenetration) khi kẹt collider tĩnh hất văng Player lên trời mà không có giới hạn trần.
  2. Rơi xuyên đất: Khi delta time bị spike hoặc Player rơi nhanh, CharacterController lún qua mặt đất. Khi rơi sâu hơn 1.2m, điều kiện kiểm tra trước đây bỏ qua không cứu Player, khiến Player rơi tự do xuống vô tận.
- **Nguyên tắc khắc phục & bắt buộc tuân thủ:**
  1. Raycast dò mặt đất chỉ bắn từ ngang thắt lưng (`pos.y + 0.6f`) xuống và chỉ chấp nhận điểm va chạm $\le pos.y + 0.35m$ (bàn chân/bậc thềm nhỏ), loại trừ 100% vật thể trên cao.
  2. Khóa trần chống bay lên trời (Anti-Sky Launch Clamp): Chiều cao nhảy tối đa của Player chỉ là 1.6m, trần tối đa cho phép là $groundY + 2.5m$. Hễ vượt quá là ngay lập tức triệt tiêu vận tốc hướng lên và kẹp hạ cánh xuống mặt đất. Kẹp vận tốc nhảy tối đa $\le 10m/s$.
  3. Khóa sàn chống rơi xuyên đất (Underground Shield): Nếu $pos.y < groundY$, lập tức kéo lên $groundY$ và triệt tiêu vận tốc rơi. Nếu rơi khỏi đảo ($Y < 6m$), tự động cứu hộ về vị trí an toàn trên đảo `lastSafeGroundedPos`.
  4. Trượt ngang khi kẹt (Horizontal Depenetration): Khi kẹt va chạm với chướng ngại vật, chỉ trượt theo phương ngang ($Y = 0$), tuyệt đối không đẩy lên trời.

---

### ❌ Lỗi 16: Dùng hàm `CompareTag` với tag chưa được khai báo trong `TagManager` gây văng ngoại lệ `UnityException` và spam Console
- **Mô tả chi tiết:**
  - Trong quá trình raycast dò độ cao mặt đất (`GetTrueGroundHeight`), code gọi `h.collider.CompareTag("Projectile")` và `col.CompareTag("Rock")`.
  - Hậu quả: Vì tag `"Projectile"` và `"Rock"` chưa từng được đăng ký trong danh sách thẻ của Unity (`ProjectSettings/TagManager.asset`), Unity lập tức quăng ngoại lệ `UnityException: Tag: Projectile is not defined.` mỗi frame trong hàm `Update()`, gây spam đỏ liên tục trên Console và làm nghẽn luồng xử lý.
- **Nguyên nhân gốc rễ:**
  - Khác với việc so sánh chuỗi `collider.tag == "..."` (chỉ trả về false nếu đối tượng chưa gán tag), hàm `Component.CompareTag(string tag)` của Unity kiểm tra tính hợp lệ của chuỗi `tag` trước. Nếu tag truyền vào chưa được định nghĩa trong `TagManager`, Unity sẽ ném ra ngoại lệ nghiêm trọng.
- **Nguyên tắc khắc phục & bắt buộc tuân thủ:**
  1. Chỉ sử dụng `CompareTag` với các tag mặc định của Unity (như `"Player"`) hoặc tag đã chắc chắn được đăng ký trong `TagManager` (như `"Enemy"`).
  2. Khi lọc va chạm với các vật thể như đạn, tên lửa, chướng ngại vật: ưu tiên kiểm tra Component (`GetComponentInParent<Bullet>() != null`, `GetComponentInParent<HomingRocket>() != null`) hoặc kiểm tra tên GameObject (`name.ToLower().Contains(...)`).
  3. Luôn khai báo đầy đủ các tag cần dùng vào `ProjectSettings/TagManager.asset` để tránh tình trạng các Prefab hay script khác truy vấn bị lỗi.

---

### ❌ Lỗi 17: Tự ý can thiệp dịch chuyển cưỡng bức `transform.position` mỗi frame (`ResolveOverlapWithObstacles`) làm Player trượt rơi khỏi Terrain và nhấp nháy màn hình
- **Mô tả chi tiết:**
  - Nhằm mục đích chống kẹt va chạm với công trình, code đã thêm hàm `ResolveOverlapWithObstacles` chạy mỗi frame trong `PlayerController.Update()`. Hàm này quét `Physics.OverlapSphere` và khi chạm vào Collider vật cản (như tảng đá `Rock`), nó tự ý tắt `cc.enabled`, cộng thêm `transform.position += pushDir * 0.12f` rồi bật lại `cc.enabled`.
  - Hậu quả:
    1. Khi vừa bấm Run game, Player đứng gần tảng đá tự nhiên liền bị hàm này liên tục đẩy trôi về phía mép đảo với tốc độ cực nhanh (~7.2m/s) và rơi khỏi Terrain xuống biển.
    2. Khi rơi xuống biển ($Y < 6m$), hàm cứu hộ teleport Player ngược lại đảo -> lại bị đẩy -> lại rơi -> lại cứu hộ... Vòng lặp giật vị trí 60 lần/giây khiến Cinemachine Camera liên tục nhảy tọa độ làm **toàn bộ màn hình game bị nhấp nháy / chớp giật liên tục**.
    3. Việc tắt/bật `CharacterController.enabled` mỗi frame phá hủy bộ tính toán va chạm và bám đất (`isGrounded`) gốc của Unity PhysX.
- **Nguyên nhân gốc rễ:**
  - Can thiệp thô bạo vào hệ thống vật lý CharacterController của Unity bằng phép dịch chuyển `transform.position` cưỡng bức mỗi frame thay vì để `cc.Move()` tự giải quyết va chạm theo thiết kế chuẩn.
  - Nhầm lẫn giữa việc chống đè trùng khi người chơi kéo thả công trình (vốn đã được `WallSelectionManager` xử lý triệt để bằng kiểm tra khoảng cách và rollback) với việc đẩy vật lý runtime.
- **Nguyên tắc khắc phục & bắt buộc tuân thủ:**
  - **TUYỆT ĐỐI KHÔNG** viết code tự ý dịch chuyển `transform.position` của Player mỗi frame khi đứng gần vật cản. CharacterController đã tự động xử lý trượt và cản va chạm với mọi vật thể tĩnh (đá, cây, tường).
  - Không bao giờ tắt/bật `cc.enabled` liên tục trong `Update()` để can thiệp độ cao Y. Hãy để `CharacterController.Move()` tự bám mặt đất tự nhiên.
  - Chỉ can thiệp vị trí bằng script trong 1 trường hợp duy nhất: Khi Player rơi khỏi bản đồ xuống biển ($Y < 4m$), teleport cứu hộ 1 lần duy nhất về trung tâm đảo `(0f, 10.5f, -32f)`.

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