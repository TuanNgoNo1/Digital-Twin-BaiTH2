using UnityEngine;

namespace PDTwin.RuntimeUI
{
    /// <summary>Read-only status adapter for the Bài 2 COM5 telemetry workspace.</summary>
    public sealed class Bai2RuntimeStatusProvider : MonoBehaviour
    {
        private const float RefreshInterval = 0.25f;

        private PDTwinStatusBar statusBar;
        private GatewayModeStatusClient modeStatus;
        private PLCController_v2 plc;
        private float nextRefreshAt;
        private string lastMode = "UNKNOWN";
        private bool gxSessionObserved;

        public static Bai2RuntimeStatusProvider Attach(GameObject host, PDTwinStatusBar bar)
        {
            Bai2RuntimeStatusProvider provider = host.GetComponent<Bai2RuntimeStatusProvider>();
            if (provider == null)
                provider = host.AddComponent<Bai2RuntimeStatusProvider>();
            provider.statusBar = bar;
            provider.modeStatus = GatewayModeStatusClient.Attach(host);
            provider.RefreshStatus();
            return provider;
        }

        private void Update()
        {
            BindPlc();
            if (Time.unscaledTime < nextRefreshAt)
                return;
            nextRefreshAt = Time.unscaledTime + RefreshInterval;
            RefreshStatus();
        }

        private void BindPlc()
        {
            if (plc != null)
                return;
            plc = PLCController_v2.Instance != null
                ? PLCController_v2.Instance
                : FindFirstObjectByType<PLCController_v2>(FindObjectsInactive.Include);
        }

        private void RefreshStatus()
        {
            if (statusBar == null)
                return;

            bool telemetryOnly = plc != null && plc.IsTelemetryOnly;
            RefreshCom3Status(telemetryOnly);

            bool fresh = plc != null && plc.IsTelemetryFresh;
            float age = plc != null ? plc.TelemetryAgeSeconds : float.PositiveInfinity;
            statusBar.SetStatus(
                "COM5",
                "COM5 / TELEMETRY",
                fresh ? "ĐANG NHẬN" : "MẤT DỮ LIỆU",
                fresh ? $"Frame mới {age:F1} giây" : "Telemetry RS485 offline hoặc stale",
                fresh ? RuntimeHealth.Online : RuntimeHealth.Offline);

            bool synced = fresh && plc.LatestTelemetry != null && plc.LatestTelemetry.backendSynced;
            statusBar.SetStatus(
                "PLC",
                "PLC FX3U",
                synced ? "ONLINE" : fresh ? "CHƯA ĐỒNG BỘ" : "OFFLINE",
                synced ? "Xác nhận từ telemetry vật lý COM5" : "Chưa xác nhận được PLC thật",
                synced ? RuntimeHealth.Online : fresh ? RuntimeHealth.Warning : RuntimeHealth.Offline);
        }

