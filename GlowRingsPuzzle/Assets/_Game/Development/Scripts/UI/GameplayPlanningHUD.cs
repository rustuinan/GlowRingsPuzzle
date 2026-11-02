using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayPlanningHUD : MonoBehaviour
{
    [SerializeField] private GameManager game;
    [SerializeField] private PieceSpawner spawner;
    [SerializeField] private Button bombButton;
    [SerializeField] private TMP_Text bombCount;
    [SerializeField] private TMP_Text instruction;
    [SerializeField] private NextPiecePreview preview;
    [SerializeField] private BombTargetGraphic targets;
    [SerializeField] private CanvasGroup previewGroup;
    [SerializeField] private BoosterOfferPanelUI boosterOffer;

    public void Configure(GameManager manager, PieceSpawner pieceSpawner, Button button, TMP_Text count,
        TMP_Text hint, NextPiecePreview next, BombTargetGraphic targetGraphic, CanvasGroup group, BoosterOfferPanelUI offer)
    {
        game = manager; spawner = pieceSpawner; bombButton = button; bombCount = count;
        instruction = hint; preview = next; targets = targetGraphic; previewGroup = group; boosterOffer = offer;
    }

    private void OnEnable() { if (bombButton != null) bombButton.onClick.AddListener(OnBombClicked); }
    private void OnDisable()
    {
        if (bombButton != null) bombButton.onClick.RemoveListener(OnBombClicked);
        if (game != null) game.CancelBombTargeting();
    }
    private void OnBombClicked() { if (game != null) game.ToggleBombTargeting(); }

    private void LateUpdate()
    {
        if (game == null || spawner == null) return;
        bool tutorial = game.IsTutorialActive;
        bombButton.interactable = game.IsBombTargeting || game.CanUseBomb;
        bombCount.text = game.CurrentBombCount.ToString();
        targets.gameObject.SetActive(game.IsBombTargeting);
        boosterOffer.gameObject.SetActive(game.IsBoosterOfferOpen);
        bool showPreview = !tutorial && !game.IsGameOver && !game.IsBoosterOfferOpen && spawner.NextPieceData != null;
        previewGroup.alpha = showPreview ? 1f : 0f;
        preview.Show(showPreview ? spawner.NextPieceData : null, spawner.NextPieceColor);
        if (game.IsBombTargeting)
        {
            instruction.text = "TAP A CELL TO CLEAR\nTap bomb again to cancel";
            instruction.color = new Color32(255, 225, 126, 255);
        }
        else instruction.text = "";
    }
}
