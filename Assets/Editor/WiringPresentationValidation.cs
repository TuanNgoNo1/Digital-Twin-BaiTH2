using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Opt-in validation: create Logs/WiringPresentationValidation.request or use the menu.
[InitializeOnLoad]
public static class WiringPresentationValidation
{
    private const string Request = "Logs/WiringPresentationValidation.request";
    private const string Result = "Logs/WiringPresentationValidation.txt";
    private const string StartedPlay = "WiringPresentationValidation.StartedPlay";
    private static int stage;
    private static double nextAction;
    private static int originalStep;
    private static bool waitingForCapture;
    private static bool captureComplete;
    private static double captureDeadline;

    static WiringPresentationValidation()
    {
        EditorApplication.update += Update;
    }

    [MenuItem("Tools/Digital Twin/Validate connected wires and workspace")]
    public static void Run()
    {
        if (!SceneManager.GetSceneByPath("Assets/Scenes/Sy_scene.unity").isLoaded)
            throw new InvalidOperationException("Open Sy_scene before validating the workspace.");
        File.WriteAllText(Request, "Validate all three wiring review steps.");
        File.WriteAllText(Result, "Validation requested.\n");
        stage = 0;
        nextAction = 0;
    }

    private static void Update()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.timeSinceStartup < nextAction)
            return;

        try
        {
            if (!EditorApplication.isPlaying)
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                if (!SceneManager.GetSceneByPath("Assets/Scenes/Sy_scene.unity").isLoaded)
                    throw new InvalidOperationException("Sy_scene must be open for runtime validation.");
                SessionState.SetBool(StartedPlay, true);
                EditorApplication.isPlaying = true;
                return;
            }

            CircuitManager manager = UnityEngine.Object.FindFirstObjectByType<CircuitManager>();
            if (manager == null)
            {
                nextAction = EditorApplication.timeSinceStartup + 1;
                return;
            }

            if (stage == 0)
            {
                originalStep = manager.VisibleStepIndex;
                if (originalStep == 3)
                    Require(manager.GetComponent<Bai2PracticalWorkspaceSkin>().IsGuidePopupVisible,
                        "Operation guide opens automatically on first arrival");
                if (!manager.IsCompletedReviewMode)
                    throw new InvalidOperationException("Validation requires the completed Bai 2 review mode.");
                File.WriteAllText(Result, "Runtime validation of Bai 2 wires and workspace (Tuan V3)\n");
                Type gameView = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(assembly => assembly.GetType("UnityEditor.GameView")).FirstOrDefault(type => type != null);
                if (gameView != null)
                    EditorWindow.GetWindow(gameView).Focus();
                Require(manager.TryShowStepView(0), "Open step 1");
                stage = 1;
                nextAction = EditorApplication.timeSinceStartup + 3;
                return;
            }

            if (waitingForCapture)
            {
                if (!captureComplete)
                {
                    Require(EditorApplication.timeSinceStartup < captureDeadline, "Game view renders a screenshot within 15 seconds");
                    EditorApplication.QueuePlayerLoopUpdate();
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                    nextAction = EditorApplication.timeSinceStartup + 0.1;
                    return;
                }
                waitingForCapture = false;
                if (stage <= 3)
                    Require(manager.TryShowStepView(stage - 1), "Switch review step");
                nextAction = EditorApplication.timeSinceStartup + 2;
                return;
            }

            if (stage >= 1 && stage <= 3)
            {
                ValidateStep(manager, stage - 1);
                captureComplete = false;
                captureDeadline = EditorApplication.timeSinceStartup + 15;
                manager.StartCoroutine(CaptureFrame(Path.GetFullPath("Logs/Bai2Wires-Step" + stage + ".png")));
                stage++;
                waitingForCapture = true;
                nextAction = EditorApplication.timeSinceStartup + 0.75;
                return;
            }

            if (stage == 4)
            {
                Require(manager.TryShowStepView(3), "Open operation view");
                stage = 5;
                nextAction = EditorApplication.timeSinceStartup + 2;
                return;
            }

            File.AppendAllText(Result, "Operation camera: " + (Camera.main != null ? Camera.main.rect.ToString() : "missing") + "\n");
            Require(Camera.main != null && Camera.main.rect == new Rect(0f, 0f, 1f, 1f),
                "Operation camera viewport restored");
            SceneAuthoredWiringBoard board = UnityEngine.Object.FindFirstObjectByType<SceneAuthoredWiringBoard>();
            Require(board != null && !board.boardRenderer.gameObject.activeSelf, "Flat board hidden during operation");
            for (int i = 0; i < board.sockets.Count; i++)
                Require(Vector3.Distance(board.sockets[i].transform.position, board.operationPositions[i]) < 0.0001f,
                    board.sockets[i].socketID + " operation pose restored");
            if (stage == 5)
            {
                var skin = manager.GetComponent<Bai2PracticalWorkspaceSkin>();
                skin.ShowGuidePopup();
                Require(skin.IsGuidePopupVisible, "Operation guide can be reopened");
                var popup = GameObject.Find("Bai2GuidePopup_Canvas");
                Require(popup.transform.Find("GuidePopup/Title").GetComponent<TMPro.TextMeshProUGUI>().text == "Vận hành",
                    "Popup title omits step number");
                skin.HideGuidePopup();
                Require(GameObject.Find("ViewGuideButton") != null, "Operation guide button visible");
                Require(manager.stepRoots.All(root => root.activeInHierarchy), "All wiring restored during operation");
                var plc = UnityEngine.Object.FindFirstObjectByType<PLCController_v2>();
                var field = typeof(PLCController_v2).GetField("canvasHmiPanelRect",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                RectTransform hmi = field.GetValue(plc) as RectTransform;
                Require(hmi != null && Mathf.Approximately(hmi.localScale.x, 0.65f), "Telemetry HMI reduced to 65 percent");
                Require(hmi.anchoredPosition.y <= -164f, "Telemetry HMI below step navigation");
                captureComplete = false;
                captureDeadline = EditorApplication.timeSinceStartup + 15;
                manager.StartCoroutine(CaptureFrame(Path.GetFullPath("Logs/Bai2Operation.png")));
                stage = 6;
                waitingForCapture = true;
                nextAction = EditorApplication.timeSinceStartup + 0.75;
                return;
            }
            Require(manager.TryShowStepView(originalStep), "Restore initial review step");
            File.AppendAllText(Result, "PASS: operation camera restored; all three wiring steps passed.\n");
            Finish();
        }
        catch (Exception exception)
        {
            File.AppendAllText(Result, "FAIL: " + exception + "\n");
            Debug.LogException(exception);
            Finish();
        }
    }

    private static IEnumerator CaptureFrame(string path)
    {
        yield return new WaitForEndOfFrame();
        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(path, screenshot.EncodeToPNG());
        UnityEngine.Object.Destroy(screenshot);
        captureComplete = true;
    }

    private static void ValidateStep(CircuitManager manager, int step)
    {
        Bai2PracticalWorkspaceSkin skin = manager.GetComponent<Bai2PracticalWorkspaceSkin>();
        Require(skin != null, "Workspace skin exists");
        skin.ShowGuidePopup();
        Require(!skin.IsGuidePopupVisible, "No guide popup for wiring steps");
        Require(GameObject.Find("ViewGuideButton") == null, "No guide button for wiring steps");
        for (int i = 0; i < manager.stepRoots.Count; i++)
            Require(manager.stepRoots[i].activeSelf == (i == step), "Only selected step root is visible");
        SceneAuthoredWiringBoard board = UnityEngine.Object.FindFirstObjectByType<SceneAuthoredWiringBoard>();
        Require(board != null && board.boardRenderer.gameObject.activeInHierarchy, "New flat board visible");
        Require(Quaternion.Angle(board.boardRenderer.transform.rotation, Camera.main.transform.rotation) < 0.1f,
            "Flat board faces the runtime camera");
        Require(board.sockets.Count == 28 && board.jackVisualRoot.transform.childCount == 28, "All 28 sockets use new jacks");
        WireBody[] wires = manager.stepRoots.SelectMany(root => root.GetComponentsInChildren<WireBody>(true)).ToArray();
        Require(wires.Length == 15, "Exactly 15 wiring objects");
        foreach (WireBody wire in wires)
        {
            Require(wire.isCorrect && wire.plugA.isSnapped && wire.plugB.isSnapped, wire.name + " stays connected");
            if (!wire.gameObject.activeInHierarchy) continue;
            Require(Vector3.Distance(wire.plugA.transform.position, wire.plugA.connectedSocket.transform.position) < 0.0001f &&
                Vector3.Distance(wire.plugB.transform.position, wire.plugB.connectedSocket.transform.position) < 0.0001f,
                wire.name + " plugs follow the flat board sockets");
            Require(!wire.GetComponent<LineRenderer>().enabled, wire.name + " legacy line hidden");
            ProceduralConnectedWire visual = wire.GetComponentInChildren<ProceduralConnectedWire>();
            Require(visual != null && visual.gameObject.activeInHierarchy, wire.name + " uses the new 3D cable");
            Mesh mesh = visual.GetComponent<MeshFilter>().sharedMesh;
            Require(mesh != null && mesh.vertexCount > 6000, wire.name + " has the reference tube mesh");
            Require(mesh.vertices.All(vertex => float.IsFinite(vertex.x) && float.IsFinite(vertex.y) && float.IsFinite(vertex.z)),
                wire.name + " has finite mesh vertices");
            Require(wire.plugA.GetComponentsInChildren<Renderer>().All(renderer => !renderer.enabled) &&
                wire.plugB.GetComponentsInChildren<Renderer>().All(renderer => !renderer.enabled), wire.name + " old plug meshes hidden");
        }

        GameObject workspace = GameObject.Find("Bai2PracticalWorkspace_Canvas");
        Require(workspace != null && workspace.transform.Find("ModelPanel") != null, "Reference model panel exists");
        Require(workspace.transform.Find("WireCard") == null, "Old wire inventory panel removed");
        Require(Camera.main.rect.width < 0.8f && Camera.main.rect.height < 0.85f, "Camera fits the model panel");
        RectTransform navigation = GameObject.Find("StepNavigation_Canvas").transform.Find("StepNavigationBar") as RectTransform;
        Require(navigation != null && navigation.anchoredPosition.y <= -70f, "Navigation below status bar");
        File.AppendAllText(Result, "PASS step " + (step + 1) + ": selected cables only; all 15 connections preserved; no wiring popup/button; workspace/nav OK.\n");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Finish()
    {
        File.Delete(Request);
        stage = 0;
        waitingForCapture = false;
        if (SessionState.GetBool(StartedPlay, false))
        {
            SessionState.EraseBool(StartedPlay);
            EditorApplication.isPlaying = false;
        }
    }
}
