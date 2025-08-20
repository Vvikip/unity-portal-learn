using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
        var rt = fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Start fully transparent
        canvasGroup.alpha = 0f;
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
    }
}
