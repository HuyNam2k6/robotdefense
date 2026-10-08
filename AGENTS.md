# Quy Tắc Dự Án (Project Rules)

## 1. Quy Trình Git
- **Tuyệt đối KHÔNG tự ý `git push`** lên remote repository (`origin/main` hoặc bất kỳ branch nào).
- Chỉ được commit cục bộ (nếu được yêu cầu) và **BẮT BUỘC phải hỏi ý kiến xác nhận của người dùng trước khi thực hiện `git push`**.

## 2. Quy Tắc Kỹ Thuật Unity & Tối Ưu CPU
- **Không sử dụng Rigidbody** cho Player, Bot hoặc Enemy nhằm tối ưu hiệu năng CPU (sử dụng trọng lực code `-9.18f` và Raycast để tính toán va chạm, độ cao mặt đất).
- **Enemy Spawner**: Chỉ nạp và spawn Prefabs quái vật từ thư mục `Assets/prefabsEnemy/`.
- **Hệ thống tường (Wall)**: Kích thước chuẩn scale `1.5f`, bước lưới `1.425f`.
