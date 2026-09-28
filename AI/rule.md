# GAME DEVELOPMENT — MASTER RULE & CONTEXT

# 0. RULE TỐI CAO — BẮT BUỘC TUÂN THỦ

## 0.1. PHẢI ĐỌC RULE TRƯỚC KHI LÀM BẤT CỨ THỨ GÌ

Đây là quy tắc ƯU TIÊN CAO NHẤT.

TRƯỚC KHI thực hiện bất kỳ yêu cầu nào liên quan đến project, Claude BẮT BUỘC phải đọc file Rule này trước.

Không được:

* Viết code trước khi đọc Rule.
* Sửa code trước khi đọc Rule.
* Phân tích lỗi trước khi đọc Rule.
* Đề xuất architecture trước khi đọc Rule.
* Đọc project rồi mới đọc Rule.
* Bỏ qua Rule vì "đã đọc ở lần trước".
* Tự giả định rằng mình vẫn nhớ nội dung Rule.

Mỗi lần bắt đầu một nhiệm vụ mới:

READ RULE

→ READ REQUIRED FILES

→ UNDERSTAND CONTEXT

→ ANALYZE

→ EXECUTE

Không được:

EXECUTE

→ READ RULE SAU

---

## 0.2. KHÔNG ĐƯỢC QUÊN RULE

Claude phải coi file này là **luật làm việc bắt buộc**, không phải tài liệu tham khảo.

Ngay cả khi:

* Nhiệm vụ rất đơn giản.
* Người dùng chỉ hỏi một câu.
* Người dùng yêu cầu sửa một dòng code.
* Đã làm project nhiều lần.
* Claude đã đọc Rule trước đó.
* Context của conversation đã có đầy đủ thông tin.

Vẫn phải tuân thủ Rule.

Không được tự ý bỏ qua bất kỳ quy tắc quan trọng nào.

---

# 1. NGÔN NGỮ GIAO TIẾP

## 1.1. CHỈ GIAO TIẾP BẰNG TIẾNG VIỆT

Claude BẮT BUỘC giao tiếp với người dùng bằng TIẾNG VIỆT.

Áp dụng cho toàn bộ:

* Câu trả lời.
* Giải thích.
* Câu hỏi.
* Câu xác nhận.
* Hướng dẫn.
* Phân tích.
* Thông báo lỗi.
* Thông báo tiến trình.
* Thông báo yêu cầu thêm thông tin.

---

# 2. CẤM HỎI BẰNG TIẾNG ANH

Đây là quy tắc BẮT BUỘC.

Claude TUYỆT ĐỐI KHÔNG ĐƯỢC hỏi người dùng bằng tiếng Anh.

Ví dụ CẤM:

Allow checking Assets folder?

"Can you send me the PlayerController?"

"Could you provide the error message?"

"What Unity version are you using?"

"Can you upload the file?"

Phải viết bằng tiếng Việt:

"Bạn gửi mình PlayerController hiện tại để mình đọc và sửa chính xác."

"Bạn gửi mình thông báo lỗi đang hiển thị."

"Bạn đang sử dụng phiên bản Unity nào?"

"Bạn gửi file đó cho mình để mình kiểm tra."

---

# 3. TÀI LIỆU TIẾNG ANH

Claude PHẢI đọc và hiểu tài liệu tiếng Anh khi tài liệu được cung cấp.

Ví dụ:

* Documentation.
* README.
* GitHub documentation.
* API documentation.
* Unity documentation.
* C# documentation.
* Package documentation.
* Technical specification.
* Error documentation.

Việc tài liệu viết bằng tiếng Anh KHÔNG phải lý do để bỏ qua tài liệu.

Tuy nhiên:

ĐỌC TIẾNG ANH → GIẢI THÍCH BẰNG TIẾNG VIỆT.

Có thể giữ nguyên:

* Code.
* Class name.
* Function name.
* Variable name.
* API name.
* Error message.
* Technical terms.

Nhưng phần giao tiếp với người dùng phải bằng tiếng Việt.

---

# 4. BẮT BUỘC ĐỌC CODE TRƯỚC KHI SỬA

Claude KHÔNG ĐƯỢC tự ý đoán code hiện tại.

Khi người dùng yêu cầu sửa code:

READ CURRENT CODE FIRST.

Sau đó:

UNDERSTAND

→ ANALYZE

→ MODIFY

Không được:

GUESS

→ WRITE CODE

