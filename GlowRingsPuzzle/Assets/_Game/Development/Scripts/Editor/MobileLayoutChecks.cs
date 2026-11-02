using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MobileLayoutChecks
{
    private static IEnumerator routine;
    private static int oldSize;
    private static bool oldBackground;
    private static int scenarios;
    private static readonly int[,] Sizes = {{320,568},{360,640},{390,844},{412,915},{768,1024},{1024,1366},{1920,1080}};

    [MenuItem("Tools/Glow Rings/Validate Mobile Screens (Play Mode)")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "MainMenu")
            throw new InvalidOperationException("Start Play Mode in MainMenu first.");
        if (routine != null) return;
        oldSize = (int)GameView().GetType().GetProperty("selectedSizeIndex", Private).GetValue(GameView());
        oldBackground = Application.runInBackground; Application.runInBackground = true;
        if (AdMobManager.Instance != null) UnityEngine.Object.DestroyImmediate(AdMobManager.Instance.gameObject);
        scenarios = 0; routine = Check(); EditorApplication.update += Tick;
    }
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static void Tick()
    {
        try { if (routine != null && routine.MoveNext()) return; }
        catch (Exception error) { Debug.LogException(error); }
        finally { if (routine == null) EditorApplication.update -= Tick; }
        routine = null; EditorApplication.update -= Tick;
        foreach (MobileSafeArea safe in UnityEngine.Object.FindObjectsOfType<MobileSafeArea>()) safe.PreviewSafeArea = null;
        Application.runInBackground = oldBackground;
        GameView().GetType().GetProperty("selectedSizeIndex", Private).SetValue(GameView(), oldSize);
    }
    private static IEnumerator Check()
    {
        MainMenuUI menu = UnityEngine.Object.FindObjectOfType<MainMenuUI>();
        Transform content = UnityEngine.Object.FindObjectOfType<ScrollRect>().content;
        Require(content.Find("Personal Best") == null && content.Find("Footer") == null && content.Find("Explore") == null,
            "Old record row, footer or daily-puzzle grid remains.");
        Require(content.Find("PuzzleRow").GetSiblingIndex() == content.Find("PlayRow").GetSiblingIndex() + 1,
            "Puzzle Mode must be immediately below Highscore Play.");
        Require(content.Find("Activities").childCount == 3, "The bottom dock must contain reward, spin and shop.");
        Require(content.Find("Brand/Best Record") != null, "Record must be part of the brand area.");
        Require(content.Find("Brand/Title").GetComponent<TMP_Text>().text.Contains("MATCH"), "Ring Match title is missing.");
        Require(GameObject.Find("Main Menu Canvas/Safe Area/Header/Wallet/Coin Surface/Plus Background") != null,
            "Coin plus needs its own background.");
        Transform ringRig = GameObject.Find("Menu Ring Preview/Gameplay Ring Models").transform;
        Transform animatedPart = ringRig.GetChild(0);
        Vector3 initialPosition = animatedPart.localPosition;
        Quaternion initialRotation = animatedPart.localRotation;
        double animationReady = EditorApplication.timeSinceStartup + 0.6;
        while (EditorApplication.timeSinceStartup < animationReady) yield return null;
        Require(Vector3.Distance(initialPosition, animatedPart.localPosition) > 0.001f ||
            Quaternion.Angle(initialRotation, animatedPart.localRotation) > 1f, "The 3D hero is static.");
        Require(!ringRig.parent.GetComponentInChildren<Camera>().orthographic, "Hero must use a perspective camera.");
        for (int size = 0; size < Sizes.GetLength(0); size++)
        {
            SetSize(Sizes[size,0], Sizes[size,1]);
            double ready = EditorApplication.timeSinceStartup + 0.7;
            while (EditorApplication.timeSinceStartup < ready) yield return null;
            for (int notch = 0; notch < 2; notch++)
            {
                ApplySafe(notch == 0 ? new Rect(0,0,Screen.width,Screen.height) :
                    new Rect(Screen.width * 0.035f, Screen.height * 0.035f, Screen.width * 0.93f, Screen.height * 0.91f));
                menu.ClosePage(); Audit(true);
                foreach (MenuPage page in Enum.GetValues(typeof(MenuPage)))
                { menu.ShowPage(page); Canvas.ForceUpdateCanvases(); Audit(true); }
                menu.ClosePage();
                ScrollRect scroll = UnityEngine.Object.FindObjectOfType<ScrollRect>();
                scroll.verticalNormalizedPosition = 0; Canvas.ForceUpdateCanvases(); Audit(true);
                scroll.verticalNormalizedPosition = 1;
                scenarios++;
            }
        }
        // Audio toggles must use and restore the real saved audio settings.
        menu.ShowPage(MenuPage.Settings);
        bool music = AudioManager.Instance.MusicMuted; bool sound = AudioManager.Instance.SoundMuted;
        GameObject.Find("Main Menu Canvas/Safe Area/Menu Dialog/Dialog Frame/Card/Audio Settings/Music").GetComponent<Button>().onClick.Invoke();
        Require(AudioManager.Instance.MusicMuted != music, "Music setting did not toggle.");
        GameObject.Find("Main Menu Canvas/Safe Area/Menu Dialog/Dialog Frame/Card/Audio Settings/Sound").GetComponent<Button>().onClick.Invoke();
        Require(AudioManager.Instance.SoundMuted != sound, "Sound setting did not toggle.");
        AudioManager.Instance.SetMusicMuted(music); AudioManager.Instance.SetSoundMuted(sound);
        menu.ClosePage();
        foreach (MobileSafeArea safe in UnityEngine.Object.FindObjectsOfType<MobileSafeArea>()) safe.PreviewSafeArea = null;
        menu.Play();
        double loadLimit = EditorApplication.timeSinceStartup + 15;
        while (SceneManager.GetActiveScene().name != "Game" && EditorApplication.timeSinceStartup < loadLimit) yield return null;
        Require(SceneManager.GetActiveScene().name == "Game", "Classic Play did not load Game.");
        yield return null;
        FirstMoveTutorialManager tutorial = UnityEngine.Object.FindObjectOfType<FirstMoveTutorialManager>();
        if (tutorial != null)
        {
            tutorial.StopAllCoroutines(); tutorial.enabled = false;
            typeof(FirstMoveTutorialManager).GetField("isTutorialActive", Private).SetValue(tutorial, false);
            UnityEngine.Object.FindObjectOfType<TutorialHandHintUI>(true)?.HideInstant();
        }
        GameManager game = GameManager.Instance;
        Ring ring = AssetDatabase.LoadAssetAtPath<Ring>("Assets/_Game/Development/Prefab/TutorialRing.prefab");
        for (int size = 0; size < Sizes.GetLength(0); size++)
        {
            SetSize(Sizes[size,0], Sizes[size,1]);
            double ready = EditorApplication.timeSinceStartup + 0.7;
            while (EditorApplication.timeSinceStartup < ready) yield return null;
            ApplySafe(new Rect(Screen.width*0.035f, Screen.height*0.035f, Screen.width*0.93f, Screen.height*0.91f));
            game.StartGame(); yield return null;
            UnityEngine.Object.FindObjectOfType<CameraController>().SnapToBoard();
            Audit(false);
            foreach (Cell cell in game.Board.Cells)
                for (int layer=0; layer<3; layer++) cell.ForcePlaceRing(ring,(RingLayer)layer,RingColorType.Blue);
            typeof(GameManager).GetMethod("CheckGameOver",Private).Invoke(game,null);
            yield return null; Audit(false);
            game.DeclineBoosterRescue();
            double animation = EditorApplication.timeSinceStartup + 0.5;
            while (EditorApplication.timeSinceStartup < animation) yield return null;
            Audit(false); scenarios++;
        }
        Debug.Log("MobileLayoutChecks PASS: " + scenarios + " screen/safe-area scenarios; all menu pages, scrolling, audio controls, Play transition, gameplay, booster offer and game-over controls.");
    }
    private static void ApplySafe(Rect area)
    {
        foreach (MobileSafeArea safe in UnityEngine.Object.FindObjectsOfType<MobileSafeArea>()) { safe.PreviewSafeArea = area; safe.Apply(); }
        Canvas.ForceUpdateCanvases();
        foreach (ReferenceFrameFit fit in UnityEngine.Object.FindObjectsOfType<ReferenceFrameFit>()) fit.Apply();
        Canvas.ForceUpdateCanvases();
    }
    private static void Audit(bool menu)
    {
        Canvas.ForceUpdateCanvases();
        MobileSafeArea safe = UnityEngine.Object.FindObjectOfType<MobileSafeArea>();
        Rect safeBounds = Bounds((RectTransform)safe.transform);
        foreach (Button button in UnityEngine.Object.FindObjectsOfType<Button>())
        {
            if (button.GetComponentInParent<Canvas>() == null || Hidden(button.transform)) continue;
            ScrollRect scroll = button.GetComponentInParent<ScrollRect>();
            Rect bounds = Bounds((RectTransform)button.transform);
            if (scroll != null)
            {
                Rect content = Bounds(scroll.content);
                Require(bounds.xMin >= content.xMin-2 && bounds.xMax <= content.xMax+2, "Horizontal overflow: " + button.name);
            }
            else Require(Contains(safeBounds,bounds), "Control outside safe area: " + button.name + " at " + Screen.width + "x" + Screen.height);
        }
        foreach (TMP_Text text in UnityEngine.Object.FindObjectsOfType<TMP_Text>())
        {
            if (Hidden(text.transform) || string.IsNullOrEmpty(text.text)) continue;
            text.ForceMeshUpdate();
            Require(!text.isTextOverflowing, "Text overflow: " + text.name + " / " + text.text);
        }
        if (!menu)
        {
            Camera camera = Camera.main;
            Require(Contains(safeBounds, camera.pixelRect), "Gameplay camera outside the safe area.");
            Bounds board = GameManager.Instance.Board.GetComponentsInChildren<Renderer>().First().bounds;
            foreach (Renderer renderer in GameManager.Instance.Board.GetComponentsInChildren<Renderer>()) board.Encapsulate(renderer.bounds);
            Vector3 low = camera.WorldToScreenPoint(board.min); Vector3 high = camera.WorldToScreenPoint(board.max);
            Require(safeBounds.Contains(new Vector2(low.x,low.y)) && safeBounds.Contains(new Vector2(high.x,high.y)), "Board outside safe area.");
        }
    }
    private static bool Hidden(Transform transform)
    {
        foreach (CanvasGroup group in transform.GetComponentsInParent<CanvasGroup>()) if (group.alpha < 0.01f) return true;
        return false;
    }
    private static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin-2 && inner.xMax <= outer.xMax+2 && inner.yMin >= outer.yMin-2 && inner.yMax <= outer.yMax+2;
    private static Rect Bounds(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4]; rect.GetWorldCorners(corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(null,corners[0]); Vector2 max = RectTransformUtility.WorldToScreenPoint(null,corners[2]);
        return Rect.MinMaxRect(Mathf.Min(min.x,max.x),Mathf.Min(min.y,max.y),Mathf.Max(min.x,max.x),Mathf.Max(min.y,max.y));
    }
    private static EditorWindow GameView()
    {
        return EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
    }
    private static void SetSize(int width, int height)
    {
        Assembly assembly = typeof(Editor).Assembly;
        Type sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        object sizes = sizesType.BaseType.GetProperty("instance",Private | BindingFlags.Static).GetValue(null);
        object groupType = Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeGroupType"), "Standalone");
        object group = sizesType.GetMethod("GetGroup",Private).Invoke(sizes,new[] {groupType});
        Type sizeType = assembly.GetType("UnityEditor.GameViewSize");
        object fixedSize = Enum.ToObject(assembly.GetType("UnityEditor.GameViewSizeType"),1);
        string label = "GlowRings QA " + width + "x" + height;
        int total = (int)group.GetType().GetMethod("GetTotalCount",Private).Invoke(group,null);
        int index = -1;
        for (int i=0;i<total;i++)
        {
            object existing = group.GetType().GetMethod("GetGameViewSize",Private).Invoke(group,new object[]{i});
            if ((string)sizeType.GetProperty("baseText",Private).GetValue(existing) == label) { index=i; break; }
        }
        if (index < 0)
        {
            object value = Activator.CreateInstance(sizeType,Private,null,new object[]{fixedSize,width,height,label},null);
            group.GetType().GetMethod("AddCustomSize",Private).Invoke(group,new[]{value}); index=total;
        }
        EditorWindow view = GameView(); view.GetType().GetProperty("selectedSizeIndex",Private).SetValue(view,index); view.Focus(); view.Repaint();
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
