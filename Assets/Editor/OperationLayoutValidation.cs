using System;
using System.IO;
using PDTwin.Point;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Opt-in editor check that runs in this project even when another Editor owns the MCP connection.
[InitializeOnLoad]
public static class OperationLayoutValidation
{
    private const string Request = "Logs/OperationLayoutValidation.request";
    private const string Result = "Logs/OperationLayoutValidation.txt";
    private static int stage;
    private static double nextAction;
    static OperationLayoutValidation() { EditorApplication.update += Update; }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Update()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.timeSinceStartup < nextAction) return;
        try
        {
            if (stage == 0 && !SessionState.GetBool("Bai2.Layout.Fresh", false))
            {
                SessionState.SetBool("Bai2.Layout.Fresh", true);
                if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; return; }
            }
            if (!EditorApplication.isPlaying)
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = true;
                return;
            }
            CircuitManager manager = UnityEngine.Object.FindFirstObjectByType<CircuitManager>();
            PointRuntimePanel panel = UnityEngine.Object.FindFirstObjectByType<PointRuntimePanel>();
            if (manager == null || panel == null) { nextAction = EditorApplication.timeSinceStartup + 1; return; }
            if (stage == 0)
            {
                File.WriteAllText(Result, "Project: " + Application.dataPath + "\n");
                EditorApplication.ExecuteMenuItem("Window/General/Game");
                Require(manager.TryShowStepView(3), "Open operation view");
                stage = 1; nextAction = EditorApplication.timeSinceStartup + 3; return;
            }
            var skin = manager.GetComponent<Bai2PracticalWorkspaceSkin>();
            if (stage == 1)
            {
                skin.HideGuidePopup(); panel.OpenTestOverview();
                stage = 2; nextAction = EditorApplication.timeSinceStartup + 2; return;
            }
            Transform overlay = panel.transform.Find("ScoreDrawerOverlay");
            RectTransform drawer = overlay.Find("ScoreDrawer") as RectTransform;
            Transform body = drawer.Find("BodyViewport/Body");
            if (stage == 2)
            {
                Require(Mathf.Abs(drawer.rect.width - 470f) < 1, "Compact Test width matches Bai 1");
                Require(!overlay.Find("Scrim").gameObject.activeSelf, "No gray scrim");
                var foreground = GameObject.Find("OperationTestWorkspace");
                Require(foreground != null && foreground.GetComponent<Image>() == null, "No white workspace backdrop");
                Require(GameObject.Find("OperationBackground") != null, "Large apparatus backdrop exists");
                Require(PLCController_v2.Instance.OperationHmiPanel.anchoredPosition.x >
                    foreground.transform.Find("MotorStatus").GetComponent<RectTransform>().anchoredPosition.x,
                    "Monitor is to the right of motor/status column");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/Bai2Reference-Overview.png"));
                File.AppendAllText(Result, "PASS: compact width, no scrim/white backdrop, monitor centered, motor/status at left.\n");
                stage = 3; nextAction = EditorApplication.timeSinceStartup + 2; return;
            }
            if (stage == 3)
            {
                body.Find("Overview_0").GetComponent<Button>().onClick.Invoke();
                stage = 4; nextAction = EditorApplication.timeSinceStartup + 2; return;
            }
            if (stage == 4)
            {
                Require(Mathf.Abs(drawer.rect.width - 650f) < 1, "Expanded Test width matches Bai 1");
                Require(body.Find("TestCard").gameObject.activeSelf, "Details open on Test click");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/Bai2Reference-Details.png"));
                stage = 5; nextAction = EditorApplication.timeSinceStartup + 2; return;
            }
            if (stage == 5)
            {
                drawer.Find("Header/CloseButton").GetComponent<Button>().onClick.Invoke();
                Require(manager.TryShowStepView(0), "Open wiring view");
                stage = 6; nextAction = EditorApplication.timeSinceStartup + 2; return;
            }
            if (stage == 6)
            {
                Require(GameObject.Find("OperationBackground") == null, "Background hidden during wiring");
                Require(PLCController_v2.Instance.OperationHmiPanel.parent.name != "OperationTestWorkspace", "HMI restored");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/Bai2Reference-Wiring.png"));
                stage = 7; nextAction = EditorApplication.timeSinceStartup + 2; return;
            }
            manager.TryShowStepView(3);
            panel.OpenTestOverview();
            File.AppendAllText(Result, "PASS: details expand to 650; wiring restores layout.\n");
            File.Delete(Request); stage = 0;
            SessionState.EraseBool("Bai2.Layout.Fresh");
        }
        catch (Exception error)
        {
            File.AppendAllText(Result, "FAIL: " + error + "\n");
            File.Delete(Request); stage = 0;
            SessionState.EraseBool("Bai2.Layout.Fresh");
            Debug.LogException(error);
        }
    }
}
