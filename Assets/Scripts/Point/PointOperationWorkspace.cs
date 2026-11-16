using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PDTwin.RuntimeUI;

namespace PDTwin.Point
{
    // Present the live apparatus and telemetry on the same foreground layer as the tests.
    [DefaultExecutionOrder(1100)]
    public sealed class PointOperationWorkspace : MonoBehaviour
    {
        private GameObject overlay;
        private RectTransform drawer;
        private RectTransform workspace;
        private RawImage modelView;
        private Camera previewCamera;
        private RenderTexture previewTexture;
        private GameObject backgroundRoot;
        private Texture2D gradientTexture;
        private RectTransform motorStatus;
        private TextMeshProUGUI connectionValue, angleValue, rotationsValue, speedValue;
        private PanelPose hmiPose;
        private PanelPose motorPose;
        private bool showing;
        private PLCController_v2 plc;
        private Vector2 lastCanvasSize;
        private float lastDrawerWidth;

        private sealed class PanelPose
        {
            public RectTransform rect;
            private Transform parent;
            private int sibling;
            private Vector2 min, max, pivot, position, size;
            private Vector3 scale;
            private Quaternion rotation;

            public PanelPose(RectTransform target)
            {
                rect = target;
                parent = target.parent;
                sibling = target.GetSiblingIndex();
                min = target.anchorMin; max = target.anchorMax; pivot = target.pivot;
                position = target.anchoredPosition; size = target.sizeDelta;
                scale = target.localScale; rotation = target.localRotation;
            }

            public void Restore()
            {
                if (rect == null || parent == null) return;
                rect.SetParent(parent, false);
                rect.SetSiblingIndex(sibling);
                rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot;
                rect.anchoredPosition = position; rect.sizeDelta = size;
                rect.localScale = scale; rect.localRotation = rotation;
            }
        }

        public void Configure(GameObject drawerOverlay, RectTransform drawerRect)
        {
            overlay = drawerOverlay;
            drawer = drawerRect;
        }

        private void LateUpdate()
        {
            CircuitManager manager = CircuitManager.Instance;
            bool requested = manager != null && manager.VisibleStepIndex == 3;
            if (!requested) { Hide(); return; }
            plc = PLCController_v2.Instance;
            if (plc == null || plc.OperationHmiPanel == null || Camera.main == null) return;
            if (!showing) Show();
            UpdateStatus();
            Vector2 canvasSize = ((RectTransform)transform).rect.size;
            if (canvasSize != lastCanvasSize || !Mathf.Approximately(drawer.rect.width, lastDrawerWidth))
            {
                Layout();
                lastCanvasSize = canvasSize;
                lastDrawerWidth = drawer.rect.width;
            }
        }

