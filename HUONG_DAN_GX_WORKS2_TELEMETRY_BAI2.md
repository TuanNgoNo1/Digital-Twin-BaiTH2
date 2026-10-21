# Bài thực hành 2 – Hướng dẫn GX Works2 và telemetry

## 1. Thiết bị và thanh ghi điều khiển

| Thiết bị | Chức năng | Cách dùng |
|---|---|---|
| `M1` | Phát lệnh chạy motor | Xung ON rồi OFF ngay |
| `M2` | Chọn chiều thuận | Xung ON rồi OFF |
| `M8` | Chọn chiều ngược | Xung ON rồi OFF |
| `M4` | Chế độ theo số vòng | Xung ON rồi OFF |
| `M5` | Chế độ theo góc | Xung ON rồi OFF |
| `M3` | Cho phép đếm encoder | Giữ ON khi thực hành |
| `M12` | Reset encoder | Xung ON rồi OFF |
| `M13` | Reset lệnh/dữ liệu cũ | Thực hiện trước khi cài đặt |
| `M17` | Dừng khẩn cấp | Xung ON rồi OFF |
| `D146` | Tốc độ đặt (RPM) | Nhập giá trị tốc độ |
| `D112` | Số vòng đặt | Dùng với `M4` |
| `D114` | Góc đặt (độ) | Dùng với `M5` |
| `D100–D101` | Tần số phát xung (Hz) | Ladder tự tính, chỉ kiểm tra |
| `D110–D111` | Tổng số xung cần phát | Ladder tự tính, chỉ kiểm tra |
| `Y000` | Ngõ phát xung | Hoạt động khi motor quay |
| `Y001` | Ngõ chọn chiều | Theo logic hiện tại: `1` thuận, `0` ngược |

Kiểm tra các bit hệ thống như `M8000`, `M8013`, `M8122`, `M8161`, `M8029`; không tự ý Force nếu chương trình không yêu cầu.

## 2. Công thức tính

Quy ước driver: **PPR = 5000 xung/vòng**.

### Tốc độ → tần số xung

\[
f = \frac{RPM \times 5000}{60}
\]

`f` là tần số xung (Hz), `RPM` là giá trị trong `D146`.

| RPM | Tần số xấp xỉ |
|---:|---:|
| 5 | 417 Hz |
| 10 | 833 Hz |
| 20 | 1667 Hz |

### Số vòng → số xung

\[
N = R \times 5000
\]

`R` là số vòng trong `D112`. Ví dụ: 1 vòng = 5000 xung.

### Góc → số xung

\[
N = \frac{\theta}{360} \times 5000
\]

`θ` là góc trong `D114`. Ví dụ: 180° = 2500 xung; 90° = 1250 xung.

### Các công thức quy đổi telemetry

\[
RPM = \frac{f \times 60}{5000},\qquad
R = \frac{encoderCount}{5000}
\]

Góc rotor trong một vòng:

\[
\theta = (encoderCount \bmod 5000) \times \frac{360}{5000}
\]

## 3. Trình tự chạy mẫu

### Test theo số vòng: thuận 1 vòng ở 10 RPM

1. Xung `M13`, xung `M12`.
2. Giữ `M3 = ON`.
3. Ghi `D146 = 10`, `D112 = 1`.
4. Xung `M2` (thuận), xung `M4` (chế độ số vòng).
5. Kiểm tra `D100 ≈ 833 Hz`, `D110–D111 = 5000`.
6. Xung `M1` để chạy; motor tự dừng sau 1 vòng.

### Test theo góc: ngược 180° ở 5 RPM

1. Ghi `D146 = 5`, `D114 = 180`.
2. Xung `M8` (ngược), xung `M5` (chế độ góc).
3. Kiểm tra `D100 ≈ 417 Hz`, `D110–D111 = 2500`.
4. Xung `M1`; motor tự dừng sau 180°.

Nếu nhập cả `D112` và `D114`, chương trình không cộng hai giá trị: `M4` dùng `D112`, còn `M5` dùng `D114`.

## 4. Diễn giải telemetry Bài 2

| Trường | Ý nghĩa |
|---|---|
| `lessonId` | Bài thực hành, ví dụ `TH2` |
| `running` | Motor đang chạy hay dừng |
| `speedRpm` | Tốc độ thực tế |
| `setSpeedRpm` | Tốc độ đã cài đặt |
| `pulseFrequency` | Tần số xung hiện tại |
| `encoderCount` | Tổng xung encoder |
| `rotationsExact` | Số vòng quy đổi từ encoder |
| `angle` | Góc rotor trong vòng hiện tại |
| `direction` | Chiều gần nhất: `forward` hoặc `reverse` |
| `backendSynced` | Gateway đang đồng bộ hay không |
| `backendStatus` | Trạng thái kênh, ví dụ `COM5 SYNCED` |
| `motionMode` | Nhãn nguồn dữ liệu `telemetry`, không phải M4/M5 |

Ví dụ mẫu: `running=false`, `speedRpm=0`, `setSpeedRpm=5` nghĩa là motor đang dừng nhưng tốc độ đặt gần nhất là 5 RPM; `encoderCount=60006` tương đương `12.0012` vòng; `backendStatus=COM5 SYNCED` xác nhận telemetry RS485 đang hoạt động.

