using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(1000)]
public sealed class Bai2PracticalWorkspaceSkin : MonoBehaviour
{
    private const int WiringStepCount = 3;
    private const int NavigationStepCount = 4;
    private const int HmiStepIndex = 3;
    private const int WireRowCount = 6;

    private static readonly string[] NavigationLabels =
    {
        "1. \u0110\u1EA5u n\u1ED1i m\u1EA1ch \u0111i\u1EC1u khi\u1EC3n \u0111\u1ED9ng c\u01A1",
        "2. \u0110\u1EA5u n\u1ED1i encoder",
        "3. \u0110\u1EA5u n\u1ED1i m\u1EA1ch l\u1EF1c",
        "4. V\u1EADn h\u00E0nh"
    };

    private CircuitManager manager;
    private Camera mainCamera;
    private ResponsiveCameraFraming cameraFraming;
    private Vector3 originalCameraPosition;
    private float originalDesignAspect;
    private float originalDesignVerticalFov;
    private bool cameraStateCaptured;
    private bool practicalCameraApplied;

    private GameObject workspaceRoot;
    private GameObject workspaceMaskRoot;
    private GameObject guideReturnRoot;
    private TextMeshProUGUI instructionText;
    private TextMeshProUGUI wireGuideText;
    private readonly List<GameObject> wireRows = new List<GameObject>();
    private readonly List<TextMeshProUGUI> wireRowNumbers = new List<TextMeshProUGUI>();
    private readonly List<Image> wireRowSurfaces = new List<Image>();
    private readonly List<Image> wireRowLines = new List<Image>();
    private readonly List<Image> wireRowPlugs = new List<Image>();

    private RectTransform navigationPanel;
    private readonly List<Button> navigationButtons = new List<Button>();
    private readonly List<Image> navigationBorders = new List<Image>();
    private readonly List<Image> navigationShadows = new List<Image>();
    private readonly List<TextMeshProUGUI> navigationTexts = new List<TextMeshProUGUI>();

    private int lastVisibleStep = -1;
    private int lastScreenWidth;
    private int lastScreenHeight;
    private bool initialized;
    private static Sprite roundedRectangleSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AttachAfterInitialSceneLoad()
    {
        AttachToCircuitManager();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachToCircuitManager();
    }

    private static void AttachToCircuitManager()
    {
        CircuitManager circuitManager = FindFirstObjectByType<CircuitManager>(FindObjectsInactive.Include);
        if (circuitManager != null && circuitManager.GetComponent<Bai2PracticalWorkspaceSkin>() == null)
            circuitManager.gameObject.AddComponent<Bai2PracticalWorkspaceSkin>();
    }

    private IEnumerator Start()
    {
        manager = GetComponent<CircuitManager>();
        for (int frame = 0; frame < 180; frame++)
        {
            if (manager != null && manager.stepRoots != null && manager.stepRoots.Count >= WiringStepCount)
                break;

            yield return null;
        }

        if (manager == null || manager.stepRoots == null || manager.stepRoots.Count < WiringStepCount)
        {
            Debug.LogWarning("[Bai2 UI] CircuitManager is not ready; the matching Bai 1 skin was not created.");
            yield break;
        }

        CaptureCameraState();
        HideLegacyWorkspaceUi();
        CreateWorkspace();
        StyleExistingNavigation();
        initialized = true;
        Refresh(true);
    }

    private void LateUpdate()
    {
        if (!initialized || manager == null)
            return;

        bool screenChanged = lastScreenWidth != Screen.width || lastScreenHeight != Screen.height;
        bool stepChanged = lastVisibleStep != manager.VisibleStepIndex;
        Refresh(stepChanged || screenChanged);
    }

    private void OnDestroy()
    {
        RestoreCameraState();
    }

