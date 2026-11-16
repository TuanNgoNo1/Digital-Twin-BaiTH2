using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class WireBody : MonoBehaviour
{
    [Header("Hai dau day (Plugs)")]
    public WirePlug plugA;
    public WirePlug plugB;

    [Header("Muc tieu cam (Dap an)")]
    public string correctSocketA;
    public string correctSocketB;

    [Header("Giao dien day")]
    public WireColor wireColor = WireColor.Yellow;
    [Range(0.002f, 0.02f)]
    public float wireWidth = 0.005f;
    [Range(0f, 0.3f)]
    public float sagAmount = 0.05f;
    [Range(2, 20)]
    public int curveSegments = 8;

    [Header("Trang thai")]
    public bool isFullyConnected = false;
    public bool isCorrect = false;

    private bool wasCorrect = false;
    private LineRenderer lr;
    private Vector3 lastP0;
    private Vector3 lastP3;
    private MeshRenderer meshRend;
    private ProceduralConnectedWire connectedVisual;
    private readonly Dictionary<Renderer, bool> originalPlugVisibility = new Dictionary<Renderer, bool>();
    private Vector3 visualStart;
    private Vector3 visualEnd;
    private Vector3 visualCameraPosition;
    private Quaternion visualCameraRotation;
    private Vector2Int visualResolution;
    private Color presentationColor;
    private bool visualBuilt;
    private Material ownedWireMaterial;
    private const int WireOverlayQueue = 5000;
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    void Start()
    {
        RebindPlugs();

        meshRend = GetComponent<MeshRenderer>();
        if (meshRend != null)
            meshRend.enabled = false;

        lr = GetComponent<LineRenderer>();
        if (lr == null)
            lr = gameObject.AddComponent<LineRenderer>();

        lr.positionCount = curveSegments;
        lr.startWidth = wireWidth;
        lr.endWidth = wireWidth;
        lr.useWorldSpace = true;
        lr.numCapVertices = 3;
        lr.numCornerVertices = 3;
        lr.alignment = LineAlignment.View;
        lr.textureMode = LineTextureMode.Stretch;
        lr.shadowCastingMode = ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.sortingOrder = WireOverlayQueue;
        lr.enabled = plugA != null && plugB != null;

        SetWireMaterial();
        presentationColor = GetWireColor();
        ForceRefreshLine();
    }

    private void RebindPlugs()
    {
        if (plugA != null)
            plugA.parentWire = this;

        if (plugB != null)
            plugB.parentWire = this;

        bool hasDistinctTargets = !string.IsNullOrWhiteSpace(correctSocketA)
            && !string.IsNullOrWhiteSpace(correctSocketB)
            && !string.Equals(correctSocketA, correctSocketB, StringComparison.OrdinalIgnoreCase);
        bool duplicatePreferred = plugA != null
            && plugB != null
            && !string.IsNullOrWhiteSpace(plugA.preferredSocketID)
            && string.Equals(plugA.preferredSocketID, plugB.preferredSocketID, StringComparison.OrdinalIgnoreCase);

        if (hasDistinctTargets && duplicatePreferred)
        {
            plugA.preferredSocketID = correctSocketA;
            plugB.preferredSocketID = correctSocketB;
        }
    }

    void Update()
    {
        if (lr == null)
            return;

        if (plugA == null || plugB == null)
        {
            lr.enabled = false;
            if (connectedVisual != null)
                connectedVisual.gameObject.SetActive(false);
            SetLegacyPlugsVisible(true);
            return;
        }

        if (plugA.isSnapped && plugB.isSnapped)
        {
            UpdateConnectedVisual();
            lr.enabled = false;
            SetLegacyPlugsVisible(false);
            return;
        }

        if (connectedVisual != null)
            connectedVisual.gameObject.SetActive(false);
        SetLegacyPlugsVisible(true);
        if (!lr.enabled)
            lr.enabled = true;

        Vector3 p0 = plugA.transform.position;
        Vector3 p3 = plugB.transform.position;
        lastP0 = p0;
        lastP3 = p3;
        UpdateLinePositions();
    }

    private void UpdateConnectedVisual()
    {
        if (connectedVisual == null)
        {
            GameObject visual = new GameObject(name + "_ConnectedWire");
            visual.transform.SetParent(transform, false);
            connectedVisual = visual.AddComponent<ProceduralConnectedWire>();
            connectedVisual.Configure(this, GetWireColor());
        }

        Camera camera = Camera.main;
        Vector3 cameraPosition = camera != null ? camera.transform.position : Vector3.zero;
        Quaternion cameraRotation = camera != null ? camera.transform.rotation : Quaternion.identity;
        Vector2Int resolution = new Vector2Int(Screen.width, Screen.height);
        Vector3 start = plugA.transform.position;
        Vector3 end = plugB.transform.position;
        bool needsRebuild = !visualBuilt || !connectedVisual.gameObject.activeSelf ||
            (visualStart - start).sqrMagnitude > 0.00000001f ||
            (visualEnd - end).sqrMagnitude > 0.00000001f ||
            (visualCameraPosition - cameraPosition).sqrMagnitude > 0.00000001f ||
            Quaternion.Angle(visualCameraRotation, cameraRotation) > 0.01f || visualResolution != resolution;

        if (needsRebuild)
        {
            visualBuilt = connectedVisual.Rebuild();
            visualStart = start;
            visualEnd = end;
            visualCameraPosition = cameraPosition;
            visualCameraRotation = cameraRotation;
            visualResolution = resolution;
            connectedVisual.SetPresentationColor(presentationColor);
        }
        connectedVisual.gameObject.SetActive(visualBuilt);
    }

    public void SetConnectedPresentationColor(Color color)
    {
        presentationColor = color;
        if (connectedVisual != null)
            connectedVisual.SetPresentationColor(color);
    }

    private void SetLegacyPlugsVisible(bool visible)
    {
        if (!visible && originalPlugVisibility.Count == 0)
        {
            RememberPlugRenderers(plugA);
            RememberPlugRenderers(plugB);
        }
        foreach (KeyValuePair<Renderer, bool> entry in originalPlugVisibility)
        {
            if (entry.Key != null)
                entry.Key.enabled = visible && entry.Value;
        }
    }

    private void RememberPlugRenderers(WirePlug plug)
    {
        if (plug == null)
            return;
        foreach (Renderer renderer in plug.GetComponentsInChildren<Renderer>(true))
            originalPlugVisibility[renderer] = renderer.enabled;
    }

    private void OnDestroy()
    {
        SetLegacyPlugsVisible(true);
        if (ownedWireMaterial != null)
            Destroy(ownedWireMaterial);
    }

    void SetWireMaterial()
    {
        Color wireColorValue = GetWireColor();
        Material sourceMaterial = Resources.Load<Material>("WireOverlayMaterial");
        Material mat = sourceMaterial != null
            ? new Material(sourceMaterial)
            : CreateFallbackWireMaterial();

        mat.renderQueue = WireOverlayQueue;
        ownedWireMaterial = mat;
        if (mat.HasProperty(ColorProperty))
        {
            mat.SetColor(ColorProperty, wireColorValue);
            wireColorValue = Color.white;
        }

        lr.startColor = wireColorValue;
        lr.endColor = wireColorValue;
        lr.material = mat;
    }

    private Color GetWireColor()
    {
        switch (wireColor)
        {
            case WireColor.Yellow: return new Color(1f, 0.72f, 0.02f);
            case WireColor.Red: return new Color(0.82f, 0.025f, 0.035f);
            case WireColor.Black: return new Color(0.035f, 0.04f, 0.05f);
            case WireColor.Green: return new Color(0.02f, 0.55f, 0.14f);
            case WireColor.Blue: return new Color(0.05f, 0.25f, 0.85f);
            default: return Color.white;
        }
    }

    private Material CreateFallbackWireMaterial()
    {
        Shader shader = Shader.Find("DigitalTwin/WireOverlay");
        if (shader == null)
            shader = Shader.Find("UI/Default");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader);
        mat.renderQueue = WireOverlayQueue;
        mat.SetFloat("unity_GUIZTestMode", (float)CompareFunction.Always);
        return mat;
    }

    void UpdateLinePositions()
    {
        Vector3 mid = (lastP0 + lastP3) * 0.5f + Vector3.down * sagAmount;

        for (int i = 0; i < curveSegments; i++)
        {
            float t = (float)i / (curveSegments - 1);
            Vector3 pos = (1 - t) * (1 - t) * lastP0 + 2 * (1 - t) * t * mid + t * t * lastP3;
            lr.SetPosition(i, pos);
        }
    }

    private void ForceRefreshLine()
    {
        if (plugA == null || plugB == null || lr == null)
            return;

        lastP0 = plugA.transform.position;
        lastP3 = plugB.transform.position;
        UpdateLinePositions();
    }

    public void CheckConnection()
    {
        bool previousCorrect = wasCorrect;
        RefreshConnectionState();

        if (isFullyConnected)
        {
            string actual = GetSocketSummary();
            string expected = $"{correctSocketA}-{correctSocketB}";
            if (isCorrect)
                Debug.Log($"<color=green>✓ ĐÚNG: {name} → {actual}</color>");
            else
                Debug.LogWarning($"<color=red>✗ SAI SOCKET: {name} → {actual} (đúng: {expected})</color>");
        }

        if (isCorrect && !previousCorrect)
        {
            wasCorrect = true;
            if (CircuitManager.Instance != null)
                CircuitManager.Instance.OnWireConnectedCorrectly(this);
        }
        else if (!isCorrect && previousCorrect)
        {
            wasCorrect = false;
            if (CircuitManager.Instance != null)
                CircuitManager.Instance.EvaluateCircuit();
        }
        else if (isFullyConnected && !isCorrect)
        {
            if (CircuitManager.Instance != null)
                CircuitManager.Instance.EvaluateCircuit();
        }
    }

    public void RefreshConnectionState(bool logResult = false)
    {
        if (plugA == null || plugB == null)
        {
            isFullyConnected = false;
            isCorrect = false;
            return;
        }

        isFullyConnected = plugA.isSnapped && plugB.isSnapped;

        if (!isFullyConnected)
        {
            isCorrect = false;
        }
        else if (plugA.connectedSocket != null && plugB.connectedSocket != null)
        {
            string a = plugA.connectedSocket.socketID.Trim();
            string b = plugB.connectedSocket.socketID.Trim();
            string targetA = correctSocketA.Trim();
            string targetB = correctSocketB.Trim();

            isCorrect =
                (string.Equals(a, targetA, StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(b, targetB, StringComparison.OrdinalIgnoreCase)) ||
                (string.Equals(a, targetB, StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(b, targetA, StringComparison.OrdinalIgnoreCase));

            if (logResult)
                Debug.Log($"[WireBody] {name}: dang cam {a}-{b}, dap an {targetA}-{targetB}, correct={isCorrect}");
        }
        else
        {
            isCorrect = false;
            if (logResult)
                Debug.LogWarning($"[WireBody] {name}: da snap ca 2 dau nhung connectedSocket bi null. A={GetSocketId(plugA)}, B={GetSocketId(plugB)}");
        }
    }

    public string GetSocketSummary()
    {
        return $"{GetSocketId(plugA)}-{GetSocketId(plugB)}";
    }

    private static string GetSocketId(WirePlug plug)
    {
        if (plug == null)
            return "missing-plug";

        if (!plug.isSnapped)
            return "chua-snap";

        return plug.connectedSocket != null ? plug.connectedSocket.socketID : "null-socket";
    }
}
