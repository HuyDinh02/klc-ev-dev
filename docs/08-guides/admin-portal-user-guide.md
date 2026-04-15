# Hướng dẫn sử dụng Admin Portal — K-Charge

**URL**: https://ev.odcall.com  
**Tài khoản mặc định**: admin / Admin@123

---

## 1. Trạm sạc (Stations)

**Mục đích**: Quản lý thông tin trạm sạc, đầu sạc, ảnh và tiện ích.

**Thao tác chính**:
- **Thêm trạm**: Nhập tên, địa chỉ, tọa độ GPS, mã trạm (station code)
- **Sửa/Xóa**: Cập nhật thông tin, vô hiệu hóa trạm
- **Bật/Tắt**: Enable/Disable đầu sạc riêng lẻ
- **Ảnh**: Upload ảnh trạm, đặt ảnh chính
- **Tiện ích**: Thêm tiện ích xung quanh (WC, cafe, wifi...)

**Trạng thái trạm**: Online (xanh), Offline (xám), Disabled (đỏ)  
**Trạng thái đầu sạc**: Available, Charging, Faulted, Unavailable

---

## 2. Giám sát (Monitoring)

**Mục đích**: Dashboard real-time hiển thị trạng thái toàn hệ thống.

**Dữ liệu hiển thị**:
- Số trạm online/offline
- Số đầu sạc: available / đang sạc / lỗi
- Số phiên sạc đang hoạt động
- Năng lượng + doanh thu hôm nay
- Lưới trạm sạc (12 trạm đầu) với trạng thái real-time
- Cảnh báo mới nhất (10 cảnh báo)

**Real-time**: Cập nhật tự động qua SignalR — không cần refresh.

---

## 3. Phiên sạc (Sessions)

**Mục đích**: Xem lịch sử và phiên sạc đang hoạt động.

**Thao tác**:
- **Tìm kiếm**: Theo tên trạm hoặc tên người dùng
- **Lọc**: Tất cả / Đang sạc / Đã hoàn thành
- **Xuất CSV**: Export danh sách phiên sạc
- **Chi tiết**: Nhấn vào phiên → xem biểu đồ meter values (công suất, năng lượng, SoC)

**Thống kê**: Phiên đang sạc, tổng năng lượng (kWh), doanh thu, tổng phiên

**Thời lượng**: Hiển thị dạng HH:mm:ss. Phiên đang sạc có đồng hồ đếm real-time.

---

## 4. Thanh toán (Payments)

**Mục đích**: Quản lý giao dịch thanh toán phiên sạc và nạp ví.

**2 tab**:

### Tab Thanh toán phiên sạc
- Danh sách giao dịch: mã, trạm, user, phiên, số tiền, gateway, trạng thái
- **Hoàn tiền**: Chọn giao dịch đã hoàn thành → nhấn Hoàn tiền
- Thống kê: doanh thu hôm nay, tháng, đang chờ, thất bại

### Tab Giao dịch ví
- Danh sách: mã tham chiếu, user, loại (Nạp ví/Thanh toán/Hoàn tiền/Điều chỉnh), số tiền, gateway, trạng thái
- **Query VnPay**: Cho giao dịch VnPay đang Pending → kiểm tra trạng thái từ gateway
- Xuất CSV

---

## 5. Biểu phí (Tariffs)

**Mục đích**: Cấu hình giá điện cho phiên sạc.

**Thao tác**:
- **Thêm biểu phí**: Tên, mô tả, giá cơ bản/kWh, thuế suất (%)
- **Kích hoạt/Tắt**: Bật hoặc tắt biểu phí
- **Đặt mặc định**: Biểu phí áp dụng cho trạm chưa gán biểu phí riêng
- **Xóa**: Chỉ xóa được biểu phí không hoạt động

**Thống kê**: Tổng biểu phí, đang hoạt động, giá TB/kWh, gói mặc định

**Gán cho trạm**: Vào Trạm sạc → Sửa → chọn biểu phí. Trạm không gán sẽ dùng biểu phí mặc định.

---

## 6. Lỗi & Cảnh báo (Faults & Alerts)

Xem chi tiết tại: [faults-and-alerts.md](faults-and-alerts.md)

---

## 7. Bảo trì (Maintenance)

**Mục đích**: Lên lịch và theo dõi công việc bảo trì trạm sạc.