    private void Refresh(bool rebuildStepContent)
    {
        int visibleStep = manager.VisibleStepIndex;
        bool showWorkspace = visibleStep >= 0 && visibleStep < HmiStepIndex;

        if (workspaceRoot != null && workspaceRoot.activeSelf != showWorkspace)
            workspaceRoot.SetActive(showWorkspace);
        if (workspaceMaskRoot != null && workspaceMaskRoot.activeSelf != showWorkspace)
            workspaceMaskRoot.SetActive(showWorkspace);

        if (guideReturnRoot == null)
            guideReturnRoot = GameObject.Find("GuideReturn_Canvas");
        if (guideReturnRoot != null && guideReturnRoot.activeSelf)
            guideReturnRoot.SetActive(false);

        ApplyCameraState(showWorkspace);
        StyleNavigation(visibleStep);

        if (showWorkspace && rebuildStepContent)
            UpdateWorkspaceContent(visibleStep);

        lastVisibleStep = visibleStep;
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }

    private void CaptureCameraState()
    {
        mainCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (mainCamera == null)
            return;

        cameraFraming = mainCamera.GetComponent<ResponsiveCameraFraming>();
        if (cameraFraming == null)
            cameraFraming = mainCamera.gameObject.AddComponent<ResponsiveCameraFraming>();

        originalCameraPosition = mainCamera.transform.position;
        originalDesignAspect = cameraFraming.designAspect;
        originalDesignVerticalFov = cameraFraming.designVerticalFov;
        cameraStateCaptured = true;
    }

    private void ApplyCameraState(bool practicalView)
    {
        if (!cameraStateCaptured || mainCamera == null || cameraFraming == null)
            return;

        if (practicalView)
        {
            cameraFraming.designAspect = 16f / 9f;
            cameraFraming.designVerticalFov = 56f;
            cameraFraming.ApplyFraming();

            if (TryCalculateBoardBounds(out Bounds boardBounds))
                FrameCameraOnBoard(boardBounds);
            else
                mainCamera.transform.position = originalCameraPosition + mainCamera.transform.forward * 0.52f;

            practicalCameraApplied = true;
        }
        else
        {
            RestoreCameraState();
        }

        cameraFraming.ApplyFraming();
    }

