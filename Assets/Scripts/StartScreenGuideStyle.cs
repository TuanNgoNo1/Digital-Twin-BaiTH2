using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class StartScreenController
{
    private static Sprite navigationPillSprite;

    // Match Bai 1: 208 x 72, 29 pt, #CA1417, pill radius 25, same arrows and hover.
    private static Button CreateGuideNavigationButton(RectTransform parent, string name, string label, bool previous)
    {
        Vector2 anchor = new Vector2(previous ? 0f : 1f, 0f);
        RectTransform rect = CreateRect(parent, name, anchor, anchor);
        rect.pivot = anchor;
        rect.anchoredPosition = new Vector2(previous ? 48f : -50f, 16f);
        rect.sizeDelta = new Vector2(208f, 72f);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = GetNavigationPillSprite();
        image.type = Image.Type.Sliced;
        image.color = new Color32(202, 20, 23, 255);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        ColorBlock colors = button.colors;
        colors.normalColor = colors.selectedColor = new Color32(202, 20, 23, 255);
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color32(250, 246, 246, 255);
        colors.disabledColor = new Color32(202, 20, 23, 150);
        button.colors = colors;
        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color32(202, 20, 23, 255);
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = false;
        Shadow shadow = rect.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color32(32, 20, 20, 30);
        shadow.effectDistance = new Vector2(0f, -4f);
        TextMeshProUGUI text = CreateText(rect, "Label", label, 29f, FontStyles.Normal, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), TextAlignmentOptions.Center);
        text.enableAutoSizing = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        text.rectTransform.anchoredPosition = new Vector2(previous ? 20f : -20f, 0f);
        text.rectTransform.sizeDelta = new Vector2(120f, 72f);
        RectTransform arrow = CreatePillArrow(rect, previous ? -60f : 64f, previous);
        EventTrigger trigger = rect.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => SetGuideNavigationHover(image, text, arrow, shadow, true));
        trigger.triggers.Add(enter);
        EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => SetGuideNavigationHover(image, text, arrow, shadow, false));
        trigger.triggers.Add(exit);
        return button;
    }

    private static void SetGuideNavigationHover(Image image, TextMeshProUGUI text, RectTransform arrow, Shadow shadow, bool hovered)
    {
        Color red = new Color32(202, 20, 23, 255);
        image.color = hovered ? Color.white : red;
        text.color = hovered ? red : Color.white;
        SetArrowColor(arrow, text.color);
        shadow.enabled = !hovered;
    }

    private static void CreateGuideBackground(RectTransform page)
    {
        RectTransform backdrop = CreateRect(page, "CircuitBackground", Vector2.zero, Vector2.one);
        Image image = backdrop.gameObject.AddComponent<Image>();
        image.color = Surface;
        image.raycastTarget = false;
        // Reuse Bai 1's circuit paths in the same 1920 x 1080 design space.
        RectTransform traces = CreateRect(backdrop, "CircuitTraces", Vector2.zero, Vector2.one);
        Vector2[][] paths =
        {
            new[] { new Vector2(0f, 0f), new Vector2(455f, 0f), new Vector2(601f, 27f), new Vector2(626f, 66f) },
            new[] { new Vector2(0f, 87f), new Vector2(451f, 161f), new Vector2(482f, 111f), new Vector2(568f, 121f) },
            new[] { new Vector2(0f, 135f), new Vector2(540f, 225f), new Vector2(567f, 175f), new Vector2(717f, 195f) },
            new[] { new Vector2(0f, 190f), new Vector2(557f, 286f), new Vector2(580f, 327f), new Vector2(746f, 351f) },
            new[] { new Vector2(1192f, 10f), new Vector2(1632f, 83f), new Vector2(1655f, 115f), new Vector2(1752f, 131f), new Vector2(1786f, 92f), new Vector2(1920f, 110f) },
            new[] { new Vector2(1417f, 98f), new Vector2(1630f, 136f), new Vector2(1648f, 163f), new Vector2(1920f, 209f) },
            new[] { new Vector2(1518f, 154f), new Vector2(1629f, 174f), new Vector2(1648f, 204f), new Vector2(1920f, 249f) },
            new[] { new Vector2(1607f, 203f), new Vector2(1642f, 210f), new Vector2(1662f, 241f), new Vector2(1920f, 285f) },
            new[] { new Vector2(1354f, 248f), new Vector2(1643f, 296f), new Vector2(1669f, 252f), new Vector2(1920f, 295f) },
            new[] { new Vector2(1436f, 331f), new Vector2(1519f, 345f), new Vector2(1559f, 303f), new Vector2(1920f, 364f) },
            new[] { new Vector2(1590f, 361f), new Vector2(1681f, 377f), new Vector2(1704f, 350f), new Vector2(1920f, 386f) },
            new[] { new Vector2(0f, 603f), new Vector2(511f, 694f), new Vector2(535f, 735f), new Vector2(737f, 769f) },
            new[] { new Vector2(0f, 650f), new Vector2(486f, 735f), new Vector2(513f, 783f), new Vector2(712f, 817f) },
            new[] { new Vector2(0f, 700f), new Vector2(418f, 772f), new Vector2(442f, 811f), new Vector2(629f, 842f) },
            new[] { new Vector2(0f, 776f), new Vector2(340f, 833f), new Vector2(380f, 806f), new Vector2(568f, 836f) },
            new[] { new Vector2(0f, 827f), new Vector2(483f, 908f), new Vector2(506f, 951f), new Vector2(706f, 984f) },
            new[] { new Vector2(0f, 965f), new Vector2(338f, 1022f), new Vector2(381f, 972f), new Vector2(600f, 1009f) },
            new[] { new Vector2(1236f, 919f), new Vector2(1421f, 950f), new Vector2(1438f, 981f), new Vector2(1572f, 1004f) },
            new[] { new Vector2(1441f, 877f), new Vector2(1533f, 892f), new Vector2(1563f, 955f), new Vector2(1728f, 982f), new Vector2(1770f, 927f), new Vector2(1920f, 952f) },
            new[] { new Vector2(1604f, 875f), new Vector2(1680f, 888f), new Vector2(1704f, 850f), new Vector2(1920f, 887f) },
            new[] { new Vector2(1310f, 962f), new Vector2(1418f, 980f), new Vector2(1433f, 1011f), new Vector2(1684f, 1054f) }
        };

        foreach (Vector2[] path in paths)
        {
            for (int i = 0; i < path.Length - 1; i++)
            {
                Vector2 start = path[i];
                Vector2 delta = path[i + 1] - start;
                RectTransform line = CreateRect(traces, "CircuitLine", new Vector2(0f, 1f), new Vector2(0f, 1f));
                line.pivot = new Vector2(0f, 0.5f);
                line.anchoredPosition = new Vector2(start.x, -start.y);
                line.sizeDelta = new Vector2(delta.magnitude, 1.25f);
                line.localEulerAngles = new Vector3(0f, 0f, -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                Image stroke = line.gameObject.AddComponent<Image>();
                stroke.color = new Color32(213, 217, 221, 150);
                stroke.raycastTarget = false;
            }
        }
    }

    private static RectTransform CreatePillArrow(Transform parent, float x, bool pointsLeft)
    {
        GameObject arrow = new GameObject("PillArrow", typeof(RectTransform));
        arrow.transform.SetParent(parent, false);
        arrow.layer = parent.gameObject.layer;
        RectTransform rect = arrow.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 0f);
        rect.sizeDelta = new Vector2(40f, 40f);
        rect.localRotation = Quaternion.Euler(0f, 0f, pointsLeft ? 180f : 0f);

        CreateArrowStroke(rect, "Shaft", new Vector2(-2f, 0f), new Vector2(25f, 3f), 0f);
        CreateArrowStroke(rect, "UpperHead", new Vector2(6f, 5f), new Vector2(14f, 3f), -45f);
        CreateArrowStroke(rect, "LowerHead", new Vector2(6f, -5f), new Vector2(14f, 3f), 45f);
        return rect;
    }

    private static void CreateArrowStroke(RectTransform parent, string name, Vector2 position, Vector2 size, float angle)
    {
        GameObject stroke = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        stroke.transform.SetParent(parent, false);
        stroke.layer = parent.gameObject.layer;
        RectTransform rect = stroke.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        Image image = stroke.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private static void SetArrowColor(RectTransform arrow, Color color)
    {
        foreach (Image stroke in arrow.GetComponentsInChildren<Image>())
        {
            stroke.color = color;
        }
    }

    private static Sprite GetNavigationPillSprite()
    {
        if (navigationPillSprite != null)
        {
            return navigationPillSprite;
        }

        const int width = 132;
        const int height = 72;
        const float radius = 25f;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "NavigationPillTexture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color32[] pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float sampleX = x + 0.5f;
                float sampleY = y + 0.5f;
                float centerX = Mathf.Clamp(sampleX, radius, width - radius);
                float centerY = Mathf.Clamp(sampleY, radius, height - radius);
                float distance = Vector2.Distance(new Vector2(sampleX, sampleY), new Vector2(centerX, centerY));
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius + 0.5f - distance) * 255f);
                pixels[y * width + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        navigationPillSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        navigationPillSprite.name = "NavigationPillSprite";
        navigationPillSprite.hideFlags = HideFlags.HideAndDontSave;
        return navigationPillSprite;
    }

}
