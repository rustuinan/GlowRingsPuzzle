using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MobileMenuSetup
{
    private const string MenuScene = "Assets/_Game/Development/Scene/MainMenu.unity";
    private const string GameScene = "Assets/_Game/Development/Scene/Game.unity";
    private const string Icons = "Assets/GUIPackCartoon/Demo/Sprites/Icons/";
    private static readonly Color Ink = new Color32(28, 21, 72, 245);
    private static readonly Color Cyan = new Color32(100, 224, 255, 255);
    private static readonly Color Violet = new Color32(157, 123, 255, 255);
    private static TMP_FontAsset font;
    private static MainMenuUI controller;

    [MenuItem("Tools/Glow Rings/Build Mobile Menu")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Build in Edit Mode.");
        Scene menu = SceneManager.GetActiveScene();
        if (menu.path != MenuScene) throw new InvalidOperationException("Open MainMenu first.");
        PlayerSettings.productName = "RingMatch";
        Scene game = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Additive);
        GameObject audio = game.GetRootGameObjects().FirstOrDefault(o => o.GetComponent<AudioManager>() != null);
        if (audio != null && !menu.GetRootGameObjects().Any(o => o.GetComponent<AudioManager>() != null))
        {
            GameObject clone = UnityEngine.Object.Instantiate(audio); clone.name = "AudioManager";
            SceneManager.MoveGameObjectToScene(clone, menu);
        }
        EditorSceneManager.CloseScene(game, true);
        SceneManager.SetActiveScene(menu);
        foreach (Canvas old in UnityEngine.Object.FindObjectsOfType<Canvas>()) UnityEngine.Object.DestroyImmediate(old.gameObject);
        Material background = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Development/Material/Background/New Material.mat");
        BuildBackdrop(background);
        Camera main = Camera.main;
        main.rect = new Rect(0, 0, 1, 1); main.clearFlags = CameraClearFlags.Depth;
        main.cullingMask = ~(1 << 30); main.orthographic = true;
        main.transform.SetPositionAndRotation(new Vector3(0, 0, -10), Quaternion.identity);
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("015573da50ed04390be9f20287a541d6"));
        RectTransform canvasRect = Rect("Main Menu Canvas", null, Vector2.zero, Vector2.zero);
        Canvas canvas = canvasRect.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasRect.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0f;
        canvasRect.gameObject.AddComponent<GraphicRaycaster>();
        controller = canvasRect.gameObject.AddComponent<MainMenuUI>();
        RectTransform safe = Rect("Safe Area", canvasRect, Vector2.zero, Vector2.zero); Stretch(safe);
        safe.gameObject.AddComponent<MobileSafeArea>();
        RectTransform header = Rect("Header", safe, Vector2.zero, Vector2.zero);
        header.anchorMin = new Vector2(0, 1); header.anchorMax = Vector2.one;
        header.offsetMin = new Vector2(48, -180); header.offsetMax = new Vector2(-48, -32);
        RectTransform walletRect = Rect("Wallet", header, Vector2.zero, new Vector2(320, 148));
        walletRect.anchorMin = walletRect.anchorMax = new Vector2(0, 0.5f); walletRect.pivot = new Vector2(0, 0.5f);
        Image walletHit = walletRect.gameObject.AddComponent<Image>(); walletHit.color = Color.clear;
        Button wallet = walletRect.gameObject.AddComponent<Button>();
        RectTransform walletSurface = Rect("Coin Surface", walletRect, Vector2.zero, new Vector2(320, 110));
        RoundedPanelGraphic walletGraphic = walletSurface.gameObject.AddComponent<RoundedPanelGraphic>();
        walletGraphic.SetStyle(Ink, new Color32(178, 146, 94, 255), 28); walletGraphic.raycastTarget = false;
        wallet.targetGraphic = walletGraphic;
        Action(wallet, MenuPage.Wallet);
        Icon("Coin", walletSurface, "Icons Colored/Coins/Coin.png", new Vector2(-116, 0), 60);
        Text("CoinsTitle", walletSurface, "COINS", new Vector2(-28, 22), new Vector2(135, 36), 25);
        Text("CoinsValue", walletSurface, "SOON", new Vector2(-28, -19), new Vector2(135, 38), 28, new Color32(245, 213, 144, 255));
        RectTransform plus = Rect("Plus Background", walletSurface, new Vector2(108, 0), new Vector2(76, 76));
        RoundedPanelGraphic plusGraphic = plus.gameObject.AddComponent<RoundedPanelGraphic>();
        plusGraphic.SetStyle(new Color32(64, 48, 116, 255), Violet, 20); plusGraphic.raycastTarget = false;
        Text("Plus", plus, "+", Vector2.zero, new Vector2(64, 64), 48, Cyan);
        HeaderButton("Statistics", header, -164, "Icons Colored/Badges/Cup (Big).png", MenuPage.Statistics);
        HeaderButton("Settings", header, 0, "Icons White/Basic/Settings.png", MenuPage.Settings);

        RectTransform scrollRoot = Rect("Menu Scroll", safe, Vector2.zero, Vector2.zero); Stretch(scrollRoot);
        scrollRoot.offsetMin = new Vector2(48, 30); scrollRoot.offsetMax = new Vector2(-48, -210);
        ScrollRect scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
        RectTransform viewport = Rect("Viewport", scrollRoot, Vector2.zero, Vector2.zero); Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();
        Image scrollHit = viewport.gameObject.AddComponent<Image>(); scrollHit.color = Color.clear;
        RectTransform content = Rect("Content", viewport, Vector2.zero, Vector2.zero);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0.5f, 1);
        content.offsetMin = content.offsetMax = Vector2.zero;
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 22; layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport; scroll.content = content;
        scroll.scrollSensitivity = 40f;
        RectTransform barRect = Rect("Scroll Indicator", scrollRoot, Vector2.zero, Vector2.zero);
        barRect.anchorMin = new Vector2(1, 0); barRect.anchorMax = Vector2.one;
        barRect.offsetMin = new Vector2(14, 4); barRect.offsetMax = new Vector2(24, -4);
        Image track = barRect.gameObject.AddComponent<Image>(); track.color = new Color(0.5f, 0.45f, 0.8f, 0.15f);
        Scrollbar bar = barRect.gameObject.AddComponent<Scrollbar>(); bar.direction = Scrollbar.Direction.BottomToTop;
        RectTransform handle = Rect("Handle", barRect, Vector2.zero, Vector2.zero); Stretch(handle);
        Image handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = new Color(0.4f, 0.88f, 1f, 0.6f);
        bar.handleRect = handle; bar.targetGraphic = handleImage;
        scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        RectTransform brand = Item("Brand", content, 180);
        Text("Title", brand, "RING <color=#8CEAFF>MATCH</color>", new Vector2(0, 30), new Vector2(960, 120), 108, Color.white);
        RectTransform recordPill = Rect("Best Record", brand, new Vector2(0, -62), new Vector2(330, 66));
        recordPill.gameObject.AddComponent<RoundedPanelGraphic>().SetStyle(new Color32(27, 22, 66, 175), new Color32(91, 77, 147, 200), 33);
        Icon("Cup", recordPill, "Icons Colored/Badges/Cup (Big).png", new Vector2(-118, 0), 40);
        Text("BestLabel", recordPill, "BEST", new Vector2(-52, 0), new Vector2(90, 40), 25);
        TMP_Text record = Text("BestValue", recordPill, SaveManager.LoadHighScore().ToString("N0"), new Vector2(65, 0), new Vector2(150, 50), 37, new Color32(255, 218, 144, 255));
        RectTransform hero = Item("Hero", content, 420);
        RectTransform heroImage = Rect("Gameplay Rings", hero, Vector2.zero, new Vector2(780, 780));
        RawImage heroGraphic = heroImage.gameObject.AddComponent<RawImage>(); heroGraphic.raycastTarget = false;
        heroImage.gameObject.AddComponent<MenuRingHero>().Configure(AssetDatabase.LoadAssetAtPath<ThemeData>("Assets/_Game/Development/Data/Theme/ThemeData.asset"));
        content.gameObject.AddComponent<MenuContentLayout>().Configure(scroll, hero.GetComponent<LayoutElement>(), heroImage);
        RectTransform playRow = Item("PlayRow", content, 158);
        Button play = Card("Play Classic", playRow, new Color32(72, 40, 157, 255), Cyan); Stretch((RectTransform)play.transform);
        Icon("Arrow", play.transform, "Icons White/Arrows/Forward.png", new Vector2(-360, 0), 76);
        Text("PlayTitle", play.transform, "PLAY", new Vector2(0, 23), new Vector2(620, 80), 65, Color.white);
        Text("Mode", play.transform, "HIGHSCORE MODE", new Vector2(0, -43), new Vector2(620, 42), 27, Cyan);
        RectTransform puzzleRow = Item("PuzzleRow", content, 140);
        Button puzzle = Card("Puzzle Mode", puzzleRow, new Color32(35, 38, 101, 250), new Color32(133, 154, 255, 255));
        Stretch((RectTransform)puzzle.transform); Action(puzzle, MenuPage.PuzzleMode);
        Icon("PuzzleIcon", puzzle.transform, "Icons White/Navigation/Level.png", new Vector2(-360, 0), 64);
        Text("PuzzleTitle", puzzle.transform, "PUZZLE MODE", new Vector2(0, 20), new Vector2(670, 65), 45);
        Text("PuzzleAvailability", puzzle.transform, "COMING SOON", new Vector2(0, -35), new Vector2(670, 40), 25, new Color32(169, 178, 231, 255));
        RectTransform activities = Row("Activities", content, 212);
        Feature("Daily Reward", activities, "DAILY\nREWARD", "Icons Colored/Gifts/Gift - Yellow (Border).png", MenuPage.DailyReward, new Color32(66, 37, 87, 245), new Color32(222, 148, 208, 255));
        Feature("Daily Spin", activities, "DAILY\nSPIN", "Icons Colored/Spin Wheel/Spin Wheel.png", MenuPage.DailySpin, new Color32(30, 54, 91, 245), new Color32(118, 195, 232, 255));
        Feature("Shop", activities, "SHOP", "Icons Colored/Commerce/Shop.png", MenuPage.Shop, new Color32(50, 43, 85, 245), new Color32(213, 179, 113, 255));

        RectTransform modal = Rect("Menu Dialog", safe, Vector2.zero, Vector2.zero); Stretch(modal);
        Image shade = modal.gameObject.AddComponent<Image>(); shade.color = new Color(0.015f, 0.008f, 0.05f, 0.9f);
        RectTransform dialogFrame = Rect("Dialog Frame", modal, Vector2.zero, new Vector2(1080, 1920));
        dialogFrame.gameObject.AddComponent<ReferenceFrameFit>();
        RectTransform card = Rect("Card", dialogFrame, Vector2.zero, new Vector2(940, 950));
        card.gameObject.AddComponent<RoundedPanelGraphic>().SetStyle(new Color32(27, 19, 74, 255), Violet, 40);
        TMP_Text modalTitle = Text("Title", card, "SETTINGS", new Vector2(0, 345), new Vector2(640, 110), 55);
        Button close = Card("Close", card, new Color32(61, 45, 111, 255), Violet);
        ((RectTransform)close.transform).sizeDelta = new Vector2(144, 144); ((RectTransform)close.transform).anchoredPosition = new Vector2(370, 345);
        Text("X", close.transform, "X", Vector2.zero, new Vector2(80, 80), 43);
        TMP_Text body = Text("Body", card, "", new Vector2(0, 35), new Vector2(780, 510), 36);
        RectTransform settings = Rect("Audio Settings", card, new Vector2(0, -10), new Vector2(780, 540));
        Button music = AudioRow("Music", settings, 135, "MUSIC", "Icons White/Media/Music - Note.png", out TMP_Text musicValue);
        Button sound = AudioRow("Sound", settings, -35, "SOUND", "Icons White/Media/Sound.png", out TMP_Text soundValue);
        Text("Note", settings, "More options coming soon", new Vector2(0, -210), new Vector2(760, 70), 28, new Color32(178, 167, 216, 255));
        controller.Configure(play, modal.gameObject, modalTitle, body, record, settings.gameObject, close, music, sound, musicValue, soundValue);
        modal.gameObject.SetActive(false);
        EditorUtility.SetDirty(controller);
        Canvas.ForceUpdateCanvases();
        EditorSceneManager.MarkSceneDirty(menu); EditorSceneManager.SaveScene(menu);
        Debug.Log("Mobile menu installed: shader backdrop, live classic/audio/record, future content entries and safe scrolling.");
    }

    [MenuItem("Tools/Glow Rings/Fit Game UI To Safe Area")]
    public static void FitGame()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Fit in Edit Mode.");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != GameScene) throw new InvalidOperationException("Open Game first.");
        Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
        if (canvas.transform.Find("Safe Area") != null) return;
        Transform[] roots = Enumerable.Range(0, canvas.transform.childCount).Select(i => canvas.transform.GetChild(i)).ToArray();
        RectTransform safe = Rect("Safe Area", canvas.transform, Vector2.zero, Vector2.zero); Stretch(safe); safe.gameObject.AddComponent<MobileSafeArea>();
        RectTransform frame = Rect("Game Reference Frame", safe, Vector2.zero, new Vector2(1080, 1920));
        foreach (Transform root in roots) root.SetParent(frame, false);
        frame.gameObject.AddComponent<ReferenceFrameFit>().Configure(Camera.main);
        Camera.main.cullingMask &= ~(1 << 30 | 1 << 31);
        Camera.main.clearFlags = CameraClearFlags.Depth;
        BuildBackdrop(AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Development/Material/Background/New Material.mat"), true);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("Game, game over, booster prompt, tutorial and feedback share one safe frame and camera viewport.");
    }

    private static void BuildBackdrop(Material material, bool behindGameplay = false)
    {
        GameObject old = GameObject.Find("Shader Backdrop"); if (old != null) UnityEngine.Object.DestroyImmediate(old);
        GameObject root = new GameObject("Shader Backdrop");
        Camera camera = new GameObject("Background Camera", typeof(Camera)).GetComponent<Camera>();
        camera.transform.SetParent(root.transform); camera.transform.position = new Vector3(0, 0, -100);
        camera.orthographic = true; camera.orthographicSize = 10; camera.depth = -20;
        camera.nearClipPlane = 0.1f; camera.farClipPlane = 40; camera.cullingMask = 1 << 31;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.035f, 0.015f, 0.11f);
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad); quad.name = "Gameplay Background Shader";
        quad.transform.SetParent(root.transform); quad.transform.position = new Vector3(0, 0, -80); quad.layer = 31;
        UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>()); quad.GetComponent<Renderer>().sharedMaterial = material;
        if (!behindGameplay)
        {
            camera.enabled = false;
            quad.transform.position = new Vector3(0, 0, 10);
            root.AddComponent<ShaderBackdrop>().Configure(Camera.main, quad.transform);
        }
        else root.AddComponent<ShaderBackdrop>().Configure(camera, quad.transform);
    }
    private static Button HeaderButton(string name, RectTransform parent, float x, string icon, MenuPage page)
    {
        Button button = Card(name, parent, Ink, Violet);
        RectTransform rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(1, 0.5f); rect.pivot = new Vector2(1, 0.5f);
        rect.sizeDelta = new Vector2(148, 148); rect.anchoredPosition = new Vector2(x, 0);
        RoundedPanelGraphic original = button.GetComponent<RoundedPanelGraphic>(); UnityEngine.Object.DestroyImmediate(original);
        Image hit = button.gameObject.AddComponent<Image>(); hit.color = Color.clear;
        RectTransform surface = Rect("Surface", button.transform, Vector2.zero, new Vector2(104, 104));
        RoundedPanelGraphic graphic = surface.gameObject.AddComponent<RoundedPanelGraphic>(); graphic.SetStyle(Ink, Violet, 26); graphic.raycastTarget = false;
        button.targetGraphic = graphic;
        Icon("Icon", surface, icon, Vector2.zero, 56); Action(button, page); return button;
    }
    private static Button AudioRow(string name, Transform parent, float y, string caption, string icon, out TMP_Text value)
    {
        Button button = Card(name, parent, Ink, Violet); RectTransform rect = (RectTransform)button.transform;
        rect.anchoredPosition = new Vector2(0, y); rect.sizeDelta = new Vector2(780, 150);
        Icon("Icon", rect, icon, new Vector2(-300, 0), 64);
        Text("Title", rect, caption, new Vector2(-65, 0), new Vector2(320, 80), 40);
        value = Text("Value", rect, "ON", new Vector2(285, 0), new Vector2(160, 80), 40, Cyan); return button;
    }
    private static void Feature(string name, Transform parent, string caption, string icon, MenuPage page, Color fill, Color border)
    {
        Button button = Card(name, parent, fill, border); LayoutElement le = button.gameObject.AddComponent<LayoutElement>(); le.flexibleWidth = 1; le.minWidth = 0;
        RectTransform rect = (RectTransform)button.transform;
        Icon("Icon", rect, icon, new Vector2(0, 45), 82);
        Text("Title", rect, caption, new Vector2(0, -50), new Vector2(275, 86), 34);
        Text("Availability", rect, "SOON", new Vector2(92, 78), new Vector2(105, 30), 21, border);
        Action(button, page);
    }
    private static void Action(Button button, MenuPage page) { button.gameObject.AddComponent<MenuActionButton>().Configure(controller, page); }
    private static Button Card(string name, Transform parent, Color fill, Color border)
    {
        RectTransform rect = Rect(name, parent, Vector2.zero, new Vector2(100, 100));
        RoundedPanelGraphic graphic = rect.gameObject.AddComponent<RoundedPanelGraphic>(); graphic.SetStyle(fill, border);
        Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = graphic;
        rect.gameObject.AddComponent<MenuButtonFeedback>();
        Navigation navigation = button.navigation; navigation.mode = Navigation.Mode.None; button.navigation = navigation;
        return button;
    }
    private static RectTransform Row(string name, Transform parent, float height)
    {
        RectTransform rect = Item(name, parent, height); HorizontalLayoutGroup row = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 22; row.childControlWidth = row.childControlHeight = true; row.childForceExpandWidth = row.childForceExpandHeight = true; return rect;
    }
    private static RectTransform Item(string name, Transform parent, float height)
    {
        RectTransform rect = Rect(name, parent, Vector2.zero, new Vector2(100, height));
        LayoutElement element = rect.gameObject.AddComponent<LayoutElement>(); element.preferredHeight = height; element.minHeight = height; return rect;
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform)); go.layer = 5; RectTransform rect = (RectTransform)go.transform;
        if (parent != null) rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
    }
    private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private static TMP_Text Text(string name, Transform parent, string value, Vector2 position, Vector2 size, float fontSize, Color? tint = null)
    {
        TextMeshProUGUI text = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font; text.text = value; text.fontSize = fontSize; text.enableAutoSizing = true;
        text.fontSizeMin = fontSize * 0.72f; text.fontSizeMax = fontSize; text.alignment = TextAlignmentOptions.Center;
        text.color = tint ?? new Color32(239, 235, 255, 255); text.raycastTarget = false; text.enableWordWrapping = true; return text;
    }
    private static void Icon(string name, Transform parent, string path, Vector2 position, float size)
    {
        Image image = Rect(name, parent, position, new Vector2(size, size)).gameObject.AddComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Icons + path); image.preserveAspect = true; image.raycastTarget = false;
    }
}
