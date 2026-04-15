# Ma trận phân quyền — KLC EV Charging Admin Portal

Ngày tạo: 15/04/2026

Tổng số quyền: 91

## VẬN HÀNH

| Nhóm quyền | Quyền | Mã quyền | admin | KLC Admin | operator | thu ngan | viewer |
|-----------|-------|----------|:-----:|:---------:|:--------:|:-------:|:------:|
| **Trạm sạc** | Xem | `KLC.Stations` | ✅ | ✅ | ✅ | ❌ | ✅ |
| | Tạo trạm sạc | `KLC.Stations.Create` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Cập nhật trạm sạc | `KLC.Stations.Update` | ✅ | ✅ | ✅ | ❌ | ❌ |
| | Xóa trạm sạc | `KLC.Stations.Delete` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Vô hiệu hóa trạm sạc | `KLC.Stations.Decommission` | ✅ | ✅ | ❌ | ❌ | ❌ |
| **Đầu sạc** | Xem | `KLC.Connectors` | ✅ | ✅ | ✅ | ❌ | ✅ |
| | Tạo đầu sạc | `KLC.Connectors.Create` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Cập nhật đầu sạc | `KLC.Connectors.Update` | ✅ | ✅ | ✅ | ❌ | ❌ |
| | Xóa đầu sạc | `KLC.Connectors.Delete` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Bật đầu sạc | `KLC.Connectors.Enable` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Tắt đầu sạc | `KLC.Connectors.Disable` | ✅ | ✅ | ❌ | ❌ | ❌ |
| **Giám sát** | Xem | `KLC.Monitoring` | ✅ | ✅ | ✅ | ❌ | ✅ |
| | Xem bảng điều khiển giám sát | `KLC.Monitoring.Dashboard` | ✅ | ✅ | ✅ | ❌ | ✅ |
| | Xem lịch sử trạng thái | `KLC.Monitoring.StatusHistory` | ✅ | ✅ | ✅ | ❌ | ✅ |
| | Xem tổng kết năng lượng | `KLC.Monitoring.EnergySummary` | ✅ | ✅ | ✅ | ❌ | ✅ |
| **Phiên sạc** | Xem | `KLC.Sessions` | ✅ | ✅ | ✅ | ✅ | ✅ |
| | Xem tất cả phiên sạc | `KLC.Sessions.ViewAll` | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Chia sẻ công suất** | Xem | `KLC.PowerSharing` | ✅ | ✅ | ✅ | ❌ | ✅ |
| | Tạo nhóm chia sẻ công suất | `KLC.PowerSharing.Create` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Cập nhật nhóm chia sẻ công suất | `KLC.PowerSharing.Update` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Xóa nhóm chia sẻ công suất | `KLC.PowerSharing.Delete` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Quản lý thành viên nhóm chia sẻ công suất | `KLC.PowerSharing.ManageMembers` | ✅ | ✅ | ❌ | ❌ | ❌ |

## SỰ CỐ

| Nhóm quyền | Quyền | Mã quyền | admin | KLC Admin | operator | thu ngan | viewer |
|-----------|-------|----------|:-----:|:---------:|:--------:|:-------:|:------:|
| **Lỗi & Cảnh báo** | Xem | `KLC.Faults` | ✅ | ✅ | ✅ | ❌ | ✅ |
| | Cập nhật trạng thái sự cố | `KLC.Faults.Update` | ✅ | ✅ | ✅ | ❌ | ❌ |
| **Cảnh báo** | Xem | `KLC.Alerts` | ✅ | ✅ | ✅ | ❌ | ✅ |
| | Xác nhận cảnh báo | `KLC.Alerts.Acknowledge` | ✅ | ✅ | ✅ | ❌ | ❌ |
| **Bảo trì** | Xem | `KLC.Maintenance` | ✅ | ✅ | ✅ | ❌ | ✅ |
| | Tạo tác vụ bảo trì | `KLC.Maintenance.Create` | ✅ | ✅ | ✅ | ❌ | ❌ |
| | Cập nhật tác vụ bảo trì | `KLC.Maintenance.Update` | ✅ | ✅ | ✅ | ❌ | ❌ |
| | Xóa tác vụ bảo trì | `KLC.Maintenance.Delete` | ✅ | ✅ | ✅ | ❌ | ❌ |
| **Thông báo** | Xem | `KLC.Notifications` | ✅ | ✅ | ✅ | ❌ | ✅ |
| | Gửi thông báo hàng loạt | `KLC.Notifications.Broadcast` | ✅ | ✅ | ❌ | ❌ | ❌ |

