using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class GameplayPlanningSetup
{
    private const string BoosterIconPath = "Assets/_Game/Art/InterfaceIcons/";

    [MenuItem("Tools/Glow Rings/Install Planning HUD")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Install in Edit Mode.");
        GameManager manager = UnityEngine.Object.FindObjectOfType<GameManager>();
        PieceSpawner spawner = UnityEngine.Object.FindObjectOfType<PieceSpawner>();
        if (manager == null || spawner == null || manager.gameObject.scene.path != "Assets/_Game/Development/Scene/Game.unity")
            throw new InvalidOperationException("Open the Game scene first.");
        SerializedObject settings = new SerializedObject(manager);
        Button trash = settings.FindProperty("trashButton").objectReferenceValue as Button;
        if (trash == null) throw new InvalidOperationException("Trash button is required as the visual reference.");
        Transform parent = trash.transform.parent;
        Transform existing = parent.Find("PlanningHUD");
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }
        TMP_Text reference = trash.GetComponentInChildren<TMP_Text>(true);
        TMP_FontAsset font = reference != null ? reference.font : TMP_Settings.defaultFontAsset;
        RectTransform root = Rect("PlanningHUD", parent, Vector2.zero, Vector2.zero);
        Stretch(root);

        // Existing score-card art gives the booster the same neon frame as the game UI.
        Image frameReference = parent.GetComponentsInChildren<Image>(true)
            .FirstOrDefault(i => i.transform.parent != null && i.transform.parent.name == "HighScore" && i.sprite != null);
        Sprite frame = frameReference != null ? frameReference.sprite :
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GUIPackCartoon/Demo/Sprites/Buttons/Square/Purple.png");
        RectTransform trashRect = (RectTransform)trash.transform;
        Vector2 boosterSize = Vector2.Scale(trashRect.rect.size, trashRect.localScale);
        Sprite boosterFrame = trash.image != null ? trash.image.sprite : frame;
        RectTransform bombRect = Rect("CellBombButton", root,
            new Vector2(-trashRect.anchoredPosition.x, trashRect.anchoredPosition.y), boosterSize);
        Image background = bombRect.gameObject.AddComponent<Image>();
        background.sprite = boosterFrame;
        Button bomb = bombRect.gameObject.AddComponent<Button>();
        bomb.targetGraphic = background;
        RectTransform badge = Rect("CountBadge", bombRect, new Vector2(-2, -82), new Vector2(101, 112));
        Image badgeImage = badge.gameObject.AddComponent<Image>();
        badgeImage.sprite = boosterFrame; badgeImage.raycastTarget = false;
        TMP_Text count = Label("Count", badge, font, GameManager.BombPrice + "\n<size=55%>COIN</size>", Vector2.zero, new Vector2(76, 50), 27);
        Sprite bombIcon = AssetDatabase.LoadAssetAtPath<Sprite>(BoosterIconPath + "CellBomb.png");
        Sprite changeIcon = AssetDatabase.LoadAssetAtPath<Sprite>(BoosterIconPath + "ChangePiece.png");
        AddBoosterIcon("BombIcon", bombRect, new Vector2(-4, 4), 110, bombIcon);
        RectTransform nextCard = Rect("NextPiece", root, new Vector2(0, -824), new Vector2(160, 148));
        CanvasGroup previewGroup = nextCard.gameObject.AddComponent<CanvasGroup>();
        previewGroup.blocksRaycasts = false;
        NextPiecePreview preview = nextCard.gameObject.AddComponent<NextPiecePreview>();
        preview.Configure(spawner);

        TMP_Text hint = Label("BoosterHint", root, font, "", new Vector2(0, -370), new Vector2(500, 100), 25);
        RectTransform overlayRect = Rect("BombTargets", root, Vector2.zero, Vector2.zero);
        Stretch(overlayRect);
        overlayRect.SetAsFirstSibling();
        BombTargetGraphic targets = overlayRect.gameObject.AddComponent<BombTargetGraphic>();
        targets.Configure(manager.Board != null ? manager.Board : UnityEngine.Object.FindObjectOfType<BoardManager>());
        overlayRect.gameObject.SetActive(false);
        GameplayWalletUI.Install(root, manager, font,
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Coins/Coin.png"));
        RectTransform offerRoot = Rect("BoosterOffer", root, Vector2.zero, Vector2.zero);
        Stretch(offerRoot);
        Image shade = offerRoot.gameObject.AddComponent<Image>();
        shade.color = new Color32(10, 7, 32, 218);
        RectTransform card = Rect("Card", offerRoot, Vector2.zero, new Vector2(860, 752));
        card.gameObject.AddComponent<RoundedPanelGraphic>().SetStyle(
            new Color32(25, 18, 65, 255), new Color32(154, 128, 243, 255), 44);
        Label("Title", card, font, "NO MOVES", new Vector2(0, 274), new Vector2(740, 84), 58)
            .color = new Color32(148, 237, 255, 255);
        Label("Question", card, font, "Use a booster to keep playing.", new Vector2(0, 197), new Vector2(740, 60), 34);
        Button offerBomb = OfferButton("UseBomb", card, font, 76, "CELL BOMB", "Clear one cell", GameManager.BombPrice, bombIcon);
        Button offerDiscard = OfferButton("UseDiscard", card, font, -76, "CHANGE PIECE", "Get the next ring", GameManager.ChangePiecePrice, changeIcon);
        Button endRun = SettingsAction("EndRun", card, font, "END RUN", -257, 448, 94,
            new Color32(41, 30, 77, 255), new Color32(130, 106, 173, 255), 32);
        BoosterOfferPanelUI offer = offerRoot.gameObject.AddComponent<BoosterOfferPanelUI>();
        offer.Configure(manager, offerBomb, offerDiscard, endRun);
        offerRoot.gameObject.SetActive(false);
        GameplayPlanningHUD hud = root.gameObject.AddComponent<GameplayPlanningHUD>();
        hud.Configure(manager, spawner, bomb, count, hint, preview, targets, previewGroup, offer);
        InstallSettings(root, manager, font);
        foreach (string name in new[] { "MusicButton", "SoundButton" })
        {
            Transform oldButton = parent.Find(name);
            if (oldButton != null) oldButton.gameObject.SetActive(false);
        }
        Transform spawn = (Transform)new SerializedObject(spawner).FindProperty("spawnPoint").objectReferenceValue;
        Vector3 spawnPosition = spawn.localPosition;
        spawnPosition.z = -2.55f;
        spawn.localPosition = spawnPosition;
        EditorUtility.SetDirty(hud);
        EditorUtility.SetDirty(targets);
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        EditorSceneManager.SaveScene(manager.gameObject.scene);
        Debug.Log("Planning HUD installed and Game scene saved.");
    }

    private static void InstallSettings(RectTransform root, GameManager game, TMP_FontAsset font)
    {
        RectTransform gear = Rect("SettingsButton", root, new Vector2(-90, -79), new Vector2(96, 96));
        gear.anchorMin = gear.anchorMax = Vector2.one;
        gear.anchoredPosition = new Vector2(-90, -79);
        var gearFrame = gear.gameObject.AddComponent<RoundedPanelGraphic>();
        gearFrame.SetStyle(new Color32(33, 24, 86, 255), new Color32(151, 132, 240, 255), 24);
        Button open = gear.gameObject.AddComponent<Button>();
        open.targetGraphic = gearFrame;
        Image icon = Rect("GearIcon", gear, Vector2.zero, new Vector2(48, 48)).gameObject.AddComponent<Image>();
        icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GUIPackCartoon/Demo/Sprites/Icons/Icons White/Basic/Settings.png");
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        RectTransform panel = Rect("GameplaySettings", root, Vector2.zero, Vector2.zero);
        Stretch(panel);
        panel.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.01f, 0.08f, 0.88f);
        RectTransform card = Rect("Card", panel, Vector2.zero, new Vector2(820, 740));
        card.gameObject.AddComponent<RoundedPanelGraphic>().SetStyle(
            new Color32(26, 17, 78, 255), new Color32(145, 128, 233, 255));
        Label("Title", card, font, "SETTINGS", new Vector2(0, 270), new Vector2(680, 90), 55);
        Toggle music = AudioToggle("Music", "MUSIC", card, font, 115);
        Toggle sound = AudioToggle("Sound", "SOUND", card, font, -12);
        Button close = SettingsAction("Close", card, font, "CONTINUE", -164, 600, 104,
            new Color32(75, 57, 179, 255), new Color32(135, 240, 255, 255), 35);
        Button menu = SettingsAction("MainMenu", card, font, "<  MAIN MENU", -283, 480, 82,
            new Color32(36, 27, 81, 255), new Color32(146, 128, 202, 255), 29);

        GameplaySettingsUI settings = root.gameObject.AddComponent<GameplaySettingsUI>();
        settings.Configure(game, panel.gameObject, open, music, sound);
        UnityEventTools.AddPersistentListener(open.onClick, settings.Open);
        UnityEventTools.AddPersistentListener(music.onValueChanged, settings.SetMusic);
        UnityEventTools.AddPersistentListener(sound.onValueChanged, settings.SetSound);
        UnityEventTools.AddPersistentListener(menu.onClick, settings.ReturnToMenu);
        UnityEventTools.AddPersistentListener(close.onClick, settings.Close);
        panel.gameObject.SetActive(false);
    }

    private static Button SettingsAction(string name, Transform parent, TMP_FontAsset font, string caption,
        float y, float width, float height, Color fill, Color border, float fontSize)
    {
        RectTransform rect = Rect(name, parent, new Vector2(0, y), new Vector2(width, height));
        var frame = rect.gameObject.AddComponent<RoundedPanelGraphic>();
        frame.SetStyle(fill, border, 24);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = frame;
        Label("Label", rect, font, caption, Vector2.zero, new Vector2(width - 32, height), fontSize);
        return button;
    }

    private static Toggle AudioToggle(string name, string caption, Transform parent, TMP_FontAsset font, float y)
    {
        RectTransform row = Rect(name, parent, new Vector2(0, y), new Vector2(680, 102));
        var background = row.gameObject.AddComponent<RoundedPanelGraphic>();
        background.SetStyle(new Color32(44, 33, 100, 255), new Color32(94, 78, 160, 255), 24);
        Image icon = Rect("Icon", row, new Vector2(-276, 0), new Vector2(44, 44)).gameObject.AddComponent<Image>();
        string asset = name == "Music" ? "Music - Note" : "Sound On";
        icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            $"Assets/GUIPackCartoon/Demo/Sprites/Icons/Icons White/Media/{asset}.png");
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        Label("Label", row, font, caption, new Vector2(-112, 0), new Vector2(240, 72), 33)
            .alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform trackRect = Rect("Track", row, new Vector2(225, 0), new Vector2(126, 58));
        var track = trackRect.gameObject.AddComponent<RoundedPanelGraphic>();
        var thumb = Rect("Thumb", trackRect, Vector2.zero, new Vector2(45, 45)).gameObject.AddComponent<RoundedPanelGraphic>();
        thumb.SetStyle(Color.white, new Color32(208, 221, 255, 255), 23);
        thumb.raycastTarget = false;
        Toggle toggle = row.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = track;
        toggle.transition = Selectable.Transition.None;
        return toggle;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static Button OfferButton(string name, Transform parent, TMP_FontAsset font,
        float y, string caption, string description, int price, Sprite icon)
    {
        RectTransform rect = Rect(name, parent, new Vector2(0, y), new Vector2(740, 130));
        var surface = rect.gameObject.AddComponent<RoundedPanelGraphic>();
        surface.SetStyle(new Color32(57, 43, 130, 255), new Color32(119, 148, 230, 255), 28);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = surface;
        AddBoosterIcon("BoosterIcon", rect, new Vector2(-285, 0), 96, icon);
        Label("Label", rect, font, caption, new Vector2(-50, 23), new Vector2(346, 48), 36)
            .alignment = TextAlignmentOptions.MidlineLeft;
        var detail = Label("Description", rect, font, description, new Vector2(-50, -25), new Vector2(346, 42), 27);
        detail.alignment = TextAlignmentOptions.MidlineLeft;
        detail.color = new Color32(184, 190, 227, 255);
        RectTransform badge = Rect("Price", rect, new Vector2(233, 0), new Vector2(202, 72));
        var badgeSurface = badge.gameObject.AddComponent<RoundedPanelGraphic>();
        badgeSurface.SetStyle(new Color32(39, 28, 74, 255), new Color32(179, 136, 96, 255), 22);
        badgeSurface.raycastTarget = false;
        Label("PriceLabel", badge, font, price + " COIN", Vector2.zero, new Vector2(178, 58), 28)
            .color = new Color32(255, 218, 143, 255);
        return button;
    }

    private static void AddBoosterIcon(string name, Transform parent, Vector2 position, float size, Sprite sprite)
    {
        Image icon = Rect(name, parent, position, Vector2.one * size).gameObject.AddComponent<Image>();
        icon.sprite = sprite;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, string value,
        Vector2 position, Vector2 size, float fontSize)
    {
        TextMeshProUGUI text = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font; text.text = value; text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color32(210, 223, 255, 255);
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        return text;
    }
}
