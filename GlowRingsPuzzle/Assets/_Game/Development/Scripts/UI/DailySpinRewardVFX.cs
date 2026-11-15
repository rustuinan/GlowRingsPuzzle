using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Presentation only. Currency is credited by DailySpinSave before this celebration starts.</summary>
public sealed class DailySpinRewardVFX : MonoBehaviour
{
    private readonly List<RectTransform> coins = new List<RectTransform>();
    private DailySpinBurstGraphic burst;
    private RectTransform walletTarget;
    private RectTransform resultTarget;
    public bool IsPlaying { get; private set; }
    public int ActiveCoinCount => coins.Count;
    public void Play(Sprite icon, RectTransform source, RectTransform wallet, RectTransform result, int seed,
        Action<float> countProgress, Action finished)
    {
        StopEffect(); gameObject.SetActive(true); transform.SetAsLastSibling();
        walletTarget = wallet; resultTarget = result;
        if (burst == null) burst = gameObject.AddComponent<DailySpinBurstGraphic>();
        Vector2 start = transform.InverseTransformPoint(source.position);
        Vector2 end = transform.InverseTransformPoint(wallet.position);
        end.x -= wallet.rect.width * 0.34f; // Land on the badge's coin icon, leaving the counter readable.
        burst.Initialize(start, seed); IsPlaying = true;
        StartCoroutine(Animate(icon, start, end, seed, countProgress, finished));
    }
    private IEnumerator Animate(Sprite icon, Vector2 start, Vector2 end, int seed, Action<float> countProgress, Action finished)
    {
        var random = new System.Random(seed);
        const int count = 16;
        var launch = new Vector2[count]; var control = new Vector2[count]; var delays = new float[count];
        var completed = new bool[count];
        for (int i = 0; i < count; i++)
        {
            var rect = new GameObject("Flying Coin " + i, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(transform, false); rect.sizeDelta = Vector2.one * Range(random, 45, 62); rect.anchoredPosition = start;
            var image = rect.gameObject.AddComponent<Image>(); image.sprite = icon; image.raycastTarget = false;
            if (icon == null) image.color = new Color32(255, 212, 102, 255);
            coins.Add(rect);
            float angle = Range(random, 0, Mathf.PI * 2);
            launch[i] = start + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Range(random, 100, 210);
            control[i] = new Vector2(Range(random, -360, 360), Mathf.Max(start.y, end.y) + Range(random, 100, 220));
            delays[i] = 0.15f + i * 0.045f;
        }
        float elapsed = 0, walletPulse = 0;
        while (elapsed < 3.25f)
        {
            elapsed += Time.unscaledDeltaTime; burst.SetTime(elapsed);
            for (int i = 0; i < count; i++)
            {
                float u = (elapsed - delays[i]) / 1.3f;
                var image = coins[i].GetComponent<Image>(); Color color = image.color;
                if (u < 0) { color.a = 0; image.color = color; continue; }
                if (u >= 1)
                {
                    color.a = 0; image.color = color;
                    if (!completed[i]) { completed[i] = true; walletPulse = 1; }
                    continue;
                }
                float progress = Mathf.Clamp01((u - 0.22f) / 0.78f);
                coins[i].anchoredPosition = u < 0.22f ? Vector2.Lerp(start, launch[i], 1 - Mathf.Pow(1 - u / 0.22f, 3)) :
                    (1 - progress) * (1 - progress) * launch[i] + 2 * (1 - progress) * progress * control[i] + progress * progress * end;
                float scale = 0.8f + Mathf.Sin(u * Mathf.PI) * 0.3f;
                coins[i].localScale = new Vector3(scale * (0.4f + Mathf.Abs(Mathf.Cos(u * Mathf.PI * 3)) * 0.6f), scale, 1);
                coins[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(u * Mathf.PI) * (i % 2 == 0 ? 35 : -35));
                color.a = Mathf.Clamp01((1 - u) / 0.08f); image.color = color;
            }
            walletPulse = Mathf.MoveTowards(walletPulse, 0, Time.unscaledDeltaTime * 5);
            if (walletTarget != null) walletTarget.localScale = Vector3.one * (1 + walletPulse * 0.10f);
            if (resultTarget != null)
            {
                float bounce = Mathf.Clamp01(elapsed / 0.65f);
                resultTarget.localScale = Vector3.one * (1 + Mathf.Sin(bounce * Mathf.PI) * (1 - bounce) * 0.30f);
            }
            countProgress?.Invoke(Mathf.SmoothStep(0, 1, Mathf.Clamp01((elapsed - 0.65f) / 1.45f)));
            yield return null;
        }
        countProgress?.Invoke(1); finished?.Invoke();
        StopEffect();
    }
    public void StopEffect()
    {
        StopAllCoroutines(); Cleanup();
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }
    private void OnDisable() { StopAllCoroutines(); Cleanup(); }
    private void Cleanup()
    {
        foreach (var coin in coins) if (coin != null) { coin.gameObject.SetActive(false); Destroy(coin.gameObject); }
        coins.Clear();
        if (burst != null) burst.Clear();
        if (walletTarget != null) walletTarget.localScale = Vector3.one;
        if (resultTarget != null) resultTarget.localScale = Vector3.one;
        walletTarget = null; resultTarget = null; IsPlaying = false;
    }
    private static float Range(System.Random random, float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
}