        private void Show()
        {
            if (workspace == null)
            {
                workspace = new GameObject("OperationTestWorkspace", typeof(RectTransform)).GetComponent<RectTransform>();
                workspace.SetParent(transform, false);
                workspace.SetSiblingIndex(1);
                backgroundRoot = new GameObject("OperationBackground", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                Canvas canvas = backgroundRoot.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 900;
                CanvasScaler scaler = backgroundRoot.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                GameObject gradient = new GameObject("SkyBackground", typeof(RectTransform), typeof(RawImage));
                gradient.transform.SetParent(backgroundRoot.transform, false);
                PDTwinRuntimeUiFactory.Stretch(gradient.GetComponent<RectTransform>());
                gradientTexture = new Texture2D(1, 128, TextureFormat.RGBA32, false);
                gradientTexture.wrapMode = TextureWrapMode.Clamp;
                for (int y = 0; y < 128; y++)
                {
                    float t = y / 127f;
                    Color color = t < 0.43f ? new Color32(150, 156, 164, 255)
                        : t < 0.65f ? Color.Lerp(new Color32(150, 156, 164, 255), new Color32(211, 245, 255, 255), (t - 0.43f) / 0.22f)
                        : Color.Lerp(new Color32(211, 245, 255, 255), new Color32(157, 180, 211, 255), (t - 0.65f) / 0.35f);
                    gradientTexture.SetPixel(0, y, color);
                }
                gradientTexture.Apply();
                gradient.GetComponent<RawImage>().texture = gradientTexture;
                gradient.GetComponent<RawImage>().raycastTarget = false;
                GameObject view = new GameObject("CenteredApparatus", typeof(RectTransform), typeof(RawImage));
                view.transform.SetParent(backgroundRoot.transform, false);
                modelView = view.GetComponent<RawImage>();
                modelView.raycastTarget = false;
                previewCamera = new GameObject("TestWorkspaceCamera", typeof(Camera)).GetComponent<Camera>();
                previewCamera.CopyFrom(Camera.main);
                previewCamera.tag = "Untagged";
                previewCamera.rect = new Rect(0f, 0f, 1f, 1f);
                previewCamera.clearFlags = CameraClearFlags.SolidColor;
                previewCamera.backgroundColor = Color.clear;
                previewCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));
                previewCamera.orthographic = false;
                previewCamera.fieldOfView = 35f;
                previewTexture = new RenderTexture(Mathf.Max(256, Screen.width / 6), Mathf.Max(144, Screen.height / 6),
                    24, RenderTextureFormat.ARGB32) { name = "OperationBackgroundBlur", filterMode = FilterMode.Bilinear };
                previewCamera.targetTexture = previewTexture;
                modelView.texture = previewTexture;
                GameObject veil = PDTwinRuntimeUiFactory.CreateRect("CoolLightVeil", backgroundRoot.transform,
                    new Color(0.86f, 0.92f, 0.98f, 0.30f));
                PDTwinRuntimeUiFactory.Stretch(veil.GetComponent<RectTransform>());
                veil.GetComponent<Image>().raycastTarget = false;
                CreateMotorStatus();
            }
            hmiPose = new PanelPose(plc.OperationHmiPanel);
            hmiPose.rect.SetParent(workspace, false);
            if (plc.OperationMotorPanel != null)
            {
                motorPose = new PanelPose(plc.OperationMotorPanel);
                motorPose.rect.SetParent(workspace, false);
                Image surface = motorPose.rect.GetComponent<Image>();
                if (surface != null)
                {
                    surface.sprite = Resources.Load<Sprite>("UI/RoundedRect");
                    surface.type = Image.Type.Sliced;
                }
            }
            showing = true;
            lastCanvasSize = Vector2.zero;
            workspace.gameObject.SetActive(true);
            backgroundRoot.SetActive(true);
            previewCamera.enabled = true;
        }

