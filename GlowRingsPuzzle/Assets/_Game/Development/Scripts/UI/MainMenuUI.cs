using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Mobile safe-area layout and the main menu's Play action.</summary>
[DisallowMultipleComponent]
public sealed class MainMenuUI : MonoBehaviour
{
    [SerializeField] private RectTransform safeAreaRoot;
    [SerializeField] private Button playButton;
    [SerializeField] private string gameScenePath = "Assets/_Game/Development/Scene/Game.unity";

    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private bool loading;

    private void OnEnable()
    {
        ApplySafeArea();
        if (playButton != null)
            playButton.onClick.AddListener(Play);
    }

    private void OnDisable()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(Play);
    }

    private void Update()
    {
        if (lastSafeArea != Screen.safeArea ||
            lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
            ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        if (safeAreaRoot == null || Screen.width <= 0 || Screen.height <= 0)
            return;

        lastSafeArea = Screen.safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        safeAreaRoot.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
        safeAreaRoot.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
        safeAreaRoot.offsetMin = Vector2.zero;
        safeAreaRoot.offsetMax = Vector2.zero;
    }

    public void Play()
    {
        if (loading)
            return;

        if (!Application.CanStreamedLevelBeLoaded(gameScenePath))
        {
            Debug.LogError("Main menu: Game scene is missing from Build Settings.", this);
            return;
        }

        loading = true;
        if (playButton != null)
            playButton.interactable = false;
        SceneManager.LoadSceneAsync(gameScenePath, LoadSceneMode.Single);
    }
}
