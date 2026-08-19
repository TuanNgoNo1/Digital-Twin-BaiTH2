using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartScreenController : MonoBehaviour
{
    public static bool OpenGuidePageOnStart;
    public static bool ContinuePracticeFromGuide;

    [Header("Legacy panels")]
    [SerializeField] private GameObject practicePanel;
    [SerializeField] private GameObject introductionPanel;
    [SerializeField] private GameObject guidePanel;

    [Header("Page navigation")]
    [SerializeField] private string practiceSceneName = "Sy_scene";
    [SerializeField] private GameObject[] pages;
    [SerializeField] private RectTransform background;

    // PTIT visual identity: red accents on clean white surfaces.
    private static readonly Color Navy = new Color32(139, 0, 0, 255);
    private static readonly Color Blue = new Color32(215, 25, 32, 255);
    private static readonly Color LightBlue = Color.white;
    private static readonly Color Ink = Color.black;
    private static readonly Color Muted = Color.black;
    private static readonly Color Surface = Color.white;

    private int currentPageIndex;
    private Button previousButton;
    private Button nextButton;
    private TextMeshProUGUI nextButtonLabel;
    private TextMeshProUGUI pageIndicator;
    private Image backgroundImage;
    private Camera sceneCamera;

    private sealed class PageSpec
    {
        public string Section;
        public string Eyebrow;
        public string Title;
        public string LeftTitle;
        public string LeftBody;
        public string RightTitle;
        public string RightBody;
        public string ImagePath;
        public string ImageCaption;
        public string Note;
        public bool WideImage;
        public bool ShortcutGrid;
        public bool GuacamoleLogin;
    }

    private void Awake()
    {
        ResolveReferences();
        BuildGuidePages();
        BuildNavigation();
        ShowPage(OpenGuidePageOnStart ? pages.Length - 1 : 0);
        OpenGuidePageOnStart = false;
    }

    public void ShowPractice() => SceneManager.LoadScene(practiceSceneName);
    public void LoadPracticeScene() => SceneManager.LoadScene(practiceSceneName);
    public void ShowIntroduction() => ShowLegacyPanel(introductionPanel);
    public void ShowGuide() => ShowLegacyPanel(guidePanel);

    public void PreviousPage()
    {
        if (currentPageIndex > 0)
            ShowPage(currentPageIndex - 1);
    }

    public void NextPage()
    {
        if (currentPageIndex >= pages.Length - 1)
        {
            ContinuePracticeFromGuide = true;
            SceneManager.LoadScene(practiceSceneName);
            return;
        }
        ShowPage(currentPageIndex + 1);
    }

    private void ResolveReferences()
    {
        if (background == null)
        {
            GameObject found = GameObject.Find("Background");
            background = found != null ? found.GetComponent<RectTransform>() : null;
        }
        backgroundImage = background != null ? background.GetComponent<Image>() : null;
        if (backgroundImage != null)
        {
            backgroundImage.sprite = null;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.color = Color.white;
        }
        sceneCamera = Camera.main;
        if (sceneCamera != null)
            sceneCamera.allowHDR = false;
    }

    private void BuildGuidePages()
    {
        if (background == null)
        {
            Debug.LogError("[StartScreen] Không tìm thấy Background.");
            pages = Array.Empty<GameObject>();
            return;
        }

        DisableLegacyPages();
        Transform oldRoot = background.Find("GXWorks2GuidePages");
        if (oldRoot != null)
            Destroy(oldRoot.gameObject);

        RectTransform root = CreateRect(background, "GXWorks2GuidePages", Vector2.zero, Vector2.one);
        List<GameObject> results = new List<GameObject>();
        foreach (PageSpec spec in GetPageSpecs())
            results.Add(BuildPage(root, spec));
        pages = results.ToArray();
    }

    private static IEnumerable<PageSpec> GetPageSpecs()
    {
        yield return new PageSpec
        {
            Section = "01", Eyebrow = "MỤC TIÊU", Title = "HƯỚNG DẪN THỰC HÀNH TRÊN GX WORKS2",
            LeftTitle = "PLC Mitsubishi FX3U",
            LeftBody = "<size=125%><b>Quy trình làm việc\ntrên GX Works2</b></size>\n\nTừ khởi tạo dự án đến giám sát chương trình PLC trực tuyến.",
            RightTitle = "Sau khi hoàn thành, sinh viên có thể",
            RightBody = "<color=#DA1F2D><b>01</b></color>  Tạo dự án PLC Mitsubishi FX3U.\n\n" +
                        "<color=#DA1F2D><b>02</b></color>  Khai báo và chú thích thiết bị PLC.\n\n" +
                        "<color=#DA1F2D><b>03</b></color>  Làm quen với giao diện Ladder.\n\n" +
                        "<color=#DA1F2D><b>04</b></color>  Biên dịch và kiểm tra lỗi.\n\n" +
                        "<color=#DA1F2D><b>05</b></color>  Thiết lập kết nối giữa GX Works2 và PLC.\n\n" +
                        "<color=#DA1F2D><b>06</b></color>  Nạp chương trình xuống PLC.\n\n" +
                        "<color=#DA1F2D><b>07</b></color>  Giám sát trạng thái chương trình và giá trị thiết bị trực tuyến."
        };
        yield return new PageSpec
        {
            Section = "02", Eyebrow = "GIỚI THIỆU", Title = "Đăng nhập Apache Guacamole",
            RightTitle = "Hướng dẫn đăng nhập",
            RightBody = "Sinh viên nhập tài khoản và mật khẩu đã được cấp và nhấn <b>Login</b> để vào giao diện làm việc của Gxworks 2",
            GuacamoleLogin = true
        };
        yield return new PageSpec
        {
            Section = "02", Eyebrow = "KHỞI TẠO DỰ ÁN", Title = "Tạo dự án mới",
            LeftTitle = "Các bước thực hiện",
            LeftBody = "<b>1.</b> Khởi động phần mềm GX Works2.\n\n<b>2.</b> Chọn <color=#DA1F2D><b>Project → New</b></color>.\n\n" +
                       "<b>3.</b> Trong cửa sổ New Project, thiết lập:\n    • Series: <b>FXCPU</b>\n    • Type: <b>FX3U/FX3UC</b>\n" +
                       "    • Project Type: <b>Simple Project</b>\n    • Language: <b>Ladder</b>\n\n<b>4.</b> Kiểm tra và nhấn <b>OK</b>.",
            ImagePath = "GXWorks2Guide/01_new_project", ImageCaption = "Cửa sổ New Project",
            Note = "Chọn đúng CPU của trạm thực hành; chọn sai CPU có thể làm chương trình không tương thích hoặc không nạp được xuống PLC."
        };
        yield return new PageSpec
        {
            Section = "03", Eyebrow = "DEVICE COMMENT", Title = "Gán chú thích thiết bị",
            LeftTitle = "Thao tác",
            LeftBody = "Trong cây thư mục <b>Navigation</b>, nhấp đúp vào:\n\n<color=#DA1F2D><b>Global Device Comment</b></color>\n\n" +
                       "Tại bảng khai báo:\n\n• Nhập địa chỉ vào cột <b>Device Name</b>.\n\n• Nhập nội dung mô tả vào cột <b>Comment</b>.",
            ImagePath = "GXWorks2Guide/02_device_comment", ImageCaption = "Cửa sổ khai báo Device Comment",
            Note = "Sau khi khai báo, bật hiển thị Device Comment trong cửa sổ Ladder để dễ đọc và kiểm tra chương trình."
        };
        yield return new PageSpec
        {
            Section = "04", Eyebrow = "LADDER EDITOR", Title = "Mở và thao tác trong cửa sổ Ladder",
            LeftTitle = "Mở chương trình",
            LeftBody = "Trong cây dự án, mở:\n\n<color=#DA1F2D><b>POU → Program → MAIN</b></color>\n\n" +
                       "Chuyển sang chế độ chỉnh sửa bằng:\n<b>Edit → Ladder Edit Mode → Write Mode</b> hoặc nhấn <b>F2</b>.",
            RightTitle = "Các phím tắt thường dùng",
            RightBody = "shortcut-grid",
            ShortcutGrid = true
        };
        yield return new PageSpec
        {
            Section = "04", Eyebrow = "LADDER EDITOR — TIẾP", Title = "Quy trình nhập phần tử Ladder",
            LeftTitle = "Bốn bước nhập lệnh",
            LeftBody = "<color=#DA1F2D><b>01</b></color>  Đặt con trỏ tại vị trí cần chèn.\n\n" +
                       "<color=#DA1F2D><b>02</b></color>  Nhấn phím tương ứng.\n\n" +
                       "<color=#DA1F2D><b>03</b></color>  Nhập địa chỉ thiết bị hoặc nội dung lệnh.\n\n" +
                       "<color=#DA1F2D><b>04</b></color>  Nhấn Enter để xác nhận.",
            RightTitle = "Kiểm tra trước khi tiếp tục",
            RightBody = "Sau khi hoàn thành một khối lệnh, kiểm tra lại:\n\n• <b>Địa chỉ thiết bị</b>\n\n• <b>Thứ tự các toán hạng</b>\n\nSau đó mới chuyển sang khối tiếp theo."
        };
        yield return new PageSpec
        {
            Section = "05", Eyebrow = "COMPILE & BUILD", Title = "Biên dịch chương trình",
            LeftTitle = "Thực hiện",
            LeftBody = "<b>1.</b> Chọn <color=#DA1F2D><b>Compile → Build</b></color> hoặc nhấn <b>F4</b>.\n\n<b>2.</b> Kiểm tra cửa sổ Output.\n\n" +
                       "<b>3.</b> Chương trình hợp lệ khi không còn lỗi.\n\nNếu có lỗi: đọc thông báo, nhấp đúp vào dòng lỗi, sửa và nhấn F4 để biên dịch lại.\n\n" +
                       "<color=#168047><b>0 Error(s), 0 Warning(s)</b></color>",
            ImagePath = "GXWorks2Guide/03_compile_output", ImageCaption = "Cửa sổ Output sau khi Build",
            Note = "Chỉ thực hiện nạp chương trình khi quá trình biên dịch không còn lỗi."
        };
        yield return new PageSpec
        {
            Section = "06", Eyebrow = "XÁC ĐỊNH CỔNG COM", Title = "Thiết lập kết nối PLC",
            LeftTitle = "6.1. Xác định cổng COM",
            LeftBody = "<b>1.</b> Kết nối cáp lập trình với máy tính.\n\n<b>2.</b> Mở <b>Device Manager</b> của Windows.\n\n" +
                       "<b>3.</b> Mở nhóm <b>Ports (COM & LPT)</b>.\n\n<b>4.</b> Ghi lại cổng COM của cáp lập trình, ví dụ <b>COM4</b> hoặc <b>COM6</b>.",
            RightTitle = "Thông tin cần ghi nhớ",
            RightBody = "<size=170%><color=#DA1F2D><b>COM4</b></color></size>\n\nCổng COM trên mỗi máy có thể khác nhau. Luôn kiểm tra lại trong Device Manager trước khi cấu hình GX Works2."
        };
        yield return new PageSpec
        {
            Section = "06", Eyebrow = "CẤU HÌNH GX WORKS2", Title = "Thiết lập kết nối PLC",
            LeftTitle = "6.2. Cấu hình",
            LeftBody = "<b>1.</b> Chọn <b>Connection Destination → Connection1</b>.\n\n<b>2.</b> Nhấp đúp vào <b>Serial USB</b>.\n\n" +
                       "<b>3.</b> Chọn đúng cổng COM.\n\n<b>4.</b> Thiết lập tốc độ truyền → <b>OK</b>.\n\n<b>5.</b> Nhấn <b>Connection Test</b>.",
            ImagePath = "GXWorks2Guide/04_connection_setup", ImageCaption = "Transfer Setup Connection1",
            Note = "Nếu kết nối thất bại, kiểm tra cổng COM, driver cáp, loại CPU và phần mềm khác đang sử dụng cổng COM."
        };
        yield return new PageSpec
        {
            Section = "07", Eyebrow = "WRITE TO PLC", Title = "Nạp chương trình xuống PLC",
            LeftTitle = "Trình tự thực hiện",
            LeftBody = "<b>1.</b> Chọn <b>Online → Write to PLC</b>.\n<b>2.</b> Chọn Parameter, Program hoặc MAIN và Device Comment nếu cần.\n" +
                       "<b>3.</b> Nhấn <b>Execute</b>.\n<b>4.</b> Chọn Yes khi được yêu cầu dừng PLC hoặc ghi đè.\n" +
                       "<b>5.</b> Chờ tiến trình hoàn thành.\n<b>6.</b> Kiểm tra kết quả và nhấn Close.\n<b>7.</b> Chuyển PLC sang RUN.",
            ImagePath = "GXWorks2Guide/05_write_to_plc", ImageCaption = "Online Data Operation — Write",
            Note = "Không ngắt kết nối hoặc đóng GX Works2 khi đang ghi. Sau khi sửa chương trình: Build → Write to PLC → RUN."
        };
        yield return new PageSpec
        {
            Section = "08", Eyebrow = "MONITOR MODE", Title = "Giám sát chương trình",
            LeftTitle = "Bắt đầu giám sát",
            LeftBody = "Nhấn <color=#DA1F2D><b>F3</b></color> để chuyển sang Monitor Mode.\n\n" +
                       "Tiếp điểm và cuộn dây đang ON được làm nổi bật; trạng thái bit và giá trị thanh ghi được cập nhật trực tuyến.",
            ImagePath = "GXWorks2Guide/06_monitor_mode", ImageCaption = "Start Monitoring (All Windows)",
            Note = "Có thể theo dõi trạng thái tiếp điểm, cuộn dây, bit M/X/Y, thanh ghi D và bộ đếm C theo thời gian thực."
        };
        yield return new PageSpec
        {
            Section = "08", Eyebrow = "DEBUG → MODIFY VALUE", Title = "Thay đổi giá trị khi giám sát",
            LeftTitle = "Các bước thực hiện",
            LeftBody = "<b>1.</b> Nhấp chuột phải vào thiết bị.\n\n<b>2.</b> Chọn <color=#DA1F2D><b>Debug → Modify Value</b></color>.\n\n" +
                       "<b>3.</b> Nhập giá trị hoặc chọn trạng thái ON/OFF.\n\n<b>4.</b> Nhấn xác nhận.",
            RightTitle = "Lưu ý khi sử dụng",
            RightBody = "• Kiểm tra đúng địa chỉ trước khi thay đổi.\n\n• Không cưỡng bức đồng thời các lệnh đối nghịch.\n\n" +
                        "• Đưa bit về OFF sau khi thử nếu chương trình không tự reset.\n\n• Không sửa tham số khi đang phát xung.\n\n" +
                        "• Kết thúc giám sát trước khi thay đổi cấu trúc chương trình."
        };
    }

    private GameObject BuildPage(RectTransform parent, PageSpec spec)
    {
        RectTransform page = CreateRect(parent, "GXW_Page_" + (parent.childCount + 1).ToString("00"), Vector2.zero, Vector2.one);
        page.gameObject.AddComponent<Image>().color = Surface;
        CreateHeader(page, spec);

        if (spec.GuacamoleLogin)
        {
            CreateGuacamoleLoginIllustration(page, new Vector2(0.06f, 0.17f), new Vector2(0.54f, 0.78f));
            CreateTextCard(page, "LoginGuide", new Vector2(0.58f, 0.27f), new Vector2(0.94f, 0.69f),
                spec.RightTitle, spec.RightBody, false);
            return page.gameObject;
        }

        float bottom = string.IsNullOrEmpty(spec.Note) ? 0.17f : 0.21f;
        float leftMax = string.IsNullOrEmpty(spec.ImagePath) && string.IsNullOrEmpty(spec.RightBody) ? 0.94f : 0.45f;
        CreateTextCard(page, "LeftCard", new Vector2(0.06f, bottom), new Vector2(leftMax, 0.78f), spec.LeftTitle, spec.LeftBody, false);

        if (!string.IsNullOrEmpty(spec.ImagePath))
            CreateImageCard(page, spec.ImagePath, spec.ImageCaption, new Vector2(0.49f, bottom), new Vector2(0.94f, 0.78f));
        else if (spec.ShortcutGrid)
            CreateShortcutCard(page, new Vector2(0.49f, bottom), new Vector2(0.94f, 0.78f), spec.RightTitle);
        else if (!string.IsNullOrEmpty(spec.RightBody))
            CreateTextCard(page, "RightCard", new Vector2(0.49f, bottom), new Vector2(0.94f, 0.78f), spec.RightTitle, spec.RightBody, spec.Section == "01");

        if (!string.IsNullOrEmpty(spec.Note))
            CreateNote(page, spec.Note);
        return page.gameObject;
    }

    private static void CreateHeader(RectTransform page, PageSpec spec)
    {
        RectTransform header = CreateRect(page, "Header", new Vector2(0.055f, 0.81f), new Vector2(0.945f, 0.96f));
        CreateText(header, "Section", spec.Section, 28f, FontStyles.Bold, Blue, new Vector2(0f, 0.65f), new Vector2(0.07f, 1f), TextAlignmentOptions.Left);
        CreateText(header, "Eyebrow", spec.Eyebrow, 21f, FontStyles.Bold, Muted, new Vector2(0.08f, 0.67f), new Vector2(1f, 1f), TextAlignmentOptions.Left);
        CreateText(header, "Title", spec.Title, 44f, FontStyles.Bold, Navy, new Vector2(0f, 0.02f), new Vector2(1f, 0.66f), TextAlignmentOptions.Left);
        RectTransform line = CreateRect(header, "Accent", new Vector2(0f, 0f), new Vector2(0.12f, 0.025f));
        line.gameObject.AddComponent<Image>().color = Blue;
    }

    private static void CreateTextCard(RectTransform parent, string name, Vector2 min, Vector2 max, string title, string body, bool emphasized)
    {
        RectTransform card = CreateCard(parent, name, min, max, emphasized ? LightBlue : Color.white);
        CreateText(card, "Title", title, 28f, FontStyles.Bold, Navy, new Vector2(0.055f, 0.82f), new Vector2(0.945f, 0.95f), TextAlignmentOptions.Left);
        TextMeshProUGUI content = CreateText(card, "Body", body, 26f, FontStyles.Normal, Ink, new Vector2(0.055f, 0.06f), new Vector2(0.945f, 0.80f), TextAlignmentOptions.TopLeft);
        content.lineSpacing = 3f;
    }

    private static void CreateImageCard(RectTransform parent, string resourcePath, string caption, Vector2 min, Vector2 max)
    {
        RectTransform card = CreateCard(parent, "ImageCard", min, max, Color.white);
        RectTransform imageRect = CreateRect(card, "Image", new Vector2(0.035f, 0.04f), new Vector2(0.965f, 0.96f));
        RawImage image = imageRect.gameObject.AddComponent<RawImage>();
        image.texture = Resources.Load<Texture2D>(resourcePath);
        image.raycastTarget = false;
        AspectRatioFitter fitter = imageRect.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        if (image.texture != null)
            fitter.aspectRatio = (float)image.texture.width / image.texture.height;
        else
            Debug.LogWarning("[StartScreen] Không tải được ảnh " + resourcePath);
    }

    private static void CreateShortcutCard(RectTransform parent, Vector2 min, Vector2 max, string title)
    {
        RectTransform card = CreateCard(parent, "ShortcutCard", min, max, Color.white);
        CreateText(card, "Title", title, 28f, FontStyles.Bold, Navy,
            new Vector2(0.055f, 0.82f), new Vector2(0.945f, 0.95f), TextAlignmentOptions.Left);

        string[] leftKeys = { "F5", "F7", "Shift + F5", "Delete", "F3" };
        string[] leftDescriptions =
        {
            "Tiếp điểm thường mở",
            "Cuộn dây OUT",
            "Tạo nhánh song song",
            "Xóa phần tử",
            "Chuyển sang Monitor Mode"
        };
        string[] rightKeys = { "F6", "F8", "", "F4", "" };
        string[] rightDescriptions = { "Tiếp điểm thường đóng", "Lệnh ứng dụng", "", "Biên dịch", "" };

        const float firstRowTop = 0.79f;
        const float rowHeight = 0.135f;
        for (int i = 0; i < leftKeys.Length; i++)
        {
            float top = firstRowTop - i * rowHeight;
            float bottom = top - 0.105f;

            CreateText(card, "LeftKey_" + i, leftKeys[i], 25f, FontStyles.Bold, Blue,
                new Vector2(0.055f, bottom), new Vector2(0.29f, top), TextAlignmentOptions.Left);
            CreateText(card, "LeftDescription_" + i, leftDescriptions[i], 24f, FontStyles.Normal, Ink,
                new Vector2(0.31f, bottom), new Vector2(0.60f, top), TextAlignmentOptions.Left);

            if (string.IsNullOrEmpty(rightKeys[i]))
                continue;

            CreateText(card, "RightKey_" + i, rightKeys[i], 25f, FontStyles.Bold, Blue,
                new Vector2(0.63f, bottom), new Vector2(0.73f, top), TextAlignmentOptions.Left);
            CreateText(card, "RightDescription_" + i, rightDescriptions[i], 24f, FontStyles.Normal, Ink,
                new Vector2(0.75f, bottom), new Vector2(0.96f, top), TextAlignmentOptions.Left);
        }
    }

    private static void CreateGuacamoleLoginIllustration(RectTransform parent, Vector2 min, Vector2 max)
    {
        RectTransform canvas = CreateCard(parent, "GuacamoleIllustration", min, max, Color.white);
        RectTransform loginPanel = CreateCard(canvas, "LoginPanel", new Vector2(0.20f, 0.08f), new Vector2(0.80f, 0.92f), Color.white);
        Outline panelOutline = loginPanel.GetComponent<Outline>();
        panelOutline.effectColor = new Color32(175, 175, 175, 255);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        CreateText(loginPanel, "Logo", "●", 68f, FontStyles.Bold, new Color32(20, 20, 20, 255),
            new Vector2(0.38f, 0.72f), new Vector2(0.62f, 0.92f), TextAlignmentOptions.Center);
        CreateText(loginPanel, "LogoAccent", "●", 30f, FontStyles.Bold, new Color32(102, 153, 51, 255),
            new Vector2(0.43f, 0.785f), new Vector2(0.57f, 0.875f), TextAlignmentOptions.Center);
        CreateText(loginPanel, "Brand", "APACHE GUACAMOLE", 27f, FontStyles.Bold, Color.black,
            new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.71f), TextAlignmentOptions.Center);

        CreateLoginField(loginPanel, "UsernameField", "Username", 0.43f, 0.56f);
        CreateLoginField(loginPanel, "PasswordField", "Password", 0.29f, 0.42f);

        RectTransform loginButton = CreateRect(loginPanel, "LoginButton", new Vector2(0.08f, 0.11f), new Vector2(0.92f, 0.245f));
        loginButton.gameObject.AddComponent<Image>().color = new Color32(57, 57, 57, 255);
        CreateText(loginButton, "Label", "Login", 25f, FontStyles.Bold, Color.white,
            Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
    }

    private static void CreateLoginField(RectTransform parent, string name, string placeholder, float bottom, float top)
    {
        RectTransform field = CreateCard(parent, name, new Vector2(0.08f, bottom), new Vector2(0.92f, top), Color.white);
        Outline outline = field.GetComponent<Outline>();
        outline.effectColor = new Color32(185, 185, 185, 255);
        outline.effectDistance = new Vector2(1f, -1f);
        CreateText(field, "Placeholder", placeholder, 23f, FontStyles.Normal, new Color32(125, 125, 125, 255),
            new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), TextAlignmentOptions.Left);
    }

    private static void CreateNote(RectTransform page, string value)
    {
        RectTransform note = CreateCard(page, "Note", new Vector2(0.08f, 0.13f), new Vector2(0.92f, 0.19f), new Color32(255, 221, 0, 255));
        CreateText(note, "Text", "LƯU Ý  •  " + value, 23f, FontStyles.Bold, Color.black, new Vector2(0.025f, 0.08f), new Vector2(0.975f, 0.92f), TextAlignmentOptions.Center);
    }

    private void BuildNavigation()
    {
        if (background == null)
            return;
        RectTransform navigation = CreateRect(background, "GXWorks2Navigation", new Vector2(0f, 0f), new Vector2(1f, 0.115f));
        navigation.gameObject.AddComponent<Image>().color = Color.white;
        previousButton = CreateButton(navigation, "Previous", "←  Trước", new Vector2(0.055f, 0.19f), new Vector2(0.20f, 0.82f));
        nextButton = CreateButton(navigation, "Next", "Sau  →", new Vector2(0.80f, 0.19f), new Vector2(0.945f, 0.82f));
        nextButtonLabel = nextButton.GetComponentInChildren<TextMeshProUGUI>();
        previousButton.onClick.AddListener(PreviousPage);
        nextButton.onClick.AddListener(NextPage);
        pageIndicator = CreateText(navigation, "PageIndicator", "1 / 1", 25f, FontStyles.Bold, Muted, new Vector2(0.42f, 0.18f), new Vector2(0.58f, 0.82f), TextAlignmentOptions.Center);
    }

    private static Button CreateButton(RectTransform parent, string name, string label, Vector2 min, Vector2 max)
    {
        RectTransform rect = CreateCard(parent, name, min, max, Color.white);
        Button button = rect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color32(245, 245, 245, 255);
        colors.pressedColor = new Color32(215, 25, 32, 255);
        colors.disabledColor = new Color32(225, 225, 225, 200);
        button.colors = colors;
        CreateText(rect, "Label", label, 25f, FontStyles.Bold, Navy, Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
        return button;
    }

    private void ShowPage(int index)
    {
        if (pages == null || pages.Length == 0)
            return;
        currentPageIndex = Mathf.Clamp(index, 0, pages.Length - 1);
        for (int i = 0; i < pages.Length; i++)
            pages[i].SetActive(i == currentPageIndex);
        previousButton.interactable = currentPageIndex > 0;
        nextButtonLabel.text = currentPageIndex == pages.Length - 1 ? "Bắt đầu  →" : "Sau  →";
        pageIndicator.text = $"{currentPageIndex + 1} / {pages.Length}";

        if (backgroundImage != null)
        {
            backgroundImage.sprite = null;
            backgroundImage.color = Color.white;
        }
        if (sceneCamera != null)
        {
            sceneCamera.allowHDR = false;
            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.backgroundColor = Color.white;
        }
    }

    private void DisableLegacyPages()
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name.StartsWith("Trang ", StringComparison.OrdinalIgnoreCase))
                child.gameObject.SetActive(false);
    }

    private static RectTransform CreateCard(Transform parent, string name, Vector2 min, Vector2 max, Color color)
    {
        RectTransform card = CreateRect(parent, name, min, max);
        card.gameObject.AddComponent<Image>().color = color;
        Outline outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color32(205, 205, 205, 255);
        outline.effectDistance = new Vector2(1f, -1f);
        return card;
    }

    private static RectTransform CreateRect(Transform parent, string name, Vector2 min, Vector2 max)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        obj.layer = parent.gameObject.layer;
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string value, float size, FontStyles style,
        Color color, Vector2 min, Vector2 max, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(parent, name, min, max);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(15f, size - 7f);
        text.fontSizeMax = size;
        text.richText = true;
        text.raycastTarget = false;
        return text;
    }

    private void ShowLegacyPanel(GameObject selectedPanel)
    {
        if (practicePanel != null) practicePanel.SetActive(practicePanel == selectedPanel);
        if (introductionPanel != null) introductionPanel.SetActive(introductionPanel == selectedPanel);
        if (guidePanel != null) guidePanel.SetActive(guidePanel == selectedPanel);
    }
}
