using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class MovePuzzleUI : MonoBehaviour
{
    private static readonly Color Ink = new Color32(27, 19, 74, 255);
    private static readonly Color Violet = new Color32(151, 132, 240, 255);
    private static readonly Color Cyan = new Color32(141, 237, 255, 255);
    private TMP_Text level;
    private TMP_Text moves;
    private TMP_Text cleared;
    private RectTransform result;
    private TMP_Text resultTitle;
    private TMP_Text resultProgress;
    private Button retryButton;
    private Button menuButton;

    public static GameObject CreateMenu(Transform parent, TMP_FontAsset font, Action<int> open)
    {
        var menu = Rect("Move Puzzle Levels", parent, 0, -42, 800, 580);
        for (int i = 0; i < MovePuzzle.Count; i++)
        {
            int index = i;
            var puzzle = new MovePuzzle(index);
            var row = Panel("Puzzle Level " + (index + 1), menu, 0, 190 - i * 180, 780, 144);
            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = row.GetComponent<RoundedPanelGraphic>();
            button.onClick.AddListener(() => open(index));
            Label(row, font, LevelName(index), -36, 29, 610, 55, 39, Color.white);
            Label(row, font, Goal(puzzle.Target, puzzle.MoveLimit), -36, -30, 610, 50, 29, Cyan);
            if (MovePuzzle.IsCompleted(index))
                Label(row, font, GameLanguage.Choose("DONE", "BİTTİ"), 321, 0, 120, 70, 23, Cyan);
            else
                Label(row, font, ">", 320, 0, 90, 72, 40, Violet);
        }
        return menu.gameObject;
    }

    public static MovePuzzleUI Create(Transform frame, TMP_FontAsset font)
    {
        var root = Rect("Move Puzzle HUD", frame, 0, 0, 1080, 1920);
        var ui = root.gameObject.AddComponent<MovePuzzleUI>();
        var header = Panel("Puzzle Progress", root, 0, 657, 880, 190);
        header.GetComponent<RoundedPanelGraphic>().raycastTarget = false;
        ui.level = Label(header, font, "", 0, 49, 820, 62, 47, Color.white);
        ui.moves = Label(header, font, "", -200, -41, 388, 65, 36, Cyan);
        ui.cleared = Label(header, font, "", 200, -41, 388, 65, 36, Cyan);

        var overlay = Rect("Puzzle Result", root, 0, 0, 1080, 1920);
        overlay.gameObject.AddComponent<Image>().color = new Color(.02f, .01f, .08f, .88f);
        ui.result = overlay;
        var card = Panel("Card", overlay, 0, 0, 860, 620);
        ui.resultTitle = Label(card, font, "", 0, 185, 780, 92, 52, Color.white);
        ui.resultProgress = Label(card, font, "", 0, 68, 760, 74, 37, Cyan);
        ui.retryButton = ActionButton(card, font, "Retry Puzzle", GameLanguage.Text("TRY AGAIN"), -65, true);
        ui.menuButton = ActionButton(card, font, "Puzzle Main Menu", GameLanguage.Text("<  MAIN MENU"), -198, false);
        overlay.gameObject.SetActive(false);
        return ui;
    }

    public void UpdateProgress(MovePuzzle puzzle)
    {
        level.text = LevelName(puzzle.Level);
        moves.text = GameLanguage.Format("Moves left {0}", "Kalan hamle {0}", puzzle.MoveLimit - puzzle.Moves);
        cleared.text = Cleared(puzzle);
        result.gameObject.SetActive(false);
    }

    public void ShowResult(bool won, Action retry, Action menu)
    {
        resultTitle.text = won ? GameLanguage.Text("PUZZLE COMPLETE") : GameLanguage.Text("NO MOVES");
        resultProgress.text = cleared.text;
        retryButton.onClick.RemoveAllListeners();
        menuButton.onClick.RemoveAllListeners();
        retryButton.onClick.AddListener(() => retry());
        menuButton.onClick.AddListener(() =>
        {
            retryButton.interactable = menuButton.interactable = false;
            menu();
        });
        result.gameObject.SetActive(true);
    }

    private static string LevelName(int level) => GameLanguage.Format("Puzzle {0}", "Bulmaca {0}", level + 1);
    private static string Goal(int rings, int moves) =>
        GameLanguage.Format("Clear {0} rings in {1} moves", "{1} hamlede {0} halka temizle", rings, moves);
    private static string Cleared(MovePuzzle puzzle) =>
        GameLanguage.Format("Rings cleared {0}/{1}", "Temizlenen halka {0}/{1}", puzzle.Cleared, puzzle.Target);

    private static Button ActionButton(Transform parent, TMP_FontAsset font, string name, string caption, float y, bool primary)
    {
        var row = Panel(name, parent, 0, y, 650, 100);
        var graphic = row.GetComponent<RoundedPanelGraphic>();
        if (primary) graphic.SetStyle(new Color32(75, 57, 179, 255), Cyan, 24);
        var button = row.gameObject.AddComponent<Button>();
        button.targetGraphic = graphic;
        Label(row, font, caption, 0, 0, 580, 74, 37, Color.white);
        return button;
    }

    private static TMP_Text Label(Transform parent, TMP_FontAsset font, string value, float x, float y,
        float width, float height, float size, Color color)
    {
        var label = Rect("Label", parent, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = GameLanguage.UiFont(font);
        label.text = value;
        label.fontSize = size;
        label.fontSizeMin = size * .65f;
        label.fontSizeMax = size;
        label.enableAutoSizing = true;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform Panel(string name, Transform parent, float x, float y, float width, float height)
    {
        var rect = Rect(name, parent, x, y, width, height);
        rect.gameObject.AddComponent<RoundedPanelGraphic>().SetStyle(Ink, Violet, 24);
        return rect;
    }

    private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = parent.gameObject.layer;
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(x, y);
        return rect;
    }
}