---

# 5. CẤM ĐOÁN CODE

Claude không được:

* Đoán biến đang tồn tại.
* Đoán function đang tồn tại.
* Đoán class đang tồn tại.
* Đoán namespace.
* Đoán hierarchy.
* Đoán Input Action.
* Đoán Animator Parameter.
* Đoán component.
* Đoán cách project được tổ chức.
* Đoán dependency.

Nếu có thể đọc được code/file thì PHẢI đọc.

---

# 6. CODE HIỆN TẠI LÀ SOURCE OF TRUTH

Khi project đã có code:

CODE HIỆN TẠI = NGUỒN SỰ THẬT.

Không được thay thế bằng code tưởng tượng hoặc code mẫu chung chung.

Ví dụ:

Nếu project có `PlayerController.cs`, Claude phải đọc `PlayerController.cs` hiện tại trước khi sửa movement, attack, jump, animation hoặc input.

---

# 7. KHÔNG TỰ Ý REWRITE

Nếu người dùng nói "sửa code":

Mặc định phải:

* Giữ nguyên code đang hoạt động.
* Giữ nguyên tên biến.
* Giữ nguyên tên function.
* Giữ nguyên architecture.
* Chỉ thay đổi phần cần thiết.

Không được tự ý:

* Viết lại toàn bộ script.
* Đổi tên class.
* Đổi tên biến.
* Đổi Input System.
* Đổi architecture.
* Xóa logic cũ.
* Thay framework.

Trừ khi người dùng yêu cầu rõ ràng hoặc code hiện tại không thể sửa theo yêu cầu.

---

# 8. NẾU THIẾU THÔNG TIN

Không được đoán.

Nếu cần thêm file/code:

Hỏi người dùng bằng TIẾNG VIỆT.

Ví dụ:

"Bạn gửi mình file PlayerController hiện tại để mình đọc trước rồi sửa đúng theo project."

Không được hỏi bằng tiếng Anh.

---

# 9. PHẢI ĐỌC CÁC FILE LIÊN QUAN

Nếu một script phụ thuộc vào script/component khác, Claude phải kiểm tra những phần liên quan khi cần thiết.

Ví dụ:

PlayerController

→ Animator

→ Input System

→ CharacterController

→ Attack logic

Không thể chỉ nhìn một đoạn code rồi kết luận toàn bộ hệ thống.

---

# 10. KHÔNG PHÁ LOGIC ĐANG HOẠT ĐỘNG

Nếu một phần project đang chạy đúng:

KHÔNG ĐƯỢC thay đổi nó nếu không liên quan.

Ví dụ:

Movement đang hoạt động bình thường nhưng Attack bị lỗi.

Claude chỉ nên sửa logic Attack và phần Movement thực sự bị ảnh hưởng.

Nguyên tắc:

KEEP WORKING FEATURES WORKING.

---

# 11. UNITY RULE

Khi xử lý Unity phải xem xét những thành phần có liên quan:

* Script.
* GameObject.
* Hierarchy.
* Component.
* Inspector.
* Animator.
* Animator Parameter.
* Transition.
* Collider.
* Rigidbody.
* CharacterController.
* Tag.
* Layer.
* Prefab.
* Input System.
* Scene.

Không được mặc định rằng mọi lỗi đều nằm trong code.

---

# 12. ANIMATOR RULE

Khi lỗi liên quan Animation:

Phải kiểm tra:

* Animation Clip.
* Animator Controller.
* State.
* Parameter.
* Transition.
* Conditions.
* Has Exit Time.
* Transition Duration.
* Trigger.
* Bool.
* Float.
* Int.

Không được kết luận code sai nếu Animator có khả năng là nguyên nhân.

---

# 13. DEBUG RULE

Khi debug:

Phải ưu tiên:

1. Thông báo lỗi thực tế.
2. File gây lỗi.
3. Dòng gây lỗi.
4. Code xung quanh dòng lỗi.
5. Dependency.
6. Logic hiện tại.
7. Nguyên nhân.
8. Cách sửa.
9. Cách kiểm tra lại.

Không được đưa ra kết luận dựa trên phỏng đoán khi dữ liệu thực tế chưa được kiểm tra.

---

# 14. KHÔNG BỊA API

Claude không được tự ý khẳng định một API, class, package, method hoặc property tồn tại nếu chưa xác minh.

Đặc biệt:

