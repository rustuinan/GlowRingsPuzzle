using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class GameplayPlanningSetup
{
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
        RectTransform bombRect = Rect("CellBombButton", root, new Vector2(336, -651), new Vector2(220, 160));
        Image background = bombRect.gameObject.AddComponent<Image>();
        background.sprite = frame;
        Button bomb = bombRect.gameObject.AddComponent<Button>();
        bomb.targetGraphic = background;
        RectTransform badge = Rect("CountBadge", bombRect, new Vector2(0, -104), new Vector2(76, 50));
        Image badgeImage = badge.gameObject.AddComponent<Image>();
        badgeImage.sprite = frame; badgeImage.raycastTarget = false;
        TMP_Text count = Label("Count", badge, font, "2", Vector2.zero, new Vector2(76, 50), 31);
        Sprite bombSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Bombs/Bomb.png");
        Image icon = Rect("BombIcon", bombRect, Vector2.zero, new Vector2(105, 105)).gameObject.AddComponent<Image>();
        if (bombSprite == null) throw new InvalidOperationException("Bomb sprite is missing.");
        icon.sprite = bombSprite;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        Label("BombLabel", root, font, "CELL BOMB", new Vector2(336, -795), new Vector2(260, 45), 27);

        RectTransform nextCard = Rect("NextPiece", root, new Vector2(330, -455), new Vector2(210, 235));
        CanvasGroup previewGroup = nextCard.gameObject.AddComponent<CanvasGroup>();
        previewGroup.blocksRaycasts = false;
        TMP_Text nextTitle = Label("Title", nextCard, font, "NEXT", new Vector2(0, 93), new Vector2(280, 42), 27);
        RectTransform ringRect = Rect("Rings", nextCard, new Vector2(0, -12), new Vector2(185, 185));
        NextPiecePreview preview = ringRect.gameObject.AddComponent<NextPiecePreview>();
        preview.Configure(spawner, nextTitle);

        TMP_Text hint = Label("BoosterHint", root, font, "", new Vector2(0, -370), new Vector2(500, 100), 25);
        RectTransform overlayRect = Rect("BombTargets", root, Vector2.zero, Vector2.zero);
        Stretch(overlayRect);
        overlayRect.SetAsFirstSibling();
        BombTargetGraphic targets = overlayRect.gameObject.AddComponent<BombTargetGraphic>();
        targets.Configure(manager.Board != null ? manager.Board : UnityEngine.Object.FindObjectOfType<BoardManager>());
        overlayRect.gameObject.SetActive(false);
        RectTransform offerRoot = Rect("BoosterOffer", root, Vector2.zero, Vector2.zero);
        Stretch(offerRoot);
        Image shade = offerRoot.gameObject.AddComponent<Image>();
        shade.color = new Color(0.015f, 0.01f, 0.06f, 0.88f);
        RectTransform card = Rect("Card", offerRoot, Vector2.zero, new Vector2(900, 820));
        RoundedPanelGraphic cardImage = card.gameObject.AddComponent<RoundedPanelGraphic>();
        cardImage.color = new Color32(26, 17, 78, 255);
        Label("Title", card, font, "NO MOVES", new Vector2(0, 280), new Vector2(760, 90), 55);
        Label("Question", card, font, "Would you like to use a booster?\nYou can also end this run.", new Vector2(0, 170), new Vector2(760, 100), 33);
        Button offerBomb = OfferButton("UseBomb", card, frame, font, new Vector2(0, 25), "USE CELL BOMB", out TMP_Text bombLabel);
        Button offerDiscard = OfferButton("UseDiscard", card, frame, font, new Vector2(0, -120), "CHANGE PIECE", out TMP_Text discardLabel);
        Button endRun = OfferButton("EndRun", card, frame, font, new Vector2(0, -285), "END GAME", out TMP_Text endLabel);
        endLabel.color = new Color32(255, 194, 204, 255);
        BoosterOfferPanelUI offer = offerRoot.gameObject.AddComponent<BoosterOfferPanelUI>();
        offer.Configure(manager, offerBomb, offerDiscard, endRun, bombLabel, discardLabel);
        offerRoot.gameObject.SetActive(false);
        GameplayPlanningHUD hud = root.gameObject.AddComponent<GameplayPlanningHUD>();
        hud.Configure(manager, spawner, bomb, count, hint, preview, targets, previewGroup, offer);
        EditorUtility.SetDirty(hud);
        EditorUtility.SetDirty(targets);
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        EditorSceneManager.SaveScene(manager.gameObject.scene);
        Debug.Log("Planning HUD installed and Game scene saved.");
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

    private static Button OfferButton(string name, Transform parent, Sprite frame, TMP_FontAsset font,
        Vector2 position, string caption, out TMP_Text label)
    {
        RectTransform rect = Rect(name, parent, position, new Vector2(700, 125));
        Image image = rect.gameObject.AddComponent<Image>(); image.sprite = frame;
        Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        label = Label("Label", rect, font, caption, Vector2.zero, new Vector2(660, 100), 34);
        return button;
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
