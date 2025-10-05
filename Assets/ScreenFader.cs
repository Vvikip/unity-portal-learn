using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class ScreenFader : MonoBehaviour
{
    private static ScreenFader _instance;
    public static ScreenFader Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("ScreenFader");
                _instance = go.AddComponent<ScreenFader>();
                DontDestroyOnLoad(go);
                _instance.Initialize();
            }
            return _instance;
        }
    }

    private Canvas canvas;
    private Image fadeImage;
    private CanvasGroup canvasGroup;

    [Header("Defaults")] public Color fadeColor = Color.black;

    private void Awake()
    {
        // Enforce singleton even if a ScreenFader is placed in a scene
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        // Canvas setup
        canvas = new GameObject("FaderCanvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue; // on top
        DontDestroyOnLoad(canvas.gameObject);

        // CanvasGroup for alpha control
        canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();

        // Fullscreen Image
        var imgGO = new GameObject("Fader");
        imgGO.transform.SetParent(canvas.transform, false);
        fadeImage = imgGO.AddComponent<Image>();
        fadeImage.color = fadeColor;
        // Do not block UI raycasts
        fadeImage.raycastTarget = false;
        var rt = fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Start fully transparent and disable canvas to avoid any interference
        canvasGroup.alpha = 0f;
        canvas.enabled = false;
    }

    public IEnumerator FadeOut(float duration)
    {
        if (canvas == null) Initialize();
        yield return FadeTo(1f, duration);
    }

    public IEnumerator FadeIn(float duration)
    {
        if (canvas == null) Initialize();
        yield return FadeTo(0f, duration);
    }

    private IEnumerator FadeTo(float target, float duration)
    {
        if (canvas == null) Initialize();

        // Ensure canvas is enabled during fades
        canvas.enabled = true;

        float start = canvasGroup.alpha;
        float t = 0f;
        duration = Mathf.Max(0.0001f, duration);
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // ignore timescale during fades
            float a = Mathf.Lerp(start, target, t / duration);
            canvasGroup.alpha = a;
            yield return null;
        }
        canvasGroup.alpha = target;

        // If fully transparent after fade, disable canvas
        if (canvasGroup.alpha <= 0.001f)
        {
            canvas.enabled = false;
        }
    }

    // Public API to restart the current level with a fade-out and fade-in
    public static void RestartLevel(float fadeDuration = 0.5f)
    {
        Instance.RestartLevelWithFade(fadeDuration);
    }

    public void RestartLevelWithFade(float fadeDuration = 0.5f)
    {
        // Prevent overlapping fade sequences
        StopAllCoroutines();
        StartCoroutine(RestartLevelRoutine(fadeDuration));
    }

    private IEnumerator RestartLevelRoutine(float duration)
    {
        if (canvas == null) Initialize();

        // Fade screen to full opacity
        yield return FadeOut(duration);

        // Reload the current active scene
        int buildIndex = SceneManager.GetActiveScene().buildIndex;
        AsyncOperation op = SceneManager.LoadSceneAsync(buildIndex);
        if (op != null)
        {
            while (!op.isDone)
            {
                yield return null;
            }
        }
        else
        {
            SceneManager.LoadScene(buildIndex);
            yield return null; // wait a frame
        }

        // Ensure fader remains fully opaque at the start of new scene to avoid flashes
        if (canvasGroup != null)
        {
            canvas.enabled = true;
            canvasGroup.alpha = 1f;
        }

        // Allow one frame for scene initialization
        yield return null;

        // Fade back to transparent
        yield return FadeIn(duration);
    }

    // Safety: force-clear any fade and disable the canvas
    public void ClearToTransparent()
    {
        if (canvasGroup == null || canvas == null) return;
        canvasGroup.alpha = 0f;
        canvas.enabled = false;
    }
}