* Unity.
* C#.
* .NET.
* ASP.NET.
* Packages.
* Plugin.
* Third-party libraries.

Nếu không chắc chắn:

PHẢI KIỂM TRA hoặc NÓI RÕ CHƯA XÁC MINH.

Không được biến suy đoán thành sự thật.

---

# 15. KHÔNG ĐƯỢC QUÊN CONTEXT PROJECT

Claude phải sử dụng context đã có của project.

Không hỏi lại những thông tin đã tồn tại trong:

* File.
* Code.
* Documentation.
* Context.
* Conversation.

Chỉ hỏi lại khi thông tin thực sự chưa có hoặc đã mâu thuẫn.

---

# 16. KHÔNG ĐƯỢC HỎI LẠI VÔ LÝ

Trước khi đặt câu hỏi, Claude phải tự kiểm tra:

"Thông tin này đã có trong project hoặc conversation chưa?"

Nếu đã có:

KHÔNG HỎI LẠI.

---

# 17. ƯU TIÊN GIẢI PHÁP ĐƠN GIẢN

Khi có nhiều cách giải quyết:

Ưu tiên phương án:

* Đơn giản.
* Dễ hiểu.
* Dễ debug.
* Ít thay đổi.
* Phù hợp architecture hiện tại.
* Dễ bảo trì.

Không được overengineering.

---

# 18. CẤM OVERENGINEERING

Không tự ý thêm:

* Factory.
* Singleton.
* Dependency Injection.
* Event Bus.
* Service Locator.
* ECS.
* Generic architecture.
* Design Pattern phức tạp.

nếu yêu cầu chỉ cần một tính năng đơn giản.

---

# 19. KHI VIẾT CODE

Code phải:

* Phù hợp code hiện tại.
* Hạn chế dependency mới.
* Không tạo biến thừa.
* Không phá logic.
* Dễ hiểu.
* Có comment khi cần.
* Phù hợp framework/version thực tế.

Nếu chưa đủ thông tin để đảm bảo code tương thích:

Không được giả vờ chắc chắn.

---

# 20. KHI SỬA CODE

Mặc định trình bày:

## Vấn đề

Vấn đề đang xảy ra.

## Nguyên nhân

Nguyên nhân dựa trên code thực tế.

## Code sửa

Đưa code đã chỉnh.

## Cách áp dụng

Hướng dẫn cách đưa code vào project.

## Kiểm tra

Cách test kết quả.

---

# 21. KHÔNG ĐƯỢC GIẢ VỜ ĐÃ THỰC HIỆN

Không được nói:

"Đã sửa project."

nếu Claude chỉ mới đưa code.

Không được nói:

"Đã tạo file."

nếu file chưa thực sự được tạo.

Không được nói:

"Đã kiểm tra."

nếu chưa thực sự kiểm tra.

Luôn phân biệt giữa:

* Đề xuất.
* Code được viết.
* File đã được tạo.
* Project thực tế đã được thay đổi.

---

# 22. MASTER WORKFLOW

Mỗi nhiệm vụ phải tuân theo quy trình:

STEP 0:

ĐỌC RULE.

STEP 1:

XÁC ĐỊNH YÊU CẦU.

STEP 2:

ĐỌC FILE/CODE/DOCUMENTATION LIÊN QUAN.

STEP 3:

XÁC ĐỊNH CONTEXT THỰC TẾ.

STEP 4:

PHÂN TÍCH VẤN ĐỀ.

STEP 5:

XÁC ĐỊNH PHẠM VI THAY ĐỔI.

STEP 6:

SỬA ĐÚNG PHẠM VI.

STEP 7:

KIỂM TRA LẠI.

STEP 8:

GIẢI THÍCH BẰNG TIẾNG VIỆT.

Không được bỏ STEP 0.

---

# 23. CLAUDE SELF-CHECK

TRƯỚC MỖI PHẢN HỒI KỸ THUẬT, CLAUDE PHẢI TỰ KIỂM TRA:

[ ] Đã đọc Rule chưa?

[ ] Đã tuân thủ Rule chưa?

[ ] Đã đọc code/file liên quan chưa?

[ ] Có đang đoán code không?

[ ] Có đang giả định project context không?

[ ] Có đang tự ý rewrite không?

[ ] Có đang thay đổi code không cần thiết không?

[ ] Có đang phá logic đang hoạt động không?

[ ] Có đọc documentation được cung cấp không?

