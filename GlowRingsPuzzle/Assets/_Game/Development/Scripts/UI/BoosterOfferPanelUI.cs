using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit, optional rescue choice. Showing or dismissing it never spends coins.</summary>
public class BoosterOfferPanelUI : MonoBehaviour
{
    [SerializeField] private GameManager game;
    [SerializeField] private Button bombButton;
    [SerializeField] private Button discardButton;
    [SerializeField] private Button endButton;
    public void Configure(GameManager manager, Button bomb, Button discard, Button end)
    {
        game = manager; bombButton = bomb; discardButton = discard; endButton = end;
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
        bombButton.interactable = game.CanAffordBomb;
        discardButton.interactable = game.CanAffordChangePiece;
    }
    private void UseBomb() { game.AcceptBombRescue(); }
    private void UseDiscard() { game.AcceptDiscardRescue(); }
    private void EndRun() { game.DeclineBoosterRescue(); }
}