**Loại tác vụ**: Bảo trì định kỳ, Kiểm tra, Khẩn cấp

**Quy trình**:
```
Đã lên kế hoạch → Đang thực hiện → Hoàn thành
                                  → Đã hủy
```

**Thao tác**:
- **Tạo**: Tiêu đề, mô tả, trạm, loại, người thực hiện, ngày dự kiến
- **Bắt đầu**: Chuyển từ Kế hoạch → Đang thực hiện
- **Hoàn thành**: Đánh dấu hoàn thành + ghi chú
- **Hủy**: Hủy tác vụ chưa hoàn thành

**Thống kê**: Đã lên kế hoạch, đang thực hiện, hoàn thành, quá hạn

---

## 8. Người dùng app (Mobile Users)

**Mục đích**: Quản lý tài khoản người dùng ứng dụng di động.

**Danh sách**: Tên, SĐT, email, số dư ví, hạng thành viên, trạng thái, lần đăng nhập cuối

**Chi tiết (3 tab)**:
- **Hồ sơ**: Thông tin cá nhân, hạng thành viên, tổng phiên sạc + chi tiêu
- **Ví**: Số dư, giao dịch gần đây, nút **Điều chỉnh số dư** (nhập số tiền + lý do)
- **Phiên sạc**: Lịch sử phiên sạc của người dùng

**Thao tác**:
- **Tạm ngưng**: Khóa tài khoản vi phạm
- **Mở khóa**: Bỏ tạm ngưng
- **Điều chỉnh ví**: Cộng/trừ tiền thủ công (ví dụ: hoàn tiền sạc nhầm)

---

## 9. Quản lý người dùng (User Management)

**Mục đích**: Quản lý tài khoản quản trị và phân quyền.

### Tab Người dùng
- **Tạo**: Tên đăng nhập, email, mật khẩu, họ tên, SĐT, gán vai trò
- **Sửa**: Cập nhật thông tin (không đổi mật khẩu ở đây)
- **Đặt lại mật khẩu**: Tạo mật khẩu mới cho người dùng
- **Khóa/Mở khóa**: Tạm khóa tài khoản

### Tab Vai trò
- **Tạo vai trò**: Tên, mô tả
- **Phân quyền**: Dialog phân quyền theo nhóm (Vận hành, Sự cố, Kinh doanh, Người dùng, Hệ thống)
- Toggle nhóm: Bật/tắt toàn bộ quyền trong nhóm
- Toggle đơn lẻ: Bật/tắt từng quyền

Xem ma trận phân quyền đầy đủ: [permission-matrix.md](../06-project-management/permission-matrix.md)

---

## 10. Mã giảm giá (Vouchers)

**Mục đích**: Tạo và quản lý mã giảm giá cho người dùng.

**Loại voucher**: Giảm cố định (VNĐ), Giảm % , Sạc miễn phí

**Thao tác**:
- **Tạo**: Mã (tự nhập hoặc tạo tự động), loại, giá trị, ngày hết hạn, số lượng, đơn tối thiểu, giảm tối đa
- **Sửa/Xóa**: Cập nhật hoặc xóa voucher
- **Xem sử dụng**: Danh sách người dùng đã sử dụng voucher

**Lọc**: Tất cả / Đang hoạt động / Không hoạt động

---

## 11. Khuyến mãi (Promotions)

**Mục đích**: Tạo chương trình khuyến mãi hiển thị trên app.

**Thao tác**:
- **Tạo**: Tiêu đề, mô tả, hình ảnh (kéo thả hoặc chọn file, max 5MB), ngày bắt đầu/kết thúc, loại
- **Sửa/Xóa**: Cập nhật nội dung
- **Bật/Tắt**: Kích hoạt/tắt khuyến mãi

**Giao diện**: Hiển thị dạng card với ảnh preview, badge loại + trạng thái.

---

## 12. Hóa đơn điện tử (E-Invoices)

**Mục đích**: Quản lý hóa đơn điện tử theo quy định.

**Trạng thái**: Nháp → Đang xử lý → Đã phát hành / Thất bại / Đã hủy

**Thao tác**:
- **Tạo**: Tạo hóa đơn từ giao dịch
- **Thử lại**: Gửi lại hóa đơn thất bại
- **Hủy**: Hủy hóa đơn đã phát hành

**Bộ lọc**: Nhà cung cấp, trạng thái, khoảng thời gian  
**Thống kê**: Tổng, đã phát hành, thất bại, đã hủy