## KINH DOANH

| Nhóm quyền | Quyền | Mã quyền | admin | KLC Admin | operator | thu ngan | viewer |
|-----------|-------|----------|:-----:|:---------:|:--------:|:-------:|:------:|
| **Biểu phí** | Xem | `KLC.Tariffs` | ✅ | ✅ | ✅ | ✅ | ✅ |
| | Tạo biểu giá | `KLC.Tariffs.Create` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Cập nhật biểu giá | `KLC.Tariffs.Update` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Kích hoạt biểu giá | `KLC.Tariffs.Activate` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Tắt biểu giá | `KLC.Tariffs.Deactivate` | ✅ | ✅ | ❌ | ❌ | ❌ |
| **Thanh toán** | Xem | `KLC.Payments` | ✅ | ✅ | ✅ | ✅ | ✅ |
| | Xem tất cả thanh toán | `KLC.Payments.ViewAll` | ✅ | ✅ | ✅ | ✅ | ✅ |
| | Xử lý hoàn tiền | `KLC.Payments.Refund` | ✅ | ✅ | ❌ | ✅ | ❌ |
| **Mã giảm giá** | Xem | `KLC.Vouchers` | ✅ | ✅ | ❌ | ✅ | ✅ |
| | Tạo mã giảm giá | `KLC.Vouchers.Create` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Cập nhật mã giảm giá | `KLC.Vouchers.Update` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Xóa mã giảm giá | `KLC.Vouchers.Delete` | ✅ | ✅ | ❌ | ❌ | ❌ |
| **Khuyến mãi** | Xem | `KLC.Promotions` | ✅ | ✅ | ❌ | ✅ | ✅ |
| | Tạo khuyến mãi | `KLC.Promotions.Create` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Cập nhật khuyến mãi | `KLC.Promotions.Update` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Xóa khuyến mãi | `KLC.Promotions.Delete` | ✅ | ✅ | ❌ | ❌ | ❌ |
| **Nhà vận hành** | Xem | `KLC.Operators` | ✅ | ✅ | ✅ | ❌ | ✅ |
| | Tạo nhà vận hành | `KLC.Operators.Create` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Cập nhật nhà vận hành | `KLC.Operators.Update` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Xóa nhà vận hành | `KLC.Operators.Delete` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Quản lý trạm sạc nhà vận hành | `KLC.Operators.ManageStations` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Quản lý webhook nhà vận hành | `KLC.Operators.ManageWebhooks` | ✅ | ✅ | ❌ | ❌ | ❌ |
| **Đội xe** | Xem | `KLC.Fleets` | ✅ | ✅ | ✅ | ❌ | ✅ |
| | Tạo đội xe | `KLC.Fleets.Create` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Cập nhật đội xe | `KLC.Fleets.Update` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Xóa đội xe | `KLC.Fleets.Delete` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Quản lý xe trong đội | `KLC.Fleets.ManageVehicles` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Quản lý lịch sạc đội xe | `KLC.Fleets.ManageSchedules` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Xem phân tích đội xe | `KLC.Fleets.ViewAnalytics` | ✅ | ✅ | ❌ | ❌ | ❌ |

## NGƯỜI DÙNG

