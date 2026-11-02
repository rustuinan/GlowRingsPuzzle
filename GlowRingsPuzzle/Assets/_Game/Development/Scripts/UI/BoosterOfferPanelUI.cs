using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit, optional rescue choice. Showing or dismissing it never spends inventory.</summary>
public class BoosterOfferPanelUI : MonoBehaviour
{
    [SerializeField] private GameManager game;
    [SerializeField] private Button bombButton;
    [SerializeField] private Button discardButton;
    [SerializeField] private Button endButton;
    [SerializeField] private TMP_Text bombLabel;
    [SerializeField] private TMP_Text discardLabel;

    public void Configure(GameManager manager, Button bomb, Button discard, Button end, TMP_Text bombText, TMP_Text discardText)
    {
        game = manager; bombButton = bomb; discardButton = discard; endButton = end;
        bombLabel = bombText; discardLabel = discardText;
    }

    private void OnEnable()
    {
        if (game == null) return;
        bombButton.onClick.AddListener(UseBomb);
        discardButton.onClick.AddListener(UseDiscard);
        endButton.onClick.AddListener(EndRun);
        Refresh();
    }

    private void OnDisable()
    {
        if (bombButton != null) bombButton.onClick.RemoveListener(UseBomb);
        if (discardButton != null) discardButton.onClick.RemoveListener(UseDiscard);
        if (endButton != null) endButton.onClick.RemoveListener(EndRun);
    }

    private void LateUpdate() { Refresh(); }
    private void Refresh()
    {
        if (game == null) return;
        bombButton.gameObject.SetActive(game.CurrentBombCount > 0);
        discardButton.gameObject.SetActive(game.CurrentTrashCount > 0);
        bombLabel.text = "USE CELL BOMB  (" + game.CurrentBombCount + ")";
        discardLabel.text = "CHANGE PIECE  (" + game.CurrentTrashCount + ")";
    }
    private void UseBomb() { game.AcceptBombRescue(); }
    private void UseDiscard() { game.AcceptDiscardRescue(); }
    private void EndRun() { game.DeclineBoosterRescue(); }
}
