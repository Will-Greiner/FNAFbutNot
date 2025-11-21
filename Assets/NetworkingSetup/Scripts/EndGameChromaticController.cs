using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class EndGameChromaticController : MonoBehaviour
{
    public static EndGameChromaticController Instance { get; private set; }

    [Header("Volume / Effect")]
    [SerializeField] private Volume volume;                 // Global volume with Chromatic Aberration
    [SerializeField] private float endGameIntensity = 0.8f; // target intensity when end game starts
    [SerializeField] private float fadeDuration = 0.5f;     // seconds

    private ChromaticAberration chroma;
    private float originalIntensity = 0f;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (volume == null)
            volume = GetComponent<Volume>();

        if (volume == null || volume.profile == null)
        {
            Debug.LogWarning("EndGameChromaticController: Volume or VolumeProfile is missing.", this);
            return;
        }

        if (!volume.profile.TryGet(out chroma))
        {
            Debug.LogWarning("EndGameChromaticController: ChromaticAberration override not found in VolumeProfile.", this);
            return;
        }

        // Store original intensity and start disabled
        originalIntensity = chroma.intensity.value;
        SetIntensityImmediate(0f);
    }

    // Call this when end game begins
    public void TriggerEndGameEffect()
    {
        if (chroma == null) return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeChromatic(0f, endGameIntensity, fadeDuration));
    }

    // Optional: reset effect (e.g. when returning to menu / restarting)
    public void ResetEffect()
    {
        if (chroma == null) return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        SetIntensityImmediate(0f);
    }

    private void SetIntensityImmediate(float value)
    {
        chroma.active = value > 0f;
        chroma.intensity.overrideState = true;
        chroma.intensity.value = value;
    }

    private IEnumerator FadeChromatic(float from, float to, float duration)
    {
        chroma.intensity.overrideState = true;
        chroma.active = true;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = duration > 0f ? t / duration : 1f;
            float value = Mathf.Lerp(from, to, lerp);
            chroma.intensity.value = value;
            yield return null;
        }

        chroma.intensity.value = to;
        chroma.active = to > 0f;
        fadeRoutine = null;
    }
}
