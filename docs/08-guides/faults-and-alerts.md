# Hướng dẫn: Lỗi & Cảnh báo (Faults & Alerts)

## Tổng quan

Hệ thống có **2 loại sự cố** riêng biệt:

| | **Lỗi (Faults)** | **Cảnh báo (Alerts)** |
|---|---|---|
| **Nguồn** | Tự động từ trụ sạc qua OCPP | Tự động từ hệ thống giám sát |
| **Ví dụ** | Lỗi kết nối đầu sạc, quá nhiệt, lỗi đo điện | Trụ mất kết nối, thanh toán thất bại |
| **Xử lý** | Điều tra → Khắc phục → Đóng | Xác nhận → Giải quyết |
| **Trang** | Admin Portal → Lỗi & Cảnh báo | Admin Portal → Cảnh báo |

---

## 1. Lỗi (Faults)

### 1.1 Lỗi được tạo khi nào?

Khi trụ sạc gửi mã lỗi qua OCPP (StatusNotification). Hệ thống tự động phân loại mức ưu tiên:

| Mức ưu tiên | Mã lỗi OCPP | Ý nghĩa |
|-------------|-------------|---------|
| **Nghiêm trọng** (đỏ) | GroundFailure | Lỗi tiếp đất |
| | HighTemperature | Quá nhiệt |
| | OverCurrentFailure | Quá dòng |
| | OverVoltage / UnderVoltage | Quá áp / thiếu áp |
| | PowerMeterFailure | Lỗi đồng hồ đo |
| **Cao** (cam) | ConnectorLockFailure | Lỗi khóa đầu sạc |
| | EVCommunicationError | Mất giao tiếp với xe |
| | ReaderFailure | Lỗi đầu đọc thẻ |
| | InternalError | Lỗi nội bộ trụ sạc |
| **Trung bình** (vàng) | Các mã khác | Lỗi khác |

**Tự động xử lý**: Khi trụ sạc báo "NoError" hoặc đầu sạc trở lại "Available", lỗi tự động được đóng.

**Chống trùng lặp**: Chỉ tạo 1 lỗi cho mỗi trạm + đầu sạc + mã lỗi nếu chưa có lỗi Mở/Đang điều tra.

### 1.2 Quy trình xử lý lỗi

```
Mở (Open) → Đang điều tra (Investigating) → Đã khắc phục (Resolved) → Đã đóng (Closed)
```

| Bước | Hành động | Ai thực hiện |
|------|----------|-------------|
| 1 | Lỗi xuất hiện tự động | Hệ thống |
| 2 | Nhấn **"Điều tra"** | Operator / KLC Admin |
| 3 | Kiểm tra trụ sạc tại hiện trường | Nhân viên kỹ thuật |
| 4 | Nhấn **"Đánh dấu đã khắc phục"** + ghi chú | Operator / KLC Admin |
| 5 | Nhấn **"Đóng lỗi"** (hoặc tự đóng khi trụ báo NoError) | KLC Admin / Hệ thống |

**Nếu là báo nhầm**: Nhấn "Đóng lỗi" trực tiếp từ trạng thái Mở.

### 1.3 Màn hình danh sách lỗi

- **Thống kê**: Tổng lỗi mở, đang điều tra, nghiêm trọng
- **Bộ lọc**: Tìm theo tên trạm / mã lỗi, lọc theo trạng thái
- **Viền màu bên trái**: Đỏ = nghiêm trọng, cam = cao, vàng = trung bình
- **Nhấn vào lỗi**: Xem chi tiết + lịch sử xử lý

### 1.4 Màn hình chi tiết lỗi

- Mã lỗi OCPP + mô tả
- Trạm sạc + đầu sạc bị lỗi (link đến trang trạm)
- Thời gian phát hiện / khắc phục
- Ghi chú khắc phục
- Nút hành động tùy theo trạng thái

---

## 2. Cảnh báo (Alerts)

### 2.1 Các loại cảnh báo

| Mức | Loại | Khi nào xảy ra |
|-----|------|----------------|
| **Nghiêm trọng** (đỏ) | Trụ mất kết nối (StationOffline) | Không nhận heartbeat > 6 phút |
| | Lỗi đầu sạc (ConnectorFault) | Đầu sạc chuyển trạng thái Faulted |
| | Thanh toán thất bại (PaymentFailure) | Giao dịch thanh toán lỗi |
| | Heartbeat timeout | Trụ không phản hồi |
| **Cảnh báo** (vàng) | Sử dụng thấp (LowUtilization) | Trụ ít được sử dụng |
| | Hóa đơn lỗi (EInvoiceFailure) | Tạo hóa đơn điện tử thất bại |
| **Thông tin** (xanh) | Cập nhật firmware (FirmwareUpdate) | Có firmware mới |
| | Sử dụng cao (HighUtilization) | Trụ được sử dụng nhiều |

