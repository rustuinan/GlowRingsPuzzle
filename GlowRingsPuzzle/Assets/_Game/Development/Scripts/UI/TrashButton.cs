using UnityEngine;
using UnityEngine.UI;

public class TrashButton : MonoBehaviour
{
    [SerializeField] private Button button;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(OnTrashClicked);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnTrashClicked);
    }

    private void OnTrashClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.DiscardCurrentPiece();
    }
}
