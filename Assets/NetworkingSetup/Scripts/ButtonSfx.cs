using UnityEngine;
using System.Collections;

public class ButtonSfx : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clip;

    [Header("Input")]
    [Tooltip("Input name from Project Settings > Input Manager, e.g. \"Submit\" or \"Fire1\"")]
    [SerializeField] private string inputName = "Submit";

    [Header("Delay")]
    [Tooltip("Optional delay (in seconds) before playing the sound.")]
    [SerializeField] private float delaySeconds = 0f;

    private Coroutine playRoutine;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
            audioSource.playOnAwake = false;
    }

    private void Update()
    {
        // Keyboard / gamepad input
        if (!string.IsNullOrEmpty(inputName) && Input.GetButtonDown(inputName))
        {
            PlaySound();
        }
    }

    /// <summary>
    /// Call this from a UI Button's OnClick or from other scripts.
    /// Respects delaySeconds.
    /// </summary>
    public void PlaySound()
    {
        if (audioSource == null || clip == null)
            return;

        // cancel any pending delayed play
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (delaySeconds <= 0f)
        {
            // Immediate
            audioSource.PlayOneShot(clip);
        }
        else
        {
            // Delayed
            playRoutine = StartCoroutine(PlaySoundDelayed());
        }
    }

    public void StopSound()
    {
        // stop delayed coroutine and any current playback
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (audioSource != null)
            audioSource.Stop();
    }

    private IEnumerator PlaySoundDelayed()
    {
        yield return new WaitForSeconds(delaySeconds);
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