### 2.2 Quy trình xử lý cảnh báo

```
Mới (New) → Đã xác nhận (Acknowledged) → Đã giải quyết (Resolved)
```

| Bước | Hành động | Mô tả |
|------|----------|-------|
| 1 | Cảnh báo xuất hiện (real-time qua SignalR) | Tự động |
| 2 | Nhấn ✓ để **xác nhận** | "Tôi đã thấy cảnh báo này" |
| 3 | Xử lý vấn đề | Tùy loại cảnh báo |
| 4 | Cảnh báo tự giải quyết hoặc admin đóng | Hệ thống / Admin |

### 2.3 Màn hình cảnh báo

- **Real-time**: Chỉ báo "Live" khi kết nối SignalR
- **Thống kê**: Nhấn vào số liệu để lọc nhanh
  - Nghiêm trọng (đỏ), Cảnh báo (vàng), Thông tin (xanh), Chưa xác nhận
- **Bộ lọc**: Theo mức độ + trạng thái
- **Nút 👁**: Xem chi tiết trong popup
- **Nút ✓**: Xác nhận cảnh báo

---

## 3. Phân quyền

| Quyền | admin | KLC Admin | operator | thu ngan | viewer |
|-------|:-----:|:---------:|:--------:|:-------:|:------:|
| Xem lỗi | ✅ | ✅ | ✅ | ❌ | ✅ |
| Cập nhật trạng thái lỗi | ✅ | ✅ | ✅ | ❌ | ❌ |
| Xem cảnh báo | ✅ | ✅ | ✅ | ❌ | ✅ |
| Xác nhận cảnh báo | ✅ | ✅ | ✅ | ❌ | ❌ |

---

## 4. Webhook (cho đối tác vận hành)

Hệ thống gửi webhook khi:

| Event | Khi nào |
|-------|---------|
| `FaultDetected` | Đầu sạc chuyển sang trạng thái Faulted |
| `StationOffline` | Trụ sạc mất kết nối (heartbeat timeout > 6 phút) |
| `ConnectorStatusChanged` | Bất kỳ thay đổi trạng thái đầu sạc |

---

## 5. Xử lý nhanh

### Khi có lỗi nghiêm trọng (đỏ)

1. Vào **Lỗi & Cảnh báo**
2. Nhấn vào lỗi → **Điều tra**
3. Cử kỹ thuật viên kiểm tra trụ sạc
4. Sau khi sửa → **Đánh dấu đã khắc phục** + ghi chú nguyên nhân
5. Trụ sạc tự báo "NoError" → lỗi tự đóng

### Khi trụ sạc mất kết nối

1. Cảnh báo "HeartbeatTimeout" xuất hiện real-time
2. **Xác nhận** cảnh báo
3. Kiểm tra: kết nối mạng, nguồn điện, trạng thái vật lý trụ sạc
4. Khi trụ kết nối lại → cảnh báo tự giải quyết

### Khi thanh toán thất bại

1. Cảnh báo "PaymentFailure" xuất hiện
2. **Xác nhận** cảnh báo
3. Kiểm tra trang **Thanh toán** → tìm giao dịch lỗi
4. Nhấn **Query** (VnPay) để kiểm tra trạng thái gateway
5. Hoàn tiền nếu cần

---

## 6. API Endpoints

| Method | Path | Mô tả | Quyền |
|--------|------|-------|-------|
| GET | `/api/v1/faults` | Danh sách lỗi | KLC.Faults |
| GET | `/api/v1/faults/{id}` | Chi tiết lỗi | KLC.Faults |
| PUT | `/api/v1/faults/{id}/status` | Cập nhật trạng thái | KLC.Faults.Update |
| GET | `/api/v1/stations/{stationId}/faults` | Lỗi theo trạm | KLC.Faults |
| GET | `/api/v1/alerts` | Danh sách cảnh báo | KLC.Alerts |
| POST | `/api/v1/alerts/{id}/acknowledge` | Xác nhận cảnh báo | KLC.Alerts.Acknowledge |