        private void RefreshCom3Status(bool telemetryOnly)
        {
            if (modeStatus == null || !modeStatus.IsFresh)
            {
                statusBar.SetStatus(
                    "COM3", "COM3 / GX WORKS2", "CHƯA XÁC NHẬN",
                    telemetryOnly ? "Đang chờ gateway mode status" : "Bài 2 chưa ở TelemetryOnly",
                    RuntimeHealth.Warning);
                return;
            }

            GatewayModeDocument document = modeStatus.Current;
            string mode = modeStatus.Mode;
            RefreshModeBanner(mode);
            switch (mode)
            {
                case "GXWORKS":
                    statusBar.SetStatus("COM3", "COM3 / GX WORKS2", "SẴN SÀNG",
                        document.sessionState == "Disconnected"
                            ? "Phiên GX Works2 đang tạm ngắt kết nối"
                            : "COM3 đã nhả cho GX Works2",
                        RuntimeHealth.Online);
                    break;
                case "PREPARING_GX":
                    statusBar.SetStatus("COM3", "COM3 / GX WORKS2", "ĐANG NHẢ COM3",
                        "Đang dừng Gateway để mở GX Works2", RuntimeHealth.Warning);
                    break;
                case "CONFIRMING_END":
                    statusBar.SetStatus("COM3", "COM3 / GX WORKS2", "CHỜ XÁC NHẬN",
                        "GX Works2 đã đóng; xác nhận trong Guacamole", RuntimeHealth.Warning);
                    break;
                case "GATEWAY":
                    statusBar.SetStatus("COM3", "COM3 / GX WORKS2", "ĐANG Ở GATEWAY",
                        "Đăng nhập phiên PLC để Agent nhả COM3", RuntimeHealth.Warning);
                    break;
                case "RETURNING_GATEWAY":
                    statusBar.SetStatus("COM3", "COM3 / GX WORKS2", "ĐANG TRẢ COM3",
                        "Phiên PLC đã kết thúc; Gateway đang khôi phục", RuntimeHealth.Warning);
                    break;
                case "FAULT":
                    statusBar.SetStatus("COM3", "COM3 / GX WORKS2", "LỖI COM3",
                        string.IsNullOrWhiteSpace(document.message) ? "Prepare Mode Agent báo lỗi" : document.message,
                        RuntimeHealth.Offline);
                    break;
                default:
                    statusBar.SetStatus("COM3", "COM3 / GX WORKS2", "CHƯA XÁC ĐỊNH",
                        "Không xác định được chủ sở hữu COM3", RuntimeHealth.Warning);
                    break;
            }
        }

        private void RefreshModeBanner(string mode)
        {
            if (mode == "PREPARING_GX" || mode == "GXWORKS" ||
                mode == "CONFIRMING_END" || mode == "RETURNING_GATEWAY")
                gxSessionObserved = true;

            if (string.Equals(mode, lastMode, System.StringComparison.Ordinal))
                return;

            string previous = lastMode;
            lastMode = mode;
            switch (mode)
            {
                case "PREPARING_GX":
                    statusBar.SetTransitionBanner(true, "ĐANG NHẢ COM3 CHO GX WORKS2",
                        "Vui lòng chờ đến khi trạng thái chuyển sang Sẵn sàng.", RuntimeHealth.Warning);
                    break;
                case "GXWORKS":
                    if (previous == "PREPARING_GX")
                        statusBar.ShowTransitionResult("COM3 / GX WORKS2 ĐÃ SẴN SÀNG",
                            "Sinh viên có thể bắt đầu lập trình PLC.", RuntimeHealth.Online);
                    else
                        statusBar.SetTransitionBanner(false, string.Empty, string.Empty, RuntimeHealth.Unknown);
                    break;
                case "CONFIRMING_END":
                    statusBar.SetTransitionBanner(true, "GX WORKS2 ĐÃ ĐÓNG",
                        "Xác nhận mở lại hoặc kết thúc bài trong cửa sổ Guacamole.", RuntimeHealth.Warning);
                    break;
                case "RETURNING_GATEWAY":
                    statusBar.SetTransitionBanner(true, "ĐANG KẾT THÚC PHIÊN BÀI 2",
                        "Đang trả COM3 về Gateway và khóa chức năng chấm điểm.", RuntimeHealth.Warning);
                    break;
                case "GATEWAY":
                    if (gxSessionObserved)
                        statusBar.SetTransitionBanner(true, "PHIÊN BÀI 2 ĐÃ KẾT THÚC",
                            "COM3 đã trả về Gateway • Điểm chưa nộp không được lưu.", RuntimeHealth.Offline);
                    else
                        statusBar.SetTransitionBanner(false, string.Empty, string.Empty, RuntimeHealth.Unknown);
                    break;
                default:
                    statusBar.SetTransitionBanner(false, string.Empty, string.Empty, RuntimeHealth.Unknown);
                    break;
            }
        }
    }
}