---

## 13. Nhật ký (Audit Logs)

**Mục đích**: Theo dõi mọi thao tác trên hệ thống để kiểm toán.

**Dữ liệu**: Người dùng, phương thức HTTP, URL, status code, thời gian xử lý (ms), IP, thông tin lỗi

**Thay đổi entity**: Loại entity, loại thay đổi (Tạo/Sửa/Xóa), ID, giá trị cũ → mới

**Bộ lọc**: Tìm theo URL, phương thức HTTP, người dùng, có lỗi hay không, khoảng thời gian

**Xuất CSV**: Export nhật ký

---

## 14. Nhóm trạm sạc (Station Groups)

**Mục đích**: Phân nhóm trạm sạc theo khu vực, vận hành, hoặc kinh doanh.

**Loại nhóm**: Địa lý, Vận hành, Kinh doanh, Tùy chỉnh

**Thao tác**:
- **Tạo nhóm**: Tên, mô tả, khu vực, loại nhóm, nhóm cha (hỗ trợ phân cấp)
- **Gán trạm**: Thêm trạm vào nhóm (1 trạm chỉ thuộc 1 nhóm)
- **Xem thống kê**: Số trạm, đầu sạc, tổng công suất (kW)

---

## 15. Thông báo (Notifications)

**Mục đích**: Gửi thông báo hàng loạt đến người dùng app.

**Thao tác**:
- **Gửi broadcast**: Chọn loại thông báo, nhập tiêu đề + nội dung → Gửi
- **Xem lịch sử**: Danh sách thông báo đã gửi với số người nhận

**Loại thông báo**: Sạc bắt đầu/hoàn thành/lỗi, thanh toán, hóa đơn, nạp ví, khuyến mãi, hệ thống

---

## 16. Chia sẻ công suất (Power Sharing)

**Mục đích**: Phân bổ công suất giữa các đầu sạc để không vượt quá giới hạn điện.

**Thao tác**:
- **Tạo nhóm**: Tên, công suất tối đa (kW), chế độ (Link/Loop), chiến lược phân bổ
- **Thêm thành viên**: Chọn đầu sạc, đặt mức ưu tiên + công suất phân bổ
- **Kích hoạt/Tắt**: Bật/tắt nhóm chia sẻ

**Chiến lược**: Chia đều (Average), Theo tỷ lệ (Proportional), Động (Dynamic)

---

## 17. Nhà vận hành (Operators)

**Mục đích**: Quản lý đối tác vận hành bên thứ 3 với API access.

**Thao tác**:
- **Tạo**: Tên, email, mô tả
- **API Key**: Xem prefix, copy, tạo mới (cần xác nhận)
- **Gán trạm**: Thêm/bỏ trạm cho nhà vận hành
- **Webhook**: Cấu hình URL callback
- **Rate limit**: Giới hạn request/phút

---

## 18. Đội xe (Fleets)

**Mục đích**: Quản lý đội xe doanh nghiệp với chính sách sạc và ngân sách.

**Chính sách sạc**: Tự do, Theo lịch, Cần phê duyệt, Giới hạn hàng ngày

**Chi tiết (3 tab)**:
- **Xe**: Biển số, tài xế, giới hạn kWh/ngày, năng lượng đã dùng
- **Lịch sạc**: Khung giờ sạc theo từng ngày trong tuần
- **Trạm cho phép**: Nhóm trạm sạc xe được phép sạc

**Ngân sách**: Ngân sách tháng tối đa (VNĐ), đã chi trong tháng, ngưỡng cảnh báo (%)

---

## 19. Cài đặt (Settings)

### Tab Tổng quát
- Tên hệ thống
- Ngôn ngữ (Tiếng Việt / English)
- Thông tin cố định: Múi giờ UTC+7, Đơn vị VNĐ

### Tab Bảo mật
- Thời gian hết phiên (phút)
- Độ dài mật khẩu tối thiểu

---

## Phím tắt & Mẹo

- **Ctrl+Shift+R**: Hard refresh (xóa cache trang)
- **Tìm kiếm**: Có trên hầu hết các trang (trạm, phiên, thanh toán, người dùng...)
- **Xuất CSV**: Có trên Phiên sạc, Thanh toán, Nhật ký
- **Real-time**: Giám sát, Phiên sạc, Cảnh báo cập nhật tự động qua SignalR