| Nhóm quyền | Quyền | Mã quyền | admin | KLC Admin | operator | thu ngan | viewer |
|-----------|-------|----------|:-----:|:---------:|:--------:|:-------:|:------:|
| **Người dùng** | Xem | `KLC.UserManagement` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Tạo người dùng | `KLC.UserManagement.Create` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Cập nhật người dùng | `KLC.UserManagement.Update` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Xóa người dùng | `KLC.UserManagement.Delete` | ✅ | ❌ | ❌ | ❌ | ❌ |
| | Gán vai trò người dùng | `KLC.UserManagement.ManageRoles` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Quản lý quyền người dùng | `KLC.UserManagement.ManagePermissions` | ✅ | ❌ | ❌ | ❌ | ❌ |
| **Vai trò** | Xem | `KLC.RoleManagement` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Tạo vai trò | `KLC.RoleManagement.Create` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Cập nhật vai trò | `KLC.RoleManagement.Update` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Xóa vai trò | `KLC.RoleManagement.Delete` | ✅ | ❌ | ❌ | ❌ | ❌ |
| | Quản lý quyền vai trò | `KLC.RoleManagement.ManagePermissions` | ✅ | ❌ | ❌ | ❌ | ❌ |
| **Người dùng app** | Xem | `KLC.MobileUsers` | ✅ | ✅ | ❌ | ✅ | ✅ |
| | Xem tất cả người dùng app | `KLC.MobileUsers.ViewAll` | ✅ | ✅ | ❌ | ✅ | ✅ |
| | Tạm ngưng người dùng | `KLC.MobileUsers.Suspend` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Điều chỉnh số dư ví | `KLC.MobileUsers.WalletAdjust` | ✅ | ✅ | ❌ | ❌ | ❌ |

## HỆ THỐNG

| Nhóm quyền | Quyền | Mã quyền | admin | KLC Admin | operator | thu ngan | viewer |
|-----------|-------|----------|:-----:|:---------:|:--------:|:-------:|:------:|
| **Nhóm trạm sạc** | Xem | `KLC.StationGroups` | ✅ | ✅ | ✅ | ❌ | ✅ |
| | Tạo nhóm trạm sạc | `KLC.StationGroups.Create` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Cập nhật nhóm trạm sạc | `KLC.StationGroups.Update` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Xóa nhóm trạm sạc | `KLC.StationGroups.Delete` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Gán trạm vào nhóm | `KLC.StationGroups.Assign` | ✅ | ✅ | ❌ | ❌ | ❌ |
| **Nhật ký** | Xem | `KLC.AuditLogs` | ✅ | ✅ | ❌ | ❌ | ✅ |
| | Xuất nhật ký | `KLC.AuditLogs.Export` | ✅ | ✅ | ❌ | ❌ | ❌ |
| **Hóa đơn điện tử** | Xem | `KLC.EInvoices` | ✅ | ✅ | ✅ | ❌ | ✅ |
| | Tạo hóa đơn điện tử | `KLC.EInvoices.Generate` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Thử lại hóa đơn điện tử | `KLC.EInvoices.Retry` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Hủy hóa đơn điện tử | `KLC.EInvoices.Cancel` | ✅ | ✅ | ❌ | ❌ | ❌ |
| **Phản hồi** | Xem | `KLC.Feedback` | ✅ | ✅ | ✅ | ❌ | ✅ |
| | Phản hồi ý kiến | `KLC.Feedback.Respond` | ✅ | ✅ | ❌ | ❌ | ❌ |
| **Cài đặt** | Xem | `KLC.Settings` | ✅ | ✅ | ❌ | ❌ | ❌ |
| | Cập nhật cài đặt | `KLC.Settings.Update` | ✅ | ✅ | ❌ | ❌ | ❌ |

---

### Vai trò

| Vai trò | Mô tả |
|---------|-------|
| **admin** | Quản trị viên hệ thống — toàn quyền |
| **KLC Admin** | Quản lý vận hành KLC — vận hành + kinh doanh, không xóa user/role |
| **operator** | Nhân viên vận hành — xem + cập nhật trạm sạc, xử lý sự cố |
| **thu ngan** | Kế toán/thu ngân — thanh toán, phiên sạc, người dùng app |
| **viewer** | Chỉ xem — giám sát, không có quyền chỉnh sửa |

### Chú thích

- ✅ = Có quyền
- ❌ = Không có quyền