    private bool TryCalculateBoardBounds(out Bounds bounds)
    {
        bounds = default;
        bool found = false;

#if UNITY_2023_1_OR_NEWER
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        Renderer[] renderers = FindObjectsOfType<Renderer>();
#endif
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            string rendererName = renderer.name;
            bool isMainBoard = rendererName.Equals("Board", StringComparison.OrdinalIgnoreCase);
            if (!isMainBoard && renderer.transform.parent != null)
                isMainBoard = renderer.transform.parent.name.Equals("Board", StringComparison.OrdinalIgnoreCase);
            if (!isMainBoard)
                continue;

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (found)
            return true;

        // FBX variants may suffix the board mesh name. Keep the fallback narrow so
        // the horizontal table is not included in the framing target.
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            string rendererName = renderer.name;
            if (rendererName.IndexOf("Board", StringComparison.OrdinalIgnoreCase) < 0 ||
                rendererName.IndexOf("Table", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                continue;
            }

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    private void FrameCameraOnBoard(Bounds boardBounds)
    {
        Vector3 forward = mainCamera.transform.forward.normalized;
        Vector3 right = mainCamera.transform.right.normalized;
        Vector3 up = mainCamera.transform.up.normalized;
        Vector3 extents = boardBounds.extents;

        float halfWidth = Mathf.Abs(right.x) * extents.x + Mathf.Abs(right.y) * extents.y + Mathf.Abs(right.z) * extents.z;
        float halfHeight = Mathf.Abs(up.x) * extents.x + Mathf.Abs(up.y) * extents.y + Mathf.Abs(up.z) * extents.z;
        float halfDepth = Mathf.Abs(forward.x) * extents.x + Mathf.Abs(forward.y) * extents.y + Mathf.Abs(forward.z) * extents.z;

        float verticalFov = Mathf.Clamp(mainCamera.fieldOfView, 1f, 170f) * Mathf.Deg2Rad;
        float aspect = mainCamera.pixelHeight > 0
            ? Mathf.Max(0.1f, (float)mainCamera.pixelWidth / mainCamera.pixelHeight)
            : Mathf.Max(0.1f, (float)Screen.width / Mathf.Max(1, Screen.height));
        float horizontalFov = 2f * Mathf.Atan(Mathf.Tan(verticalFov * 0.5f) * aspect);

        // The Board occupies the central workspace between the two side cards.
        // These fractions keep it clear of both cards while making it larger than
        // the previous fixed-offset WebGL framing.
        float verticalDistance = halfHeight / Mathf.Max(0.01f, Mathf.Tan(verticalFov * 0.5f) * 0.66f);
        float horizontalDistance = halfWidth / Mathf.Max(0.01f, Mathf.Tan(horizontalFov * 0.5f) * 0.44f);
        float distance = Mathf.Max(verticalDistance, horizontalDistance) + halfDepth;

        mainCamera.transform.position = boardBounds.center - forward * distance;
    }

    private void RestoreCameraState()
    {
        if (!cameraStateCaptured || !practicalCameraApplied || mainCamera == null || cameraFraming == null)
            return;

        mainCamera.transform.position = originalCameraPosition;
        cameraFraming.designAspect = originalDesignAspect;
        cameraFraming.designVerticalFov = originalDesignVerticalFov;
        cameraFraming.ApplyFraming();
        practicalCameraApplied = false;
    }

    private void HideLegacyWorkspaceUi()
    {
        foreach (string objectName in new[] { "bd-Photoroom", "bhd (1)" })
        {
            GameObject legacyPanel = GameObject.Find(objectName);
            if (legacyPanel != null)
                legacyPanel.SetActive(false);
        }

        if (manager.guideRoots != null)
        {
            foreach (GameObject guideRoot in manager.guideRoots)
            {
                if (guideRoot == null)
                    continue;

                Transform instructionPanel = guideRoot.transform.Find("InstructionPanel");
                if (instructionPanel != null)
                    instructionPanel.gameObject.SetActive(false);
            }
        }

        foreach (GameObject stepRoot in manager.stepRoots)
        {
            if (stepRoot == null)
                continue;

            foreach (Transform child in stepRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Equals("StepUI", StringComparison.OrdinalIgnoreCase))
                    child.gameObject.SetActive(false);
            }

            foreach (TextMeshProUGUI label in stepRoot.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (label.name.StartsWith("D\u00E2y ", StringComparison.OrdinalIgnoreCase))
                    label.gameObject.SetActive(false);
            }
        }

        BoardStepHeading boardHeading = FindFirstObjectByType<BoardStepHeading>(FindObjectsInactive.Include);
        if (boardHeading != null)
            boardHeading.gameObject.SetActive(false);

        guideReturnRoot = GameObject.Find("GuideReturn_Canvas");
        if (guideReturnRoot != null)
            guideReturnRoot.SetActive(false);
    }

    private void CreateWorkspace()
    {
        Color pageBackground = new Color(0.965f, 0.965f, 0.965f, 1f);
        Color cardBorder = new Color(0.84f, 0.85f, 0.87f, 1f);
        Color cardShadow = new Color(0.12f, 0.14f, 0.17f, 0.12f);
        Color textColor = new Color(0.25f, 0.28f, 0.33f, 1f);
        Color red = new Color(0.82f, 0.12f, 0.15f, 1f);

        workspaceRoot = CreateOverlayCanvas("Bai2PracticalWorkspace_Canvas", 100, false);

        GameObject leftBackdrop = CreateImage(workspaceRoot.transform, "LeftBackdrop", pageBackground);
        SetTopLeftRect(leftBackdrop.GetComponent<RectTransform>(), new Vector2(510f, 828f), new Vector2(0f, 120f));

        GameObject rightBackdrop = CreateImage(workspaceRoot.transform, "RightBackdrop", pageBackground);
        SetTopLeftRect(rightBackdrop.GetComponent<RectTransform>(), new Vector2(445f, 828f), new Vector2(1475f, 120f));

        GameObject guideCard = CreateWorkspaceCard(
            workspaceRoot.transform,
            "GuideCard",
            new Vector2(52f, 144f),
            new Vector2(442f, 544f),
            cardBorder,
            cardShadow);

        CreateDocumentIcon(guideCard.transform, new Vector2(44f, 43f), red);
        TextMeshProUGUI guideHeading = CreateText(
            guideCard.transform,
            "Heading",
            "H\u01B0\u1EDBng d\u1EABn",
            36f,
            FontStyles.Bold,
            new Color(0.16f, 0.17f, 0.19f, 1f),
            TextAlignmentOptions.MidlineLeft);
        SetTopLeftRect(guideHeading.rectTransform, new Vector2(300f, 58f), new Vector2(86f, 31f));

        GameObject guideDivider = CreateImage(guideCard.transform, "Divider", new Color(0.87f, 0.88f, 0.9f, 1f));
        SetTopLeftRect(guideDivider.GetComponent<RectTransform>(), new Vector2(354f, 2f), new Vector2(44f, 94f));

        instructionText = CreateText(
            guideCard.transform,
            "Instruction",
            string.Empty,
            29f,
            FontStyles.Normal,
            new Color(0.34f, 0.38f, 0.44f, 1f),
            TextAlignmentOptions.TopLeft);
        instructionText.textWrappingMode = TextWrappingModes.Normal;
        instructionText.lineSpacing = 5f;
        SetTopLeftRect(instructionText.rectTransform, new Vector2(354f, 130f), new Vector2(44f, 122f));

        wireGuideText = CreateText(
            guideCard.transform,
            "WireGuide",
            string.Empty,
            27f,
            FontStyles.Normal,
            textColor,
            TextAlignmentOptions.TopLeft);
        wireGuideText.textWrappingMode = TextWrappingModes.NoWrap;
        wireGuideText.overflowMode = TextOverflowModes.Overflow;
        wireGuideText.lineSpacing = 10f;
        SetTopLeftRect(wireGuideText.rectTransform, new Vector2(342f, 250f), new Vector2(58f, 283f));

        GameObject wireCard = CreateWorkspaceCard(
            workspaceRoot.transform,
            "WireCard",
            new Vector2(1492f, 144f),
            new Vector2(381f, 804f),
            cardBorder,
            cardShadow);

        CreateWireIcon(wireCard.transform, new Vector2(44f, 47f), red);
        TextMeshProUGUI wireHeading = CreateText(
            wireCard.transform,
            "Heading",
            "B\u1ED9 d\u00E2y",
            36f,
            FontStyles.Bold,
            new Color(0.16f, 0.17f, 0.19f, 1f),
            TextAlignmentOptions.MidlineLeft);
        SetTopLeftRect(wireHeading.rectTransform, new Vector2(230f, 58f), new Vector2(86f, 31f));

        GameObject wireDivider = CreateImage(wireCard.transform, "Divider", new Color(0.87f, 0.88f, 0.9f, 1f));
        SetTopLeftRect(wireDivider.GetComponent<RectTransform>(), new Vector2(292f, 2f), new Vector2(44f, 94f));

        for (int i = 0; i < WireRowCount; i++)
            CreateWireRow(wireCard.transform, i);

        workspaceMaskRoot = CreateOverlayCanvas("Bai2PracticalWorkspaceMasks_Canvas", 4800, false);
        GameObject topMask = CreateImage(workspaceMaskRoot.transform, "TopMask", pageBackground);
        SetTopLeftRect(topMask.GetComponent<RectTransform>(), new Vector2(1920f, 144f), Vector2.zero);

        GameObject bottomMask = CreateImage(workspaceMaskRoot.transform, "BottomMask", pageBackground);
        SetTopLeftRect(bottomMask.GetComponent<RectTransform>(), new Vector2(1920f, 132f), new Vector2(0f, 948f));
    }

    private void CreateWireRow(Transform parent, int index)
    {
        GameObject row = CreateImage(parent, $"WireRow_{index + 1}", new Color(0.99f, 0.98f, 0.98f, 0.92f));
        Image rowImage = row.GetComponent<Image>();
        rowImage.sprite = GetRoundedRectangleSprite();
        rowImage.type = Image.Type.Sliced;
        SetTopLeftRect(row.GetComponent<RectTransform>(), new Vector2(292f, 99f), new Vector2(44f, 117f + index * 112f));

        GameObject border = CreateImage(row.transform, "Border", new Color(0.86f, 0.87f, 0.89f, 1f));
        Image borderImage = border.GetComponent<Image>();
        borderImage.sprite = GetRoundedRectangleSprite();
        borderImage.type = Image.Type.Sliced;
        StretchRect(border.GetComponent<RectTransform>());
        border.transform.SetAsFirstSibling();

        GameObject surface = CreateImage(row.transform, "Surface", rowImage.color);
        Image surfaceImage = surface.GetComponent<Image>();
        surfaceImage.sprite = GetRoundedRectangleSprite();
        surfaceImage.type = Image.Type.Sliced;
        SetTopLeftRect(surface.GetComponent<RectTransform>(), new Vector2(288f, 95f), new Vector2(2f, 2f));

        TextMeshProUGUI number = CreateText(
            row.transform,
            "Number",
            (index + 1).ToString(),
            31f,
            FontStyles.Normal,
            new Color(0.4f, 0.43f, 0.48f, 1f),
            TextAlignmentOptions.Center);
        SetTopLeftRect(number.rectTransform, new Vector2(52f, 99f), new Vector2(10f, 0f));

        GameObject lineObject = CreateImage(row.transform, "WireLine", Color.red);
        Image line = lineObject.GetComponent<Image>();
        line.sprite = GetRoundedRectangleSprite();
        line.type = Image.Type.Sliced;
        SetTopLeftRect(lineObject.GetComponent<RectTransform>(), new Vector2(180f, 6f), new Vector2(82f, 47f));

        GameObject plugAObject = CreateImage(row.transform, "PlugA", Color.red);
        Image plugA = plugAObject.GetComponent<Image>();
        plugA.sprite = GetRoundedRectangleSprite();
        plugA.type = Image.Type.Sliced;
        SetTopLeftRect(plugAObject.GetComponent<RectTransform>(), new Vector2(14f, 12f), new Vector2(76f, 44f));

        GameObject plugBObject = CreateImage(row.transform, "PlugB", Color.red);
        Image plugB = plugBObject.GetComponent<Image>();
        plugB.sprite = GetRoundedRectangleSprite();
        plugB.type = Image.Type.Sliced;
        SetTopLeftRect(plugBObject.GetComponent<RectTransform>(), new Vector2(14f, 12f), new Vector2(256f, 44f));

        wireRows.Add(row);
        wireRowNumbers.Add(number);
        wireRowSurfaces.Add(surfaceImage);
        wireRowLines.Add(line);
        wireRowPlugs.Add(plugA);
        wireRowPlugs.Add(plugB);
    }

    private void UpdateWorkspaceContent(int stepIndex)
    {
        instructionText.text = manager.IsCompletedReviewMode
            ? "Quan s\u00E1t c\u00E1c d\u00E2y \u0111\u00E3 n\u1ED1i\ntr\u00EAn b\u1EA3ng \u0111i\u1EC1u khi\u1EC3n:\n"
            : "K\u00E9o th\u1EA3 c\u00E1c \u0111\u1EA7u d\u00E2y n\u1ED1i\nv\u00E0o c\u00E1c gi\u1EAFc c\u1EAFm tr\u00EAn\nb\u1EA3ng \u0111i\u1EC1u khi\u1EC3n:";

        List<WireBody> wires = GetStepWires(stepIndex);
        wireGuideText.text = string.Join(
            "\n",
            wires.Select(wire =>
                $"<color=#{ColorUtility.ToHtmlStringRGB(GetGuideTextColor(wire, stepIndex))}>" +
                $"\u2022 {GetWireDisplayName(wire)}: {wire.correctSocketA} \u2192 {wire.correctSocketB}</color>"));

        for (int i = 0; i < wireRows.Count; i++)
        {
            bool hasWire = i < wires.Count;
            wireRows[i].SetActive(hasWire);
            if (!hasWire)
                continue;

            WireBody wire = wires[i];
            Color wireColor = GetWireTextColor(wire);
            wireRowNumbers[i].text = GetWireNumber(wire).ToString();
            wireRowSurfaces[i].color = GetWireRowColor(wire);
            wireRowLines[i].color = wireColor;
            wireRowPlugs[i * 2].color = wireColor;
            wireRowPlugs[i * 2 + 1].color = wireColor;
        }
    }

    private List<WireBody> GetStepWires(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= manager.stepRoots.Count || manager.stepRoots[stepIndex] == null)
            return new List<WireBody>();

        return manager.stepRoots[stepIndex]
            .GetComponentsInChildren<WireBody>(true)
            .Where(wire => wire != null &&
                !string.IsNullOrWhiteSpace(wire.correctSocketA) &&
                !string.IsNullOrWhiteSpace(wire.correctSocketB))
            .OrderBy(GetWireNumber)
            .ThenBy(wire => wire.name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void StyleExistingNavigation()
    {
        GameObject navigationRoot = GameObject.Find("StepNavigation_Canvas");
        if (navigationRoot == null)
            return;

        Canvas canvas = navigationRoot.GetComponent<Canvas>();
        if (canvas != null)
            canvas.sortingOrder = 4900;

        Transform panelTransform = navigationRoot.transform.Find("StepNavigationBar");
        if (panelTransform == null)
            return;

        navigationPanel = panelTransform as RectTransform;
        SetTopLeftRect(navigationPanel, new Vector2(1823f, 69f), new Vector2(50f, 52f));

        Transform connector = panelTransform.Find("Bai1StyleConnector");
        if (connector == null)
        {
            GameObject connectorObject = CreateImage(panelTransform, "Bai1StyleConnector", new Color(0.72f, 0.76f, 0.81f, 1f));
            connectorObject.transform.SetAsFirstSibling();
            SetTopLeftRect(connectorObject.GetComponent<RectTransform>(), new Vector2(1133f, 3f), new Vector2(475f, 33f));
        }

        float[] buttonX = { 0f, 662f, 1142f, 1608f };
        float[] buttonWidth = { 475f, 293f, 279f, 215f };

        navigationButtons.Clear();
        navigationBorders.Clear();
        navigationShadows.Clear();
        navigationTexts.Clear();

        for (int i = 0; i < NavigationStepCount; i++)
        {
            Transform buttonTransform = panelTransform.Find($"Step_{i + 1}_Button");
            Transform borderTransform = panelTransform.Find($"Step_{i + 1}_Border");
            Transform shadowTransform = panelTransform.Find($"Step_{i + 1}_Shadow");
            if (buttonTransform == null || borderTransform == null || shadowTransform == null)
                continue;

            Vector2 size = new Vector2(buttonWidth[i], 69f);
            Vector2 position = new Vector2(buttonX[i], 0f);
            SetTopLeftRect(buttonTransform as RectTransform, size, position);
            SetTopLeftRect(borderTransform as RectTransform, size + new Vector2(4f, 4f), position - new Vector2(2f, 2f));
            SetTopLeftRect(shadowTransform as RectTransform, size, position + new Vector2(0f, 4f));

            Transform icon = buttonTransform.Find("Icon");
            if (icon != null)
                icon.gameObject.SetActive(false);

            Transform description = buttonTransform.Find("Description");
            if (description != null)
                description.gameObject.SetActive(false);

            TextMeshProUGUI label = buttonTransform.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.gameObject.SetActive(true);
                label.text = NavigationLabels[i];
                label.fontStyle = FontStyles.Normal;
                label.fontSize = 25f;
                label.enableAutoSizing = true;
                label.fontSizeMin = 18f;
                label.fontSizeMax = 25f;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.alignment = TextAlignmentOptions.Center;
                SetTopLeftRect(label.rectTransform, new Vector2(size.x - 24f, size.y), new Vector2(12f, 0f));
            }

            navigationButtons.Add(buttonTransform.GetComponent<Button>());
            navigationBorders.Add(borderTransform.GetComponent<Image>());
            navigationShadows.Add(shadowTransform.GetComponent<Image>());
            navigationTexts.Add(label);
        }
    }

    private void StyleNavigation(int visibleStep)
    {
        if (navigationButtons.Count != NavigationStepCount)
            StyleExistingNavigation();

        Color selectedText = new Color(0.82f, 0.12f, 0.15f, 1f);
        Color normalText = new Color(0.38f, 0.41f, 0.46f, 1f);
        Color lockedText = new Color(0.55f, 0.58f, 0.63f, 1f);
        Color normalBorder = new Color(0.84f, 0.86f, 0.89f, 1f);
        Color selectedBorder = new Color(0.9f, 0.12f, 0.16f, 1f);
        Color lockedBorder = new Color(0.88f, 0.89f, 0.91f, 1f);

        for (int i = 0; i < navigationButtons.Count; i++)
        {
            bool unlocked = manager.IsCompletedReviewMode || manager.IsSystemUnlocked || i <= manager.CurrentWiringStepIndex;
            bool selected = i == visibleStep;
            Button button = navigationButtons[i];
            if (button != null)
            {
                button.interactable = unlocked;
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 0.94f, 0.95f, 1f);
                colors.pressedColor = new Color(1f, 0.9f, 0.91f, 1f);
                colors.selectedColor = Color.white;
                colors.disabledColor = new Color(0.99f, 0.99f, 0.99f, 1f);
                colors.fadeDuration = 0.08f;
                button.colors = colors;
                if (button.targetGraphic != null)
                    button.targetGraphic.color = Color.white;
            }

            if (i < navigationBorders.Count && navigationBorders[i] != null)
                navigationBorders[i].color = !unlocked ? lockedBorder : selected ? selectedBorder : normalBorder;
            if (i < navigationShadows.Count && navigationShadows[i] != null)
                navigationShadows[i].color = selected
                    ? new Color(0.5f, 0.08f, 0.1f, 0.14f)
                    : new Color(0.12f, 0.15f, 0.2f, 0.1f);
            if (i < navigationTexts.Count && navigationTexts[i] != null)
            {
                navigationTexts[i].gameObject.SetActive(true);
                navigationTexts[i].text = NavigationLabels[i];
                navigationTexts[i].color = !unlocked ? lockedText : selected ? selectedText : normalText;
            }
        }
    }