        private void Layout()
        {
            workspace.anchorMin = Vector2.zero;
            workspace.anchorMax = Vector2.one;
            workspace.offsetMin = Vector2.zero;
            workspace.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            float width = workspace.rect.width - Mathf.Min(650f, workspace.rect.width * 0.4f);
            float height = workspace.rect.height;
            float leftWidth = width * 0.36f;
            const float contentTop = 300f;
            const float leftInset = 40f;
            const float cardGap = 24f;
            float monitorX = leftInset + leftWidth + cardGap;
            Place(hmiPose.rect, new Vector2(monitorX, -contentTop), hmiPose.rect.sizeDelta);
            hmiPose.rect.localScale = Vector3.one * Mathf.Min((width - monitorX - 24f) / 620f, (height - contentTop - 32f) / hmiPose.rect.sizeDelta.y);
            if (motorPose != null && motorPose.rect != null)
                Place(motorPose.rect, new Vector2(leftInset, -contentTop),
                    new Vector2(leftWidth, leftWidth * 0.6f));
            Place(motorStatus, new Vector2(leftInset, -contentTop - 20f - leftWidth * 0.6f), new Vector2(leftWidth, 270f));
            RectTransform modelRect = modelView.rectTransform;
            Place(modelRect, new Vector2(width * 0.25f, -105f),
                new Vector2(width * 0.95f, Mathf.Max(20f, height - 105f)));
            previewCamera.aspect = modelRect.rect.width / modelRect.rect.height;
            // Large apparatus remains behind the foreground cards, as in Bai 1.
            if (plc.TryGetOperationModelBounds(out Bounds bounds))
            {
                Quaternion rotation = Camera.main.transform.rotation;
                Vector3 right = rotation * Vector3.right, up = rotation * Vector3.up;
                Vector3 forward = rotation * Vector3.forward;
                float tanV = Mathf.Tan(previewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float tanH = tanV * previewCamera.aspect;
                float distance = 0.1f;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = Vector3.Scale(bounds.extents, new Vector3(
                        (i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    float depth = Vector3.Dot(corner, forward);
                    distance = Mathf.Max(distance, Mathf.Abs(Vector3.Dot(corner, right)) / tanH - depth,
                        Mathf.Abs(Vector3.Dot(corner, up)) / tanV - depth);
                }
                previewCamera.nearClipPlane = 0.005f;
                previewCamera.transform.SetPositionAndRotation(bounds.center - forward * (distance * 0.90f), rotation);
            }
        }

        private void CreateMotorStatus()
        {
            motorStatus = PDTwinRuntimeUiFactory.CreateRect("MotorStatus", workspace, Color.white, true).GetComponent<RectTransform>();
            var band = PDTwinRuntimeUiFactory.CreateRect("ConnectionBand", motorStatus,
                new Color32(255, 241, 241, 255), true).GetComponent<RectTransform>();
            Place(band, new Vector2(24f, -24f), new Vector2(410f, 56f));
            var label = PDTwinRuntimeUiFactory.CreateText("Label", band, "Trạng thái:", 24f, Color.black);
            PDTwinRuntimeUiFactory.Stretch(label.rectTransform, 18f, 0f, 170f, 0f);
            connectionValue = PDTwinRuntimeUiFactory.CreateText("Value", band, "Đang chờ", 24f,
                new Color32(0, 150, 40, 255), FontStyles.Normal, TextAlignmentOptions.Right);
            PDTwinRuntimeUiFactory.Stretch(connectionValue.rectTransform, 160f, 0f, 18f, 0f);
            string[] labels = { "Vị trí (độ):", "Đã quay:", "Tốc độ RPM:" };
            TextMeshProUGUI[] values = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
            {
                var text = PDTwinRuntimeUiFactory.CreateText("Label" + i, motorStatus, labels[i], 24f, Color.black);
                Place(text.rectTransform, new Vector2(26f, -96f - i * 45f), new Vector2(230f, 38f));
                values[i] = PDTwinRuntimeUiFactory.CreateText("Value" + i, motorStatus, "--", 24f,
                    new Color32(0, 150, 40, 255), FontStyles.Normal, TextAlignmentOptions.Right);
                Place(values[i].rectTransform, new Vector2(260f, -96f - i * 45f), new Vector2(170f, 38f));
            }
            angleValue = values[0]; rotationsValue = values[1]; speedValue = values[2];
        }

        private void UpdateStatus()
        {
            if (connectionValue == null) return;
            bool fresh = plc.IsTelemetryFresh;
            connectionValue.text = fresh ? "Đã kết nối" : "Chờ dữ liệu";
            connectionValue.color = fresh ? new Color32(0, 150, 40, 255) : new Color32(194, 29, 31, 255);
            var data = plc.LatestTelemetry;
            angleValue.text = data != null ? data.angle.ToString("F1") : "--";
            rotationsValue.text = data != null ? data.rotationsExact.ToString("F2") : "--";
            speedValue.text = data != null ? Mathf.Abs(data.speedRpm).ToString("F1") : "--";
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private void Hide()
        {
            if (!showing) return;
            hmiPose?.Restore(); motorPose?.Restore();
            hmiPose = motorPose = null;
            showing = false;
            if (workspace != null) workspace.gameObject.SetActive(false);
            if (backgroundRoot != null) backgroundRoot.SetActive(false);
            if (previewCamera != null) previewCamera.enabled = false;
        }

        private void OnDisable() { Hide(); }
        private void OnDestroy()
        {
            Hide();
            if (previewCamera != null) Destroy(previewCamera.gameObject);
            if (previewTexture != null) { previewTexture.Release(); Destroy(previewTexture); }
            if (backgroundRoot != null) Destroy(backgroundRoot);
            if (gradientTexture != null) Destroy(gradientTexture);
        }
    }
}
