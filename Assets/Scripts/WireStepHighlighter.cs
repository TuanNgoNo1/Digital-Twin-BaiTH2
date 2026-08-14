using System.Collections.Generic;
using UnityEngine;

public sealed class WireStepHighlighter : MonoBehaviour
{
    private const int NoFocusedStep = -1;
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

    [Header("Pulse day thuoc buoc dang xem")]
    [SerializeField, Min(0.2f)]
    private float pulseDuration = 0.9f;
    [SerializeField, Range(1f, 1.75f)]
    private float focusedWidthScale = 1.35f;
    [SerializeField, Range(0.1f, 1f)]
    private float focusedMinimumAlpha = 0.68f;
    [SerializeField, Range(0.2f, 0.8f)]
    private float blackWirePeakValue = 0.45f;

    [Header("Day ngoai buoc dang xem")]
    [SerializeField, Range(0.05f, 0.8f)]
    private float dimmedAlpha = 0.24f;
    [SerializeField, Range(0.25f, 1f)]
    private float dimmedWidthScale = 0.72f;

    private readonly List<GameObject> stepRoots = new List<GameObject>();
    private readonly Dictionary<WireBody, WireVisualState> wireStates =
        new Dictionary<WireBody, WireVisualState>();

    private int focusedStepIndex = NoFocusedStep;
    private int expectedWireCount;
    private bool configured;
    private bool normalStateRestored;

    public int FocusedStepIndex => focusedStepIndex;
    public int CachedWireCount => wireStates.Count;
    public bool IsAnimating => configured && focusedStepIndex >= 0;

    private sealed class WireVisualState
    {
        public int StepIndex;
        public LineRenderer Line;
        public MaterialPropertyBlock PropertyBlock;
        public int MaterialColorProperty;
        public Color BaseColor;
        public Color BaseStartColor;
        public Color BaseEndColor;
        public float BaseStartWidth;
        public float BaseEndWidth;
    }

    public void Configure(IReadOnlyList<GameObject> roots)
    {
        RestoreAll();
        stepRoots.Clear();
        wireStates.Clear();
        expectedWireCount = 0;

        if (roots != null)
        {
            for (int i = 0; i < roots.Count; i++)
            {
                GameObject root = roots[i];
                stepRoots.Add(root);
                if (root != null)
                    expectedWireCount += root.GetComponentsInChildren<WireBody>(true).Length;
            }
        }

        focusedStepIndex = NoFocusedStep;
        configured = stepRoots.Count > 0;
        normalStateRestored = false;
        RefreshCache();
        RestoreAll();
    }

    public void SetFocusedStep(int stepIndex)
    {
        if (!configured)
            return;

        if (stepIndex < 0 || stepIndex >= stepRoots.Count)
        {
            ShowAllNormal();
            return;
        }

        focusedStepIndex = stepIndex;
        normalStateRestored = false;
        RefreshCache();
        ApplyFocusedVisuals();
    }

    public void ShowAllNormal()
    {
        focusedStepIndex = NoFocusedStep;
        RefreshCache();
        RestoreAll();
    }

    private void LateUpdate()
    {
        if (!configured)
            return;

        RefreshCache();
        if (focusedStepIndex < 0)
        {
            if (!normalStateRestored)
                RestoreAll();
            return;
        }

        ApplyFocusedVisuals();
    }

    private void OnDisable()
    {
        RestoreAll();
    }

    private void OnDestroy()
    {
        RestoreAll();
    }