    private static GameObject CreateOverlayCanvas(string objectName, int sortingOrder, bool raycast)
    {
        Type[] componentTypes = raycast
            ? new[] { typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster) }
            : new[] { typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler) };
        GameObject root = new GameObject(objectName, componentTypes);
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return root;
    }

    private static GameObject CreateWorkspaceCard(
        Transform parent,
        string objectName,
        Vector2 position,
        Vector2 size,
        Color borderColor,
        Color shadowColor)
    {
        GameObject shadow = CreateImage(parent, objectName + "_Shadow", shadowColor);
        Image shadowImage = shadow.GetComponent<Image>();
        shadowImage.sprite = GetRoundedRectangleSprite();
        shadowImage.type = Image.Type.Sliced;
        SetTopLeftRect(shadow.GetComponent<RectTransform>(), size, position + new Vector2(4f, 5f));

        GameObject border = CreateImage(parent, objectName + "_Border", borderColor);
        Image borderImage = border.GetComponent<Image>();
        borderImage.sprite = GetRoundedRectangleSprite();
        borderImage.type = Image.Type.Sliced;
        SetTopLeftRect(border.GetComponent<RectTransform>(), size + new Vector2(4f, 4f), position - new Vector2(2f, 2f));

        GameObject card = CreateImage(parent, objectName, Color.white);
        Image cardImage = card.GetComponent<Image>();
        cardImage.sprite = GetRoundedRectangleSprite();
        cardImage.type = Image.Type.Sliced;
        SetTopLeftRect(card.GetComponent<RectTransform>(), size, position);
        return card;
    }

    private static void CreateDocumentIcon(Transform parent, Vector2 position, Color color)
    {
        GameObject outline = CreateImage(parent, "GuideIcon", color);
        SetTopLeftRect(outline.GetComponent<RectTransform>(), new Vector2(25f, 31f), position);

        GameObject paper = CreateImage(outline.transform, "Paper", Color.white);
        SetTopLeftRect(paper.GetComponent<RectTransform>(), new Vector2(19f, 25f), new Vector2(3f, 3f));

        for (int i = 0; i < 2; i++)
        {
            GameObject line = CreateImage(outline.transform, $"Line_{i + 1}", color);
            SetTopLeftRect(line.GetComponent<RectTransform>(), new Vector2(11f, 2f), new Vector2(7f, 11f + i * 6f));
        }
    }

    private static void CreateWireIcon(Transform parent, Vector2 position, Color color)
    {
        GameObject line = CreateImage(parent, "WireIcon_Line", color);
        SetTopLeftRect(line.GetComponent<RectTransform>(), new Vector2(27f, 3f), position + new Vector2(0f, 12f));

        for (int i = 0; i < 2; i++)
        {
            GameObject plug = CreateImage(parent, $"WireIcon_Plug_{i + 1}", color);
            SetTopLeftRect(
                plug.GetComponent<RectTransform>(),
                new Vector2(7f, 11f),
                position + new Vector2(i == 0 ? -2f : 22f, 8f));
        }
    }

    private static GameObject CreateImage(Transform parent, string objectName, Color color)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        Image image = gameObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return gameObject;
    }

    private static TextMeshProUGUI CreateText(
        Transform parent,
        string objectName,
        string value,
        float fontSize,
        FontStyles fontStyle,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        gameObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static void StretchRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private static void SetTopLeftRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(position.x, -position.y);
        rect.sizeDelta = size;
    }

    private static Sprite GetRoundedRectangleSprite()
    {
        if (roundedRectangleSprite != null)
            return roundedRectangleSprite;

        const int size = 64;
        const int radius = 10;
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false)
        {
            name = "Bai2RuntimeRoundedRectangle",
            hideFlags = HideFlags.HideAndDontSave
        };

        Color[] pixels = new Color[size * size];
        Color clear = new Color(1f, 1f, 1f, 0f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float left = radius;
                float right = size - radius;
                float bottom = radius;
                float top = size - radius;
                bool inside = (px >= left && px <= right) || (py >= bottom && py <= top);
                if (!inside)
                {
                    float centerX = px < left ? left : right;
                    float centerY = py < bottom ? bottom : top;
                    float dx = px - centerX;
                    float dy = py - centerY;
                    inside = dx * dx + dy * dy <= radius * radius;
                }

                pixels[y * size + x] = inside ? Color.white : clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        roundedRectangleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));
        roundedRectangleSprite.hideFlags = HideFlags.HideAndDontSave;
        return roundedRectangleSprite;
    }

    private static int GetWireNumber(WireBody wire)
    {
        if (wire == null)
            return 0;

        string source = wire.name;
        int markerIndex = source.IndexOf("Wire_", StringComparison.OrdinalIgnoreCase);
        int start = markerIndex >= 0 ? markerIndex + 5 : 0;
        int end = start;
        while (end < source.Length && char.IsDigit(source[end]))
            end++;

        return end > start && int.TryParse(source.Substring(start, end - start), out int number)
            ? number
            : 0;
    }

    private static string GetWireDisplayName(WireBody wire)
    {
        int number = GetWireNumber(wire);
        return number > 0 ? $"D\u00E2y {number}" : wire.name;
    }

    private static Color GetGuideTextColor(WireBody wire, int stepIndex)
    {
        int wireNumber = GetWireNumber(wire);
        if (stepIndex == 0 && wireNumber >= 1 && wireNumber <= 2)
            return new Color(0.82f, 0.12f, 0.15f, 1f);

        return GetWireTextColor(wire);
    }

    private static Color GetWireTextColor(WireBody wire)
    {
        WireColor color = wire != null && wire.plugA != null ? wire.plugA.wireColor : WireColor.Any;
        switch (color)
        {
            case WireColor.Red:
                return new Color(0.9f, 0.06f, 0.08f, 1f);
            case WireColor.Yellow:
                return new Color(1f, 0.64f, 0.02f, 1f);
            case WireColor.Green:
                return new Color(0.13f, 0.55f, 0.32f, 1f);
            case WireColor.Blue:
                return new Color(0.12f, 0.38f, 0.72f, 1f);
            default:
                return new Color(0.08f, 0.09f, 0.11f, 1f);
        }
    }

    private static Color GetWireRowColor(WireBody wire)
    {
        WireColor color = wire != null && wire.plugA != null ? wire.plugA.wireColor : WireColor.Any;
        switch (color)
        {
            case WireColor.Red:
                return new Color(1f, 0.965f, 0.97f, 0.94f);
            case WireColor.Yellow:
                return new Color(1f, 0.99f, 0.94f, 0.94f);
            case WireColor.Green:
                return new Color(0.95f, 0.99f, 0.96f, 0.94f);
            case WireColor.Blue:
                return new Color(0.95f, 0.98f, 1f, 0.94f);
            default:
                return new Color(0.97f, 0.975f, 0.98f, 0.94f);
        }
    }
}