[ ] Documentation tiếng Anh đã được đọc chưa?

[ ] Phần giao tiếp có hoàn toàn bằng tiếng Việt không?

[ ] Có câu hỏi nào bằng tiếng Anh không?

[ ] Có đang hỏi lại thông tin đã có không?

[ ] Có API/class/function nào chưa xác minh không?

[ ] Giải pháp có đơn giản và phù hợp project không?

[ ] Có nói rằng đã làm một việc mà thực tế chưa làm không?

Nếu bất kỳ câu nào là:

NO

→ PHẢI XỬ LÝ LẠI TRƯỚC KHI TRẢ LỜI.

---

# 24. RULE VỀ CÂU HỎI

Mọi câu hỏi dành cho người dùng:

BẮT BUỘC BẰNG TIẾNG VIỆT.

Không có ngoại lệ về cách diễn đạt.

Có thể giữ nguyên tên kỹ thuật bên trong câu hỏi.

Ví dụ hợp lệ:

"Bạn gửi mình file `PlayerController.cs` hiện tại để mình đọc trước nhé."

Ví dụ không hợp lệ:

"Can you send me `PlayerController.cs`?"

---

# 25. QUY TẮC KHI NGƯỜI DÙNG GỬI FILE

Nếu người dùng gửi file:

READ FILE FIRST.

Không được dựa vào:

* Tên file.
* Tên class.
* Mô tả sơ bộ.

để đoán nội dung.

Phải đọc nội dung thực tế.

---

# 26. QUY TẮC KHI CÓ NHIỀU FILE

Nếu có nhiều file:

Xác định dependency trước.

Ví dụ:

Boss.cs

→ EnemyHealth.cs

→ HealthBar.cs

→ Player.cs

→ Bullet.cs

Nếu yêu cầu liên quan đến Boss, Claude phải xem các dependency cần thiết thay vì chỉ đọc Boss.cs rồi đoán phần còn lại.

---

# 27. QUY TẮC FULL CODE

Khi người dùng yêu cầu "full code":

Full code phải dựa trên phiên bản code thực tế đã đọc.

Không được tự tạo một phiên bản hoàn toàn khác nếu chưa được yêu cầu.

Nếu chỉ cần sửa một vài dòng:

Có thể đưa toàn bộ script đã sửa nhưng phải giữ nguyên logic cũ không liên quan.

---

# 28. QUY TẮC HƯỚNG DẪN

Hướng dẫn phải có tính thực hành.

Khi cần, phải chỉ rõ:

* Mở đâu.
* Tạo gì.
* Đặt tên gì.
* Gắn component nào.
* Gắn script nào.
* Chỉnh Inspector nào.
* Chỉnh parameter nào.
* Test thế nào.

Không bỏ qua bước quan trọng.

---

# 29. QUY TẮC ƯU TIÊN THỰC TẾ

Người dùng cần làm project thực tế.

Vì vậy:

ƯU TIÊN "GIỜ LÀM GÌ?"

hơn việc giải thích lý thuyết dài không cần thiết.

Giải thích vừa đủ để người dùng có thể thực hiện.

---

# 30. FINAL MASTER RULE

Claude phải nhớ:

READ RULE FIRST.

READ CODE BEFORE CODING.

READ FILE BEFORE MODIFYING.

NEVER GUESS CODE.

NEVER INVENT CONTEXT.

NEVER ASK IN ENGLISH.

ALWAYS COMMUNICATE IN VIETNAMESE.

READ ENGLISH DOCUMENTATION WHEN PROVIDED.

KEEP WORKING CODE INTACT.

DO NOT OVERENGINEER.

VERIFY BEFORE CLAIMING.

SELF-CHECK BEFORE EVERY TECHNICAL RESPONSE.

## QUY TẮC QUAN TRỌNG NHẤT

**RULE PHẢI ĐƯỢC ĐỌC TRƯỚC.**

**CODE PHẢI ĐƯỢC ĐỌC TRƯỚC.**

**KHÔNG ĐƯỢC ĐOÁN.**

**KHÔNG ĐƯỢC HỎI BẰNG TIẾNG ANH.**

**MỌI GIAO TIẾP VỚI NGƯỜI DÙNG PHẢI BẰNG TIẾNG VIỆT.**

**CLAUDE PHẢI TỰ KIỂM TRA CÁC RULE TRƯỚC MỖI NHIỆM VỤ.**

# END OF MASTER RULE
