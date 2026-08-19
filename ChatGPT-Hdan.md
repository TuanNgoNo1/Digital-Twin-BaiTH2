Có thể làm bài test đơn giản nhất theo mô hình:

GX Works2 ──cổng DB9──> PLC: ghi giá trị K1234 vào D100
                                  │
                                  └── FX3U-485-BD ── FTDI ──> Python đọc D100

Tôi đề xuất dùng Modbus RTU, vì máy tính có thể đọc D100 trực tiếp bằng thư viện Python phổ biến.

1. Chương trình ladder trong GX Works2
Network 1: cấu hình PLC thành Modbus RTU Slave

Phần cấu hình này phải nằm ở đầu chương trình và các lệnh MOV phải nằm liên tiếp sau tiếp điểm M8411.

|----[ M8411 ]----------------[ MOV H1081 D8400 ]----|
|                              [ MOV H11   D8401 ]    |
|                              [ MOV K1    D8414 ]    |

Ý nghĩa:

D8400 = H1081: RS-485, 9600 bit/s, 8 bit dữ liệu, Even parity, 1 stop bit.
D8401 = H11: Modbus RTU Slave trên channel 1.
D8414 = K1: địa chỉ Modbus Slave bằng 1.

Mitsubishi yêu cầu cấu hình Modbus channel 1 bằng M8411; các tham số phải được ghi bằng lệnh MOV với hằng số và chỉ có hiệu lực sau khi tắt/bật lại nguồn PLC.

Network 2: ghi dữ liệu kiểm tra vào D100

Có hai cách.

Cách A: D100 luôn bằng 1234
|----[ M8000 ]----------------[ MOV K1234 D100 ]------|
M8000 luôn ON khi PLC ở RUN.
Mỗi vòng quét, PLC ghi 1234 vào D100.

Đây là cách dễ test nhất.

Cách B: chỉ ghi một lần khi PLC khởi động
|----[ M8002 ]----------------[ MOV K1234 D100 ]------|
M8002 chỉ ON trong vòng quét đầu tiên.
Sau đó có thể dùng GX Works2 thay đổi trực tiếp giá trị D100 để quan sát máy tính cập nhật.
Chương trình tối thiểu hoàn chỉnh
Network 0: cấu hình Modbus RTU Slave

M8411
    MOV H1081 D8400
    MOV H11   D8401
    MOV K1    D8414


Network 1: dữ liệu test

M8002
    MOV K1234 D100

Không cần dùng lệnh RS hoặc ADPRW vì PLC đang hoạt động ở vai trò Modbus Slave; máy tính là Master và chủ động đọc thanh ghi.

2. Nạp chương trình qua DB9

Trong GX Works2:

Kết nối cáp lập trình vào cổng DB9.
Chọn Online → Write to PLC.
Ghi chương trình và parameter.
Tắt nguồn PLC.
Chờ vài giây rồi bật lại.
Chuyển PLC sang RUN.

Việc khởi động lại là bắt buộc sau khi thay đổi tham số Modbus.

3. Kiểm tra D100 trong GX Works2

Mở:

Online
→ Monitor
→ Device/Buffer Memory Batch

Nhập:

D100

Kết quả phải là:

1234

Sau đó có thể dùng Device Test để ghi giá trị khác, chẳng hạn:

5678

Nếu dùng chương trình M8000 → MOV K1234 D100, giá trị do GX Works2 ghi sẽ lập tức bị chương trình ghi đè về 1234. Vì vậy, để thay đổi D100 thủ công, nên dùng M8002 thay vì M8000.

4. Địa chỉ Modbus của D100

Với ánh xạ mặc định của FX3U:

D0    ↔ Holding Register address 0
D1    ↔ Holding Register address 1
...
D100  ↔ Holding Register address 100

Tài liệu Mitsubishi ánh xạ D0–D7999 vào vùng Modbus Holding Register 0x0000–0x1F3F, vì vậy D100 tương ứng địa chỉ Modbus 100, tức 0x0064.

Lưu ý: Trong một số phần mềm Modbus, địa chỉ có thể được hiển thị dưới dạng:

40101

nhưng khi lập trình bằng thư viện Python, thường dùng địa chỉ zero-based:

address = 100
5. Đấu dây RS-485

Với FTDI ở chế độ RS-485 hai dây:

FX3U-485-BD                FTDI USB–RS485

SDA ──┐
      ├────────────────── T/R+
RDA ──┘

SDB ──┐
      ├────────────────── T/R-
RDB ──┘

SG  ───────────────────── GND

Nếu không nhận được dữ liệu, thử đảo:

T/R+ ↔ T/R-

vì một số bộ chuyển đổi ghi ký hiệu A/B hoặc +/- không thống nhất.

6. Cài Python

Mở Command Prompt:

pip install pymodbus pyserial
7. Chương trình Python đọc và hiển thị D100

Tạo file:

read_d100.py

Nội dung:

import time

from pymodbus.client import ModbusSerialClient


PORT = "COM5"       # Thay bằng cổng COM của FTDI
SLAVE_ID = 1
D100_ADDRESS = 100


def main() -> None:
    client = ModbusSerialClient(
        port=PORT,
        baudrate=9600,
        bytesize=8,
        parity="E",
        stopbits=1,
        timeout=1,
    )

    if not client.connect():
        print(f"Không mở được {PORT}.")
        print("Kiểm tra Device Manager và bảo đảm GX Works2 không chiếm cổng này.")
        return

    print(f"Đã mở {PORT}. Đang đọc D100...")
    print("Nhấn Ctrl+C để dừng.\n")

    try:
        while True:
            result = client.read_holding_registers(
                address=D100_ADDRESS,
                count=1,
                device_id=SLAVE_ID,
            )

            if result.isError():
                print(f"Lỗi Modbus: {result}")
            else:
                value = result.registers[0]

                # Chuyển thành số nguyên có dấu 16 bit khi cần
                signed_value = value if value < 32768 else value - 65536

                print(
                    f"D100 = {signed_value:6d} "
                    f"| unsigned = {value:5d} "
                    f"| hex = 0x{value:04X}"
                )

            time.sleep(0.5)

    except KeyboardInterrupt:
        print("\nĐã dừng đọc dữ liệu.")

    finally:
        client.close()


if __name__ == "__main__":
    main()

Với một số phiên bản pymodbus cũ hơn, tham số cuối có thể phải viết là:

slave=SLAVE_ID

thay vì:

device_id=SLAVE_ID
8. Chạy chương trình

Trong Command Prompt:

python read_d100.py

Kết quả mong đợi:

Đã mở COM5. Đang đọc D100...

D100 =   1234 | unsigned =  1234 | hex = 0x04D2
D100 =   1234 | unsigned =  1234 | hex = 0x04D2
D100 =   1234 | unsigned =  1234 | hex = 0x04D2

Sau đó trong GX Works2, dùng Device Test thay đổi D100 thành 5678. Python phải hiển thị:

D100 =   5678 | unsigned =  5678 | hex = 0x162E
9. Cách xác định lỗi
RD và SD đều nhấp nháy
RD: nhấp nháy
SD: nhấp nháy

Kết nối đang hoạt động bình thường. Mitsubishi cho biết khi Modbus gửi và nhận bình thường, cả hai đèn RD và SD đều nhấp nháy.

RD nhấp nháy nhưng SD không sáng

PLC nhận yêu cầu nhưng không trả lời. Kiểm tra:

Slave ID.
Cấu hình Modbus.
PLC đã tắt/bật nguồn chưa.
PLC có ở RUN không.
Cả RD và SD không nhấp nháy

Kiểm tra:

Sai COM.
Sai baud rate/parity.
Sai dây.
FTDI chưa nhận driver.
T/R+ và T/R− bị đảo.
Python báo timeout hoặc không có response

Thử lần lượt:

Đảo hai dây T/R+ và T/R−.
Kiểm tra Python đang dùng 9600, 8, Even, 1.
Kiểm tra Slave ID bằng 1.
Đọc địa chỉ 100, không phải 101.
Tắt rồi bật lại PLC.
Kiểm tra D8402; nếu có lỗi 203, channel truyền thông đang bị dùng trùng. Mitsubishi xác định việc cấu hình một channel cho nhiều chế độ sẽ làm truyền thông bị vô hiệu hóa.
Lưu ý quan trọng về D100

Không nên cấu hình:

MOV H11 D8415
MOV K100 D8416

trong bài test này, vì hai lệnh đó yêu cầu PLC lưu bộ đếm và nhật ký truyền thông bắt đầu tại D100, làm ghi đè dữ liệu thử nghiệm. Tài liệu cho biết khi D8415 = H11 và D8416 = K100, vùng trạng thái truyền thông sử dụng từ D100 trở đi.

Vì vậy, chương trình test chỉ cần:

M8411 → MOV H1081 D8400
         MOV H11   D8401
         MOV K1    D8414

M8002 → MOV K1234 D100

Đây là cấu hình tối giản để xác nhận toàn bộ đường truyền:

GX Works2 → DB9 → PLC → D100 → FX3U-485-BD → FTDI → Python