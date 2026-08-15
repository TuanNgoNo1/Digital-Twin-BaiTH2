# Phien 2026-07-22 - Bai 2 GX Works2, Modbus RTU va Unity WebGL

> **QUAN TRONG:** Project Unity chinh thuc:
> `C:\Users\Server-Lab602\Desktop\Bai2 (ProjectUnity)\Digital-Twin-main\Digital-Twin-main`
>
> Khong copy de file Unity tu ban cu o D: sang C:. Thu muc D: chi dang giu script van hanh server va bo gateway COM5.

## Muc tieu

```text
Luong 1: GX Works2 -> COM3/SC09 -> PLC -> motor that
Luong 2: PLC/FX3U-485-BD -> COM5/Modbus RTU -> HTTP gateway -> Unity WebGL
```

- GX Works2 va ladder la noi dieu khien.
- Unity Bai 2 chi doc telemetry, hien HMI va quay motor ao.
- Gateway COM5 phai read-only.

## Da hoan thanh

- COM3 la cap SC09/CH340 cho GX Works2; COM5 la FTDI/DTech RS485.
- Da dau one-pair: `T/R+ -> RDA`, `T/R- -> RDB`, cau `RDA-SDA`, cau `RDB-SDB`, va noi `SG` neu hai thiet bi co chan signal ground.
- Unity `Sy_scene` da co che do `Bai2CompletedReview`, mo Buoc 4, khoa 15 day, cho xem lai Buoc 1-3 va highlight dung nhom day.
- HMI Bai 2 da o `TelemetryOnly`: an dieu khien va chan HTTP control request.
- Motor ao da dung RPM/chieu/goc telemetry va dung khi stale/offline.
- Da bo huong Computer Link cho Luong 2 va chuyen sang Modbus RTU.
- Da tao gateway C# tai `gateway/modbus_rtu_gateway`:
  - Serial: `COM5`, `38400`, `8N1`, slave `3`.
  - Read FC03 cho `D100..D165`, FC01 cho `M0..M17`.
  - `GET /health`, `GET /telemetry`, `GET /debug` tren `127.0.0.1:5002`.
  - `POST /control` tra HTTP `423`; gateway khong co ham ghi PLC.
- `ModbusRtuGateway.exe` build thanh cong; health va khoa write da duoc kiem tra.
- Frame test D500 dung: `03 03 01 F4 00 01 C5 E6`.
- Da nap ladder va monitor xac nhan `D8120 = H40A1`, `D8121 = 0003`.
- COM5 van timeout, nhan `0 byte`; da them probe lap 10 lan de quan sat LED TXD/RXD.
- Da chay `TEST-DTECH-COM5-LOOPBACK.bat` ngay 23/07/2026: TX/RX trung nhau va `DTECH_LOOPBACK=PASS`. COM5, FTDI va DT-5119 da dat; loi timeout nam sau adapter.
- Da phat hien `PlcBridge.exe COM5 9090` chiem nham COM5. `Start-PixelStack.ps1` da duoc sua de chi chon `CH340/COM3` va bo qua FTDI/COM5.
- Test 10 request sau khi noi lai PLC: `RD` tren FX3U-485-BD nhay, `SD` khong nhay, COM5 nhan `0 byte`. Duong dien PC -> board da dat, nhung PLC/board khong phan hoi Modbus.
- Khong tiep tuc dao day/doi client. Modbus native tren `FX3U-485-BD` hien tai bi loai; chuyen sang non-protocol/Computer Link hoac thay `FX3U-485ADP-MB` neu bat buoc Modbus.
- COM3/gateway hien huu khong bi thay doi trong qua trinh lam COM5.

## Vi tri file

Project Unity chinh:

```text
C:\Users\Server-Lab602\Desktop\Bai2 (ProjectUnity)\Digital-Twin-main\Digital-Twin-main
```

Script van hanh tren server:

```text
D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\TEST-RS485-COM5-READONLY.bat
D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\START-RS485-MODBUS-GATEWAY.bat
D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\ops\rs485\README.md
```

## Buoc bat buoc tiep theo trong GX Works2

Tai `PLC Parameter -> PLC System (2) -> CH1`, bo chon `Operate Communication Setting` de Computer Link cu khong ghi de `D8120`.

Them hai lenh first-scan vao ladder:

```text
M8002 ----[ MOV H40A1 D8120 ]
      ----[ MOV K3    D8121 ]
```

Cau hinh muc tieu:

```text
Modbus RTU slave
38400 bps
8 data bits
No parity
1 stop bit
Slave ID 3
```

Luu y: bai Factory Automation ghi `9600` o mot chu thich, nhung `H40A1` va doan Python cua bai tuong ung `38400/8N1`. Gateway dang co cau hinh dung theo `H40A1`: `38400/8N1`.

Sau do:

1. Compile project.
2. Chuyen PLC sang STOP.
3. Write ca `Parameter` va `Program`.
4. Tat/bat nguon rieng PLC.
5. Chuyen PLC sang RUN.
6. Monitor `D8120` theo HEX va `D8121` theo decimal.

Gia tri mong doi:

```text
D8120 = H40A1
D8121 = 3
```

## Test COM3 va COM5 dong thoi

1. Giu GX Works2 ket noi PLC qua COM3.
2. Trong Device/Buffer Memory Batch Monitor, ghi `D500 = 1234`.
3. Chay `TEST-RS485-COM5-READONLY.bat`.

Ket qua mong doi:

```text
D500 unsigned=1234, signed=1234
MODBUS_RTU_READ=PASS
```

Neu timeout:

- Kiem tra runtime `D8120/D8121` truoc.
- Dam bao khong co chuong trinh khac giu COM5.
- Quan sat ca TXD va RXD khi test.
- Neu chi co TXD, thu dao nguyen cap `T/R+` va `T/R-`; khong dao mot day rieng.
- Kiem tra dien tro ket thuc mang va chan `SG`.
- Neu `D8120 = H40A1`, `D8121 = 3` van khong co RX, xac minh model/firmware PLC. Tai lieu chinh hang cua `FX3U-485-BD` khong liet ke Modbus RTU native; cach nay phu thuoc PLC FX3U-compatible/clone co firmware Modbus hoac phan cung Modbus tuong ung.

## Sau khi D500 PASS

1. Chay `START-RS485-MODBUS-GATEWAY.bat` va giu cua so mo.
2. Mo `http://127.0.0.1:5002/health`.
3. Mo `http://127.0.0.1:5002/telemetry`.
4. Doi cac thanh ghi that va xac nhan JSON thay doi.
5. Kiem tra lai mapping RPM/encoder/chieu; cac cong thuc gateway hien tai la mapping tam theo ladder cu.
6. Tao reverse proxy `/plc-rs485/telemetry` den `127.0.0.1:5002/telemetry`.
7. Chi luc do moi doi endpoint trong `Sy_scene` va build WebGL.

## Chua hoan thanh

- PLC da duoc nap ladder khoi tao Modbus `H40A1/K3` va gia tri runtime da dung.
- COM5 chua doc thanh cong `D500`; buoc chan hien tai nam o wiring/polarity, firmware hoac kha nang Modbus cua CPU/board.
- Mapping telemetry chua duoc hieu chuan voi motor/encoder that.
- Reverse proxy va endpoint Unity chua doi sang gateway COM5.
- Chua test end-to-end va chua build WebGL cuoi.

## An toan van hanh

- Khong de hai process cung mo COM5.
- Khong de gateway COM3 va GX Works2 cung giu COM3.
- Tat nguon PLC truoc khi sua day hoac dien tro terminal.
- Khong bat ghi PLC tu Unity/gateway COM5.
- Khong ghi mat khau tai khoan sinh vien vao file handoff.