    private void RefreshCache()
    {
        if (wireStates.Count >= expectedWireCount)
            return;

        for (int stepIndex = 0; stepIndex < stepRoots.Count; stepIndex++)
        {
            GameObject root = stepRoots[stepIndex];
            if (root == null)
                continue;

            WireBody[] wires = root.GetComponentsInChildren<WireBody>(true);
            foreach (WireBody wire in wires)
            {
                if (wire == null || wireStates.ContainsKey(wire))
                    continue;

                LineRenderer line = wire.GetComponent<LineRenderer>();
                if (line == null || line.sharedMaterial == null)
                    continue;

                Material material = line.sharedMaterial;
                int materialColorProperty = material.HasProperty(BaseColorProperty)
                    ? BaseColorProperty
                    : material.HasProperty(ColorProperty)
                        ? ColorProperty
                        : -1;
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                line.GetPropertyBlock(propertyBlock);

                wireStates.Add(
                    wire,
                    new WireVisualState
                    {
                        StepIndex = stepIndex,
                        Line = line,
                        PropertyBlock = propertyBlock,
                        MaterialColorProperty = materialColorProperty,
                        BaseColor = materialColorProperty >= 0
                            ? material.GetColor(materialColorProperty)
                            : line.startColor,
                        BaseStartColor = line.startColor,
                        BaseEndColor = line.endColor,
                        BaseStartWidth = line.startWidth,
                        BaseEndWidth = line.endWidth
                    });
            }
        }

        normalStateRestored = false;
    }

    private void ApplyFocusedVisuals()
    {
        float safeDuration = Mathf.Max(0.2f, pulseDuration);
        float radians = Time.unscaledTime * Mathf.PI * 2f / safeDuration;
        float pulse = (Mathf.Sin(radians) + 1f) * 0.5f;

        foreach (WireVisualState state in wireStates.Values)
        {
            if (state == null || state.Line == null)
                continue;

            if (state.StepIndex == focusedStepIndex)
            {
                float widthScale = Mathf.Lerp(1f, focusedWidthScale, pulse);
                SetWidth(state, widthScale);
                SetColor(state, CreateFocusedColor(state.BaseColor, pulse));
            }
            else
            {
                SetWidth(state, dimmedWidthScale);
                Color dimmedColor = state.BaseColor;
                dimmedColor.a *= dimmedAlpha;
                SetColor(state, dimmedColor);
            }
        }

        normalStateRestored = false;
    }

    private Color CreateFocusedColor(Color baseColor, float pulse)
    {
        Color focusedColor = baseColor;
        float maxChannel = Mathf.Max(baseColor.r, Mathf.Max(baseColor.g, baseColor.b));
        if (maxChannel < 0.2f)
        {
            float peakValue = Mathf.Max(maxChannel, blackWirePeakValue);
            float value = Mathf.Lerp(maxChannel, peakValue, pulse);
            if (maxChannel > 0.001f)
            {
                float scale = value / maxChannel;
                focusedColor.r = Mathf.Clamp01(baseColor.r * scale);
                focusedColor.g = Mathf.Clamp01(baseColor.g * scale);
                focusedColor.b = Mathf.Clamp01(baseColor.b * scale);
            }
            else
            {
                focusedColor.r = value;
                focusedColor.g = value;
                focusedColor.b = value;
            }
        }

        focusedColor.a = baseColor.a * Mathf.Lerp(focusedMinimumAlpha, 1f, pulse);
        return focusedColor;
    }

    private static void SetWidth(WireVisualState state, float scale)
    {
        state.Line.startWidth = state.BaseStartWidth * scale;
        state.Line.endWidth = state.BaseEndWidth * scale;
    }

    private static void SetColor(WireVisualState state, Color color)
    {
        if (state.MaterialColorProperty >= 0)
        {
            state.PropertyBlock.SetColor(state.MaterialColorProperty, color);
            state.Line.SetPropertyBlock(state.PropertyBlock);
            state.Line.startColor = state.BaseStartColor;
            state.Line.endColor = state.BaseEndColor;
            return;
        }

        state.Line.startColor = color;
        state.Line.endColor = color;
    }

    private void RestoreAll()
    {
        foreach (WireVisualState state in wireStates.Values)
        {
            if (state == null || state.Line == null)
                continue;

            state.Line.startWidth = state.BaseStartWidth;
            state.Line.endWidth = state.BaseEndWidth;
            state.Line.startColor = state.BaseStartColor;
            state.Line.endColor = state.BaseEndColor;

            if (state.MaterialColorProperty >= 0)
            {
                state.PropertyBlock.SetColor(state.MaterialColorProperty, state.BaseColor);
                state.Line.SetPropertyBlock(state.PropertyBlock);
            }
        }

        normalStateRestored = true;
    }
}
