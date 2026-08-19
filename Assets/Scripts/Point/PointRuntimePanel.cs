using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PDTwin.Point
{
    public sealed class PointRuntimePanel : MonoBehaviour
    {
        private TextMeshProUGUI targetText;
        private TextMeshProUGUI statusText;
        private TextMeshProUGUI breakdownText;
        private TextMeshProUGUI scoreText;
        private Button startButton;
        private Button regradeButton;
        private Button submitButton;

        public event Action StartRequested;
        public event Action RegradeRequested;
        public event Action SubmitRequested;

        public static PointRuntimePanel Create(string title)
        {
            GameObject root = new GameObject("PDTwinPointRuntime_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5250;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject card = new GameObject("PointCard", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            card.transform.SetParent(root.transform, false);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(1f, 1f);
            cardRect.anchorMax = new Vector2(1f, 1f);
            cardRect.pivot = new Vector2(1f, 1f);
            cardRect.anchoredPosition = new Vector2(-22f, -102f);
            cardRect.sizeDelta = new Vector2(455f, 500f);

            Image cardImage = card.GetComponent<Image>();
            cardImage.color = new Color(0.055f, 0.075f, 0.11f, 0.96f);

            VerticalLayoutGroup layout = card.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 18, 18);
            layout.spacing = 9f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            PointRuntimePanel panel = root.AddComponent<PointRuntimePanel>();
            panel.Build(card.transform, title);
            return panel;
        }

        private void Build(Transform parent, string title)
        {
            TextMeshProUGUI titleText = CreateText(parent, "Title", title, 26f, FontStyles.Bold, new Color(0.3f, 0.82f, 1f), 38f);
            titleText.alignment = TextAlignmentOptions.Left;

            targetText = CreateText(parent, "Target", "Dang tai muc tieu...", 18f, FontStyles.Normal, Color.white, 78f);
            statusText = CreateText(parent, "Status", "Dang khoi tao...", 18f, FontStyles.Bold, new Color(1f, 0.82f, 0.3f), 52f);
            breakdownText = CreateText(parent, "Breakdown", string.Empty, 18f, FontStyles.Normal, new Color(0.9f, 0.93f, 0.97f), 142f);
            scoreText = CreateText(parent, "Score", "Diem tam: 0/10", 25f, FontStyles.Bold, new Color(0.4f, 1f, 0.62f), 42f);

            GameObject buttonRow = new GameObject("Actions", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            buttonRow.transform.SetParent(parent, false);
            LayoutElement rowLayout = buttonRow.GetComponent<LayoutElement>();
            rowLayout.preferredHeight = 52f;
            HorizontalLayoutGroup row = buttonRow.GetComponent<HorizontalLayoutGroup>();
            row.spacing = 8f;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;

            startButton = CreateButton(buttonRow.transform, "StartButton", "Bat dau cham", new Color(0.1f, 0.48f, 0.78f));
            regradeButton = CreateButton(buttonRow.transform, "RegradeButton", "Cham lai", new Color(0.28f, 0.38f, 0.53f));
            submitButton = CreateButton(buttonRow.transform, "SubmitButton", "Nop bai", new Color(0.08f, 0.62f, 0.34f));

            startButton.onClick.AddListener(() => StartRequested?.Invoke());
            regradeButton.onClick.AddListener(() => RegradeRequested?.Invoke());
            submitButton.onClick.AddListener(() => SubmitRequested?.Invoke());
        }

        public void SetTarget(PointTarget target)
        {
            if (targetText == null || target == null)
                return;

            targetText.text =
                $"Muc tieu: {DirectionLabel(target.direction)} | {target.speedRpm:F1} RPM\n" +
                $"Encoder: {target.encoderPulses} ±{target.encoderTolerancePulses} xung | Nguon: {target.source}";
        }

        public void SetStatus(string value, bool isError = false)
        {
            if (statusText == null)
                return;
            statusText.text = value ?? string.Empty;
            statusText.color = isError ? new Color(1f, 0.38f, 0.38f) : new Color(1f, 0.82f, 0.3f);
        }

        public void SetBreakdown(string value)
        {
            if (breakdownText != null)
                breakdownText.text = value ?? string.Empty;
        }

        public void SetScore(float score, bool submitted)
        {
            if (scoreText == null)
                return;
            scoreText.text = submitted ? $"DA NOP: {score:F2}/10" : $"Diem tam: {score:F2}/10";
            scoreText.color = submitted ? new Color(0.3f, 0.82f, 1f) : new Color(0.4f, 1f, 0.62f);
        }

        public void SetButtons(bool canStart, bool canRegrade, bool canSubmit)
        {
            if (startButton != null) startButton.interactable = canStart;
            if (regradeButton != null) regradeButton.interactable = canRegrade;
            if (submitButton != null) submitButton.interactable = canSubmit;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string objectName, string value, float fontSize, FontStyles style, Color color, float height)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            textObject.transform.SetParent(parent, false);
            LayoutElement layout = textObject.GetComponent<LayoutElement>();
            layout.preferredHeight = height;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.text = value;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string objectName, string label, Color color)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.GetComponent<Image>();
            image.color = color;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            button.colors = colors;

            TextMeshProUGUI text = CreateText(buttonObject.transform, "Label", label, 16f, FontStyles.Bold, Color.white, 42f);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            text.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private static string DirectionLabel(string direction)
        {
            return PointMath.NormalizeDirection(direction) == "reverse" ? "Nghich" : "Thuan";
        }
    }
}
