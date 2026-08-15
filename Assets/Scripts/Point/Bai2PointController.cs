using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PDTwin.Point
{
    public sealed class Bai2PointController : MonoBehaviour
    {
        private const string LessonId = "BAI_2";

        private readonly PointOperationCollector collector = new PointOperationCollector();
        private PLCController_v2 plc;
        private PointRuntimePanel panel;
        private PointTarget target;
        private PointOperationResult lastOperation;
        private float currentScore;
        private bool submitArmed;
        private float submitConfirmUntil;

        private IEnumerator Start()
        {
            target = PointTargetProvider.Load(LessonId);
            panel = PointRuntimePanel.Create("POINT - BAI THUC HANH 2");
            panel.transform.SetParent(transform, false);
            panel.SetTarget(target);
            panel.StartRequested += BeginAttempt;
            panel.RegradeRequested += BeginAttempt;
            panel.SubmitRequested += RequestSubmit;

            float deadline = Time.realtimeSinceStartup + 12f;
            while (plc == null && Time.realtimeSinceStartup < deadline)
            {
                BindPlcIfAvailable();
                if (plc == null)
                    yield return null;
            }

            PDTwinBridge.ReportProgress(0f);
            RefreshPanel();
            if (plc == null)
                panel.SetStatus("Khong tim thay PLCController_v2; POINT chua san sang.", true);
        }

        private void Update()
        {
            if (PDTwinBridge.IsSubmitted)
            {
                panel?.SetButtons(false, false, false);
                return;
            }

            BindPlcIfAvailable();
            if (submitArmed && Time.unscaledTime > submitConfirmUntil)
                submitArmed = false;

            if (collector.State == PointAttemptState.Collecting &&
                (plc == null || !plc.IsTelemetryOnly || !plc.IsTelemetryFresh))
            {
                collector.Cancel("Telemetry bi mat hoac khong con TelemetryOnly; hay cham lai.");
                panel.SetStatus(collector.Status, true);
                RefreshPanel();
            }
        }

        private void OnDestroy()
        {
            if (plc != null)
                plc.OnTelemetryUpdated -= OnTelemetryUpdated;

            if (panel != null)
            {
                panel.StartRequested -= BeginAttempt;
                panel.RegradeRequested -= BeginAttempt;
                panel.SubmitRequested -= RequestSubmit;
            }
        }

        private void BindPlcIfAvailable()
        {
            if (plc != null)
                return;

            plc = PLCController_v2.Instance != null
                ? PLCController_v2.Instance
                : FindFirstObjectByType<PLCController_v2>(FindObjectsInactive.Include);
            if (plc != null)
                plc.OnTelemetryUpdated += OnTelemetryUpdated;
        }

        private void BeginAttempt()
        {
            submitArmed = false;
            if (PDTwinBridge.IsSubmitted)
                return;

            BindPlcIfAvailable();
            if (plc == null)
            {
                panel.SetStatus("Khong tim thay PLCController_v2.", true);
                return;
            }
            if (!plc.IsTelemetryOnly)
            {
                panel.SetStatus("Bai 2 khong o TelemetryOnly; khong cho phep cham.", true);
                return;
            }
            if (!plc.IsTelemetryFresh)
            {
                panel.SetStatus("Telemetry COM5 dang offline hoac stale.", true);
                return;
            }

            collector.Begin(target);
            panel.SetStatus(collector.Status);
            RefreshPanel();
        }

        private void OnTelemetryUpdated(PLCController_v2.MotorTelemetry telemetry)
        {
            if (telemetry == null || collector.State != PointAttemptState.Collecting)
                return;

            bool valid = plc != null && plc.IsTelemetryOnly && plc.IsTelemetryFresh;
            int encoder = telemetry.encoderCount != 0 ? telemetry.encoderCount : telemetry.count;
            bool completed = collector.Accept(
                valid,
                telemetry.running,
                telemetry.speedRpm,
                encoder,
                telemetry.direction,
                Time.unscaledTime);

            panel.SetStatus(collector.Status, !valid);
            if (!completed)
            {
                RefreshPanel();
                return;
            }

            lastOperation = collector.Evaluate();
            RecalculateScore();
            PDTwinBridge.ReportProgress(currentScore);
            panel.SetStatus("Da co diem tam. Co the cham lai hoac nop bai.");
            RefreshPanel();
        }

        private void RecalculateScore()
        {
            float score = 0f;
            if (lastOperation != null)
            {
                if (lastOperation.directionPassed) score += 2f;
                if (lastOperation.rpmPassed) score += 3.5f;
                if (lastOperation.encoderPassed) score += 4.5f;
            }
            currentScore = PointMath.RoundScore(score);
        }

        private void RequestSubmit()
        {
            if (PDTwinBridge.IsSubmitted)
                return;

            if (!submitArmed || Time.unscaledTime > submitConfirmUntil)
            {
                submitArmed = true;
                submitConfirmUntil = Time.unscaledTime + 8f;
                panel.SetStatus($"Xac nhan nop {currentScore:F2}/10: bam Nop bai lan nua trong 8 giay.");
                return;
            }

            SubmitFinal("MANUAL");
        }

        public void FinalizeOnTimeout()
        {
            if (!PDTwinBridge.IsSubmitted)
                SubmitFinal("TIMEOUT");
        }

        private void SubmitFinal(string reason)
        {
            submitArmed = false;
            RecalculateScore();
            PointSubmissionDetails details = BuildDetails(reason);
            PDTwinBridge.Submit(currentScore, JsonUtility.ToJson(details));
            panel.SetScore(currentScore, true);
            panel.SetStatus("Da nop diem chinh thuc. Luot cham da khoa.");
            panel.SetButtons(false, false, false);
        }

        private PointSubmissionDetails BuildDetails(string reason)
        {
            PointSubmissionDetails details = new PointSubmissionDetails
            {
                lesson = LessonId,
                submitReason = reason,
                timestampUtc = DateTime.UtcNow.ToString("o"),
                totalScore = currentScore,
                target = target,
                breakdown = new PointBreakdownPayload
                {
                    wiringScore = 0f,
                    directionScore = lastOperation != null && lastOperation.directionPassed ? 2f : 0f,
                    rpmScore = lastOperation != null && lastOperation.rpmPassed ? 3.5f : 0f,
                    encoderScore = lastOperation != null && lastOperation.encoderPassed ? 4.5f : 0f
                },
                validation = new PointValidationPayload
                {
                    telemetryOnline = plc != null && plc.IsPiOnline,
                    telemetryFresh = plc != null && plc.IsTelemetryFresh,
                    telemetryOnly = plc != null && plc.IsTelemetryOnly,
                    motorRunObserved = lastOperation != null,
                    motorStopObserved = lastOperation != null,
                    runningSamples = lastOperation?.runningSamples ?? 0,
                    stoppedSamples = lastOperation?.stoppedSamples ?? 0,
                    directionMatchRatio = lastOperation?.directionMatchRatio ?? 0f,
                    rpmErrorPercent = lastOperation?.rpmErrorPercent ?? 0f,
                    encoderErrorPulses = lastOperation?.encoderErrorPulses ?? 0,
                    note = lastOperation == null ? "NO_VALID_ATTEMPT" : string.Empty
                }
            };

            if (lastOperation != null)
            {
                details.measured.direction = lastOperation.measuredDirection;
                details.measured.speedRpm = lastOperation.measuredRpm;
                details.measured.encoderPulses = lastOperation.measuredEncoder;
            }
            return details;
        }

        private void RefreshPanel()
        {
            if (panel == null)
                return;

            float directionScore = lastOperation != null && lastOperation.directionPassed ? 2f : 0f;
            float rpmScore = lastOperation != null && lastOperation.rpmPassed ? 3.5f : 0f;
            float encoderScore = lastOperation != null && lastOperation.encoderPassed ? 4.5f : 0f;
            string telemetryStatus = plc == null
                ? "KHONG TIM THAY"
                : !plc.IsTelemetryOnly
                    ? "SAI MODE"
                    : plc.IsTelemetryFresh ? "ONLINE" : "OFFLINE/STALE";

            panel.SetBreakdown(
                $"Telemetry COM5: {telemetryStatus}\n" +
                $"Chieu quay: {(lastOperation == null ? "Chua cham" : lastOperation.directionPassed ? "Dat" : "Chua dat")}     {directionScore:F2}/2.00\n" +
                $"RPM: {(lastOperation == null ? "Chua cham" : lastOperation.rpmPassed ? "Dat" : "Chua dat")}     {rpmScore:F2}/3.50\n" +
                $"Encoder: {(lastOperation == null ? "Chua cham" : lastOperation.encoderPassed ? "Dat" : "Chua dat")}     {encoderScore:F2}/4.50");
            panel.SetScore(currentScore, PDTwinBridge.IsSubmitted);

            bool ready = plc != null && plc.IsTelemetryOnly && plc.IsTelemetryFresh;
            bool collecting = collector.State == PointAttemptState.Collecting;
            panel.SetButtons(ready && !collecting, lastOperation != null && ready && !collecting, !collecting);

            if (collecting)
                panel.SetStatus(collector.Status);
            else if (lastOperation == null && ready)
                panel.SetStatus("Bam Bat dau cham, sau do cho motor chay va dung.");
        }
    }

    internal static class Bai2PointRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallAfterInitialLoad()
        {
            Install(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Install(scene);
        }

        private static void Install(Scene scene)
        {
            if (!scene.IsValid() || !string.Equals(scene.name, "Sy_scene", StringComparison.OrdinalIgnoreCase))
                return;
            if (UnityEngine.Object.FindFirstObjectByType<Bai2PointController>(FindObjectsInactive.Include) != null)
                return;

            GameObject root = new GameObject("Bai2PointRuntime");
            root.AddComponent<Bai2PointController>();
        }
    }
}
